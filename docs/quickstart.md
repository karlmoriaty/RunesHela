# Quickstart

RunesHela turns command declarations into a complete CLI at compile time: parsing,
binding, help, completion, man pages, JSON Schema, and MCP tools all come from one
command model, with zero reflection and zero third-party runtime dependencies.

## 1. Create a project

```text
dotnet new console -n MyCli
cd MyCli
dotnet add package RunesHela
```

Until the package is published, use a local build:

```text
dotnet pack src/RunesHela/RunesHela.csproj -c Release -o artifacts/packages
dotnet add package RunesHela --source <repo>/artifacts/packages
```

## 2. Declare a command

```csharp
using RunesHela;

return RunesHelaApp.Run(args);

[Command("greet", Description = "Greets a person.")]
public sealed class GreetCommand
{
    /// <summary>Person to greet.</summary>
    [Argument(0, Description = "Person to greet.")]
    public string Name { get; init; } = "world";

    /// <summary>Repeat count.</summary>
    [Option(Short = 'c', Description = "Repeat count.", Env = "GREET_COUNT")]
    [Range(1, 5)]
    public int Count { get; init; } = 1;

    [Option(Description = "Uppercase the greeting.")]
    public bool Loud { get; init; }

    public void Run()
    {
        for (var i = 0; i < Count; i++)
        {
            var text = "Hello, " + Name + "!";
            Console.WriteLine(Loud ? text.ToUpperInvariant() : text);
        }
    }
}
```

No `partial`, interface, base class, or manual parser is required.

## 3. Run it

```text
dotnet run -- greet Ada --count 2 --loud
dotnet run -- greet --help
dotnet run -- greet --help --json
dotnet run -- describe
dotnet run -- schema
dotnet run -- mcp --manifest
```

Key conventions:

- Long options use kebab-case derived from the property name; override with
  `[Option("name")]`.
- Booleans are flags; `--no-loud` is generated automatically.
- Arrays accept repeated options and comma-separated values.
- `--` terminates option parsing.
- Exit codes: `0` success, `1` failure, `2` usage, `3` configuration.

## 4. Add configuration (optional)

```csharp
[assembly: RunesHelaConfigFile]

[Option(Description = "Target environment.", Config = "deploy.environment")]
public string Environment { get; init; } = "dev";
```

```json
{ "deploy": { "environment": "live" } }
```

Precedence is `CLI > environment > configuration file > property default`.
Use `--config <PATH>` or `RHL_CONFIG` for an explicit file; the attribute enables
implicit `appsettings.json` discovery in the current directory and
`~/.config/<app>/`.

## 5. Consume from an agent

```text
dotnet run -- mcp --stdio
```

The server speaks JSON-RPC 2.0 over stdin/stdout:

```json
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18"}}
{"jsonrpc":"2.0","id":2,"method":"tools/list"}
{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"greet","arguments":{"name":"Ada","count":2}}}
```

Destructive commands require `"yes": true`; the server never waits for
interactive input.

## 6. Publish Native AOT

```text
dotnet publish -c Release -r linux-x64 -p:PublishAot=true
```

The generated app has no runtime reflection and no third-party runtime
dependencies. Check `docs/benchmarks/M8-基准报告.md` for the current size
measurement before shipping size-sensitive tools.

## Next steps

- [Technical design](02-技术设计文档.md)
- [Error codes](errors.md)
- [Migration guide](migration.md)
- [Release process](release.md)
