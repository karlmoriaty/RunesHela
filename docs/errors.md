# Error codes

RunesHela uses two code families:

- `RHLxxx` — compile-time diagnostics reported by the source generator.
- `RHL_*` — runtime error codes written to stderr and exposed in the JSON error envelope.

`docsUrl` values emitted by the runtime point at the anchors in this file.

## Runtime error codes

<a id="rhl_usage_unknown_command"></a>
<a id="rhl_usage_unknown_option"></a>
<a id="rhl_usage_missing_value"></a>
<a id="rhl_usage_invalid_value"></a>
<a id="rhl_usage_missing_required"></a>
<a id="rhl_usage_unexpected_argument"></a>
<a id="rhl_usage_validation_failed"></a>
<a id="rhl_usage_confirmation_required"></a>
<a id="rhl_usage_aborted"></a>
<a id="rhl_usage_unsupported_format"></a>
<a id="rhl_usage_unknown_shell"></a>
<a id="rhl_config_not_found"></a>
<a id="rhl_config_invalid"></a>
<a id="rhl_config_invalid_value"></a>
<a id="rhl_di_services_missing"></a>
<a id="rhl_di_service_missing"></a>
<a id="rhl_internal_not_implemented"></a>
<a id="rhl_internal_unexpected"></a>

| Code | Exit code | Meaning | Typical cause |
|---|---:|---|---|
| `RHL_USAGE_UNKNOWN_COMMAND` | 2 | Command name is not registered | Typo in a command or subcommand |
| `RHL_USAGE_UNKNOWN_OPTION` | 2 | Option is not declared on the command | Typo in an option name |
| `RHL_USAGE_MISSING_VALUE` | 2 | An option was supplied without a value | `--timeout` at the end of the line |
| `RHL_USAGE_INVALID_VALUE` | 2 | Value cannot be converted to the declared type | `--count abc` |
| `RHL_USAGE_MISSING_REQUIRED` | 2 | Required option or argument was not supplied | Missing `--environment` |
| `RHL_USAGE_UNEXPECTED_ARGUMENT` | 2 | Too many positional arguments | Extra token after the last argument |
| `RHL_USAGE_VALIDATION_FAILED` | 2 | Value failed `Range`, `Regex`, or `Length` | `--count 99` with `[Range(1, 10)]` |
| `RHL_USAGE_CONFIRMATION_REQUIRED` | 2 | Destructive command was not confirmed | Non-interactive run without `--yes` |
| `RHL_USAGE_ABORTED` | 2 | Interactive confirmation was declined | Answering `n` at the prompt |
| `RHL_USAGE_UNSUPPORTED_FORMAT` | 2 | Unknown `--format` value | `--format yaml` |
| `RHL_USAGE_UNKNOWN_SHELL` | 2 | Unknown completion shell | `completion bas` |
| `RHL_CONFIG_NOT_FOUND` | 3 | Explicit configuration file does not exist | `--config missing.json` or `RHL_CONFIG` |
| `RHL_CONFIG_INVALID` | 3 | Configuration file cannot be read or parsed | Malformed JSON, non-object root, non-scalar array |
| `RHL_CONFIG_INVALID_VALUE` | 3 | Configuration value cannot be converted to the option type | `"count": "abc"` |
| `RHL_DI_SERVICES_MISSING` | 1 | Command needs constructor dependencies but no provider was supplied | `RunesHelaApp.Run(args)` for a DI command |
| `RHL_DI_SERVICE_MISSING` | 1 | Constructor dependency could not be resolved | Service not registered |
| `RHL_INTERNAL_NOT_IMPLEMENTED` | 1 | Command declares no handler | `[Command]` class without `Run` |
| `RHL_INTERNAL_UNEXPECTED` | 1 | Unhandled exception escaped the command | Handler threw |

Precedence is `CLI > environment variable > configuration file > property initializer`. Usage errors use exit code `2`; configuration errors use `3`; dependency and internal errors use `1`. Unknown names include a did-you-mean hint when a close candidate exists.

Configuration errors never include the offending value, and sensitive keys are redacted, so a malformed secret cannot leak through stderr or the JSON envelope. Implicit file discovery is enabled with `[assembly: RunesHelaConfigFile]`; without it, only `--config` and `RHL_CONFIG` load a file, which keeps the binding hot path free of file IO and allocations.

## Exit code matrix

