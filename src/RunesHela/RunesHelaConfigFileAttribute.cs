namespace RunesHela;

/// <summary>
/// Enables implicit configuration file discovery for the assembly.
/// Add <c>[assembly: RunesHelaConfigFile]</c> to read <c>appsettings.json</c> from the current
/// directory or <c>~/.config/&lt;app&gt;/</c> when no explicit path is supplied.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class RunesHelaConfigFileAttribute : Attribute
{
    /// <summary>
    /// Creates the attribute using <c>appsettings.json</c> as the file name.
    /// </summary>
    public RunesHelaConfigFileAttribute()
    {
    }

    /// <summary>
    /// Creates the attribute with a custom file name.
    /// </summary>
    /// <param name="fileName">The implicit configuration file name.</param>
    public RunesHelaConfigFileAttribute(string fileName)
    {
        FileName = fileName;
    }

    /// <summary>
    /// Gets the implicit configuration file name.
    /// </summary>
    public string FileName { get; } = "appsettings.json";
}
