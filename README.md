# RunesHela

**Treat the CLI as a compiler problem: one declaration, every artifact.**

RunesHela is a compile-time CLI framework for .NET. Declare commands as plain classes;
the compiler generates the parser, binder, router, help, shell completions, man pages,
JSON Schema, and MCP tools. Generated binding uses no reflection, the runtime has zero
third-party dependencies, and the hot path is allocation-free.

[![NuGet](https://img.shields.io/nuget/v/RunesHela.svg)](https://www.nuget.org/packages/RunesHela)
[![NuGet downloads](https://img.shields.io/nuget/dt/RunesHela.svg)](https://www.nuget.org/packages/RunesHela)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](LICENSE)

**Status:** v1.0.0 released on NuGet.org and GitHub Releases (2026-11-28).

[Quickstart](docs/quickstart.md) · [Changelog](CHANGELOG.md) · [Documentation](#documentation) · [简体中文](README.zh-CN.md)

---

## Why RunesHela

- **One model, every artifact.** Help, `--help --json`, `describe`, `schema`,
  completion scripts, man pages, Markdown docs, and the MCP manifest all come from
  the same command model. They cannot drift.
- **Zero reflection, zero runtime dependencies.** The generator emits a parser and
  binder at compile time; generated code does not scan assemblies or invoke reflection.
- **Allocation-free hot path.** Parsing and binding five options performs no allocations
  beyond the command instance.
- **Native AOT first.** Generated apps publish with Native AOT, trimming, single-file,
  and ReadyToRun configurations; CI validates all of them.
- **Agent-native contract.** Non-TTY behavior, structured errors with next steps,
  destructive-operation guards, stable exit codes, self-description, and a zero-dependency
  MCP stdio server.

---

## Install

```text
dotnet add package RunesHela --version 1.0.0
```

Target framework: `net10.0` (C# 14). The package includes the runtime, XML docs, and the
source generator under `analyzers/dotnet/cs`.

---

## Quickstart

```csharp
using RunesHela;

return RunesHelaApp.Run(args);

/// <summary>
/// Greets a person.
/// </summary>
/// <example>greet Ada --count 2</example>
[Command("greet", Description = "Greets a person.")]
public sealed class GreetCommand
{
    /// <summary>Person to greet.</summary>
    [Argument(0, Description = "Person to greet.")]
    public string Name { get; init; } = "world";

    /// <summary>Repeat count.</summary>
    [Option(Short = 'c', Description = "Repeat count.", Env = "APP_COUNT")]
    [Range(1, 5)]
    public int Count { get; init; } = 1;

    public int Run()
    {
        for (var i = 0; i < Count; i++)
        {
            Console.WriteLine("Hello, " + Name + "!");
        }

        return 0;
    }
}
```

No `partial`, interface, base class, or manual parser is required.

```text
app greet Ada --count 2
app greet --help
app greet --help --json
app describe
app schema
app mcp --manifest
```

Document comments (`<summary>` / `<example>`) require
`<GenerateDocumentationFile>true</GenerateDocumentationFile>`; otherwise use
`[Command(Description = ...)]` and `[Option(Description = ...)]`.

---

## What gets generated

| Artifact | Command | Contents |
|---|---|---|
| Human help | `app --help`, `app <command> --help` | Usage, arguments, options, defaults, ENV, examples, exit codes |
| Machine help | `--help --json` | Stable JSON model for tools and agents |
| Full model | `app describe` | Every command, option, argument, default, ENV, config key, exit code |
| JSON Schema | `app schema` | Configuration schema (JSON Schema 2020-12) |
| Completions | `app completion bash\|zsh\|fish\|pwsh` | Static completion scripts; hidden `__complete` for dynamic candidates |
| Documentation | `app docs`, `app man` | Markdown and roff generated from the model |
| MCP | `app mcp --manifest`, `app mcp --stdio` | Tool manifest and JSON-RPC stdio server |

Reserved command names (`describe`, `schema`, `mcp`, `completion`, `docs`, `man`,
`__complete`) are protected by diagnostic `RHL025`.

---

## Binding, validation, and configuration

- **Syntax:** `--opt=value`, `--opt value`, `-o value`, `-o=value`, boolean flags,
  `--no-flag`, `--` terminator, repeated and comma-separated arrays, negative positional numbers.
- **Types:** primitives, enums, `Guid`, `DateTime(Offset)`, `TimeSpan`, `Uri`,
  `FileInfo`, `DirectoryInfo`, arrays, nullable values, and `IParsable<T>`.
- **Precedence:** CLI > environment > JSON configuration > property initializer.
- **Environment:** explicit `[Option(Env = "...")]` or the `APP_COMMAND_OPTION`
  convention; booleans accept `1/0`, `true/false`, `yes/no`, `on/off`.
- **Configuration:** `--config <PATH>` or `RHL_CONFIG`; dot-path keys
  (`deploy.environment`) with `[Option(Config = "...")]` overrides; opt-in implicit
  `appsettings.json` discovery via `[assembly: RunesHelaConfigFile]`.
- **Validation:** `[Required]`, `[Range]`, `[Regex]`, and `[Length]` execute inside
  generated code.
- **Errors:** stable `RHL_*` codes, did-you-mean hints, `nextSteps`, `docsUrl`, and the
  0/1/2/3/4/5/6/7/130/141 exit-code matrix. `[Secret]` values are redacted everywhere.

See [configuration and errors](docs/errors.md) for the full contract.

---

## Agent contract

- Non-TTY output never contains ANSI escapes; data goes to stdout and diagnostics to stderr.
- Destructive commands (`IsDestructive = true`) fail immediately without `--yes` when
  interactive input is unavailable; they never hang.
- `describe` and `--help --json` expose the complete model for generation and repair.
- `mcp --stdio` serves `initialize`, `tools/list`, and `tools/call` over JSON-RPC;
  tool results carry stdout/stderr, `isError`, and `structuredContent.exitCode`.
- Errors include actionable next steps, so an agent can retry without guessing.

---

## Performance and AOT

| Metric | Target | Current |
|---|---|---|
| Parsing + binding (5 options) | ≤ 1 µs, 0 B | budget enforced in CI; 0 B net allocation |
| Help / error allocation | 0 B / small | enforced by performance tests |
| Native AOT startup delta | ≤ 1 ms | 0.11 ms |
| Native AOT size delta | ≤ 100 KB target, 150 KB fail threshold | 432.7 KB (known limitation, V1.1) |

The AOT size gap is documented in the
[M8 benchmark report](docs/benchmarks/M8-benchmark-report.md). `System.Text.Json` was removed in
1.0.0; the remaining delta is native code and reachable framework members rather than
embedded strings. A minimal profile is planned for V1.1.

---

## Compatibility

- Semantic Versioning; the current release is `1.0.0`.
- Public API is tracked by `tests/RunesHela.Tests/PublicApiTests.approved.txt`; changes
  require an explicit baseline update.
- Generated JSON artifacts expose `schemaVersion`; additive fields are allowed in minor
  releases, removals require a major version.
- Exit codes and stdout/stderr separation are part of the compatibility contract.

See [CHANGELOG.md](CHANGELOG.md) and [release process](docs/release.md).

---

## Documentation

| Document | Contents |
|---|---|
| [Quickstart](docs/quickstart.md) | Install, declare commands, configuration, MCP, AOT |
| [Research and opportunity analysis](docs/00-research-and-opportunity.md) | Competitive landscape, market gap, positioning |
| [Product requirements](docs/01-product-requirements.md) | Goals, personas, requirements, non-functional targets |
| [Technical design](docs/02-technical-design.md) | Packages, compile-time model, generator pipeline, runtime, artifacts |
| [Implementation plan](docs/03-implementation-plan.md) | Milestones, task breakdown, dependencies |
| [Acceptance checklist](docs/04-acceptance-checklist.md) | Acceptance criteria and quality gates |
| [Architecture decisions](docs/05-architecture-decisions.md) | ADR-001 to ADR-016 |
| [Decision log](docs/06-decisions.md) | Closed naming, licensing, and scope decisions |
| [Migration guide](docs/migration.md) | From System.CommandLine, Spectre.Console.Cli, ConsoleAppFramework, hand-written `Main` |
| [Error codes](docs/errors.md) | Runtime `RHL_*` codes, `RHLxxx` diagnostics, exit codes |
| [Release process](docs/release.md) | Versioning, trusted publishing, rollback, supply chain |
| [Benchmarks](docs/benchmarks/M8-benchmark-report.md) | M0–M8 performance, AOT, packaging reports |
| [Chinese README](README.zh-CN.md) | 中文说明（副文档） |

---

## Build from source

```text
dotnet restore RunesHela.slnx
dotnet build RunesHela.slnx -c Release -m:1
dotnet test tests/RunesHela.Tests -c Release
dotnet test tests/RunesHela.Generator.Tests -c Release
dotnet test tests/RunesHela.Integration.Tests -c Release
dotnet test tests/RunesHela.Performance.Tests -c Release
```

Pack the NuGet package (runtime + analyzer + symbols):

```text
dotnet pack src/RunesHela/RunesHela.csproj -c Release -o artifacts/packages
```

---

## Contributing, security, and license

- [Contributing](CONTRIBUTING.md) — build, test, DCO sign-off (`git commit -s`), and PR checks.
- [Security policy](SECURITY.md) — private vulnerability reporting and disclosure timeline.
- [Apache-2.0](LICENSE) — see also [NOTICE](NOTICE).

RunesHela is not affiliated with any other CLI framework. The runtime depends only on
the BCL.
