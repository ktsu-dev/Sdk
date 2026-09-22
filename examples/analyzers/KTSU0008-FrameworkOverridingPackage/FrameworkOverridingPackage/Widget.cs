namespace FrameworkOverridingPackage;

/// <summary>Uses the overriding package so the reference is not merely declared.</summary>
public static class Widget
{
    /// <summary>Serializes the answer.</summary>
    /// <returns>The answer as JSON.</returns>
    public static string Answer() => System.Text.Json.JsonSerializer.Serialize(42);
}
