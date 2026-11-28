# 01 Product Requirements Document (PRD)

> Project name: **RunesHela** (selected, 2026-11-26)
> Version: v0.1 (pending decision)
> Status: pending review
> Related: [00 Research](./00-research-and-opportunity.md) · [02 Technical Design](./02-technical-design.md) · [03 Implementation Plan](./03-implementation-plan.md) · [04 Acceptance Checklist](./04-acceptance-checklist.md) · [06 Decision Document](./06-decisions.md)

---

## 1. Overview

### 1.1 One-sentence summary

**RunesHela is a compile-time CLI framework for .NET: developers only declare commands (classes or methods), and the compiler generates the parser, binder, help, completion, docs, schema and MCP tools; zero reflection at runtime, zero third-party dependencies, zero allocations on the hot path, and friendly by default to all three kinds of consumers — humans, scripts and AI agents.**

### 1.2 Purpose (why we build it)

- **Eliminate boilerplate**: command definition, parsing, binding, help, validation, completion and docs are each maintained separately, which is the largest source of duplicated work and drift in CLI development.
- **Eliminate runtime uncertainty**: reflection, dynamic code generation and runtime formatting make startup slow, AOT difficult and behavior unpredictable.
- **Complete the agent contract**: agents have become important CLI callers, yet existing frameworks provide no non-interactive, structured, self-healing output contract.
- **Establish verifiable performance trust**: turn performance from a "slogan" into CI budgets and public benchmarks.

### 1.3 Target users

| Priority | User | Key scenario |
|---|---|---|
| Primary | .NET platform/infrastructure engineers | Internal tools, deployment CLIs, data pipelines (10–100 commands) |
| Primary | OSS tool authors | Single-file distribution, cross-platform, startup-sensitive |
| Secondary | Enterprise .NET teams | Compliance, testability, maintainability, supply chain security |
| Secondary | AI/agent developers | Exposing CLIs to agents/MCP |
| Observational | Learners/hobbyists | 5-minute onboarding, runnable samples |

### 1.4 Design philosophy

1. **Compile-time first**: anything that can be determined at compile time must never be deferred to runtime.
2. **Convention over configuration**: naming, types and doc comments alone should be enough to derive CLI behavior.
3. **Single source of truth**: all artifacts are generated from the same model; hand-written drift is forbidden.
4. **Contract first**: output formats, error structures and exit codes are API, not implementation details.
5. **Verifiable**: every promise has a test, a benchmark or a gate.
6. **Composable**: do not hold rendering, DI, logging or serialization hostage.

---

## 2. Goals

### 2.1 Product goals

| ID | Goal | Success criteria |
|---|---|---|
| G-01 | **Elegance**: defining one class yields a complete CLI | Quickstart ≤ 20 lines of effective code; no `partial`/interface/base class |
| G-02 | **Performance**: overhead close to hand-written parsing | 0 B on the hot path; parse overhead ≤ 1 µs (5 options); additional AOT startup ≤ 1 ms |
| G-03 | **Contract**: usable by all three consumer types — humans/scripts/agents | Automatic degradation when non-TTY; structured errors; stable exit codes; `schema` self-description |
| G-04 | **Consistency**: artifacts do not drift | help/completion/man/schema/docs/MCP all come from the same model |
| G-05 | **Trust**: performance and quality are reproducible | CI benchmark gate; zero AOT warnings; public benchmark reports |
| G-06 | **Maintainability**: long-term evolution does not break users | SemVer + API compatibility gate + stable diagnostic codes |

### 2.2 Business/community goals (non-commitments)

| ID | Goal | Description |
|---|---|---|
| B-01 | 1,500 stars / 200 thousand downloads within 12 months | Assumption based on the scale of comparable projects, recalibrated after M0 |
| B-02 | Enter the dependency graph of 50 real projects | Based on the GitHub dependency graph/public cases |
| B-03 | Establish the "compile-time CLI" category awareness | Through benchmarks, docs, talks and comparison articles |
| B-04 | Sustainable maintenance | Sponsorship/consulting/co-maintainers, not relying on a single author |

---

## 3. Non-goals (explicitly out of scope)

