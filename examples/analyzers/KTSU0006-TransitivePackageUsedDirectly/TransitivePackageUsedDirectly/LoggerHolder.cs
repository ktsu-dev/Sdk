namespace TransitivePackageUsedDirectly;

/// <summary>References a type from a transitive package directly (KTSU0006).</summary>
public static class LoggerHolder
{
    /// <summary>Returns the full name of ILogger, defined in the transitive Abstractions package.</summary>
    /// <returns>The full type name.</returns>
    public static string LoggerTypeName() => typeof(Microsoft.Extensions.Logging.ILogger).FullName!;
}
