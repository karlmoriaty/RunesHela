using Xunit;

namespace RunesHela.Generator.Tests;

internal static class Snapshot
{
    public static void Verify(string name, string actual)
    {
        var normalized = actual.Replace("\r\n", "\n");
        var directory = GetSnapshotDirectory();
        Directory.CreateDirectory(directory);

        var verifiedPath = Path.Combine(directory, name + ".verified.g.cs");
        var receivedPath = Path.Combine(directory, name + ".received.g.cs");

        if (Environment.GetEnvironmentVariable("UPDATE_SNAPSHOTS") == "1")
        {
            File.WriteAllText(verifiedPath, normalized);
            return;
        }

        if (!File.Exists(verifiedPath))
        {
            File.WriteAllText(receivedPath, normalized);
            Assert.Fail($"Missing snapshot '{name}'. Run tests with UPDATE_SNAPSHOTS=1 to create it.");
        }

        var expected = File.ReadAllText(verifiedPath).Replace("\r\n", "\n");

        if (!string.Equals(expected, normalized, StringComparison.Ordinal))
        {
            File.WriteAllText(receivedPath, normalized);
            Assert.Fail($"Snapshot mismatch '{name}'. Compare '{receivedPath}' with '{verifiedPath}'.");
        }
    }

    private static string GetSnapshotDirectory()
    {
        var directory = AppContext.BaseDirectory;

        while (directory is not null &&
               !File.Exists(Path.Combine(directory, "RunesHela.Generator.Tests.csproj")))
        {
            directory = Directory.GetParent(directory)?.FullName;
        }

        if (directory is null)
        {
            throw new InvalidOperationException("Could not locate the test project directory.");
        }

        return Path.Combine(directory, "Snapshots");
    }
}