| ID | Non-goal | Rationale |
|---|---|---|
| NG-01 | Rich terminal TUI/table/chart rendering engine | Spectre.Console already occupies this space; compose through pluggable output |
| NG-02 | General-purpose dependency injection container | Generated constructor injection + user containers; do not rebuild MEDI |
| NG-03 | Serialization framework (JSON/MessagePack, etc.) | Use `System.Text.Json` source generation or bring your own |
| NG-04 | Interactive prompt (wizard/multi-select/password) engine | Evaluate after V1; first guarantee reliable non-interactive behavior |
| NG-05 | GUI/Web/desktop | Focus on CLI |
| NG-06 | .NET Framework / netstandard2.0 runtime | Zero overhead depends on modern APIs; support net10.0 only |
| NG-07 | Unity runtime | Unrelated to CLI scenarios; avoid scope creep |
| NG-08 | Plugin marketplace/dynamically loaded commands | Breaks AOT and predictability |
| NG-09 | Built-in telemetry/cloud services | Privacy and trust first; zero telemetry by default |
| NG-10 | Compatibility with every POSIX/GNU syntax detail | Explicit trade-offs; prioritize single-pass parsing and predictability |

---

## 4. Background and context

### 4.1 Market openings (excerpted from document 00)

- No solution makes class-first + zero reflection + AOT hold true at the same time.
- No framework treats help/completion/man/schema/docs/MCP as artifacts of one and the same model.
- No framework provides an agent-native contract (non-TTY, structured errors, `--yes` guard, self-description).
- Performance lacks CI gates and public reproducible evidence.

### 4.2 Technology stack

| Layer | Choice | Description |
|---|---|---|
| Target runtime | .NET 10 (LTS) | Single TFM net10.0 (D-02=C), supported until 2028-11 |
| Language | C# 14 | Extension members, `field`, collection expressions, etc. |
| Generator | Roslyn Incremental Generator (netstandard2.0) | Incremental, cacheable, snapshot-tested |
| Runtime dependencies | 0 third-party packages | BCL only |
| Testing | xUnit + Verify (snapshots) + BenchmarkDotNet + SharpFuzz (optional) | See document 02 |
| CI | GitHub Actions (Win/Linux/macOS × .NET 8/9/10) | AOT/Trim/single-file matrix |

### 4.3 Design constraints (summary)

- No reflection/dynamic code generation at runtime (except in user code).
- The generator must not read `Compilation` in a way that invalidates caching; use `ForAttributeWithMetadataName` and an equivalent model.
- Every public behavior must have a diagnostic code or documentation.
- Builds must be deterministic and reproducible.

---

## 5. Functional requirements (FR)

> Priority: **P0** = required for MVP; **P1** = V0.2–V1.0; **P2** = after V1.x.
> "Version" indicates the planned assignment; decision document D-05 is authoritative.

### 5.1 Command definition and discovery

| ID | Requirement | Priority | Version | Acceptance points |
|---|---|---|---|---|
| FR-01 | **Class-first**: a plain class + `[Command]` defines a command; no `partial`, no interface implementation, no base class inheritance required | P0 | V0.1 | Pure POCOs can be recognized and have bindings generated |
| FR-02 | **Method as handler**: conventional method names (`Run`/`RunAsync`/`Execute`/`ExecuteAsync`) or explicit annotation | P0 | V0.1 | Sync/async/return values/`CancellationToken` all supported |
| FR-03 | **Nested subcommands**: nested classes automatically form `parent child` paths; arbitrary depth | P0 | V0.1 | A 3-level nested example passes |
| FR-04 | **Naming conventions**: type name/property name → kebab-case command/option names; explicit override allowed | P0 | V0.1 | Both convention and override have snapshots |
| FR-05 | **Method-first micro-commands**: `RunesHela.Run(args, (int x, int y) => ...)` — one line becomes a CLI | P1 | V0.2 | Call-site analysis generates the parser |
| FR-06 | **Command registration**: `RunesHela.Create().Add<T>()` / `[RegisterCommands]` bulk registration | P1 | V0.2 | Multiple commands combined with filters |
| FR-07 | **Hidden commands/hidden options**: for internal/experimental capabilities | P1 | V0.2 | Not shown in help but still executable |

