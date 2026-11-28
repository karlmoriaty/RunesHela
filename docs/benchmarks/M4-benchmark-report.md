# M4 Benchmark Report (Agent Contract)

> Project: **RunesHela**
> Phase: M4 (T-034, T-036 ~ T-042)
> Date: 2026-11-28
> Verdict: **AC-07 / AC-08 / AC-10 pass.** Non-TTY color stripping and stream separation, structured errors (including did-you-mean), fast-fail destructive guards, the exit-code matrix, EPIPE→141, the full `describe` model, and the filter chain are all implemented with automated tests. **The AOT size delta is 279.6 KB, exceeding the 150 KB failure threshold, and is recorded as technical debt that must converge before the M8 release.**

---

## 1. Scope and Status

| Task | Status | Notes |
|---|---|---|
| T-034 Filter chain | ✅ | `[Command(Filters = ...)]`, static chain in declaration order, `CommandContext` |
| T-036 Non-TTY automatic mode | ✅ | Strip ANSI, `--json`/`--format`, leading scan of global options; Markdown default deferred |
| T-037 Structured error enhancements | ✅ | did-you-mean, hint/nextSteps/docsUrl, JSON envelope |
| T-038 Destructive guard | ✅ | `[Command(IsDestructive = true)]` + `--yes` + `--no-input` + TTY confirmation |
| T-039 Self-description | ✅ (describe) | `describe` outputs the whole tree as JSON; `schema` follows M6 configuration |
| T-040 Exit-code matrix | ✅ | 0/1/2/3/4/5/6/7/130/141 constants and documentation |
| T-041 Output channels / EPIPE | ✅ | Data on stdout, diagnostics on stderr; IOException → 141 |
| T-042 Agent contract suite | ✅ | 44 integration tests covering AC-07/08/10 |

---

## 2. Non-TTY and Output Modes (T-036)

- Help is always constant text and never contains ANSI; errors are colorized only when `IConsole.IsAnsiSupported` is true (`NO_COLOR`/`TERM=dumb`/non-TTY turn it off automatically).
- Global options can be parsed before the command: `--json`, `--plain`, `--no-color`, `--quiet`, `--verbose`, `--no-input`, `--yes`, `--format <text|plain|json>`; an unknown format → exit code 2.
- `--json` or `--format json` makes help and errors use JSON; data is always written to stdout and diagnostics to stderr.
- `describe` ignores the output mode and always outputs JSON (for Agent consumption).

**Deviation from the design**: the design specifies Markdown as the non-TTY default format; this round defaults to plain text (`--plain` semantics), and Markdown rendering ships together with the M5 documentation artifacts (T-045) to avoid drift between two renderers.

---

## 3. Structured Errors (T-037)

- All error objects contain `code`, `message`, `hint`, `nextSteps`, `docsUrl`, `command`, and `exitCode`.
- did-you-mean: for unknown commands/options, candidates are produced by edit distance (≤2, prefix matches take priority); the candidate set is determined at generation time, and the runtime only reads an array.
  Distance computation uses `stackalloc`, keeping heap allocation on the error path at 304 B.
- New error codes: `RHL_USAGE_CONFIRMATION_REQUIRED`, `RHL_USAGE_ABORTED`, `RHL_USAGE_UNSUPPORTED_FORMAT`, `RHL_INTERNAL_UNEXPECTED`.
- Unhandled exceptions are caught at the top level: the message, plus the full exception text under `--verbose`; exit code 1, with no stack trace printed to stdout.

---

## 4. Destructive Guard (T-038 / AC-08)

Behavior matrix (consistent with design §10.5):

| Environment | `--yes` | Behavior |
|---|---|---|
| Non-TTY / `--no-input` | No | **Fails immediately**: exit code 2, `nextSteps` suggests `--yes`, no input wait |
| TTY | No | stderr prompts `Continue? [y/N]`; `y/yes` executes, otherwise exit code 2 |
| Any | Yes | Executes directly |

Help and the JSON model append `-y, --yes` and `--no-input` options for destructive commands. Integration tests cover four paths: non-TTY, rejection, confirmation, and `--no-input`.

---

## 5. Exit Codes and EPIPE (T-040 / T-041)

| Code | Constant | Scenario |
|---:|---|---|
| 0 | `Success` | Success |
| 1 | `Failure` | General failure / unhandled exception |
| 2 | `Usage` | Usage error, validation failure, unconfirmed |
| 3~7 | `Configuration` / `NotFound` / `Permission` / `Conflict` / `Timeout` | Business semantics |
| 130 | `Interrupted` | `CancellationToken` cancellation |
| 141 | `BrokenPipe` | Top-level `IOException` (pipe closed) |

`RunesHelaApp` maps them uniformly at the top level; 141 writes no error message (the diagnostic channel may be closed as well). An integration test verifies 141 with a `TextWriter` that throws `IOException`.

---

## 6. Self-Description (T-039 / AC-10)

`app describe` outputs:

```json
{
  "schemaVersion": "1.0",
  "kind": "describe",
  "application": "App",
  "commands": [
    {
      "name": "parent",
      "path": "parent",
      "arguments": [],
      "options": [],
      "commands": [ { "name": "child", "...": "..." } ],
      "exitCodes": [ { "code": 0, "description": "Success" } ]
    }
  ]
}
```

