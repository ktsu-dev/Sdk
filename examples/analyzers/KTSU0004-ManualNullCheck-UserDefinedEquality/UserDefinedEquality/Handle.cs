namespace UserDefinedEquality;

/// <summary>
/// A handle that compares equal to null once released, the way UnityEngine.Object does once destroyed.
/// </summary>
public sealed class Handle : System.IEquatable<Handle>
{
    /// <summary>Gets a value indicating whether the handle has been released.</summary>
    public bool IsReleased { get; private set; }

    /// <summary>Releases the handle.</summary>
    public void Release() => IsReleased = true;

    /// <summary>Compares two handles, treating a released handle as null.</summary>
    /// <param name="left">The first handle.</param>
    /// <param name="right">The second handle.</param>
    /// <returns>Whether both are null or released, or both are the same live handle.</returns>
    public static bool operator ==(Handle? left, Handle? right)
    {
        bool leftAlive = left is not null && !left.IsReleased;
        bool rightAlive = right is not null && !right.IsReleased;
        return leftAlive == rightAlive && (!leftAlive || ReferenceEquals(left, right));
    }

    /// <summary>Compares two handles, treating a released handle as null.</summary>
    /// <param name="left">The first handle.</param>
    /// <param name="right">The second handle.</param>
    /// <returns>The negation of <c>==</c>.</returns>
    public static bool operator !=(Handle? left, Handle? right) => !(left == right);

    /// <inheritdoc/>
    public bool Equals(Handle? other) => this == other;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Handle other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
}
