# 04 Acceptance and Quality Checklist

> Project name: **RunesHela** (selected, 2026-11-26)
> Purpose: design review, development acceptance, release gate
> How to use: check each item and attach evidence (link/screenshot/report/command output); failed items must record the reason and the owner
> Related: [01 PRD](./01-product-requirements.md) · [02 Technical Design](./02-technical-design.md) · [03 Tasks](./03-implementation-plan.md) · [05 ADR](./05-architecture-decisions.md)

---

## 0. Checklist Item Legend

| Marker | Meaning |
|---|---|
| ☐ | Not checked |
| ✅ | Pass |
| ⚠️ | At risk/conditional pass |
| ❌ | Fail (blocker) |
| N/A | Not applicable (requires explanation) |

**Evidence types**: `test report` / `benchmark JSON` / `snapshot diff` / `CI link` / `screenshot` / `review record`.

---

## 1. Documentation Completeness

| # | Item | Acceptance standard | Evidence | Status |
|---|---|---|---|---|
| 1.1 | Research document complete | Doc 00 contains market data, competitor breakdown, requirements, gaps, roadmap, risks, sources | Document link | ☐ |
| 1.2 | PRD complete | 01 contains goals/non-goals/FR/NFR/constraints/assumptions/AC/open questions | Document link | ☐ |
| 1.3 | Technical design complete | 02 contains package structure/model/generator/runtime/types/contracts/artifacts/performance/AOT/diagnostics/testing/security | Document link | ☐ |
| 1.4 | Task breakdown complete | 03 gives every task a P/dependencies/deliverables/AC/tests/notes; DAG is acyclic | Document link | ☐ |
| 1.5 | ADR complete | 05 gives every key decision context/options/decision/impact/risks/related links | Document link | ☐ |
| 1.6 | Decision document complete | 06 gives every item options/recommendation/consequences of not deciding/reply template | Document link | ☐ |
| 1.7 | Cross-references valid | All inter-document links are clickable and numbering is consistent (FR/AC/RunesHela/ADR/D) | Link check | ☐ |
| 1.8 | Terminology consistent | Terms such as class-first/method-first/artifact/contract/budget are consistent throughout | Human review | ☐ |
| 1.9 | No code implementation | No implementation code/project files were created during the documentation phase | Repository check | ☐ |

---

## 2. Design Validation

### 2.1 Positioning and Differentiation

| # | Item | Acceptance standard | Status |
|---|---|---|---|
| 2.1.1 | Differentiation clear | Can state "how it differs from CAF/CliFx/SCL/Spectre" in one sentence | ☐ |
| 2.1.2 | Non-goals explicit | At least 10 non-goals, and the team agrees with them | ☐ |
| 2.1.3 | Competitor matrix accurate | Every ●/◐/○ in the matrix is justified (documentation/source code/measurement) | ☐ |
| 2.1.4 | Data traceable | All star/download counts are annotated with snapshot time and source | ☐ |
| 2.1.5 | Star expectations calibrated | Organization-level stars are not treated as a single-repository target; success metrics include downloads/adoption | ☐ |

### 2.2 Requirements Completeness

| # | Item | Acceptance standard | Status |
|---|---|---|---|
| 2.2.1 | Rigid requirements covered | R-01~R-12 in 00 §7 all map to FR/NFR | ☐ |
| 2.2.2 | Invisible requirements covered | All H/A/E class requirements have a design response or an explicit decision not to address them | ☐ |
| 2.2.3 | Agent contract complete | All 5 items present: non-TTY/structured errors/exit codes/guards/self-description | ☐ |
| 2.2.4 | Performance measurable | Every performance promise has a target value, a measurement method, and a failure threshold | ☐ |
| 2.2.5 | Security verifiable | Redaction/injection/supply chain/permissions all have checklist items | ☐ |

### 2.3 Architecture Soundness

| # | Item | Acceptance standard | Status |
|---|---|---|---|
| 2.3.1 | Compile-time/runtime boundary clear | The generator does not read the runtime; the runtime does not scan assemblies | ☐ |
| 2.3.2 | Zero reflection defensible | Prohibited APIs are listed item by item, with AOT tests proving it | ☐ |
| 2.3.3 | Minimal package dependencies | The `RunesHela` dependency graph contains only the BCL; optional packages are isolated | ☐ |
| 2.3.4 | Single source of truth | help/completion/schema/docs/MCP all come from the same model | ☐ |
| 2.3.5 | Failures diagnosable | Generator/runtime errors all have a code/a suggestion/documentation | ☐ |
| 2.3.6 | Extensible | Converters/filters/rendering/configuration sources/console all have interfaces | ☐ |
| 2.3.7 | Testable | Handlers are testable without starting a process; artifacts can be snapshotted | ☐ |

---

