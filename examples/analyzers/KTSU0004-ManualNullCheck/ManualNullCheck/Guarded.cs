namespace ManualNullCheck;

/// <summary>Uses a manual null check + ArgumentNullException, which KTSU0004 flags.</summary>
public static class Guarded
{
    /// <summary>Validates the argument the discouraged way.</summary>
    /// <param name="value">The value to validate.</param>
    public static void Check(object value)
    {
        if (value is null)
        {
            throw new System.ArgumentNullException(nameof(value));
        }
    }
}