### 5.2 Parsing and binding

| ID | Requirement | Priority | Version | Acceptance points |
|---|---|---|---|---|
| FR-08 | **Options**: long names, short names, aliases, `--opt=value`, `--opt value`, boolean flags | P0 | V0.1 | Full coverage of the syntax matrix |
| FR-09 | **Positional arguments**: bound in order, optional/required, `params` remaining arguments | P0 | V0.1 | Correct when mixed with options |
| FR-10 | **`--` terminator**: everything after it is treated as positional arguments/pass-through | P0 | V0.1 | Mixed with negative numbers and paths |
| FR-11 | **Type binding**: primitives, `string`, `char`, enums, `Guid`, `DateTime`/`DateTimeOffset`, `TimeSpan`, `Uri`, `FileInfo`/`DirectoryInfo`, nullable, arrays, `IReadOnlyList<T>` | P0 | V0.1 | Type matrix tests |
| FR-12 | **Custom types**: implementing `IParsable<T>` / `ISpanParsable<T>` is supported automatically; or register a converter | P0 | V0.1 | Custom type example |
| FR-13 | **Default values and required**: non-nullable reference type/no default → required; has default → optional; `required` semantics | P0 | V0.1 | Reports a usage error when missing |
| FR-14 | **Environment variable binding**: `[Option(Env = "APP_ENV")]`, lower precedence than the CLI | P0 | V0.1 | Precedence tests |
| FR-15 | **Configuration file binding**: JSON (V1), key path mapping; precedence CLI > ENV > file > default | P1 | V1.0 | Precedence + redaction + schema |
| FR-16 | **Response files**: `@args.txt` expansion (can be disabled) | P2 | V1.x | Recursion/escaping/security limits |
| FR-17 | **Repeated options**: can be repeatedly collected into arrays/lists | P1 | V0.2 | Order preserved |
| FR-18 | **Negated flags**: `--no-*` semantics such as `--no-color` | P1 | V0.2 | Consistent with boolean defaults |
| FR-19 | **Case policy**: case-insensitive by default, lowercase preferred on match; strict mode configurable | P0 | V0.1 | Diagnostic on conflicts |

### 5.3 Validation and diagnostics

| ID | Requirement | Priority | Version | Acceptance points |
|---|---|---|---|---|
| FR-20 | **Declarative validation**: subset of `[Required]`, `[Range]`, `[RegularExpression]`, `[Length]`, with checks generated at compile time | P0 | V0.1 | Zero reflection, customizable error messages |
| FR-21 | **Custom validation**: implement an interface or static method, discovered at generation time | P1 | V0.2 | Composable, localizable |
| FR-22 | **Compile-time diagnostics**: duplicate names, required+default conflicts, unsupported types, wrong positional argument order, etc. | P0 | V0.1 | Each error type has a diagnostic code and documentation |
| FR-23 | **Diagnostics as documentation**: error codes are searchable and include fix suggestions | P0 | V0.1 | Documentation page exists |
| FR-24 | **Generator degradation**: internal exceptions must not cause a silent build failure | P0 | V0.1 | RHL999 + readable message |

### 5.4 Help and artifacts

| ID | Requirement | Priority | Version | Acceptance points |
|---|---|---|---|---|
| FR-25 | **Human help**: Usage/Commands/Options/Arguments/Examples/Exit codes/Env/Config | P0 | V0.1 | 80 columns, CJK alignment, snapshots |
| FR-26 | **Layered help**: concise at the top level; detailed for subcommands; `--help --verbose` expands defaults/constraints/environment variables | P0 | V0.1 | Progressive disclosure |
| FR-27 | **Machine-readable help**: `--help --json` outputs stable JSON | P0 | V0.1 | Fixed, versioned schema |
| FR-28 | **Markdown help**: `--help --markdown` for agents/docs | P1 | V0.2 | Tables/code blocks correct |
| FR-29 | **Shell completion**: bash/zsh/fish/pwsh static scripts + dynamic completion (files/commands) | P1 | V0.2 | Smoke tests for all four shells |
| FR-30 | **Man page**: the `man` command outputs roff | P2 | V1.x | Readable via `man -l` |
| FR-31 | **Documentation generation**: `docs --markdown` outputs site-style command documentation | P2 | V1.x | Same source as help |
| FR-32 | **JSON Schema**: `schema` outputs the JSON Schema of the command model and configuration file | P1 | V1.0 | Consumable by editors/agents |
| FR-33 | **MCP tools from the same source**: generate the MCP tool manifest from the same model; optional stdio server | P2 | V1.x | Tool count/descriptions/parameters consistent |

