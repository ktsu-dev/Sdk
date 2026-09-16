namespace Demo.Godot;

// The project is named {Solution}.Godot so the core SDK detects the project type, which makes
// this file's namespace shadow the engine's own `Godot` namespace. `global::` is the fix, and
// it is the one thing worth knowing before naming a project this way.
using global::Godot;

/// <summary>
/// The script attached to Main.tscn. Godot instantiates it and calls <see cref="_Ready"/>;
/// nothing here is invoked by the .NET build, which only produces the assembly Godot loads.
/// </summary>
public partial class Main : Node
{
    /// <inheritdoc/>
    public override void _Ready() => GD.Print(Greeter.Greet("Godot"));
}
