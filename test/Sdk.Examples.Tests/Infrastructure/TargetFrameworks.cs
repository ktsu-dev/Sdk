namespace Sdk.Examples.Tests.Infrastructure;

using System.Text.RegularExpressions;

/// <summary>
/// The frameworks ktsu.Sdk pins, mirrored here as the suite's expected values.
/// </summary>
/// <remarks>
/// These are hand-maintained expectations rather than values read back from the SDK, so a test
/// still fails when the SDK changes underneath it. Update them in the same commit that changes
/// <c>Sdk/Sdk.props</c>. The rule that decides which frameworks belong in <see cref="Library"/>
/// is in <c>docs/plans/dotnet-11-readiness.md</c>: a framework enters the list when it ships and
/// leaves when it goes out of support.
/// </remarks>
internal static partial class TargetFrameworks
{
    /// <summary>The single framework every extension SDK and every test project pins.</summary>
    public const string Latest = "net10.0";

    /// <summary>The default multi-target list a library inherits from the core SDK.</summary>
    public const string Library = "net10.0;net9.0;net8.0;netstandard2.0;netstandard2.1";

    /// <summary>
    /// A short list used only to force a build to fan out into parallel inner builds. It needs
    /// two or more frameworks the pinned SDK can build, and stays deliberately shorter than
    /// <see cref="Library"/> so the timing-sensitive harness tests do not slow down.
    /// </summary>
    public const string MultiTargetProbe = "net10.0;net9.0;net8.0";

    /// <summary>Composes a platform-specific framework, for example <c>net10.0-ios</c>.</summary>
    /// <param name="suffix">The platform suffix including its leading hyphen, or an empty string.</param>
    /// <returns>The framework the matching platform SDK pins.</returns>
    public static string Platform(string suffix) => Latest + suffix;

    /// <summary>
    /// Repins every versioned .NET framework in a project file to <paramref name="latest"/>,
    /// preserving any platform suffix.
    /// </summary>
    /// <param name="projectXml">The project file's contents.</param>
    /// <param name="latest">The framework to pin, for example <c>net11.0</c>.</param>
    /// <returns>The rewritten contents, unchanged when the file pins no versioned framework.</returns>
    /// <remarks>
    /// Only a <c>netX.Y</c> pin is rewritten. A <c>netstandard2.0</c> pin is deliberate in the
    /// KTSU0001 examples and is left alone, which falls out of the pattern requiring a digit
    /// immediately after <c>net</c>. The plural <c>TargetFrameworks</c> element is also left
    /// alone, since the tests that build a multi-target list compose it from
    /// <see cref="MultiTargetProbe"/> rather than from whatever an example happens to declare.
    /// </remarks>
    public static string RewritePins(string projectXml, string latest) =>
        VersionedPin().Replace(projectXml, $"<TargetFramework>{latest}${{platform}}</TargetFramework>");

    [GeneratedRegex(@"<TargetFramework>net\d+\.\d+(?<platform>-[a-z][a-z0-9.]*)?</TargetFramework>")]
    private static partial Regex VersionedPin();
}
