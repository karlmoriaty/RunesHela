# M2 Benchmark Report (Binding and MVP)

> Project: **RunesHela**
> Phase: M2 (T-015 ~ T-026)
> Date: 2026-11-27
> Verdict: **M2 performance passes; AOT size exceeds the target.** Help is still 0 B; the error path allocates 304 B because of the structured envelope (below 512 B); startup delta 0.12 ms; AOT size delta 125.8 KB, above the 100 KB target but not enough to trigger the 150 KB failure threshold, handed to M3 for optimization.

---

## 1. Environment

Identical to the [M0 report](./M0-benchmark-report.md) (local Windows 11 / .NET 10.0.15 / BenchmarkDotNet 0.15.2 / ShortRun).

| Item | Value |
|---|---|
| OS | Windows 11 Home China, 10.0.26200 |
| CPU | 13th Gen Intel Core i7-13650HX |
| Runtime | .NET 10.0.15, X64 RyuJIT AVX2 |
| Benchmark framework | BenchmarkDotNet 0.15.2, ShortRun (3 iterations / 1 launch / 3 warmup) |

> ShortRun is used only for regression checks; the formal gate uses the default job on M3's fixed runner.

---

## 2. Framework Paths (10 commands)

```powershell
dotnet run --project benchmarks/RunesHela.Benchmarks -c Release -- --filter "*FrameworkBenchmarks*" --job short
```

| Method | M0 | M2 | Allocated (M2) | Budget | Verdict |
|---|---:|---:|---:|---:|---|
| Help (`--help`) | 5.98 ns | 11.66 ns | 0 B | ≤ 5 µs / ≤ 64 B | ✅ |
| Error (unknown command) | 7.77 ns | 76.94 ns | 304 B | ≤ 5 µs / ≤ 512 B | ✅ |
| Route (`cmd5`) | 5.67 ns | 14.48 ns | 24 B | ≤ 1 µs / 0 B (excluding user objects) | ✅ |

**Interpretation**:

- Help is still written to a `const string` with **0 B** allocated; the added time comes from root help now including exit codes and the command list.
- The error path was upgraded from "two lines of text" to `RunesHelaError` (code/hint/nextSteps/docsUrl/exitCode): string concatenation + array + object, measured at **304 B / 77 ns**, within the 512 B budget. `docsUrl` is now a compile-time constant, avoiding per-call concatenation (584 B before the optimization).
- The 24 B on the routing path is the `Cmd5` command instance, which the budget definition excludes from framework allocation (M3 reports "framework net allocation" separately).
- Parsing benchmarks are unchanged (the `CommandLineReader` hot path was not modified; only a `PrefixLength` field read was added).

---

## 3. Functional Verification (non-performance)

M2 adds 31 integration tests (real compilation + execution) covering:

| AC | Verification point | Test |
|---|---|---|
| AC-01 | Plain POCO binding, defaults, ENV, required | `BindsArgumentAndOptions`, `ReadsEnvironmentVariables`, `ReportsMissingRequiredOption` |
| AC-02 | 3-level nested command help and paths | `NestedCommandHelpUsesFullPath` |
| AC-09 | `--help --json` fixed schema | `JsonHelpExposesMachineReadableModel`, `RootJsonHelpListsCommands` |
| AC-16 | Generation-time diagnostics | 50 generator tests (duplicate names / required + default / unsupported types, etc.) |
| AC-17 | Secret redaction | `SecretValuesAreRedacted` |
| AC-18 | `NO_COLOR`/TTY/no ANSI | `AnsiEscapesAreOnlyWrittenWhenEnabled`, `HelpNeverContainsAnsiEscapes` |
| AC-19 | Chinese help at 80 columns | Snapshot `OptionsAndHelp.verified.g.cs` (CJK descriptions, no truncation/misalignment) |

Total tests: runtime 22 + generator 50 + integration 31 = **103**, all passing.

---

## 4. AOT (CI, linux-x64)

| Metric | EmptyAot (baseline) | AotCli (with binding/help/error) | Delta | Budget | Verdict |
|---|---:|---:|---:|---:|---|
| Binary size | 995,696 B | 1,124,552 B | **128,856 B (125.8 KB)** | target ≤ 100 KB; failure threshold > 150 KB | ⚠️ above target, failure threshold not triggered |
| Startup p50 | 1.79 ms | 1.91 ms | **0.12 ms** | ≤ 1 ms | ✅ |
| Startup p95 | 2.57 ms | 2.51 ms | -0.06 ms (within noise) | ≤ 1.5 ms | ✅ |

Notes:

- The M0 size delta was 99,880 B (97.5 KB); M2 introduces option/argument binding, human and JSON help, structured errors, and redaction, raising the delta by **29 KB**.
- Per design document §12.2, only > 150 KB counts as a failure; the current 125.8 KB sits in the "above target but acceptable" band.
- M3's T-030 (AOT matrix) and the size optimization will address this regression: first evaluating stripping the JSON error rendering, merging error strings, and including the help JSON constants on demand.
- AC-13 requires "build succeeds, 0 AOT/Trim warnings, artifact runs" — CI already passes (`Smoke test AOT binaries` runs `AotCli --help` and `AotCli greet Ada -t Hi`).

---

## 5. Comparison with Budgets

| Budget | Target | M2 measured | Verdict |
|---|---|---|---|
| NFR-01 Parsing (5 options) | ≤ 1 µs / 0 B | 60.65 ns / 0 B (unchanged) | ✅ |
| NFR-02 Help | ≤ 5 µs / ≤ 64 B | 11.66 ns / 0 B | ✅ |
| NFR-03 Error | ≤ 5 µs / ≤ 512 B | 76.94 ns / 304 B | ✅ |
| NFR-04 AOT startup overhead | ≤ 1 ms | 0.12 ms (p50) | ✅ |
| NFR-05 AOT size delta | ≤ 100 KB | 125.8 KB | ⚠️ optimize in M3 |

---

## 6. Reproduction Commands

```powershell
dotnet build RunesHela.slnx -c Release
dotnet test RunesHela.slnx -c Release --no-build

dotnet run --project samples/MinimalCli -c Release --no-build -- hello --help
dotnet run --project samples/MinimalCli -c Release --no-build -- hello --help --json
dotnet run --project samples/MinimalCli -c Release --no-build -- hello Ada -c 3

dotnet run --project benchmarks/RunesHela.Benchmarks -c Release -- --filter "*FrameworkBenchmarks*" --job short
```
