// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Sdk.Analyzers;

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

/// <summary>
/// Analyzer that flags orphaned <c>PackageVersion</c> entries in <c>Directory.Packages.props</c>.
/// A <c>PackageVersion</c> is considered orphaned when no project in the solution references the
/// corresponding package via a <c>PackageReference</c> (or <c>GlobalPackageReference</c>).
/// </summary>
/// <remarks>
/// The set of orphaned package identifiers is computed at build time by the SDK (which has
/// solution-wide visibility) and supplied to this analyzer as an additional file. The analyzer
/// then locates each orphaned entry inside <c>Directory.Packages.props</c> so the accompanying
/// code fixer can remove it.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class OrphanedPackageVersionAnalyzer : KtsuAnalyzerBase
{
	/// <summary>
	/// Diagnostic ID for this analyzer.
	/// </summary>
	public const string DiagnosticId = "KTSU0005";

	/// <summary>
	/// Diagnostic property key carrying the orphaned package identifier.
	/// </summary>
	public const string PackageIdProperty = "PackageId";

	/// <summary>
	/// File name of the build-generated list of orphaned package identifiers.
	/// </summary>
	internal const string OrphanedListFileName = "ktsu.orphaned-packageversions.g.txt";

	/// <summary>
	/// File name of the central package management file.
	/// </summary>
	internal const string DirectoryPackagesPropsFileName = "Directory.Packages.props";

	private static readonly LocalizableString Title = "Orphaned PackageVersion entry";
	private static readonly LocalizableString MessageFormat = "PackageVersion '{0}' in Directory.Packages.props is not referenced by any project and can be removed";
	private static readonly LocalizableString Description = "Central Package Management entries that are not referenced by any project add maintenance noise and should be removed.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		Title,
		MessageFormat,
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: Description,
		customTags: "CompilationEnd");

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
		CancellationToken cancellationToken = context.CancellationToken;

		AdditionalText? orphanedList = FindAdditionalFile(context.Options.AdditionalFiles, OrphanedListFileName);
		AdditionalText? packagesProps = FindAdditionalFile(context.Options.AdditionalFiles, DirectoryPackagesPropsFileName);

		if (orphanedList is null || packagesProps is null)
		{
			return;
		}

		SourceText? orphanedText = orphanedList.GetText(cancellationToken);
		SourceText? propsText = packagesProps.GetText(cancellationToken);

		if (orphanedText is null || propsText is null)
		{
			return;
		}

		foreach (TextLine orphanLine in orphanedText.Lines)
		{
			string packageId = orphanLine.ToString().Trim();
			if (packageId.Length == 0)
			{
				continue;
			}

			ReportOrphan(context, packagesProps.Path, propsText, packageId);
		}
	}

	private static void ReportOrphan(CompilationAnalysisContext context, string propsPath, SourceText propsText, string packageId)
	{
		foreach (TextLine line in propsText.Lines)
		{
			string lineText = line.ToString();
			if (IsPackageVersionLineFor(lineText, packageId))
			{
				LinePositionSpan lineSpan = propsText.Lines.GetLinePositionSpan(line.Span);
				Location location = Location.Create(propsPath, line.Span, lineSpan);

				ImmutableDictionary<string, string?> properties = ImmutableDictionary<string, string?>.Empty
					.Add(PackageIdProperty, packageId);

				context.ReportDiagnostic(Diagnostic.Create(Rule, location, properties, packageId));
				return;
			}
		}
	}

	/// <summary>
	/// Determines whether a line of <c>Directory.Packages.props</c> declares a <c>PackageVersion</c>
	/// for the supplied package identifier.
	/// </summary>
	internal static bool IsPackageVersionLineFor(string lineText, string packageId)
	{
		if (lineText.IndexOf("PackageVersion", StringComparison.OrdinalIgnoreCase) < 0)
		{
			return false;
		}

		// NuGet package identifiers are case-insensitive; match either quoting style.
		return lineText.IndexOf("Include=\"" + packageId + "\"", StringComparison.OrdinalIgnoreCase) >= 0
			|| lineText.IndexOf("Include='" + packageId + "'", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static AdditionalText? FindAdditionalFile(ImmutableArray<AdditionalText> files, string fileName)
	{
		foreach (AdditionalText file in files)
		{
			if (string.Equals(Path.GetFileName(file.Path), fileName, StringComparison.OrdinalIgnoreCase))
			{
				return file;
			}
		}

		return null;
	}
}
