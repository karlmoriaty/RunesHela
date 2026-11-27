namespace RunesHela.Benchmarks;

internal sealed class NullConsole : IConsole
{
    public static readonly NullConsole Instance = new();

    public TextWriter Out => TextWriter.Null;

    public TextWriter Error => TextWriter.Null;

    public bool IsOutputRedirected => true;
}
