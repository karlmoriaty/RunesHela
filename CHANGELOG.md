# Changelog

All notable changes are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-11-28

### Added

- Class-first commands: `[Command]` on POCOs with `[Option]` and `[Argument]`
  properties; no `partial`, interfaces, or base classes required.
- Compile-time generator: parser, binder, routing, help, completion, man pages,
  Markdown docs, JSON Schema, and MCP manifest from one model.
- CLI syntax: `--opt=value`, `--opt value`, `-o value`, negatable flags, repeated
  and comma-separated arrays, `--` terminator, negative positional numbers.
- Type conversion for primitives, enums, `Guid`, `DateTime(Offset)`, `TimeSpan`,
  `Uri`, `FileInfo`, `DirectoryInfo`, arrays, nullable values, and `IParsable<T>`.
- Binding precedence: CLI > environment > JSON configuration > property default.
- Environment conventions: `APP_COMMAND_OPTION` names and `1/0`, `true/false`,
  `yes/no`, `on/off` booleans.
- JSON configuration through the built-in parser (no reflection): `--config`,
  `RHL_CONFIG`, dot paths, scalar arrays, and opt-in implicit discovery with
  `[assembly: RunesHelaConfigFile]`.
- Configuration schema: `app schema` and `schema --config`.
- Validation attributes: `[Required]`, `[Range]`, `[Regex]`, `[Length]`.
- Structured errors with stable `RHL_*` codes, did-you-mean hints, `nextSteps`,
  and the 0/1/2/3/4/5/6/7/130/141 exit-code matrix.
- Agent contract: non-TTY behavior, destructive confirmation guard, `describe`,
  `schema`, completion scripts for bash/zsh/fish/pwsh, and `__complete`.
- MCP: `mcp --manifest` and `mcp --stdio` JSON-RPC server with tool annotations,
  destructive confirmation, and structured tool results.
- Filters: `[Command(Filters = ...)]` with declaration-order execution and
  `CommandContext`.
- Native AOT, trimmed single-file, and ReadyToRun validation in CI.
- Performance gates: zero-allocation binding, parse latency budgets, and
  generator no-op budgets.

### Known limitations

- Native AOT size delta is above the release target; see
  `docs/benchmarks/M8-基准报告.md` for the current measurement and mitigation plan.
- MCP stdio processes requests sequentially and does not yet honor
  `notifications/cancelled`.