## 3. Functional Acceptance (Grouped by FR)

### 3.1 Command Definition

| # | Item | Acceptance standard | AC | Status |
|---|---|---|---|---|
| 3.1.1 | Pure POCO commands | Works without `partial`/interfaces/base classes | AC-01 | ☐ |
| 3.1.2 | Nested subcommands | 3-level nested routing is correct | AC-02 | ☐ |
| 3.1.3 | Naming conventions | Type/property → kebab-case; overridable | AC-01 | ☐ |
| 3.1.4 | Convention precedence | Attributes > documentation comments > conventions; conflicts produce diagnostics | AC-16 | ☐ |
| 3.1.5 | Hidden commands/options | Do not appear in help but are executable | — | ☐ |

### 3.2 Parsing and Binding

| # | Item | Acceptance standard | AC | Status |
|---|---|---|---|---|
| 3.2.1 | Long options | Both `--opt value` and `--opt=value` are correct | AC-02 | ☐ |
| 3.2.2 | Short options | `-o value` is correct; combined short options are explicitly unsupported | AC-02 | ☐ |
| 3.2.3 | `--` terminator | Everything after it is passed through/treated as positional | AC-02 | ☐ |
| 3.2.4 | Negative numbers and special characters | `-1`, empty strings, Unicode, quoted paths are correct | AC-02 | ☐ |
| 3.2.5 | Positional arguments | Order, optional arguments, `params` are correct | AC-02 | ☐ |
| 3.2.6 | Type matrix | Primitives/enums/Guid/time/Uri/FileInfo/nullable/arrays all pass | AC-02 | ☐ |
| 3.2.7 | Custom types | `IParsable<T>` is supported automatically; unsupported types produce RHL004 | AC-16 | ☐ |
| 3.2.8 | Defaults/required | Missing required → exit code 2; default values are correct | AC-16 | ☐ |
| 3.2.9 | ENV precedence | CLI > ENV > default | AC-01 | ☐ |
| 3.2.10 | Repeated options | Collected in order into arrays/lists | — | ☐ |

### 3.3 Help and Artifacts

> Note: If an artifact is not yet implemented in the current version (for example, V0.1 does not include completion/schema), mark the corresponding item **N/A** and complete it in the version that owns it; "not implemented" must not be passed off as "pass".

| # | Item | Acceptance standard | AC | Status |
|---|---|---|---|---|
| 3.3.1 | Human help | Usage/parameters/options/examples/exit codes all present | AC-01 | ☐ |
| 3.3.2 | Layered help | Top-level and subcommand help differ and are correct | AC-01 | ☐ |
| 3.3.3 | JSON help | Fixed schema; stable fields | AC-09 | ☐ |
| 3.3.4 | Markdown help | Tables/code blocks are correct | — | ☐ |
| 3.3.5 | Completion scripts | Loadable and functional in all four shells | AC-12 | ☐ |
| 3.3.6 | Man/Docs | `man -l` is readable; documentation is consistent with help | AC-11 | ☐ |
| 3.3.7 | Artifact consistency | help/completion/schema/docs sets are consistent | AC-11 | ☐ |

### 3.4 Runtime

| # | Item | Acceptance standard | AC | Status |
|---|---|---|---|---|
| 3.4.1 | Filter ordering | Before/after hook order is correct; exceptions are catchable | — | ☐ |
| 3.4.2 | Constructor injection | Works with/without a container; missing dependencies produce a clear error | AC-01 | ☐ |
| 3.4.3 | Cancellation | Ctrl+C → token cancellation; timeout kills the process; exit code 130 | AC-08 | ☐ |
| 3.4.4 | Version | `--version` outputs a semantic version | — | ☐ |
| 3.4.5 | Exception policy | Unknown exception → 1; stack traces only in debug mode | AC-08 | ☐ |

---

## 4. Performance Acceptance

| # | Item | Acceptance standard | Evidence | Status |
|---|---|---|---|---|
| 4.1 | Parsing hot path | ≤ 1 µs / 0 B (5 options, reference machine) | Benchmark JSON | ☐ |
| 4.2 | Help path | ≤ 5 µs / ≤ 64 B | Benchmark JSON | ☐ |
| 4.3 | Error path | ≤ 5 µs / ≤ 512 B | Benchmark JSON | ☐ |
| 4.4 | Cold start | AOT overhead ≤ 1 ms (p50, 30 runs) | hyperfine | ☐ |
| 4.5 | Binary size | AOT increment ≤ 100 KB | publish diff | ☐ |
| 4.6 | Generator no-op | ≤ 30 ms (50 commands) | Timing report | ☐ |
| 4.7 | Runtime package size | ≤ 60 KB/TFM | Package analysis | ☐ |
| 4.8 | Relative baseline | ≤ 1/10 of System.CommandLine 3.0 elapsed time for the same scenario | Comparison report | ☐ |
| 4.9 | No regression | PR compared to the baseline shows ≤ 10% degradation | CI link | ☐ |
| 4.10 | Method reproducible | Hardware/version/command/raw data all present | Report link | ☐ |