- Option fields come from the same source as `--help --json` (name, short name, type, default value, ENV, secret, required).
- Integration tests assert that the option sets of `describe` and `help --json` are completely identical, and verify three-level nested paths.
- The full exit-code matrix is output (including 130/141).

---

## 7. Filter Chain (T-034)

```csharp
[Command("purge", IsDestructive = true, Filters = new[] { nameof(AuditFilter), nameof(TimingFilter) })]
```

- Generates a static chain: `filter0.InvokeAsync(context, ctx1 => filter1.InvokeAsync(ctx1, ctx2 => RunHandler(command)))`.
- `CommandContext` provides the command path, service provider, and cancellation token; filters enter and exit in declaration order.
- Generation-time validation: the name must resolve uniquely to an instantiable class, implement `IRunesHelaFilter`, and have a public/internal parameterless constructor (RHL037/038/039).
- Uses `nameof(...)` strings instead of `typeof(...)`: this avoids `System.Type` metadata pulling the Native AOT reflection stack (about 150 KB) into the artifact.
- Allocation: one instance plus one closure per filter; incurred only when filters are configured, while unconfigured paths keep zero extra allocation.

An integration test verifies the order: `outer:before → inner:before → handler → inner:after → outer:after`, with the exit code passed through from the handler (5).

---

## 8. Performance

Local ShortRun (Windows 11 / .NET 10.0.15):

| Method | M3 | M4 | Allocated | Budget (failure threshold) |
|---|---:|---:|---:|---|
| Help | 12.62 ns | 20.69 ns | 0 B | ≤ 5.5 µs / ≤ 128 B |
| Route | 14.22 ns | 31.46 ns | 24 B | ≤ 1.1 µs / 0 B (excluding instances) |
| Error | 58.20 ns | 1,550 ns | 304 B | ≤ 5.5 µs / ≤ 1 KB |
| FiveOptions | 201.82 ns | 208.14 ns | 56 B | ≤ 1.1 µs / 0 B (excluding instances) |

Interpretation:

- Root routing now scans global options and the leaf help, raising routing/help time by about 15~20 ns, still two orders of magnitude below budget.
- The error time increase comes from the did-you-mean candidate distance computation; allocation stays at 304 B (`stackalloc`, no new heap allocation), far below the 1 KB threshold.
- The binding path is unchanged (208 ns / 56 B = the command instance); the synchronous entry point gains an `IsCompletedSuccessfully` fast path, avoiding async state-machine overhead.

**Gate layering**:

- Allocation assertions run precisely in `tests/RunesHela.Performance.Tests` (Release), and CI fails directly.
- Latency assertions on shared runners serve only as smoke-test ceilings (binding 10 µs, help/error 20 µs), to avoid jitter caused by CPU contention.
- The absolute latency budgets (1.1 µs / 5.5 µs) are enforced by the BDN results of the `perf` job: the script parses `Statistics.Mean` and fails on exceedance.
  In the first M4 CI run, xUnit measured 1134.8 ns against the 1100 ns threshold and triggered a failure, showing that nanosecond-level assertions on shared runners are unreliable; the gate was therefore changed to a BDN mean gate plus xUnit allocation gates.

---

## 9. AOT and Publishing

The CI matrix follows M3: linux-x64 / win-x64 / osx-arm64 AOT + Trim/SingleFile + R2R, all passing.

| Metric | M3 | M4 | Budget | Verdict |
|---|---:|---:|---:|---|
| Startup delta p50 | 0.14 ms | **-0.06 ms** | ≤ 1 ms | ✅ |
| Size delta | 128,856 B | **286,264 B (279.6 KB)** | target ≤ 100 KB; failure threshold > 150 KB | ❌ exceeds threshold |

Size analysis:

- The size delta correlates with the M4 feature set, but after removing the async entry-point wrapper, splitting out interactive input, and switching filters to `nameof` string registration it **did not change**, indicating the delta comes from the combination of new code/metadata rather than any single item.
- Nodes such as `System.Console.TerminalFormatStrings` and `Reflection.Execution.NativeFormatEnumInfo` were confirmed in the Native AOT map; follow-up work must trim them item by item using the map.
- It must converge to ≤ 100 KB before the M8 release; candidate measures: strip `describe`/help JSON on demand, split the error renderer, trim unused Console capabilities with a linker descriptor, and review the `CommandAttribute` metadata.
- AC-13 (build succeeds, 0 warnings, artifact runs) still passes; missing the size budget is known technical debt.

---

## 10. Tests

| Suite | Count | Coverage |
|---|---:|---|
| Runtime | 22 | Parsing, error output, value parsing |
| Generator | 58 | Diagnostic codes, binding, snapshots (including AgentContract) |
| Integration | 44 | AC-01/02/07/08/09/10/17/18/19, filters, EPIPE, exit codes |
| Performance | 7 | Allocation and latency budgets |
| **Total** | **131** | All passing |

---

## 11. Outstanding / Follow-up

- Signal handling (SIGINT/SIGTERM registration and a second Ctrl+C) is not implemented; mapping `CancellationToken` to 130 is ready, and signal wiring follows with M6/M8.
- `schema` (configuration schema) lands with M6 (T-048).
- Non-TTY default Markdown rendering lands with M5 (T-045).
- Mapping filter exceptions is left to the filters themselves; the top-level fallback is 1 plus a structured error.
