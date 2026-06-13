namespace Library;

/// <summary>Demonstrates a minimal public API built with the core ktsu.Sdk.</summary>
public static class Greeter
{
    /// <summary>Builds a greeting for the supplied name.</summary>
    /// <param name="name">The name to greet.</param>
    /// <returns>A friendly greeting.</returns>
    public static string Greet(string name)
    {
        Ensure.NotNull(name);
        return $"Hello, {name}!";
    }
}
