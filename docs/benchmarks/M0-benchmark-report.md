# M0 Feasibility Validation Report (Benchmarks and AOT)

> Project: **RunesHela**
> Phase: M0 (T-001 ~ T-005)
> Date: 2026-11-27
> Verdict: **M0 passes.** Parsing/help/error/routing are all 0 B; AOT startup delta 0.00 ms, size delta 99,880 B (97.5 KB), both within budget.

---

## 1. Scope and Status

| Task | Status | Notes |
|---|---|---|
| T-001 Generator minimal loop | ✅ | `[Command]` class → generated entry point/routing/help/module initializer; the sample runs |
| T-002 Benchmark fixtures | ✅ | BenchmarkDotNet: parsing, help, error, routing (four groups) |
| T-003 AOT/Trim sample | ✅ | Local machine lacks the MSVC linker; AOT publish and smoke test completed by GitHub Actions `linux-x64` |
| T-004 Performance budget calibration | ✅ | Parsing/help/error/routing + AOT startup/size measured; generator no-op pending M1 |
| T-005 Single-pass parsing prototype | ✅ | `CommandLineReader` + 16 parsing tests |
| T-006 Competitor comparison (P1) | ⏳ | Not started; non-blocking for M0 |

---

## 2. Environment

### 2.1 Local micro-benchmarks

| Item | Value |
|---|---|
| OS | Windows 11 Home China, 10.0.26200 |
| CPU | 13th Gen Intel Core i7-13650HX, 14 cores / 20 threads, 2.6 GHz |
| SDK | .NET SDK 10.0.404 |
| Runtime | .NET 10.0.15, X64 RyuJIT AVX2 |
| Benchmark framework | BenchmarkDotNet 0.15.2, ShortRun (3 iterations / 1 launch / 3 warmup) |
| Build | Release, `Optimize=true` |

### 2.2 AOT benchmarks

| Item | Value |
|---|---|
| Runner | GitHub Actions `ubuntu-latest` |
| RID | `linux-x64` |
| Publish | `PublishAot=true`, `OptimizationPreference=Size`, `StripSymbols=true`, `InvariantGlobalization=true` |
| Baseline | `samples/EmptyAot` (minimal console output app, same AOT settings) |
| Sample | `samples/AotCli` (1 command + generated entry point/routing/help) |

> ShortRun is used for quick validation in M0; the numbers are not the final release baseline. Formal benchmarks are re-run with the default job on a fixed runner.

---

## 3. Parsing Benchmark (5 options)

Input:

```text
--name runes --count 42 --verbose --tag alpha --output out.txt
```

| Method | Mean | Error | StdDev | Ratio | Allocated |
|---|---:|---:|---:|---:|---:|
| HandWritten (hand-written baseline) | 24.42 ns | 1.345 ns | 0.074 ns | 1.00 | 0 B |
| Reader (`CommandLineReader`) | 60.65 ns | 3.791 ns | 0.208 ns | 2.48 | 0 B |

**Interpretation**:
- The prototype reader takes **60.65 ns / 0 B**, far below the 1 µs budget (about 16× headroom).
- It is **2.48×** slower than the hand-written baseline, mainly because of generic tokenization and state tracking; M3 targets convergence to 1.2~1.5× once generated code inlines the comparisons as constants.
- Allocation is **0 B**, consistent with the "zero-allocation hot path" direction.

---

## 4. Framework Path Benchmark (10 commands)

| Method | Mean | Error | StdDev | Allocated |
|---|---:|---:|---:|---:|
| Help (`--help`) | 5.98 ns | 0.9252 ns | 0.0507 ns | 0 B |
| Error (unknown command) | 7.77 ns | 0.6337 ns | 0.0347 ns | 0 B |
| Route (`cmd5`) | 5.67 ns | 4.1062 ns | 0.2251 ns | 0 B |

**Interpretation**:
- Help/error/routing all stay **under 10 ns**, and allocation is **0 B**.
- The empty command instance on the routing path is optimized away by the JIT (it does not escape); commands with real side effects incur exactly one command-instance allocation, which matches the budget definition (excluding user objects).

---

## 5. AOT Results (CI, linux-x64)

### 5.1 Local Windows AOT: environment blocker (does not affect CI)

```powershell
dotnet publish samples/AotCli/AotCli.csproj -c Release -r win-x64 -o artifacts/aot/win-x64
```

Error:

```text
Platform linker not found. Ensure you have all the required prerequisites documented at
https://aka.ms/nativeaot-prerequisites, in particular the Desktop Development for C++ workload
in Visual Studio. For ARM64 development also install C++ ARM64 build tools.
```

Cause: the **Desktop development with C++** workload of Visual Studio (MSVC v143 + Windows SDK) is not installed on this machine, so neither `vswhere` nor `cl.exe` exists.

Handling:
- The `aot` job in `.github/workflows/ci.yml` installs `clang`/`zlib1g-dev` on Ubuntu and performs the AOT publish, smoke test, and measurement.
- To reproduce locally: install the C++ workload of VS Build Tools, or use a Linux container.

