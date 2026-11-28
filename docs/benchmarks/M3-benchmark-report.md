# M3 Benchmark Report (Performance, AOT, and Gates)

> Project: **RunesHela**
> Phase: M3 (T-027 ~ T-032, T-035)
> Date: 2026-11-27
> Verdict: **Hot path 0 B (excluding user command instances), binding ≤ 1.1 µs, generator no-op ≤ 50 ms, and AOT on three platforms plus Trim/SingleFile/R2R all pass.** AOT size is still 125.8 KB (no dedicated optimization this round; it must converge before M8).

---

## 1. Environment

Local micro-benchmarks: Windows 11 / .NET 10.0.15 / RyuJIT AVX2 / BenchmarkDotNet 0.15.2 / ShortRun.
CI: GitHub Actions `ubuntu-latest` (performance gate), `windows-latest`, `macos-14` (AOT matrix).

> ShortRun is used only for regression observation; gates are based on absolute budgets (including the +10% failure threshold).

---

## 2. Hot Path Changes This Round (T-027)

| Change | Purpose |
|---|---|
| Compare option-name length first, then do the `OrdinalIgnoreCase` comparison | Avoid a full string comparison for every option |
| Emit `switch (name[0])` for short options | Eliminate the if/else chain |
| Add a length pre-check to enum parsing | Reduce the number of string comparisons |
| Scan `WantsJson` once and pass it into the binder | Remove the repeated scan on every bind |
| Reuse the original `string` for positional arguments (`CommandLineToken.Source`) | Positional string arguments are no longer copied |
| Keep using `SpanText` for string options (reuse the whole span) | Separate values / inline whole spans stay zero-allocation |

---

## 3. Binding Benchmark (5 options + 1 positional argument)

```powershell
dotnet run --project benchmarks/RunesHela.Benchmarks -c Release -- --filter "*BindingBenchmarks*" --job short
```

| Method | Mean | Allocated | Notes |
|---|---:|---:|---|
| FiveOptions | 201.82 ns | 56 B | 56 B = the `Bind5Command` instance itself |
| NoOptions | 14.61 ns | 24 B | 24 B = the `Cmd0` instance itself |

**Interpretation**:

- Framework net allocation = 0 B (5 options + 1 positional argument), satisfying NFR-01's allocation definition (excluding user command instances).
- Binding costs **201.82 ns**, below the 1 µs budget (about 5× headroom) and below the 1.1 µs failure threshold.
- After positional arguments reuse the original string, allocation dropped from 88 B to 56 B (only the command instance remains).

---

## 4. Framework Paths (10 + 1 commands)

| Method | M2 | M3 | Allocated | Budget (failure threshold) |
|---|---:|---:|---:|---|
| Help (`--help`) | 11.66 ns | 12.62 ns | 0 B | ≤ 5.5 µs / ≤ 128 B |
| Error (unknown command) | 76.94 ns | 58.20 ns | 304 B | ≤ 5.5 µs / ≤ 1 KB |
| Route (`cmd5`) | 14.48 ns | 14.22 ns | 24 B | ≤ 1.1 µs / 0 B (excluding instances) |

---

## 5. Parsing Benchmark (5 options)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| HandWritten | 23.67 ns | 1.00 | 0 B |
| Reader | 60.61 ns | 2.56 | 0 B |

The reader is unchanged; after generated code inlines the constant comparisons, the binding path no longer relies on the reader to compare names.

---

## 6. Generator no-op (T-031 / AC-06)

```text
dotnet test tests/RunesHela.Generator.Tests --filter "FullyQualifiedName~NoOpRunStaysWithinBudget"
```

| Commands | no-op elapsed | Budget | Verdict |
|---:|---:|---:|---|
| 1 | 0.04 ms | ≤ 30 ms (failure > 50 ms) | ✅ |
| 10 | 0.02 ms | ≤ 30 ms | ✅ |
| 50 | 0.02 ms | ≤ 30 ms | ✅ |
| 200 | 0.04 ms | ≤ 30 ms | ✅ |

On a cache hit there is no full regeneration; even 200 commands stay far below 30 ms.

---

## 7. Performance Gates (T-028 / T-032 / AC-14)

