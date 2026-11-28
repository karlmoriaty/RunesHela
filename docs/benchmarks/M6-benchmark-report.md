# M6 Benchmark Report (Configuration)

> Project: **RunesHela**
> Phase: M6 (T-047 ~ T-049)
> Date: 2026-11-28
> Verdict: **The configuration-related acceptance checks of AC-01 / AC-09 / AC-17 pass.** The precedence of CLI > ENV > JSON > default values, dotted-path keys, the `schema` artifact, exit code 3, and redaction are all covered by tests; the binding hot path stays at 0 B and within the existing latency budget. Implicit configuration-file discovery was changed to explicit opt-in via `[assembly: RunesHelaConfigFile]` in order to protect AC-04. **CI is fully green (run 46150131221); the AOT size delta rises from 291.6 KB to 560.7 KB, recorded as technical debt for M8.**

---

## 1. Scope and Status

| Task | Status | Notes |
|---|---|---|
| T-047 ENV binding refinement | ✅ | `APP_COMMAND_OPTION` prefix convention, lenient boolean parsing, CLI > ENV precedence |
| T-048 JSON configuration | ✅ | `JsonDocument`, dotted paths, arrays, `schema` (JSON Schema 2020-12) |
| T-049 Configuration errors/redaction | ✅ | Exit code 3, values never enter errors, Unix permission warning, `--config`/`RHL_CONFIG` |

---

## 2. Precedence and Sources

```text
CLI argument  >  environment variable  >  configuration file  >  property initializer default
```

| Source | Trigger | Remarks |
|---|---|---|
| CLI | `--count 2`, `--count=2`, `-c 2`, `--no-flag` | An explicit empty string also overrides other sources |
| ENV | Explicit `[Option(Env = "...")]`, otherwise `APP_COMMAND_OPTION` | Upper case, non-alphanumeric characters become `_`; booleans accept `1/0`, `true/false`, `yes/no`, `on/off` |
| JSON | `--config <PATH>` (before or after the command), `RHL_CONFIG`; once `[assembly: RunesHelaConfigFile]` is enabled, `appsettings.json` in cwd / `~/.config/<app>/` is appended | `JsonDocument`, no reflection |
| Default value | Property initializer | Keeps the original value when unset |

Configuration keys default to `command.path.option-name` (for example `deploy.environment`, `parent.child.leaf.value`) and can be overridden with `[Option(Config = "auth.token")]`; a duplicate key within the same command reports `RHL016`, an illegal key reports `RHL040`.

---

## 3. JSON Configuration

```json
{
  "deploy": {
    "environment": "live",
    "count": 3
  },
  "greet": {
    "tags": ["a", "b"],
    "loud": true
  }
}
```

- Nested objects expand into dotted paths; both scalar arrays and comma-separated `"a,b"` strings can be used for array options.
- Key matching is `OrdinalIgnoreCase`; unknown keys are ignored (for forward compatibility).
- The root must be an object; arrays may contain only scalars; a parse failure outputs no value at all.
- File changes are not watched (the CLI runs once).

---

## 4. Schema (T-048 / M6 exit condition)

`app schema` (compatible with `app schema --config`) outputs a draft 2020-12 schema:

```json
{
  "schemaVersion": "1.0",
  "kind": "config",
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "AotCli.config.schema.json",
  "type": "object",
  "additionalProperties": true,
  "properties": {
    "greet": {
      "type": "object",
      "properties": {
        "text": { "type": "string", "default": "Hello" }
      }
    }
  }
}
```

- Type mapping: `bool`→boolean, integer→integer, floating point/`decimal`→number, enum→string + `enum`, `Guid`/`Uri`→string + format, array→array + items.
- `[Secret]` does not output `default`.
- Integration test `SchemaMatchesDescribeConfigurationKeys` compares the `schema` leaf-key set with the `configKey` set of `describe`, and extracts `count`/`loud`/`mode` to validate types and default values.

---

## 5. Errors and Redaction (T-049 / AC-17)

| Scenario | Exit code | Error code |
|---|---:|---|
| Explicit file does not exist | 3 | `RHL_CONFIG_NOT_FOUND` |
| Invalid JSON / root is not an object / array contains an object | 3 | `RHL_CONFIG_INVALID` |
| Value cannot be converted | 3 | `RHL_CONFIG_INVALID_VALUE` |
| Required item missing (present in none of CLI/ENV/configuration) | 2 | `RHL_USAGE_MISSING_REQUIRED` |

- Error messages contain only the file path and the key name and **never contain values**; `[Secret]` CLI/ENV values continue to display `***`.
- On Unix, a warning is emitted when the configuration file is readable by group/other users and contains a `token`/`secret`/`password`/`key` key; `RHL_NO_CONFIG_WARNINGS=1` disables it.
- Test coverage: `supersecret` in invalid JSON and `not-a-number` for `greet.pin` never appear in stderr/JSON.

---

## 6. Performance and AOT Trade-off

- When implicit discovery is not enabled, `RunesHelaConfiguration.Load(null, ...)` returns a shared empty instance: zero allocation, zero file I/O, leaving the existing hot path unchanged.
- Files are read only when implicit discovery is enabled or `--config`/`RHL_CONFIG` is given (an expected configuration cost).
- The performance gates pass on re-run: the net allocation of five-option binding still matches the no-option baseline; parse+bind smoke test ≤ 10 µs; help/error budgets unchanged.
- `JsonDocument` is BCL, AOT-safe, and reflection-free; however, once `System.Text.Json` enters the artifact the size rises noticeably (see the table below).

| Metric | M5 | M6 | Budget | Verdict |
|---|---:|---:|---:|---|
| Startup delta p50 | 0.20 ms | **0.28 ms** | ≤ 1 ms | ✅ |
| Size delta | 291.6 KB | **560.7 KB (574,136 B)** | target ≤ 100 KB; failure threshold > 150 KB | ❌ exceeds threshold |

- Size increases by **269.1 KB** relative to M5, mainly from `System.Text.Json`/`JsonDocument` (configuration parsing).
- M8 convergence candidates: replace configuration parsing with a minimal JSON reader dedicated to generation time; or split configuration support into an optional package/optional generation switch, so applications that do not use configuration can trim `System.Text.Json`.

---

## 7. Tests

| Suite | Count | New in M6 |
|---|---:|---|
| Runtime | 22 | — |
| Generator | 63 | Configuration key derivation, illegal keys, duplicate keys, implicit discovery (+4) |
| Integration | 61 | Precedence matrix, cwd/RHL_CONFIG, error codes, redaction, nested keys, ENV prefix, schema consistency (+12) |
| Performance | 7 | — |
| **Total** | **153** | All passing |

---

## 8. Differences from the Design

1. **Implicit file discovery is opt-in**: design §9.2 looks in cwd/user directory by default; the implementation enables it via `[assembly: RunesHelaConfigFile]`. Reason: probing for a default file would generate I/O and allocation on every binding, directly violating AC-04 (0 B, ≤ 1 µs). Explicit `--config`/`RHL_CONFIG` is always available.
2. **`--help --verbose` does not expand configuration keys**: configuration keys are exposed through `--help --json`, `describe`, and the `configKey`/`Config` columns of `docs`, avoiding a second help constant for every command that would inflate the AOT size.
3. **The configuration file path is not written into help**: it is provided by `RunesHelaConfiguration.Path`; help stays a static constant.

---

## 9. Follow-up

- Converge the AOT size before the M8 release (including M6's `System.Text.Json` delta).
- M7 (optional) can reuse the configuration keys and danger-level annotations of `describe`/`schema` in `mcp --manifest`.