### 5.2 AOT startup and size

| Metric | EmptyAot (baseline) | AotCli (RunesHela) | Delta |
|---|---:|---:|---:|
| Binary size | 995,696 B (972.4 KB) | 1,095,576 B (1,045.4 KB) | **99,880 B (97.5 KB)** |
| Startup p50 (30 runs) | 1.80 ms | 1.80 ms | **0.00 ms** |
| Startup p95 | 2.29 ms | 2.12 ms | -0.16 ms (within noise) |

**Interpretation**:
- The size delta of **97.5 KB ≤ 100 KB** satisfies NFR-05.
- The startup delta is within measurement noise (p50 0.00 ms), far below 1 ms, satisfying NFR-04.
- The absolute AOT size is **1.05 MB**, suitable for single-file distribution.

### 5.3 Single-file fallback (non-AOT, recorded for reference only)

| Metric | Value |
|---|---:|
| Single-file size | 73.56 MB |
| Cold start p50 (30 runs) | 75.9 ms |
| Cold start p95 | 140.0 ms |

For comparison: framework-dependent (JIT) p50 = 71.7 ms, p95 = 378.5 ms. The single-file build bundles the complete runtime and must not be used to judge the AOT budget.

---

## 6. Budget Comparison

| Budget | Target | Measured | Verdict |
|---|---|---|---|
| NFR-01 Parsing (5 options) | ≤ 1 µs / 0 B | 60.65 ns / 0 B | ✅ Pass (16× headroom) |
| NFR-02 Help | ≤ 5 µs / ≤ 64 B | 5.98 ns / 0 B | ✅ Pass |
| NFR-03 Error | ≤ 5 µs / ≤ 512 B | 7.77 ns / 0 B | ✅ Pass |
| NFR-04 AOT startup overhead | ≤ 1 ms | 0.00 ms (p50) | ✅ Pass |
| NFR-05 AOT size delta | ≤ 100 KB | 97.5 KB (99,880 B) | ✅ Pass |
| NFR-06 Generator no-op | ≤ 30 ms / 50 commands | Not measured (M1 generator benchmark) | ⏳ M1 |
| NFR-07 Runtime package size | ≤ 60 KB/TFM | `RunesHela.dll` 9.5 KB; generator 26.5 KB | ✅ Pass |
| NFR-08 Versus System.CommandLine | ≤ 1/10 elapsed time | Not measured (T-006) | ⏳ P1 |

---

## 7. Deliverables

| Deliverable | Path |
|---|---|
| Runtime library | `src/RunesHela/` |
| Incremental generator | `src/RunesHela.Generator/` |
| Minimal sample | `samples/MinimalCli/` |
| AOT sample | `samples/AotCli/` |
| AOT baseline | `samples/EmptyAot/` |
| Parsing tests (16) | `tests/RunesHela.Tests/` |
| Generator tests (6) | `tests/RunesHela.Generator.Tests/` |
| Benchmark project | `benchmarks/RunesHela.Benchmarks/` |
| CI (build/test/AOT) | `.github/workflows/ci.yml` |
| Raw benchmark results | `BenchmarkDotNet.Artifacts/results/` (local, not committed) |

---

## 8. M0 Conclusion and Next Steps

### Conclusion

1. **The compile-time loop holds**: `[Command]` → generated entry point/routing/help/module initializer → `RunesHelaApp.Run` runs directly.
2. **The zero-reflection/zero-allocation direction holds**: help, error, routing, and parsing all allocate 0 B.
3. **The AOT path holds**: Linux AOT startup delta 0.00 ms and size delta 97.5 KB both meet their targets.
4. **Needs optimization**: the generic reader is 2.48× slower than hand-written code; it converges in M3 once generated code inlines the comparisons.
5. **Still missing**: generator no-op benchmark (M1), System.CommandLine comparison (T-006).

### Next steps (M1)

1. Model IR, diagnostics, type system, incremental pipeline.
2. Help (human/JSON) snapshots and artifact consistency.
3. Generator no-op benchmark (NFR-06).
4. Keep the CI AOT/performance gates as mandatory items.

---

## 9. Reproduction Commands

```powershell
dotnet build samples/MinimalCli/MinimalCli.csproj -c Debug
dotnet run --project samples/MinimalCli -c Debug --no-build -- --help

dotnet test tests/RunesHela.Tests/RunesHela.Tests.csproj -c Debug
dotnet test tests/RunesHela.Generator.Tests/RunesHela.Generator.Tests.csproj -c Debug

dotnet run --project benchmarks/RunesHela.Benchmarks -c Release -- --filter "*" --job short

dotnet publish samples/AotCli/AotCli.csproj -c Release -r win-x64 -o artifacts/aot/win-x64
```

CI (Linux AOT):

```text
GitHub Actions → ci → aot job → Publish Native AOT → Measure AOT startup
```