| Code | Constant | Meaning |
|---:|---|---|
| 0 | `RunesHelaExitCode.Success` | Success |
| 1 | `RunesHelaExitCode.Failure` | General failure or unhandled exception |
| 2 | `RunesHelaExitCode.Usage` | Usage error, validation failure, missing confirmation |
| 3 | `RunesHelaExitCode.Configuration` | Configuration error |
| 4 | `RunesHelaExitCode.NotFound` | Resource not found |
| 5 | `RunesHelaExitCode.Permission` | Authentication or authorization failure |
| 6 | `RunesHelaExitCode.Conflict` | State conflict |
| 7 | `RunesHelaExitCode.Timeout` | Timeout |
| 130 | `RunesHelaExitCode.Interrupted` | Cancelled by SIGINT (`128 + 2`) |
| 141 | `RunesHelaExitCode.BrokenPipe` | Output pipe closed (`128 + SIGPIPE`) |

`RunesHelaApp` maps a cancelled token to `130` and any `IOException` escaping the entry point to `141`. Both are written without an error envelope because the diagnostic channel may also be closed.

## MCP server errors

`app mcp --stdio` speaks JSON-RPC 2.0. Protocol failures use standard codes; command failures stay inside `tools/call` results.

| Code | Meaning |
|---:|---|
| -32700 | Parse error |
| -32600 | Invalid Request |
| -32601 | Method not found |
| -32602 | Unknown tool or argument |

`tools/call` never converts CLI failures into protocol errors: the result carries `isError: true`, stdout/stderr as text content, and `structuredContent.exitCode` (for example `2` for usage or confirmation failures, `3` for configuration errors). This keeps Agent retry and error handling uniform with the CLI contract.

## Generator diagnostics

| Code | Severity | Meaning |
|---|---|---|
| RHL001 | Error | Duplicate command name under the same parent |
| RHL002 | Error | Duplicate option name, alias, or short name |
| RHL003 | Error | Member is required and also declares a default value |
| RHL004 | Error | Handler has parameters (not supported in the class-first MVP) |
| RHL005 | Info | Command has no description |
| RHL006 | Warning | Command has no handler method |
| RHL007 | Error | More than one handler method |
| RHL008 | Warning | Destructive command without a confirmation option |
| RHL009 | Error | Optional argument precedes a required argument |
| RHL010 | Error | Duplicate argument position |
| RHL011 | Error | Array argument is not last |
| RHL012 | Warning | Awaitable handler is not named with an `Async` suffix |
| RHL013 | Error | Invalid command name |
| RHL014 | Error | Option name is reserved by the runtime |
| RHL015 | Warning | Option looks sensitive but is not marked `[Secret]` |
| RHL016 | Error | Duplicate configuration key |
| RHL017 | Info | Command has no example |
| RHL018 | Info | Non-idempotent command without a dry-run option |
| RHL019 | Error | Circular command nesting |
| RHL020 | Error | No usable constructor for the command type |
| RHL021 | Error | Unsupported handler return type |
| RHL022 | Error | Duplicate alias on a command |
| RHL023 | Error | Nested command parent is not a command |
| RHL024 | Error | Generic command type |
| RHL025 | Error | Reserved command name |
| RHL026 | Error | Empty command name |
| RHL027 | Error | Invalid command alias |
| RHL028 | Error | Alias conflicts with a sibling command |
| RHL029 | Error | Unsupported bound value type |
| RHL030 | Error | Invalid option name |
| RHL031 | Error | Member cannot be bound (field, static, no accessible setter, or wrong accessibility) |
| RHL032 | Error | Default value is not a compile-time constant |
| RHL033 | Error | Invalid short option name |
| RHL034 | Error | Invalid argument position |
| RHL035 | Error | Validation attribute is not valid for the member type |
| RHL036 | Error | Member is both an option and an argument |
| RHL037 | Error | Invalid filter type (not a class, generic, or abstract) |
| RHL038 | Error | Filter does not implement `RunesHela.IRunesHelaFilter` |
| RHL039 | Error | Filter has no accessible parameterless constructor |
| RHL040 | Error | Invalid configuration key (use dot-separated segments of letters, digits, `-`, or `_`) |
| RHL999 | Warning | Generator failed unexpectedly |

Diagnostics are reported with a source location, a stable code, and actionable text. An error skips code generation for the affected command instead of producing cascading failures.
