namespace Demo.Godot;

/// <summary>
/// Engine-agnostic logic, deliberately free of any GodotSharp type so the same source could be
/// shared with a non-Godot project.
/// </summary>
public static class Greeter
{
    /// <summary>Builds a greeting for the supplied name.</summary>
    /// <param name="name">The name to greet.</param>
    /// <returns>A friendly greeting.</returns>
    public static string Greet(string name) => $"Hello from {name}, built with ktsu.Sdk.Godot!";
}
