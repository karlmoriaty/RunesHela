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
<a id="rhl_di_services_missing"></a>
<a id="rhl_di_service_missing"></a>
<a id="rhl_internal_not_implemented"></a>

| Code | Exit code | Meaning | Typical cause |
|---|---:|---|---|
| `RHL_USAGE_UNKNOWN_COMMAND` | 2 | Command name is not registered | Typo in a command or subcommand |
| `RHL_USAGE_UNKNOWN_OPTION` | 2 | Option is not declared on the command | Typo in an option name |
| `RHL_USAGE_MISSING_VALUE` | 2 | An option was supplied without a value | `--timeout` at the end of the line |
| `RHL_USAGE_INVALID_VALUE` | 2 | Value cannot be converted to the declared type | `--count abc` |
| `RHL_USAGE_MISSING_REQUIRED` | 2 | Required option or argument was not supplied | Missing `--environment` |
| `RHL_USAGE_UNEXPECTED_ARGUMENT` | 2 | Too many positional arguments | Extra token after the last argument |
| `RHL_USAGE_VALIDATION_FAILED` | 2 | Value failed `Range`, `Regex`, or `Length` | `--count 99` with `[Range(1, 10)]` |
| `RHL_DI_SERVICES_MISSING` | 1 | Command needs constructor dependencies but no provider was supplied | `RunesHelaApp.Run(args)` for a DI command |
| `RHL_DI_SERVICE_MISSING` | 1 | Constructor dependency could not be resolved | Service not registered |
| `RHL_INTERNAL_NOT_IMPLEMENTED` | 1 | Command declares no handler | `[Command]` class without `Run` |

Precedence is `CLI > environment variable > property initializer`. Missing required values produce exit code `2`; unresolved dependencies and missing handlers produce `1`.

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
| RHL999 | Warning | Generator failed unexpectedly |

Diagnostics are reported with a source location, a stable code, and actionable text. An error skips code generation for the affected command instead of producing cascading failures.