Added `tests/RunesHela.Performance.Tests`, which asserts absolute budgets in Release
using `GC.GetAllocatedBytesForCurrentThread` and `Stopwatch`:

| Assertion | Threshold | Result |
|---|---:|---|
| Net allocation of 5-option binding | 0 B (compared with a same-shape command without options) | ✅ 0 B |
| Help allocation | ≤ 128 B | ✅ 0 B |
| Error allocation | ≤ 1024 B | ✅ 304 B |
| Binding + dispatch elapsed | ≤ 1100 ns | ✅ ~200 ns |
| Help elapsed | ≤ 5500 ns | ✅ ~13 ns |
| Error elapsed | ≤ 5500 ns | ✅ ~58 ns |
| Error exit code | 2 | ✅ |

Both the CI `build-test` and the standalone `perf` job run this suite; `perf` additionally runs the full micro-benchmarks and uploads the JSON artifacts.

---

## 8. AOT and Publish Matrix (T-030 / AC-13)

### 8.1 Local validation (Windows)

| Configuration | Command | Result |
|---|---|---|
| Trim + SingleFile (linux-x64) | `dotnet publish -p:PublishAot=false -p:PublishTrimmed=true -p:PublishSingleFile=true -p:SelfContained=true` | ✅ 0 warnings |
| ReadyToRun (linux-x64) | `dotnet publish -p:PublishAot=false -p:PublishReadyToRun=true -p:SelfContained=false` | ✅ 0 warnings |

The generator project adds `TreatAsLocalProperty`, so that the trimming/single-file/R2R global properties do not leak into the `netstandard2.0` analyzer.

### 8.2 CI matrix

CI run `46029396545` (2026-11-27) passed entirely:

| RID | Runner | Configuration | Smoke | Result |
|---|---|---|---|---|
| linux-x64 | ubuntu-latest | AOT | `--help`, `greet Ada -t Hi` | ✅ |
| win-x64 | windows-latest | AOT | same as above | ✅ |
| osx-arm64 | macos-latest | AOT | same as above | ✅ |
| linux-x64 | ubuntu-latest | Trim + SingleFile | `--help` | ✅ |
| linux-x64 | ubuntu-latest | ReadyToRun | `--help` | ✅ |

Artifact sizes: Trim+SingleFile **14,102,430 B**; ReadyToRun **78,192 B**.

> Full coverage of 6 RIDs (including the musl/arm64 cross toolchains) is deferred to the M8 release drill (T-055); this round covers three mainstream desktop/server RIDs.

### 8.3 AOT startup and size (linux-x64)

| Metric | EmptyAot | AotCli | Delta | Budget | Verdict |
|---|---:|---:|---:|---:|---|
| Startup p50 | 2.45 ms | 2.59 ms | **0.14 ms** | ≤ 1 ms | ✅ |
| Startup p95 | 3.48 ms | 3.41 ms | -0.07 ms | ≤ 1.5 ms | ✅ |
| Binary size | 995,696 B | 1,124,552 B | **128,856 B (125.8 KB)** | target ≤ 100 KB; failure threshold > 150 KB | ⚠️ above target, threshold not triggered |

Size is on par with M2; it must converge to ≤ 100 KB before the M8 release (candidate measures: strip the JSON error rendering on demand, merge error strings, trim help JSON constants).

---

## 9. DI Adaptation (T-035)

M2 already implemented constructor injection; M3 adds verification:

- No `IServiceProvider` while the command needs dependencies → exit code 1, `RHL_DI_SERVICES_MISSING`, no hang.
- Container provided but the service is missing → `RHL_DI_SERVICE_MISSING` + `nextSteps`.
- The combination of parameterized construction and option binding passes in integration tests.

---

## 10. Reproduction Commands

```powershell
dotnet build RunesHela.slnx -c Release
dotnet test RunesHela.slnx -c Release --no-build

dotnet test tests/RunesHela.Performance.Tests/RunesHela.Performance.Tests.csproj -c Release

dotnet run --project benchmarks/RunesHela.Benchmarks -c Release -- --filter "*" --job short

dotnet publish samples/AotCli/AotCli.csproj -c Release -r linux-x64 `
  -p:PublishAot=false -p:PublishTrimmed=true -p:PublishSingleFile=true -p:SelfContained=true `
  -o artifacts/trim/linux-x64
```