**Hard requirements for performance checks**:
- Reporting only the average is not allowed; p50/p95 or BenchmarkDotNet statistics are required.
- "Different test environments" must not be left unstated; CPU/OS/.NET/configuration must be recorded.
- Disabling allocation measurement in order to "pass" is not allowed.

---

## 5. AOT / Trim / Release

| # | Item | Acceptance standard | AC | Status |
|---|---|---|---|---|
| 5.1 | AOT build | All 6 RIDs succeed | AC-13 | ☐ |
| 5.2 | AOT warnings | 0 IL2026/IL3050 (the sample enables warnings as errors) | AC-13 | ☐ |
| 5.3 | Trim | Post-trim smoke test passes | AC-13 | ☐ |
| 5.4 | Single-file | Single file is runnable | AC-13 | ☐ |
| 5.5 | ReadyToRun | R2R is runnable | AC-13 | ☐ |
| 5.6 | Startup consistency | AOT and JIT produce identical help/error output | AC-11 | ☐ |
| 5.7 | Size budget | Within threshold | AC-13 | ☐ |
| 5.8 | Containers | glibc/musl images are runnable | AC-13 | ☐ |

---

## 6. Agent Contract Acceptance

| # | Item | Acceptance standard | AC | Status |
|---|---|---|---|---|
| 6.1 | Non-TTY color removal | Piped output contains no ANSI | AC-07 | ☐ |
| 6.2 | stdout/stderr | Data only on stdout; diagnostics only on stderr | AC-07 | ☐ |
| 6.3 | Non-TTY format | Markdown by default; `--json` overrides | AC-07 | ☐ |
| 6.4 | Structured errors | `code/message/hint/nextSteps/docsUrl/exitCode` all present | AC-08 | ☐ |
| 6.5 | Did-you-mean | Unknown options yield candidates | AC-08 | ☐ |
| 6.6 | Destructive guard | Non-TTY without `--yes` → fails within 1 second, does not hang | AC-08 | ☐ |
| 6.7 | `--no-input` | Forbids all input; combined semantics with `--yes` are correct | AC-08 | ☐ |
| 6.8 | Exit codes | 0/1/2/3/4/5/6/7/130 are semantically correct | AC-08 | ☐ |
| 6.9 | `describe` | Outputs the complete model; consistent with help | AC-10 | ☐ |
| 6.10 | `--help --json` | Schema is stable; parseable | AC-09 | ☐ |
| 6.11 | Broken pipe | `head` closing early does not crash or print a stack trace | AC-07 | ☐ |
| 6.12 | Large results | Can write to a file and return the path (if implemented) | — | ☐ |

---

## 7. Diagnostics and IDE

| # | Item | Acceptance standard | AC | Status |
|---|---|---|---|---|
| 7.1 | Diagnostic codes complete | Every error/warning has a unique code | AC-16 | ☐ |
| 7.2 | Diagnostic messages actionable | Contain the problem/the fix/a documentation link | AC-16 | ☐ |
| 7.3 | A sample per code | Covered by tests | AC-16 | ☐ |
| 7.4 | Generator degradation | Internal exception → RHL999, does not break the build | AC-16 | ☐ |
| 7.5 | Generated code readable | Naming/formatting/comments are readable | — | ☐ |
| 7.6 | No analyzer false positives | Generated code does not trigger user rules | — | ☐ |
| 7.7 | TreatWarningsAsErrors | Sample project has zero warnings | AC-13 | ☐ |
| 7.8 | Generator performance | Meets target (see 4.6) | AC-06 | ☐ |

---

## 8. Security Acceptance

| # | Item | Acceptance standard | AC | Status |
|---|---|---|---|---|
| 8.1 | Secret redaction | Help/logs/errors/describe all show `***` | AC-17 | ☐ |
| 8.2 | Logs exclude argv | Raw arguments are not logged by default | AC-17 | ☐ |
| 8.3 | Completion escaping | No injection; input is never executed | — | ☐ |
| 8.4 | Configuration permissions | World-readable on Unix + secrets → warning | — | ☐ |
| 8.5 | Dependency audit | 0 third-party runtime dependencies | — | ☐ |
| 8.6 | Locked restore | lock file takes effect | — | ☐ |
| 8.7 | SBOM | Shipped with the release | — | ☐ |
| 8.8 | Package signing | NuGet signing/trusted publishing | — | ☐ |
| 8.9 | Repository governance | 2FA/branch protection/CODEOWNERS/SECURITY | — | ☐ |
| 8.10 | Vulnerability process | Private reporting + response time limits | — | ☐ |
| 8.11 | License compliance | Repository contains LICENSE/NOTICE; packages declare Apache-2.0; DCO | — | ☐ |
| 8.12 | Third-party licensing | Zero runtime dependencies; optional package dependency licenses are compatible | — | ☐ |

