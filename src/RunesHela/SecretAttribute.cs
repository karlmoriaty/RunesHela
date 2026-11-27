namespace RunesHela;

/// <summary>
/// Marks a bound option as sensitive so help output, errors, and diagnostics redact its value.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SecretAttribute : Attribute
{
}
