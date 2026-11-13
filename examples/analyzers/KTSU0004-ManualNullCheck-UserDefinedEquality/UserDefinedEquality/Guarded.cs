namespace UserDefinedEquality;

/// <summary>
/// Guards a handle with its own operator ==, so a released handle is rejected too. Ensure.NotNull
/// would only reject a null reference, so KTSU0004 must leave this guard alone.
/// </summary>
public static class Guarded
{
    /// <summary>Uses a handle that must be live.</summary>
    /// <param name="handle">The handle to use.</param>
    /// <returns>A marker string.</returns>
    public static string Use(Handle handle)
    {
        if (handle == null)
        {
            throw new System.ArgumentNullException(nameof(handle));
        }

        return "used";
    }

    /// <summary>Uses a handle that must be live, with the null literal on the left.</summary>
    /// <param name="handle">The handle to use.</param>
    /// <returns>A marker string.</returns>
    public static string UseReversed(Handle handle)
    {
        if (null == handle)
        {
            throw new System.ArgumentNullException(nameof(handle));
        }

        return "used";
    }
}