### 5.5 Agent contract

| ID | Requirement | Priority | Version | Acceptance points |
|---|---|---|---|---|
| FR-34 | **TTY detection**: when non-TTY, automatically drop colors, spinners and interaction; can be explicitly overridden | P0 | V0.1 | Pseudo-terminal tests |
| FR-35 | **Structured errors**: `code`/`message`/`hint`/`nextSteps[]`/`docsUrl`; dual rendering for humans and Markdown/JSON | P0 | V0.1 | Error snapshots |
| FR-36 | **Stable exit codes**: 0/1/2/3/4/5/6/7/130 semantics fixed and documented | P0 | V0.1 | Integration tests |
| FR-37 | **`--yes` guard**: `[Destructive]` commands **fail fast** when non-TTY without `--yes` and must not hang | P0 | V0.1 | Timeout tests |
| FR-38 | **`--no-input`**: forbids all input prompts; combined semantics with `--yes` are explicit | P0 | V0.1 | Behavior matrix |
| FR-39 | **`--dry-run`**: the framework exposes `IsDryRun`; diagnostics warn about unused dry-run | P1 | V0.2 | Example |
| FR-40 | **`describe`**: outputs the complete command model JSON (capability self-description) | P0 | V0.1 | Consistent with help |
| FR-41 | **Output channel convention**: data goes to stdout, diagnostics/progress to stderr | P0 | V0.1 | Pipe tests |
| FR-42 | **Streaming events**: `--json-stream` outputs NDJSON events (progress/result/error) | P2 | V1.x | Long-running task example |

### 5.6 Runtime

| ID | Requirement | Priority | Version | Acceptance points |
|---|---|---|---|---|
| FR-43 | **Filters/middleware**: statically assembled at compile time; before/after hooks, exceptions, timing | P0 | V0.1 | Ordering and zero allocation |
| FR-44 | **DI constructor injection**: generates `new Command(...)`, dependencies resolved from the user's `IServiceProvider` | P0 | V0.1 | AOT-friendly |
| FR-45 | **Cancellation/signals**: SIGINT/SIGTERM/SIGHUP → `CancellationToken`; forced kill on timeout; exit code 130 | P0 | V0.1 | Integration tests |
| FR-46 | **Logging bridge**: `ILogger` optional; logs go to stderr; level controlled by `--verbose/--quiet` | P0 | V0.1 | Output separation |
| FR-47 | **Version**: `--version` reads assembly information; overridable | P0 | V0.1 | Semantic versioning |
| FR-48 | **Result rendering**: built-in basic text/table/markdown/json rendering; pluggable | P1 | V0.2 | Rendering snapshots |
| FR-49 | **Exception policy**: known errors → structured; unknown exceptions → exit code 1; stack traces only in debug mode | P0 | V0.1 | Error matrix |

### 5.7 Tooling and experience

| ID | Requirement | Priority | Version | Acceptance points |
|---|---|---|---|---|
| FR-50 | **`dotnet new runeshela-cli` template** | P0 | V0.1 | Template builds and runs |
| FR-51 | **In-memory console**: `TestApp` + `InMemoryConsole`, handlers unit-testable directly | P0 | V0.1 | Example tests |
| FR-52 | **Snapshot test support**: Verify integration for help/schema/completion | P1 | V0.2 | Example |
| FR-53 | **Migration guide**: comparison tables for System.CommandLine / CliFx / Spectre / CAF | P1 | V1.0 | Complete documentation |
| FR-54 | **Localized framework messages**: built-in messages localizable (Chinese/English) | P2 | V1.x | Resource-based |
| FR-55 | **Debuggable generated code**: readable naming, optional `#line`, generated files inspectable | P1 | V0.2 | Debugging demo |

