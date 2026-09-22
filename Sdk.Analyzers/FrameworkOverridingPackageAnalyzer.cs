// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Sdk.Analyzers;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

/// <summary>
/// Analyzer that reports a direct <c>PackageReference</c> which overrides an assembly the target
/// framework's own shared framework already supplies.
/// </summary>
/// <remarks>
/// <para>
/// A package that also ships in the shared framework - <c>System.Text.Json</c>, <c>System.Memory</c>
/// and the rest of the <c>PackageOverrides.txt</c> list - normally resolves nothing to compile
/// against: NuGet prunes the reference and the framework's copy wins. It resolves a real assembly
/// only when the referenced version is <em>higher</em> than the one that framework ships, and then
/// the compilation binds against that higher assembly version.
/// </para>
/// <para>
/// On the lower target framework of a multi-targeting library that produces a package which throws
/// <c>FileNotFoundException</c> for every consumer, because CoreCLR rolls assembly binds forward but
/// never backward. Nothing in the producing build says so, and nothing in the produced nuspec says
/// so either - with <c>PrivateAssets="all"</c> the dependency is not even declared, so the consumer
/// resolves nothing and runs against the shared framework's lower copy. The failure surfaces only
/// when something consumes the packed artifact.
/// </para>
/// <para>
/// KTSU0006 is what usually creates this hazard: it demands a direct reference for a type used
/// transitively, and its code fix writes that reference at whatever version the analyzer observed -
/// which, for a framework-supplied package, can be the higher one. This rule grades the result.
/// See ktsu-dev/Sdk#44.
/// </para>
/// <para>
/// The check is per target framework by construction. The <c>net10.0</c> inner build of a
/// <c>net10.0;net9.0</c> project sees the reference pruned and stays silent; the <c>net9.0</c> inner
/// build sees the override and reports. That is why no cross-framework reasoning is needed here: the
/// lowest-supported framework is simply the inner build with the lowest floor, and it is the one
/// that fires.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class FrameworkOverridingPackageAnalyzer : KtsuAnalyzerBase
{
	/// <summary>
	/// Diagnostic ID for this analyzer.
	/// </summary>
	public const string DiagnosticId = "KTSU0008";

	/// <summary>
	/// Diagnostic property key carrying the package identifier that should be pinned down.
	/// </summary>
	public const string PackageIdProperty = "PackageId";

	/// <summary>
	/// Diagnostic property key carrying the version the shared framework supplies, which is the
	/// version the reference should be pinned to.
	/// </summary>
	public const string FrameworkVersionProperty = "FrameworkVersion";

	/// <summary>
	/// Diagnostic property key carrying the version the reference currently resolves.
	/// </summary>
	public const string ResolvedVersionProperty = "ResolvedVersion";

	/// <summary>
	/// File name of the build-generated list of packages the shared framework supplies.
	/// </summary>
	internal const string FrameworkPackagesFileName = "ktsu.framework-packages.g.txt";

	/// <summary>
	/// File name of the build-generated resolution facts for this target framework.
	/// </summary>
	internal const string FrameworkOverrideFactsFileName = "ktsu.framework-override-facts.g.txt";

	private static readonly LocalizableString Title = "Package reference overrides the shared framework";
	private static readonly LocalizableString MessageFormat = "Package reference '{0}' resolves {1}, overriding the {2} that this target framework's shared framework supplies. A consumer running on that framework will fail to load '{0}'; pin the version to {2}.";
	private static readonly LocalizableString Description = "A package that also ships in the shared framework must not be referenced above the version the lowest supported target framework provides. Assembly binds roll forward but never backward, so the produced package throws FileNotFoundException for consumers on that framework - a failure invisible in both the producing build and its nuspec.";

	private static readonly DiagnosticDescriptor Rule = CreateRule(DiagnosticId, Title, MessageFormat, Description);

	/// <inheritdoc/>
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	/// <inheritdoc/>
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationAction(AnalyzeCompilation);
	}

	private static void AnalyzeCompilation(CompilationAnalysisContext context)
	{
		Dictionary<string, string> frameworkPackages = LoadFrameworkPackages(
			context.Options.AdditionalFiles,
			context.CancellationToken);

		if (frameworkPackages.Count == 0)
		{
			return;
		}

		ResolutionFacts facts = LoadResolutionFacts(context.Options.AdditionalFiles, context.CancellationToken);

		// Ordered so that several overriding references are reported in a stable sequence rather
		// than in dictionary order, which is not a function of the source.
		foreach (string packageId in facts.Resolved.Keys.OrderBy(id => id, StringComparer.Ordinal))
		{
			// Only a direct reference is this project's to fix. A transitive package resolving above
			// the framework is the intermediate package's problem, and KTSU0006 is what would bring
			// it into this project in the first place.
			if (!facts.Direct.TryGetValue(packageId, out string privateAssets))
			{
				continue;
			}

			// The override only reaches a consumer as a failure when the dependency is absent from
			// the produced package, which happens only for a fully private reference. One that still
			// flows carries the higher version to the consumer along with it, and resolves there.
			if (!PrivateAssets.IsFullyPrivate(privateAssets))
			{
				continue;
			}

			if (!frameworkPackages.TryGetValue(packageId, out string frameworkVersion))
			{
				continue;
			}

			string resolvedVersion = facts.Resolved[packageId];

			// Resolving at or below the framework's version is the safe case, and the usual one:
			// NuGet prunes the reference so it contributes no compile assembly at all. Comparing
			// rather than assuming keeps a pruned-but-still-listed reference from being reported.
			if (CompareVersions(resolvedVersion, frameworkVersion) <= 0)
			{
				continue;
			}

			ImmutableDictionary<string, string?> properties = ImmutableDictionary<string, string?>.Empty
				.Add(PackageIdProperty, packageId)
				.Add(FrameworkVersionProperty, frameworkVersion)
				.Add(ResolvedVersionProperty, resolvedVersion);

			context.ReportDiagnostic(Diagnostic.Create(
				Rule,
				FindReferenceLocation(context, packageId),
				properties,
				packageId,
				resolvedVersion,
				frameworkVersion));
		}
	}

	/// <summary>
	/// Reads the packages the target framework's shared framework supplies, and at what version.
	/// </summary>
	/// <param name="files">The additional files supplied to the compilation.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A map of package identifier to the version the framework ships.</returns>
	/// <remarks>
	/// The file is the targeting pack's own <c>PackageOverrides.txt</c> content as MSBuild hands it
	/// over, so entries are separated by semicolons, newlines or both and carry arbitrary leading
	/// whitespace. Tokenizing on all of those is what keeps this independent of how MSBuild happens
	/// to flatten the metadata value.
	/// </remarks>
	private static Dictionary<string, string> LoadFrameworkPackages(
		ImmutableArray<AdditionalText> files,
		CancellationToken cancellationToken)
	{
		Dictionary<string, string> map = new(StringComparer.OrdinalIgnoreCase);

		SourceText? text = BuildFileLookup.ByName(files, FrameworkPackagesFileName)?.GetText(cancellationToken);
		if (text is null)
		{
			return map;
		}

		// RemoveEmptyEntries drops empty tokens before trimming, but a whitespace-only one survives
		// it and trims to empty - which this file is full of, since the overrides arrive indented.
		foreach (string entry in text.ToString()
			.Split([';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
			.Select(static token => token.Trim())
			.Where(static token => token.Length > 0))
		{
			// Format: packageId|version
			int separator = entry.IndexOf('|');
			if (separator <= 0 || separator == entry.Length - 1)
			{
				continue;
			}

			string packageId = entry.Substring(0, separator).Trim();
			string version = entry.Substring(separator + 1).Trim();

			if (packageId.Length == 0 || version.Length == 0)
			{
				continue;
			}

			// A package can be listed by more than one platform pack (Microsoft.NETCore.App and
			// Microsoft.AspNetCore.App overlap). The highest wins: that is the version actually
			// present at run time once both framework references are in play, so pinning to a lower
			// one would still leave the reference below what ships.
			if (!map.TryGetValue(packageId, out string existing) || CompareVersions(version, existing) > 0)
			{
				map[packageId] = version;
			}
		}

		return map;
	}

	/// <summary>
	/// Reads which packages resolved a real compile assembly, and which are referenced directly.
	/// </summary>
	/// <param name="files">The additional files supplied to the compilation.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The resolution facts for this target framework.</returns>
	private static ResolutionFacts LoadResolutionFacts(
		ImmutableArray<AdditionalText> files,
		CancellationToken cancellationToken)
	{
		Dictionary<string, string> resolved = new(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> direct = new(StringComparer.OrdinalIgnoreCase);

		SourceText? text = BuildFileLookup.ByName(files, FrameworkOverrideFactsFileName)?.GetText(cancellationToken);
		if (text is null)
		{
			return new ResolutionFacts(resolved, direct);
		}

		// Format: 'R|packageId|version' for a resolved compile assembly,
		// 'D|packageId|privateAssets' for a direct reference.
		foreach (string[] parts in text.Lines
			.Select(static line => line.ToString().Trim())
			.Where(static line => line.Length > 0)
			.Select(static line => line.Split('|')))
		{
			if (parts.Length < 2 || parts[1].Length == 0)
			{
				continue;
			}

			if (string.Equals(parts[0], "D", StringComparison.Ordinal))
			{
				direct[parts[1]] = parts.Length >= 3 ? parts[2] : string.Empty;
			}
			else if (string.Equals(parts[0], "R", StringComparison.Ordinal)
				&& parts.Length >= 3
				&& parts[2].Length > 0
				// A package can contribute several assemblies; they all carry the same package
				// version, so the first one seen is as good as any.
				&& !resolved.ContainsKey(parts[1]))
			{
				resolved[parts[1]] = parts[2];
			}
		}

		return new ResolutionFacts(resolved, direct);
	}

	/// <summary>
	/// Locates the line that declares the version to change, preferring
	/// <c>Directory.Packages.props</c> - which is where a centrally managed version lives, and so
	/// what has to change - before the project file.
	/// </summary>
	/// <param name="context">The compilation analysis context.</param>
	/// <param name="packageId">The package identifier to locate.</param>
	/// <returns>The line's location, or <see cref="Location.None"/> when it cannot be found.</returns>
	/// <remarks>
	/// A syntax-tree location is deliberately not used, for the reason recorded on
	/// <see cref="ProjectSourceLocation"/>: package compile items are prepended to the compilation,
	/// so the first tree is often generated code and a diagnostic located there is silently dropped.
	/// </remarks>
	private static Location FindReferenceLocation(CompilationAnalysisContext context, string packageId)
	{
		AdditionalText? packagesProps = BuildFileLookup.ByName(
			context.Options.AdditionalFiles,
			OrphanedPackageVersionAnalyzer.DirectoryPackagesPropsFileName);

		Location fromProps = BuildFileLookup.DeclarationLocation(
			packagesProps,
			"PackageVersion",
			packageId,
			context.CancellationToken);

		return fromProps != Location.None
			? fromProps
			: BuildFileLookup.DeclarationLocation(
				BuildFileLookup.ProjectFile(context.Options.AdditionalFiles),
				"PackageReference",
				packageId,
				context.CancellationToken);
	}

	/// <summary>
	/// Compares two NuGet version strings by their numeric release parts.
	/// </summary>
	/// <param name="left">The first version.</param>
	/// <param name="right">The second version.</param>
	/// <returns>A negative value, zero, or a positive value as <paramref name="left"/> sorts before,
	/// with, or after <paramref name="right"/>.</returns>
	/// <remarks>
	/// Only the dotted numeric release is compared, and a prerelease suffix is ignored rather than
	/// ordered below its release as NuGet would. The rule grades a version against the framework's
	/// own shipped version, which is never a prerelease, so the only case the simplification reaches
	/// is a prerelease of the framework's exact version - where treating <c>10.0.2-rc.1</c> as equal
	/// to <c>10.0.2</c> declines to report rather than reporting wrongly. Taking a dependency on
	/// NuGet.Versioning from an analyzer, which must load inside the compiler, is not worth that.
	/// </remarks>
	internal static int CompareVersions(string left, string right)
	{
		int[] leftParts = ParseReleaseParts(left);
		int[] rightParts = ParseReleaseParts(right);

		int length = Math.Max(leftParts.Length, rightParts.Length);

		for (int i = 0; i < length; i++)
		{
			int leftPart = i < leftParts.Length ? leftParts[i] : 0;
			int rightPart = i < rightParts.Length ? rightParts[i] : 0;

			if (leftPart != rightPart)
			{
				return leftPart < rightPart ? -1 : 1;
			}
		}

		return 0;
	}

	private static int[] ParseReleaseParts(string version)
	{
		if (string.IsNullOrWhiteSpace(version))
		{
			return [];
		}

		// Drop any prerelease or build-metadata suffix before splitting on '.'.
		string release = version.Trim();
		int suffix = release.IndexOfAny(['-', '+']);
		if (suffix >= 0)
		{
			release = release.Substring(0, suffix);
		}

		string[] parts = release.Split('.');
		List<int> values = new(parts.Length);

		foreach (string part in parts)
		{
			// A segment that is not a number ends the comparable release: everything after it is
			// not something this comparison can order, so it is left out rather than guessed at.
			if (!int.TryParse(part, out int value))
			{
				break;
			}

			values.Add(value);
		}

		return [.. values];
	}

	private readonly struct ResolutionFacts(Dictionary<string, string> resolved, Dictionary<string, string> direct)
	{
		/// <summary>Packages that resolved a real compile assembly, mapped to the resolved version.</summary>
		public Dictionary<string, string> Resolved { get; } = resolved;

		/// <summary>Packages this project references directly, mapped to their PrivateAssets value.</summary>
		public Dictionary<string, string> Direct { get; } = direct;
	}
}
