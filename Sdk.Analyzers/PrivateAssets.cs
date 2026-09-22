// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Sdk.Analyzers;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

/// <summary>
/// Reads the <c>PrivateAssets</c> metadata of a package reference.
/// </summary>
/// <remarks>
/// NuGet omits a dependency from the produced package only when <em>every</em> asset kind is
/// private, so a partial value still leaks the dependency to consumers. That single fact is what
/// KTSU0007 enforces and what KTSU0008 keys off - KTSU0007 reports a build-time package that is not
/// private enough, KTSU0008 reports a framework-overriding package that is - so the definition lives
/// in one place rather than being spelled out twice with a chance of drifting apart.
/// </remarks>
internal static class PrivateAssets
{
	/// <summary>
	/// The asset kinds that <c>PrivateAssets="all"</c> expands to. A spelled-out value covering
	/// every one of them is equivalent to <c>all</c>.
	/// </summary>
	private static readonly ImmutableHashSet<string> AllAssetKinds = ImmutableHashSet.Create(
		StringComparer.OrdinalIgnoreCase,
		"compile",
		"runtime",
		"build",
		"buildMultitargeting",
		"buildTransitive",
		"contentFiles",
		"analyzers",
		"native");

	/// <summary>
	/// Determines whether a <c>PrivateAssets</c> value makes a reference fully private, so that the
	/// dependency does not appear in the produced package at all.
	/// </summary>
	/// <param name="privateAssets">The semicolon-separated metadata value, which may be empty.</param>
	/// <returns><see langword="true"/> when the value is <c>all</c> or names every asset kind.</returns>
	public static bool IsFullyPrivate(string? privateAssets)
	{
		if (string.IsNullOrWhiteSpace(privateAssets))
		{
			return false;
		}

		HashSet<string> tokens = new(StringComparer.OrdinalIgnoreCase);

		foreach (string token in privateAssets!.Split(';'))
		{
			string trimmed = token.Trim();

			if (trimmed.Length > 0)
			{
				tokens.Add(trimmed);
			}
		}

		return tokens.Contains("all") || AllAssetKinds.All(tokens.Contains);
	}
}
