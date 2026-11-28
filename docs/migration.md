# Migration guide

This guide maps common .NET CLI stacks to RunesHela. The main difference is that
RunesHela generates the parser, binder, help, completion, man pages, schema, and
MCP manifest from one class-first model at compile time.

## From `System.CommandLine`

| System.CommandLine | RunesHela |
|---|---|
| `new RootCommand()` + `Command` objects | `[Command]` on a class; nested `[Command]` classes become subcommands |
| `new Option<int>("--count")` | `[Option] public int Count { get; init; }` |
| `new Argument<string>("name")` | `[Argument(0)] public string Name { get; init; }` |
| `SetHandler(...)` | `public void Run()` / `int Run()` / `Task RunAsync()` / `ValueTask<int> RunAsync()` |
| `Invoke(args)` | `RunesHelaApp.Run(args)` |
| Custom validator | `[Required]`, `[Range]`, `[Regex]`, `[Length]` |
| Custom help | generated `--help`, `--help --json`, `describe`, `schema` |

Behavior differences:

- Exit code `2` is used for usage errors; configuration errors use `3`.
- Non-TTY destructive commands fail immediately unless `--yes` is supplied.
- Unknown names produce a did-you-mean hint and a structured error envelope.

## From `Spectre.Console.Cli`

| Spectre.Console.Cli | RunesHela |
|---|---|
| `Command<TSettings>` | `[Command]` class with `[Option]`/`[Argument]` properties |
| `CommandSettings` | not needed; the command class is the settings object |
| `Execute(CommandContext, TSettings)` | `Run`/`RunAsync` on the command class |
| `IAnsiConsole` rendering | use `System.Console` or `RunesHelaConsole.Current` |

RunesHela deliberately does not render tables, progress bars, or prompts; use
your preferred console library inside the handler.

## From `ConsoleAppFramework`

| ConsoleAppFramework | RunesHela |
|---|---|
| `[Command]` on methods | `[Command]` on classes (method-first is a future milestone) |
| Method parameters | properties with `[Option]`/`[Argument]` |
| `ConsoleApp.Run(args, ...)` | `RunesHelaApp.Run(args)` |
| Source-generated help | generated help, completion, man, schema, MCP |

RunesHela adds a richer Agent contract (structured errors, `describe`, `schema`,
MCP) and a validation/filter pipeline.

## From hand-written `Main`

1. Move each command's argument and option parsing into a class with settable
   properties.
2. Replace `if (args[0] == ...)` routing with `[Command]` names and nesting.
3. Replace `Console.Error.WriteLine` and `Environment.Exit` with error returns:
   - Return an `int` from `Run` for domain exit codes.
   - Throw for unexpected failures; the framework maps them to exit code `1`.
4. Keep business logic in the handler; do not parse `args` directly.

## Configuration migration

- Old environment-variable names can be preserved with
  `[Option(Env = "OLD_NAME")]`.
- Defaults move to property initializers; required values use
  `[Option(Required = true)]` or `[Required]`.
- JSON configuration keys default to `command.path.option-name`; override with
  `[Option(Config = "section.key")]`.
- Explicit files use `--config <PATH>` or `RHL_CONFIG`. Implicit discovery is
  opt-in with `[assembly: RunesHelaConfigFile]`.

## Compatibility notes

- Public API is tracked in `tests/RunesHela.Tests/PublicApiTests.approved.txt`.
- Generated help, JSON Schema, and MCP manifest fields are versioned with
  `schemaVersion`; additive fields are allowed in minor releases.
- Exit codes and stdout/stderr separation are part of the compatibility contract.