---

## 6. Non-functional requirements (NFR)

> All thresholds are **target values pending M0 validation**, not established facts. Measurement methods are described in [02 Technical Design §12](./02-technical-design.md).

### 6.1 Performance

| ID | Metric | Target | Measurement |
|---|---|---|---|
| NFR-01 | Parsing+binding+dispatch (5 options) | ≤ 1 µs, 0 B | BenchmarkDotNet (in-process) |
| NFR-02 | Help output (human) | ≤ 5 µs, ≤ 64 B | BenchmarkDotNet |
| NFR-03 | Error path | ≤ 5 µs, ≤ 512 B | BenchmarkDotNet |
| NFR-04 | Additional AOT cold-start overhead | ≤ 1 ms (relative to an empty app) | hyperfine (30 runs, p50) |
| NFR-05 | AOT binary size delta | ≤ 100 KB | `dotnet publish` size difference |
| NFR-06 | Generator no-op incremental build | ≤ 30 ms (50 commands) | MSBuild timing/generator driver benchmarks |
| NFR-07 | Runtime package size | ≤ 60 KB (single TFM) | Package analysis |
| NFR-08 | Relative baseline | ≤ 1/10 the time of System.CommandLine 3.0 in the same scenario | Public benchmark |

### 6.2 Compatibility

| ID | Requirement |
|---|---|
| NFR-09 | Single target `net10.0` |
| NFR-10 | Windows x64/ARM64, Linux x64/ARM64 (glibc/musl), macOS x64/ARM64 |
| NFR-11 | Native AOT, Trim, Single-file and ReadyToRun are all publishable |
| NFR-12 | 0 warnings in AOT/Trim builds (official samples enable `WarningsAsErrors`) |
| NFR-13 | Generator targets `netstandard2.0`, compatible with Roslyn 4.8+ |

### 6.3 Quality and maintainability

| ID | Requirement |
|---|---|
| NFR-14 | Public API follows SemVer; no destructive changes within the same major version |
| NFR-15 | API compatibility gate (ApiCompat) runs in CI |
| NFR-16 | Line coverage ≥ 85% (core parsing/binding ≥ 95%) |
| NFR-17 | Every diagnostic code has documentation and examples |
| NFR-18 | Generator snapshot tests cover all artifacts |
| NFR-19 | Deterministic builds; SourceLink; symbol packages |
| NFR-20 | Documentation examples compile and run in CI |

### 6.4 Security

| ID | Requirement |
|---|---|
| NFR-21 | Zero third-party runtime dependencies; locked restore; SBOM |
| NFR-22 | Sensitive options are redacted by default (help/logs/errors/describe) |
| NFR-23 | No shell concatenation is ever performed; completion scripts are escaped |
| NFR-24 | Configuration file permission checks (warn when world-readable on Unix and containing secrets) |
| NFR-25 | NuGet package signing + trusted publishing (OIDC) + 2FA + branch protection |

### 6.5 Accessibility and internationalization

| ID | Requirement |
|---|---|
| NFR-26 | Support for `NO_COLOR` / `CLICOLOR_FORCE` / `TERM=dumb` |
| NFR-27 | Plain-text mode (`--plain`) for screen readers/logs |
| NFR-28 | Help is readable at both 80 and 120 columns; CJK width is correct |
| NFR-29 | Framework messages are localizable (P2); user copy is not forced |

### 6.6 Developer experience

| ID | Requirement |
|---|---|
| NFR-30 | 5-minute quickstart; runnable within 20 lines |
| NFR-31 | Diagnostics include "what/why/how to fix/documentation link" |
| NFR-32 | Generated code is readable, breakpoint-able and navigable |
| NFR-33 | First issue response ≤ 72h; PR review ≤ 7 days |
| NFR-34 | At least one public progress update per month (release/changelog/benchmark) |

---

## 7. Constraints

### 7.1 Technical constraints