---

## 9. Testing and Quality

| # | Item | Acceptance standard | Status |
|---|---|---|---|
| 9.1 | Unit coverage | Core parsing/binding ≥ 95%, overall ≥ 85% | ☐ |
| 9.2 | Snapshots | help/help-json/schema/completion/man all present | ☐ |
| 9.3 | Integration | Process-level stdout/stderr/exit code | ☐ |
| 9.4 | TTY matrix | TTY/non-TTY/pseudo-terminal | ☐ |
| 9.5 | Property/fuzzing | Boundary inputs do not crash | ☐ |
| 9.6 | Consumer side | Sample project referencing the package compiles | ☐ |
| 9.7 | Documentation examples | Compiled and run in CI | ☐ |
| 9.8 | Target frameworks | net10.0 all pass | ☐ |
| 9.9 | Multi-platform | Win/Linux/macOS all pass | ☐ |
| 9.10 | Failures reproducible | Failing cases have a minimal reproduction | ☐ |

---

## 10. Documentation and Developer Experience

| # | Item | Acceptance standard | AC | Status |
|---|---|---|---|---|
| 10.1 | First screen | What it is/why/install/30-second example | AC-20 | ☐ |
| 10.2 | Quickstart | Completable in 5 minutes (≥4/5 people) | AC-03 | ☐ |
| 10.3 | Conceptual documentation | Class-first/artifacts/contracts/lifecycle | — | ☐ |
| 10.4 | Reference documentation | Every public API is documented | — | ☐ |
| 10.5 | Migration guide | SCL/CliFx/Spectre/CAF comparison | — | ☐ |
| 10.6 | Troubleshooting | Common error codes and resolutions | — | ☐ |
| 10.7 | Sample library | 5+ runnable samples | — | ☐ |
| 10.8 | Release notes | CHANGELOG (Keep a Changelog) | — | ☐ |
| 10.9 | Accessibility | `--plain`/NO_COLOR/80 columns | AC-18/19 | ☐ |

---

## 11. Release Checklist (Every Release)

### 11.1 Code and Quality

- ☐ All P0 tasks complete, all ACs pass
- ☐ CI is green (multiple TFMs/multiple OSes)
- ☐ Performance gate passes (no >10% regression)
- ☐ AOT/Trim matrix passes (0 warnings)
- ☐ Snapshot changes have been reviewed
- ☐ ApiCompat passes (no breaking changes)
- ☐ License compliance (LICENSE/NOTICE/Apache-2.0/DCO)
- ☐ Security scan passes (dependencies/secrets/SAST)
- ☐ Coverage meets target

### 11.2 Packages and Documentation

- ☐ Version number conforms to SemVer
- ☐ CHANGELOG complete
- ☐ NuGet package contents correct (no extraneous files)
- ☐ Symbol package/SourceLink available
- ☐ README/documentation site updated
- ☐ Migration/deprecation notes (if any)
- ☐ Sample projects restore and run

### 11.3 Release and Rollback

- ☐ Pre-release (alpha/beta) validation ≥ 1 week
- ☐ Release rehearsal (dry-run)
- ☐ Rollback plan (version rollback/package deletion policy)
- ☐ Announcement channels (GitHub/social media/email)
- ☐ 48h post-release monitoring of issues/downloads/crashes

---

## 12. Final Verification (Go/No-Go)

| # | Question | Verdict | Owner |
|---|---|---|---|
| 12.1 | Does it fulfill the "elegant" promise (pure POCO + 5 minutes)? | Yes/No | ☐ |
| 12.2 | Does it fulfill the "performance" promise (0 B / ≤1 µs / AOT)? | Yes/No | ☐ |
| 12.3 | Does it fulfill the "Agent contract" (non-TTY/errors/guards/self-description)? | Yes/No | ☐ |
| 12.4 | Does it fulfill "artifact consistency" (help/completion/schema/docs)? | Yes/No | ☐ |
| 12.5 | Are there any unmitigated high risks? | Yes/No | ☐ |
| 12.6 | Is the documentation sufficient for a newcomer to get started independently? | Yes/No | ☐ |
| 12.7 | Is it ready for long-term maintenance (governance/funding/people)? | Yes/No | ☐ |
| 12.8 | Do you agree to proceed to development/release? | Agree/Disagree | ☐ |

---

## 13. Review Record Template

```text
Review date:
Review version:
Participants:

Passed items:
Risk items:
Blocking items:
Decision: Go / Conditional Go / No-Go
Follow-up actions (owner/deadline):
```
