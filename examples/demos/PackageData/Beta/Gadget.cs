namespace Beta;

/// <summary>A trivial type that consumes <see cref="Alpha.Widget"/>.</summary>
public static class Gadget
{
	/// <summary>Returns a description built from the referenced library.</summary>
	public static string Describe() => $"gadget wrapping {Alpha.Widget.Name}";
}
