namespace Demo.Godot;

using global::Godot;

/// <summary>A minimal Godot node script, proving the GodotSharp bindings resolve.</summary>
public partial class Greeter : Node
{
    /// <inheritdoc/>
    public override void _Ready() => GD.Print("Hello from Demo.Godot built with ktsu.Sdk.Godot.");
}
