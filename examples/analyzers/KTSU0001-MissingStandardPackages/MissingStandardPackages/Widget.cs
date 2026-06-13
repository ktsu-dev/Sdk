namespace MissingStandardPackages;

/// <summary>Any non-test project triggers KTSU0001 when the standard packages are absent.</summary>
public static class Widget
{
    /// <summary>A trivial member so the project has compilable content.</summary>
    /// <returns>The answer.</returns>
    public static int Answer() => 42;
}