- Supports .NET 10+ only; no .NET Framework / Unity.
- No reflection, `Activator`, `Expression.Compile` or `MakeGenericType` at runtime.
- The source generator must be incremental and cacheable, and must not hold on to `Compilation`.
- Generated code must be nullable-correct and acceptable to `TreatWarningsAsErrors`.
- Every artifact must be reconstructible from the compile-time model, without relying on runtime scanning.

### 7.2 Business constraints

- License is **Apache-2.0** (D-07=B): commercial use and redistribution are allowed; LICENSE/NOTICE and modification notices must be retained.
- Do not introduce dependencies requiring paid licenses.
- Do not promise an SLA (unless a commercial support agreement exists).

### 7.3 Dependencies

- No third-party runtime dependencies.
- Optional integrations: `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Logging`, `System.Text.Json` (all chosen by the user).
- Does not depend on Spectre.Console; an adapter layer is provided (P2).

---

## 8. Assumptions

| ID | Assumption | Impact if the assumption fails |
|---|---|---|
| AS-01 | Target users use the .NET 10+ SDK | The TFM would need to be lowered, sacrificing zero-overhead capabilities |
| AS-02 | Agent/non-TTY invocation keeps growing | The value of the agent contract decreases; pivot to human experience |
| AS-03 | Users are willing to use "conventions + doc comments" instead of heavy attribute annotation | The elegance claim weakens; explicit configuration must be added |
| AS-04 | Performance-sensitive users care about cold start and package size | The return on performance investment decreases |
| AS-05 | Teams value compile-time safety over runtime flexibility | Diagnostics/strictness must be relaxed |
| AS-06 | English-first documentation covers early users; a Chinese version is provided on demand | Chinese maintenance cost must be added |

---

## 9. Acceptance criteria (Given/When/Then)

> Verification type: **P** = programmatically verifiable (tests/gates); **H** = requires human judgment (review/experience).

### 9.1 Core experience

**AC-01 (G-01, P)**
Given an empty .NET console project
When adding the package, defining 1 `[Command]` class and calling `RunesHela.Run(args)` in `Main`
Then the project compiles and runs, `--help` shows the command/options/description, and the class needs no `partial`/interface/base class.

**AC-02 (G-01, P)**
Given a command tree containing 3 levels of nested classes
When running `app parent child leaf --help`
Then the correct path, options and examples are printed, with no runtime reflection.

**AC-03 (G-01, P)**
Given a 5-minute Quickstart document
When a developer unfamiliar with the framework follows the document
Then a runnable CLI is obtained within 5 minutes (manually timed, ≥ 4/5 success rate).

### 9.2 Performance

**AC-04 (G-02, P)**
Given the official benchmark project (5 options, synchronous handler)
When running BenchmarkDotNet
Then parsing+binding+dispatch ≤ 1 µs and allocations = 0 B (allowing ±10% machine variance, subject to the baseline report).

**AC-05 (G-02, P)**
Given a Native AOT publish configuration
When publishing an empty app and an app with 10 commands
Then the startup time difference is ≤ 1 ms (p50) and the binary size difference is ≤ 100 KB.

**AC-06 (G-02, P)**
Given a project with 50 commands
When modifying an unrelated file and performing an incremental build
Then generator no-op time is ≤ 30 ms and no full regeneration occurs.

### 9.3 Contract and artifacts

**AC-07 (G-03, P)**
Given `stdout` is redirected to a pipe
When running any command
Then there are no ANSI colors/spinners, data appears only on stdout, and diagnostics only on stderr.

**AC-08 (G-03, P)**
Given a `[Destructive]` command
When executing in a non-TTY environment without `--yes`
Then the process fails within 1 second with exit code 2 and outputs an error containing `nextSteps`; it must not wait for input.

**AC-09 (G-03, P)**
Given any command
When executing `app --help --json`
Then the output is JSON conforming to a fixed schema, including commands, options, arguments, default values, environment variables and exit codes.

**AC-10 (G-03, P)**
Given any command
When executing `app describe`
Then the complete model JSON consistent with help is output and can be consumed programmatically.

**AC-11 (G-04, P)**
Given the same version of the CLI
When generating help, completion, schema and docs separately
Then the command/option sets of all four are completely identical (comparison test).

