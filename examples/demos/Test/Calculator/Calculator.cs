using System.Runtime.CompilerServices;

// Satisfies KTSU0002: expose internals to the sibling test project.
[assembly: InternalsVisibleTo("Calculator.Test")]

namespace Calculator;

/// <summary>A tiny calculator used to demonstrate the test-project workflow.</summary>
public static class Maths
{
    /// <summary>Adds two integers.</summary>
    /// <param name="a">First addend.</param>
    /// <param name="b">Second addend.</param>
    /// <returns>The sum.</returns>
    public static int Add(int a, int b) => a + b;

    // Internal helper, visible to Calculator.Test via InternalsVisibleTo.
    internal static int Triple(int value) => value * 3;
}
