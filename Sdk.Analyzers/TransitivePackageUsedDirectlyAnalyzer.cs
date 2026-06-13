// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Sdk.Analyzers;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

/// <summary>
/// Analyzer that reports when a project uses a type or member that originates from a transitive
/// package dependency (one that is pulled in indirectly) without declaring a direct
/// <c>PackageReference</c> to it.
/// </summary>
/// <remarks>
/// Relying on transitive dependencies is brittle: an upgrade of an intermediate package can silently
/// drop the dependency and break the build. The SDK supplies two build-generated additional files:
/// a map of <c>assemblySimpleName|packageId|packageVersion</c> for every package-provided assembly,
/// and the set of package identifiers that are referenced directly. This analyzer resolves each used
/// symbol to its owning assembly and reports the package if it is not referenced directly.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TransitivePackageUsedDirectlyAnalyzer : KtsuAnalyzerBase
{
	/// <summary>
	/// Diagnostic ID for this analyzer.
	/// </summary>
	public const string DiagnosticId = "KTSU0006";

	/// <summary>
	/// Diagnostic property key carrying the package identifier that should be referenced.
	/// </summary>
	public const string PackageIdProperty = "PackageId";

	/// <summary>
	/// Diagnostic property key carrying the resolved package version.
	/// </summary>
	public const string PackageVersionProperty = "PackageVersion";

	/// <summary>
	/// File name of the build-generated assembly-to-package map.
	/// </summary>
	internal const string PackageMapFileName = "ktsu.transitive-package-map.g.txt";

	/// <summary>
	/// File name of the build-generated list of direct package references.
	/// </summary>
	internal const string DirectPackagesFileName = "ktsu.direct-packages.g.txt";

	private static readonly LocalizableString Title = "Transitive package used directly";
	private static readonly LocalizableString MessageFormat = "Type or member from transitive package '{0}' is used directly; add a PackageReference to '{0}'";
	private static readonly LocalizableString Description = "Types from transitive package dependencies should not be used directly. Add an explicit PackageReference so the dependency is not silently lost when an intermediate package changes.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		Title,
		MessageFormat,
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: Description);

	/// <inheritdoc/>
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	/// <inheritdoc/>
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(OnCompilationStart);
	}

	private static void OnCompilationStart(CompilationStartAnalysisContext context)
	{
		// assemblySimpleName -> (packageId, packageVersion)
		Dictionary<string, PackageInfo> assemblyToPackage = LoadAssemblyMap(context.Options.AdditionalFiles, context.CancellationToken);
		if (assemblyToPackage.Count == 0)
		{
			return;
		}

		ImmutableHashSet<string> directPackages = LoadLineSet(context.Options.AdditionalFiles, DirectPackagesFileName, context.CancellationToken);

		// Report at most one diagnostic per transitive package across the whole compilation.
		ConcurrentDictionary<string, byte> reported = new(StringComparer.OrdinalIgnoreCase);

		context.RegisterSyntaxNodeAction(
			nodeContext => AnalyzeName(nodeContext, assemblyToPackage, directPackages, reported),
			SyntaxKind.IdentifierName,
			SyntaxKind.GenericName);
	}

	private static void AnalyzeName(
		SyntaxNodeAnalysisContext context,
		Dictionary<string, PackageInfo> assemblyToPackage,
		ImmutableHashSet<string> directPackages,
		ConcurrentDictionary<string, byte> reported)
	{
		if (context.Node is not SimpleNameSyntax name)
		{
			return;
		}

		ISymbol? symbol = context.SemanticModel.GetSymbolInfo(name, context.CancellationToken).Symbol;
		IAssemblySymbol? assembly = symbol?.ContainingAssembly;
		if (assembly is null)
		{
			return;
		}

		// Ignore symbols defined in the project being compiled.
		if (SymbolEqualityComparer.Default.Equals(assembly, context.Compilation.Assembly))
		{
			return;
		}

		if (!assemblyToPackage.TryGetValue(assembly.Identity.Name, out PackageInfo package))
		{
			return;
		}

		if (directPackages.Contains(package.Id))
		{
			return;
		}

		// Only the first usage of each transitive package produces a diagnostic.
		if (!reported.TryAdd(package.Id, 0))
		{
			return;
		}

		ImmutableDictionary<string, string?> properties = ImmutableDictionary<string, string?>.Empty
			.Add(PackageIdProperty, package.Id)
			.Add(PackageVersionProperty, package.Version);

		context.ReportDiagnostic(Diagnostic.Create(Rule, name.GetLocation(), properties, package.Id));
	}

	private static Dictionary<string, PackageInfo> LoadAssemblyMap(ImmutableArray<AdditionalText> files, CancellationToken cancellationToken)
	{
		Dictionary<string, PackageInfo> map = new(StringComparer.OrdinalIgnoreCase);

		AdditionalText? mapFile = FindAdditionalFile(files, PackageMapFileName);
		SourceText? text = mapFile?.GetText(cancellationToken);
		if (text is null)
		{
			return map;
		}

		foreach (TextLine line in text.Lines)
		{
			string raw = line.ToString().Trim();
			if (raw.Length == 0)
			{
				continue;
			}

			// Format: assemblySimpleName|packageId|packageVersion
			string[] parts = raw.Split('|');
			if (parts.Length < 2 || parts[0].Length == 0 || parts[1].Length == 0)
			{
				continue;
			}

			string assemblyName = parts[0];
			string packageId = parts[1];
			string packageVersion = parts.Length >= 3 ? parts[2] : string.Empty;

			map[assemblyName] = new PackageInfo(packageId, packageVersion);
		}

		return map;
	}

	private static ImmutableHashSet<string> LoadLineSet(ImmutableArray<AdditionalText> files, string fileName, CancellationToken cancellationToken)
	{
		AdditionalText? file = FindAdditionalFile(files, fileName);
		SourceText? text = file?.GetText(cancellationToken);
		if (text is null)
		{
			return ImmutableHashSet<string>.Empty;
		}

		ImmutableHashSet<string>.Builder builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.OrdinalIgnoreCase);
		foreach (TextLine line in text.Lines)
		{
			string value = line.ToString().Trim();
			if (value.Length > 0)
			{
				builder.Add(value);
			}
		}

		return builder.ToImmutable();
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

	private readonly struct PackageInfo(string id, string version)
	{
		public string Id { get; } = id;

		public string Version { get; } = version;
	}
}
