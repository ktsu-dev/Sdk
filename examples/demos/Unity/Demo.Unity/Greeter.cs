namespace Demo.Unity;

/// <summary>Engine-agnostic game logic compiled as a Unity managed plug-in.</summary>
public static class Greeter
{
    /// <summary>Builds a greeting for the supplied name.</summary>
    /// <param name="name">The name to greet.</param>
    /// <returns>A friendly greeting.</returns>
    public static string Greet(string name) => $"Hello, {name}!";
}
