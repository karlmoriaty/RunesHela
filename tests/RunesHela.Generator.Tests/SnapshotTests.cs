using Xunit;

namespace RunesHela.Generator.Tests;

public sealed class SnapshotTests
{
    [Fact]
    public void SingleCommand()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("hello", Description = "Say hello.")]
            public sealed class HelloCommand
            {
                public int Run()
                {
                    return 0;
                }
            }
            """;

        var result = GeneratorTests.RunGenerator(source);

        Assert.Empty(result.CompileErrors);
        Snapshot.Verify(nameof(SingleCommand), result.GeneratedSource);
    }

    [Fact]
    public void NestedCommands()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("remote", Description = "Manage remotes.")]
            public sealed class RemoteCommand
            {
                [Command("add", Description = "Add a remote.")]
                public sealed class AddCommand
                {
                    public void Run()
                    {
                    }
                }

                [Command("list", Description = "List remotes.")]
                public sealed class ListCommand
                {
                    public int Run()
                    {
                        return 0;
                    }
                }
            }
            """;

        var result = GeneratorTests.RunGenerator(source);

        Assert.Empty(result.CompileErrors);
        Snapshot.Verify(nameof(NestedCommands), result.GeneratedSource);
    }

    [Fact]
    public void AliasesAndHidden()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("hello", Aliases = new[] { "hi" }, Description = "Say hello.")]
            public sealed class HelloCommand
            {
                public void Run()
                {
                }
            }

            [Command("secret", Aliases = new[] { "hidden" }, IsHidden = true, Description = "Hidden command.")]
            public sealed class SecretCommand
            {
                public int Run()
                {
                    return 0;
                }
            }
            """;

        var result = GeneratorTests.RunGenerator(source);

        Assert.Empty(result.CompileErrors);
        Snapshot.Verify(nameof(AliasesAndHidden), result.GeneratedSource);
    }

    [Fact]
    public void AsyncHandlers()
    {
        const string source = """
            using System.Threading.Tasks;
            using RunesHela;

            namespace Demo;

            [Command("job", Description = "Run a job.")]
            public sealed class JobCommand
            {
                public async Task RunAsync()
                {
                    await Task.Yield();
                }
            }

            [Command("count", Description = "Count things.")]
            public sealed class CountCommand
            {
                public ValueTask<int> RunAsync()
                {
                    return new ValueTask<int>(0);
                }
            }
            """;

        var result = GeneratorTests.RunGenerator(source);

        Assert.Empty(result.CompileErrors);
        Snapshot.Verify(nameof(AsyncHandlers), result.GeneratedSource);
    }

    [Fact]
    public void GroupCommand()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            [Command("remote", Description = "Manage remotes.")]
            public sealed class RemoteCommand
            {
                [Command("add", Description = "Add a remote.")]
                public sealed class AddCommand
                {
                    public void Run()
                    {
                    }
                }
            }
            """;

        var result = GeneratorTests.RunGenerator(source);

        Assert.Empty(result.CompileErrors);
        Snapshot.Verify(nameof(GroupCommand), result.GeneratedSource);
    }
}
