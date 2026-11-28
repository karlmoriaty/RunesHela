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

    [Fact]
    public void OptionsAndHelp()
    {
        const string source = """
            using RunesHela;

            namespace Demo;

            /// <summary>
            /// 部署到指定环境。
            /// </summary>
            /// <example>deploy prod --environment live --count 3</example>
            [Command("deploy", Description = "部署到指定环境。")]
            public sealed class DeployCommand
            {
                /// <summary>
                /// 部署目标。
                /// </summary>
                [Argument(0, Description = "部署目标。")]
                public string Target { get; init; } = "all";

                /// <summary>
                /// 目标环境。
                /// </summary>
                [Option(Short = 'e', Description = "目标环境。", Env = "APP_ENV")]
                public string Environment { get; init; } = "dev";

                /// <summary>
                /// 重复次数。
                /// </summary>
                [Option(Short = 'c', Description = "重复次数。")]
                [Range(1, 10)]
                public int Count { get; init; } = 1;

                /// <summary>
                /// 是否演练。
                /// </summary>
                [Option(Description = "是否演练。")]
                public bool DryRun { get; init; }

                public int Run()
                {
                    return 0;
                }
            }
            """;

        var result = GeneratorTests.RunGenerator(source);

        Assert.Empty(result.CompileErrors);
        Snapshot.Verify(nameof(OptionsAndHelp), result.GeneratedSource);
    }

    [Fact]
    public void AgentContract()
    {
        const string source = """
            using System;
            using System.Threading.Tasks;
            using RunesHela;

            namespace Demo;

            public sealed class AuditFilter : IRunesHelaFilter
            {
                public ValueTask<int> InvokeAsync(CommandContext context, Func<CommandContext, ValueTask<int>> next)
                {
                    return next(context);
                }
            }

            /// <summary>
            /// Purges a target.
            /// </summary>
            [Command(
                "purge",
                Description = "Purges a target.",
                IsDestructive = true,
                Filters = new[] { nameof(AuditFilter) })]
            public sealed class PurgeCommand
            {
                /// <summary>
                /// Target to purge.
                /// </summary>
                [Argument(0, Description = "Target to purge.")]
                public string Target { get; init; } = "all";

                public int Run()
                {
                    return 0;
                }
            }
            """;

        var result = GeneratorTests.RunGenerator(source);

        Assert.Empty(result.CompileErrors);
        Snapshot.Verify(nameof(AgentContract), result.GeneratedSource);
    }
}
