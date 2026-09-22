namespace FrameworkPinned;

/// <summary>Uses the framework-supplied package at the pinned floor version.</summary>
public static class Widget
{
    /// <summary>Serializes the answer.</summary>
    /// <returns>The answer as JSON.</returns>
    public static string Answer() => System.Text.Json.JsonSerializer.Serialize(42);
}
