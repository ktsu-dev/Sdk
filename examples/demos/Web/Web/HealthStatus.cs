namespace Web;

// Public and deliberately undocumented: this type is what proves ktsu.Sdk.Web suppresses CS1591.
// The core SDK turns XML documentation on and warnings into errors, which together fail a build on
// the first undocumented public type. That is right for a library, whose public types are its
// product, and wrong for a web application, whose public types are payload shapes with no external
// consumer. The SDK keeps the documentation file, for OpenAPI generators, and drops the rule.
public sealed record HealthStatus(string Status);