**AC-12 (G-04, P)**
Given four shells (bash/zsh/fish/pwsh)
When loading the generated completion script and triggering completion
Then command names and option names complete correctly (smoke test).

### 9.4 Engineering quality

**AC-13 (G-05, P)**
Given the official sample project
When executing `dotnet publish -c Release -r <RID> /p:PublishAot=true`
Then the build succeeds, there are 0 AOT/Trim warnings, and the artifact runs.

**AC-14 (G-05, P)**
Given the CI benchmark baseline
When any PR makes parsing time or allocations exceed the budget by 10%
Then CI fails and outputs a comparison report.

**AC-15 (G-06, P)**
Given the public API baseline of the previous major version
When running ApiCompat
Then there are no destructive differences; if there are, the deprecation process must be followed.

**AC-16 (G-06, P)**
Given an invalid command definition (duplicate option names/required+default/unsupported type)
When compiling
Then stable diagnostic codes, readable messages and documentation links are produced instead of runtime exceptions.

### 9.5 Security and experience

**AC-17 (NFR-22, P)**
Given a `[Secret]` option
When showing help, outputting errors, enabling `--verbose` and executing `describe`
Then the value is redacted to `***` everywhere and does not appear in any log.

**AC-18 (NFR-26, P)**
Given `NO_COLOR=1` or `TERM=dumb`
When running any command
Then the output contains no ANSI escape sequences.

**AC-19 (NFR-28, H/P)**
Given help containing Chinese descriptions and long options
When displayed in an 80-column terminal
Then alignment is correct with no truncation and no mojibake (snapshot + manual inspection).

**AC-20 (NFR-30, H)**
Given the documentation site homepage
When a new user reads it
Then "what it is/why/30-second example/install command" is visible above the fold.

---

## 10. Version scope

| Version | Theme | Included |
|---|---|---|
| **V0.1 (MVP)** | Class-first + zero overhead + agent basics | FR-01~04, 08~14, 19~20, 22~27, 34~38, 40~41, 43~47, 49~51 |
| **V0.2** | Micro-commands + artifacts | FR-05~07, 17~18, 21, 28~29, 39, 48, 52, 55 |
| **V1.0** | Configuration + stability | FR-15, 32, 53 + API freeze + performance report |
| **V1.x** | Ecosystem | FR-16, 30~31, 33, 42, 54 |

> D-05 has confirmed the **standard MVP (B)**; the V0.1 row above is consistent with it.

---

## 11. Decided items (see 06 for details)

| ID | Decision | Result |
|---|---|---|
| D-01 | Route | **C: dual engine + agent-native** |
| D-02 | Target framework | **C: net10.0 single target, C# 14** |
| D-03 | Package shape | **B: minimal runtime + generator (single package)** |
| D-04 | API main axis | **C: class-first + method-first (V0.2)** |
| D-05 | MVP boundary | **B: standard MVP** |
| D-06 | Name | **RunesHela** |
| D-07 | License | **B: Apache-2.0** |
| D-08 | Help source | **C: attributes > doc comments > conventions** |
| D-09 | Rendering | **B: in-house minimal + adapter layer** |
| D-10 | Documentation language | **B: English first, Chinese on demand** |
| D-11 | MCP | **C: manifest + stdio server** |
| D-12 | Internationalization | **C: pluggable in V1.x** |
| D-13 | Interactive prompts | **B: separate package in V1.x** |
| D-14 | Telemetry/governance | **A: zero telemetry + sponsorship** |

---

## 12. Glossary

| Term | Meaning |
|---|---|
| Class-first | Declaring commands with classes/properties; the framework generates CLI behavior |
| Method-first | Declaring commands with lambdas/methods; call-site analysis generates the parser |
| Compile-time model | The immutable command description the generator builds from symbols |
| Artifacts | Outputs such as help/completion/man/docs/schema/MCP |
| Hot path | Parsing → binding → dispatch, excluding user business logic |
| Agent contract | Conventions such as non-TTY behavior, structured errors, exit codes and self-description |
| Zero reflection | No type metadata queries/dynamic invocation at runtime |
| AOT | Native AOT publishing (`PublishAot=true`) |
