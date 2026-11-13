namespace Service;

/// <summary>Has internals that should be exposed to the sibling test project.</summary>
public static class Worker
{
    internal static int Work() => 1;
}
