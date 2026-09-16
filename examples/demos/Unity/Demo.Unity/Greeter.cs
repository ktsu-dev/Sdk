namespace Demo.Unity;

/// <summary>
/// The plug-in's public API. Nothing here references UnityEngine: the engine-side script in
/// UnityProject/Assets/Scripts calls into this assembly, not the other way round, which is what
/// keeps the plug-in buildable (and testable) without Unity installed.
/// </summary>
public static class Greeter
{
    /// <summary>Builds a greeting for the supplied name.</summary>
    /// <param name="name">The name to greet.</param>
    /// <returns>A friendly greeting.</returns>
    public static string Greet(string name) => $"Hello from {name}, built with ktsu.Sdk.Unity!";
}
