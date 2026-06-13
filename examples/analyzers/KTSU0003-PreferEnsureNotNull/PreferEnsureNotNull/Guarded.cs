namespace PreferEnsureNotNull;

/// <summary>Uses ArgumentNullException.ThrowIfNull, which KTSU0003 flags.</summary>
public static class Guarded
{
    /// <summary>Validates the argument the discouraged way.</summary>
    /// <param name="value">The value to validate.</param>
    public static void Check(object value) => System.ArgumentNullException.ThrowIfNull(value);
}
