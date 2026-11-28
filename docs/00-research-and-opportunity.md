# 00 Research and Opportunity Analysis

> Project name: **RunesHela** (selected, 2026-11-26; naming record in [06 Decision Document](./06-decisions.md) D-06)
> Data snapshot: 2026-11-26
> Document status: research conclusions, pending decision
> Related documents: [01 PRD](./01-product-requirements.md) · [02 Technical Design](./02-technical-design.md) · [05 ADR](./05-architecture-decisions.md)

---

## 1. Bottom line up front (TL;DR)

1. **The opportunity is real, but it is not "build another CLI framework" — it is "treat the CLI as a compile-time problem"**.
   Most existing .NET frameworks treat command definition, argument binding and help generation as a **runtime** problem (reflection, dictionaries, dynamic binding, runtime formatting); only ConsoleAppFramework turns the parser into **compile-time generation**. But it leaves gaps across three dimensions: "declarative elegance", "artifact consistency" and "agent contract".

2. **"Extreme performance" and "extreme elegance" are not an either/or choice; they are two entry points into one and the same compile-time model**.
   Class-first serves developer experience, method-first (lambda/method) serves micro-commands and minimal overhead; both compile down to the same generated code. The real generational difference is: **one declaration → compiled into parser, binder, help, completion, man page, schema, docs and MCP tools**, with zero reflection at runtime, zero third-party dependencies and zero allocation on the hot path.

3. **Star expectations must be calibrated.**
   "Cysharp has nearly 50k stars" is an **organization-level** cumulative figure; ConsoleAppFramework alone sits at **2,246 stars** while its total NuGet downloads are **4.315 million**. This tells us that performance obsession can win professional users and real adoption, but does not automatically win mass-market stars. The star ceiling in the .NET CLI space is currently Spectre.Console at **11,717**. Success metrics should therefore be "downloads + real tool adoption + reputation"; stars are a lagging indicator.

4. **The new variable in 2026 is the agent.**
   CLI consumers are shifting from "humans + scripts" to "humans + scripts + AI agents". Agents do not look at colors, do not wait for interaction, only read `--help` and exit codes, and require structured errors plus next-step suggestions. **No .NET CLI framework currently treats the agent contract as a first-class citizen**, and that is the sharpest, most easily spread differentiation wedge.

5. **Recommended route: dual engine + agent-native + single source of truth** (see the scoring in section 10).
   First establish a foothold with professional developers through "class-first + zero reflection + AOT", then create a talking point through the "agent-native contract", and finally build a long-term moat with "one model generating every artifact".

6. **The biggest risk is not technical — it is being perceived as "yet another framework".**
   Generational leadership must be proven with reproducible benchmarks, quantifiable budgets and verifiable artifact consistency, not by piling on features. Scope must stay restrained: no TUI, no general-purpose DI, no serialization framework.

---

## 2. Research method and evidence levels

| Item | Description |
|---|---|
| Data sources | GitHub REST API (stars/forks/last push), NuGet Registration/Search API (total downloads/version series), official READMEs and design articles, third-party CLI guides |
| Snapshot time | 2026-11-26 |
| Evidence levels | **A**: values read directly from official APIs/official docs; **B**: relative conclusions from official articles (e.g. "280x"); **C**: third-party blogs/community posts (used only as trend reference) |
| Measurement caveats | NuGet "total downloads" includes CI, mirrors and transitive restore, and **does not equal user count**; GitHub stars are influenced by promotion and **do not equal adoption**. The two must be read together |

---

## 3. Market map: .NET CLI frameworks/libraries

### 3.1 Core data

| Project | GitHub Stars | NuGet total downloads | Latest version | Positioning | Technical approach | Maintenance |
|---|---:|---:|---|---|---|---|
| **System.CommandLine** (Microsoft official) | —¹ | **119 million** | 3.0.0 (2.0 already GA) | Official parser/binding/completion | Runtime parsing + binding; trim-friendly | Active, but the API has been rewritten repeatedly |
| **CommandLineParser** | —¹ | **279.5 million** | 2.9.1 | Long-standing attribute-based parser | Reflection | Low activity (effectively unmaintained) |
| **Spectre.Console / .Cli** | **11,717** | 74 million / 17.66 million | 0.57.2 | Rich terminal rendering + CLI | Runtime; strong rendering | Active, still not 1.0 |
| **ConsoleAppFramework** | **2,246** | **4.315 million** | 5.8.1 | Zero-overhead CLI | **Source generator, analyzer-only** | Active |
| **Cocona** | —¹ | 2.624 million | 2.2.0 | DI-first micro framework | Reflection + DI | Active |
| **CliFx** | **1,619** | **2.306 million** | 3.0.1 | Class-first declarative | Source generation + runtime | Active |
| **DotMake.CommandLine** | —¹ | 358 thousand | 3.8.0 | Declarative wrapper over System.CommandLine | Source generation | Active |
| **RunesHela (target)** | 0 | 0 | — | Compile-time CLI compiler + agent-native | Source generation, zero reflection | — |

> ¹ This table records only the stars actually measured through the GitHub API in this round; entries not measured are shown as "—" to avoid fabrication. The official System.CommandLine repository is far larger than the other projects in this table, but its value is not measured by stars.

### 3.2 Four things the data tells us

1. **Downloads and stars are badly decoupled**: CommandLineParser has 279.5 million downloads and System.CommandLine 119 million, yet neither is a "star project". A CLI is infrastructure; users "use it and leave" without starring it.
2. **Performance is not a propagation engine**: ConsoleAppFramework has 4.31 million downloads and 2,246 stars; Spectre.Console has 74 million downloads and 11,717 stars. **Looking good spreads more easily than being fast**, but being fast is more likely to be adopted long-term by professional teams.
3. **The official framework has not unified the market**: System.CommandLine was rewritten continuously from 2.0 to 3.0, still leaving room around "complex API, slow startup, unnatural class-first". That is exactly the soil third-party frameworks live on.
4. **0.x can still own mindshare**: Spectre.Console 0.57.2 is still not 1.0, yet it is already the de facto standard for rich terminals in .NET. This shows that **superior experience can outweigh version-number maturity**.

---

## 4. Competitor breakdown

### 4.1 ConsoleAppFramework (the benchmark of the performance route)

**Design philosophy**: Zero Dependency / Zero Overhead / Zero Reflection / Zero Allocation / AOT Safe.

**Key implementation (evidence levels A/B)**:
- Full source generation: the body of the `Run` method is generated, writing argument parsing as a `switch` over string constants plus `TryParse`, with no reflection, no dictionaries and no cache initialization.
- Call-site analysis: instead of relying on attribute annotation, it analyzes the **call site** of `ConsoleApp.Run(args, lambda)` (inspired by Rust function-like macros), which is how "one line of code is a CLI" is achieved.
- Help text constant-folded at compile time: help is not assembled from strings at runtime but embedded directly as constants.
- Value conversion uses `ISpanParsable<T>` / `IParsable<T>`, avoiding `TypeConverter` and JSON round-trips.
- Optional `delegate* managed<...>` function pointers eliminate even the delegate allocation.
- Filters are statically assembled at compile time, avoiding intermediate arrays and closure allocations.
- The NuGet package is an **analyzer (source generator) that references no DLLs**; even the `ConsoleApp` class is injected by the generator.
- Official benchmark: cold start (no warm-up) is **280 times faster** than System.CommandLine, with **100–1000 times** fewer memory allocations (the framework itself is close to 0; the 400 B shown is essentially system overhead).

**Strengths**: startup, allocations, package size, AOT affinity; minimal dependencies; doc comments become help; graceful shutdown on SIGINT/SIGTERM.

**Weaknesses/openings**:
- Class-first is a "second-class citizen" (`Add<T>` was added later) rather than the primary abstraction.
- Help is plain text only: no Markdown/JSON, no man page, no completion, no schema, no MCP.
- No agent contract (non-TTY behavior, structured errors, `--yes` guard, self-description).
- No configuration-file/environment-variable precedence system (only via `Microsoft.Extensions.Configuration` when integrated with DI).
- No rich output (tables, progress, prompts); deliberately out of scope.

### 4.2 CliFx (the representative of the elegance route)

**Design philosophy**: class-first, declarative, testable.

**Key facts (evidence level A)**:
- `[Command]` + `ICommand` + `[CommandParameter]` / `[CommandOption]` with attribute binding; `partial` + source generator automatically generates `Main()`.
- Supports deeply nested commands, help, validation, interrupt signals, binary reads/writes, a testable console abstraction, AOT/trim compatibility, no external dependencies, and targets .NET Standard 2.0+.
- 3.0.1 is actively maintained, with 2.306 million downloads and 1,619 stars.

**Strengths**: low cognitive load — defining a class gives you a CLI; test-friendly; broad compatibility (netstandard2.0).

**Weaknesses/openings**:
- The netstandard2.0 compatibility target limits the room for zero-allocation/low-overhead techniques such as `ISpanParsable`, `SearchValues` and `FrozenDictionary`.
- Attribute binding happens at runtime, so there is no "compile-time full parser generation" performance narrative.
- No completion, no man page, no schema, no agent contract, no MCP.
- Help styling and output capabilities are limited.

### 4.3 System.CommandLine (the official baseline)

**Strengths**: official backing; flexible syntax (POSIX + Windows conventions); completion; help; trim-friendly; continuous evolution across 2.0/3.0.

**Weaknesses/openings**:
- Syntactic flexibility brings parsing complexity and startup overhead; the official benchmark shows it is far behind ConsoleAppFramework.
- The API has been destructively rewritten multiple times (2.0 beta series → 2.0 GA → 3.0), making migration costly.
- Weak declarative experience: it centers on builders/command objects rather than "define a class".
- No agent-native artifacts (no non-TTY structured output, no MCP, no self-describing schema as a first-class capability).

### 4.4 Spectre.Console.Cli (aesthetics and rendering)

**Strengths**: tables, trees, progress, prompts, colors and layout — the de facto standard for rich terminals in .NET; `Spectre.Console.Cli` provides settings classes, DI and validation.

**Weaknesses/openings**:
- Still 0.x; the package and dependencies are heavy; AOT compatibility needs extra attention.
- Performance is not a design goal; startup and allocations are not advantages.
- No built-in shell completion; no agent contract; no MCP.
- Rendering is coupled with the CLI, so "parsing only, no rendering" is not light enough.

### 4.5 Cocona / CommandLineParser / DotMake

| Project | Strengths | Key openings |
|---|---|---|
| **Cocona** | Minimal API style, DI/Hosting, easy onboarding | Mostly reflection, not AOT-first; mediocre performance; no artifact system |
| **CommandLineParser** | Extremely widespread (279.5 million downloads), simple attribute style | Effectively unmaintained, reflection-based, no AOT, weak help customization |
| **DotMake.CommandLine** | Declarative syntax + source generation on top of System.CommandLine | Inherits the weight and performance of SCL; small ecosystem; no agent contract |

### 4.6 Competitor capability matrix

Legend: ● native support; ◐ partial/needs plugin; ○ not supported.

| Capability | System.CommandLine | Spectre.Console.Cli | CliFx | Cocona | ConsoleAppFramework | RunesHela (target) |
|---|---|---|---|---|---|---|
| Zero reflection / AOT-first | ◐ | ◐ | ● (declarative) | ○ | ● | ● |
| Cold-start performance | ○ | ○ | ◐ | ○ | ● | ● |
| Class-first declarative | ○ | ● | ● | ◐ | ◐ | ● |
| Method-first micro-commands | ◐ | ○ | ○ | ● | ● | ● |
| Multi-level subcommands | ● | ● | ● | ● | ● | ● |
| Help quality (humans) | ● | ● | ● | ● | ◐ | ● |
| Help (machine-readable) | ○ | ○ | ○ | ○ | ○ | ● |
| Shell completion | ● | ○ | ○ | ◐ | ○ | ● |
| Man page / doc generation | ○ | ○ | ○ | ○ | ○ | ● |
| Structured errors / stable exit codes | ◐ | ○ | ◐ | ◐ | ◐ | ● |
| Agent contract (non-TTY/`--yes`/self-describing) | ○ | ○ | ○ | ○ | ○ | ● |
| MCP tools generated from the same source | ○ | ○ | ○ | ○ | ○ | ● (optional package) |
| Unified CLI+ENV+config-file binding | ○ | ◐ | ○ | ◐ | ○ | ● (V1) |
| DI | ◐ | ● | ● | ● | ● | ● (generated constructor injection) |
| Rich terminal rendering | ○ | ● | ○ | ○ | ○ | ○ (pluggable) |
| Zero third-party dependencies | ● | ○ | ● | ◐ | ● | ● |
| Maturity | ● | ◐ | ● | ● | ● | — |

**How to read the matrix**: the upper-right corner (AOT + elegance + agent + artifacts) is empty space. No competitor simultaneously covers `zero reflection`, `class-first`, `machine-readable help`, `agent contract` and `artifact consistency`.

---

## 5. Cross-ecosystem benchmarking: where do stars come from

| Ecosystem | Representative | Scale (2026-11 snapshot) | Success factors |
|---|---|---:|---|
| Rust | **clap** | 16.8k stars, 21.8 million downloads/week | derive macros = compile time; help/completion out of the box; outstanding documentation |
| Go | **Cobra** | 44k stars | Ecosystem network effects (kubectl, gh, hugo, etc.); generators and completion |
| Python | **Typer** | 20.3k stars, 286 million downloads | Type annotations are the CLI; almost zero learning curve; FastAPI brand spillover |
| .NET | **Spectre.Console** | 11.7k stars, 74 million downloads | Visual impact + screenshot-driven sharing + hard demand for rich terminals |
| .NET | **ConsoleAppFramework** | 2.2k stars, 4.31 million downloads | Performance trust + Cysharp brand + minimal dependencies |

**Conclusions**:
- There are three main sources of stars: **becoming the default choice of an ecosystem**, **visual/brand propagation**, and **zero learning curve**.
- Pure performance is a "credential", not a "propagation engine". Performance must be converted into **perceivable experience** (fast startup, small binaries, no dependencies, clear help) before the mainstream can perceive it.
- Therefore RunesHela's propagation narrative cannot be just "280 times faster"; it must be: **"one class → a complete CLI + completion + docs + MCP, with AOT single-file startup at the 3 ms level (target, pending M0 validation)"**.

---

## 6. Demand side: who will use it and why

### 6.1 User personas

| Persona | Typical scenario | Core needs | Current pain points |
|---|---|---|---|
| **P1 Platform/infrastructure engineer** | Internal engineering CLIs, deployment tools, data pipelines | Multi-level subcommands, config/env vars, scriptable in CI, testable | Frameworks are too heavy; help/docs/completion drift; agent invocation is unstable |
| **P2 Tool author/OSS maintainer** | Shipping single-file CLIs, cross-platform distribution | Fast startup, small package, AOT, zero dependencies, completion, man page | Existing frameworks are either slow or missing artifacts |
| **P3 Enterprise .NET team** | Compliance, audit, maintainability | Stable API, diagnostics, testability, licensing, supply chain security | Reflection/dynamic code is hard to audit; upgrades break things |
| **P4 AI/agent developer** | Exposing CLIs to agents/MCP | Structured output, stable exit codes, non-interactive, self-describing | CLI output is unparseable; interaction hangs; errors have no next step |
| **P5 Learner/hobbyist** | Writing a first CLI, demos | 5-minute onboarding, runnable samples, good looks | Too many concepts, too much boilerplate, poor docs |

### 6.2 Jobs-to-be-Done

| JTBD | Status quo | RunesHela's answer |
|---|---|---|
| "I've defined my commands, stop making me write parsing boilerplate" | Hand-written `switch` or learning a framework API | Declare a class/method, generated at compile time |
| "Give me a `--help` that never goes stale" | Hand-written help that constantly drifts | Generated from the model, single source of truth |
| "Let agents call my tool reliably" | Human-oriented tables that agents must guess at | Dual-modal output + structured errors + self-description |
| "Ship a single file with 3 ms-level startup" (target) | The framework slows it down or bloats it | Zero-reflection generation, AOT budget gate |
| "Don't make me start a process just to test" | Only end-to-end testing | In-memory console + plain class handlers |
| "Don't blow up my build when I upgrade" | Frequent API breaks | SemVer + API compatibility gate + diagnostics |

---

## 7. Hard requirements (must-have)

> Criterion: if missing, target users **abandon the framework outright** rather than "deal with it later".

| ID | Hard requirement | Why it is hard | Consequence if unmet | How to verify |
|---|---|---|---|---|
| R-01 | Multi-level subcommands + options/positional arguments/remaining arguments | Covers 80% of real CLI scenarios | The product shape cannot be expressed | Sample matrix |
| R-02 | Type-safe binding (primitives/enums/Guid/time/arrays/nullable/custom) | Hand-written parsing is error-prone | Users fall back to hand-written parsing | Unit tests |
| R-03 | Help generated automatically and readable | The user's first touchpoint | Judged as "incomplete" | Snapshot tests |
| R-04 | Stable exit codes + stdout/stderr separation | Scripts/CI/agents depend on it | Automation becomes unusable | Integration tests |
| R-05 | AOT/Trim publishable with zero warnings | Hard requirement for single-file distribution | Target scenario fails | AOT matrix |
| R-06 | Zero third-party runtime dependencies | Enterprise/supply chain requirement | Procurement is blocked | Dependency graph |
| R-07 | Performance no worse than hand-written parsing by an order of magnitude | The raison d'être of the "performance route" | The narrative collapses | Benchmark gate |
| R-08 | 5-minute quickstart | The entry point of the adoption funnel | Churn | Documentation walkthrough |
| R-09 | Testable (without starting a process) | The adoption threshold for teams | Cannot enter engineering systems | Test samples |
| R-10 | Clear license (Apache-2.0, including patent grant) | Enterprise compliance | Cannot be used | Legal review |
| R-11 | Cancellation/signal handling | Essential for long-running tasks | Zombie processes/data corruption | Integration tests |
| R-12 | Cross-platform consistency (Win/Linux/macOS) | Tool distribution | Platform-specific defects | CI matrix |

---

## 8. Hidden requirements (users won't say them, but will judge by them)

### 8.1 Human user experience

| ID | Hidden requirement | How it is perceived | Design response |
|---|---|---|---|
| H-01 | Readable in an 80-column terminal, correct CJK width | Misaligned help = unprofessional | Width-aware layout + CJK width algorithm |
| H-02 | Errors say "what to do next" | Whether you are rescued when stuck | `hint` + `nextSteps[]` + `docsUrl` |
| H-03 | "Did you mean X" | The experience of mistyping an option | Did-you-mean (compile-time candidate set) |
| H-04 | Examples can be copied and run directly | Documentation credibility | Examples participate in CI compilation/execution |
| H-05 | Redirection automatically drops color and spinner | Clean pipe output | TTY detection + `NO_COLOR` |
| H-06 | Graceful exit on Ctrl+C, exit code 130 | Long-running task interruption | Signal hooks + forced kill on timeout |
| H-07 | No crash when the downstream closes early | `head` pipelines do not error | EPIPE/SIGPIPE handling |
| H-08 | No mojibake for UTF-8 on Windows | Chinese output | Console encoding setup/VT enablement |
| H-09 | Negative numbers, `--`, `=`, and repeated options all handled correctly | Edge-case input | Explicit grammar rules + property-based tests |
| H-10 | Environment variables/config files are discoverable | Visible in help | Help displays ENV/Config keys |
| H-11 | Sensitive values are neither echoed nor logged | Security audit | `[Secret]` redaction + log filtering |
| H-12 | Semantically versioned, script-readable version number | Upgrade/compatibility decisions | Stable `--version` output |

### 8.2 Agent / automation experience

| ID | Hidden requirement | How it is perceived | Design response |
|---|---|---|---|
| A-01 | Non-TTY automatically switches to a machine-readable format | Whether the agent succeeds on the first try | TTY detection + `--json` + Markdown by default |
| A-02 | Layered, parseable `--help` | Fewer agent trial-and-error loops | Top-level summary + subcommand details + `--help --json` |
| A-03 | Error structure contains code/hint/nextSteps | Whether the agent can self-heal | Structured error model |
| A-04 | Destructive operations fail fast when non-interactive | Prevents agents from hanging | `--yes` guard + refusal to execute when non-TTY |
| A-05 | `--no-input` forbids all prompts | Predictability | Unified input policy |
| A-06 | `--dry-run` with consistent semantics | Safe trial and error | Framework-level convention + diagnostics |
| A-07 | Exit codes distinguish usage/config/business errors | Basis for decisions | Exit code table |
| A-08 | Large results written to files with a returned path | Avoids context explosion | Result output policy |
| A-09 | Stable output ordering, no noise | diff/replay | Deterministic ordering |
| A-10 | Capability self-description (`schema`/`describe`) | Zero guessing | Generated JSON model |
| A-11 | MCP tools share the same source as the CLI | Consistency across consumers | Manifest generated from the same model |
| A-12 | Long-running tasks can stream events | Observable progress | NDJSON event stream (P2) |

### 8.3 Engineering and ecosystem experience

| ID | Hidden requirement | Design response |
|---|---|---|
| E-01 | The generator does not slow down the IDE | Incremental generation + strict caching + benchmarks |
| E-02 | Generated code does not trigger false positives in user analyzers | `GeneratedCode` marking + correct nullability |
| E-03 | `TreatWarningsAsErrors` is usable | Zero-warning goal |
| E-04 | A generator crash must not "blow up the build" | RHL999 degradation + explicit error |
| E-05 | Generated code is debuggable and navigable | Readable naming + optional `#line` |
| E-06 | Supports strong naming/deterministic builds/SourceLink | CI configuration |
| E-07 | Upgrades do not break the API | API compatibility gate + deprecation window |
| E-08 | Small package, fast install, no transitive dependencies | Dependency audit |
| E-09 | Docs and code in the same repo, examples verifiable | Docs-as-code |
| E-10 | Predictable issue/PR response | Templates + SLA + roadmap |
| E-11 | Trustworthy supply chain (SBOM/signing/2FA) | NuGet trusted publishing |
| E-12 | Multiple frameworks/architectures/containers | CI matrix |

---

## 9. Opportunity gap matrix

| Gap | Status quo | RunesHela's opportunity | Difficulty | Defensibility |
|---|---|---|---|---|
| **W1 Class-first + zero reflection + AOT all hold true at once** | CliFx is class-first but binds at runtime; CAF is zero-reflection but weak at class-first | Generate constructor/assignment code so classes can be plain POCOs (no `partial` needed) | Medium | Medium |
| **W2 A single model generates every artifact** | Each framework covers only 1–2 kinds of artifacts | help/completion/man/schema/docs/MCP from one source, eliminating drift | Medium-high | **High** |
| **W3 Agent-native contract** | Missing everywhere | Non-TTY, structured errors, `--yes` guard, self-description | Medium | Medium (easy to imitate) |
| **W4 Performance as CI gate and public benchmark** | CAF has benchmarks but no gates/budgets | Budgeted, reproducible, regression fails the build | Medium | Medium |
| **W5 Unified zero-reflection binding for CLI+ENV+config file** | MEDI configuration binding relies on reflection and allocates heavily | Generated binding + precedence + redaction + schema | Medium-high | Medium |
| **W6 Compile-time diagnostics and fixes** | Most frameworks only fail at runtime | Duplicate names/required+default/unsupported types reported at compile time | Medium | Medium |

**Priorities**: W1 + W3 form the minimum combination for entering the market ("elegant and agent-friendly"); W2 is the long-term moat; W5 is the V1 growth point; W4 is the trust infrastructure.

---

## 10. Route evaluation

### 10.1 Three routes

| Dimension | A. Pure extreme performance | B. Pure extreme elegance | C. Dual engine + agent-native (recommended) |
|---|---|---|---|
| Core proposition | Faster and smaller than CAF | More elegant than CliFx | One declaration compiles into every artifact; friendly to humans/scripts/agents |
| Direct opponent | ConsoleAppFramework | CliFx / Spectre.Console.Cli | No direct opponent (empty space) |
| Technical difficulty | High (must surpass an already heavily optimized CAF) | Medium | High (but phaseable) |
| Differentiation | Weak (performance is already near physical limits) | Medium (easy to imitate) | **Strong** (the combination of model + artifacts + contract) |
| Star potential | Low (professional niche) | Medium | Medium-high |
| Maintenance cost | Low | Medium | Medium-high |
| Failure risk | Seen as "a shadow of CAF" | Seen as "yet another CliFx" | Scope spirals out of control |

### 10.2 Weighted scoring

Weights: differentiation 30%, feasibility 20%, market space 25%, maintenance cost 15%, propagation 10%.

| Route | Differentiation | Feasibility | Market | Maintenance | Propagation | Weighted score |
|---|---:|---:|---:|---:|---:|---:|
| A Pure performance | 5 | 7 | 5 | 9 | 3 | **5.80** |
| B Pure elegance | 5 | 9 | 6 | 6 | 6 | **6.30** |
| **C Dual engine + agent** | **9** | **6** | **9** | **5** | **9** | **7.80** |

> The scores are subjective judgments used to align direction, not facts. C's feasibility score of 6 means it must be phased strictly, otherwise it will be abandoned half-finished.

### 10.3 Why now

1. **.NET 10 LTS + Native AOT maturity**: AOT is no longer an experimental feature, and single-file CLIs have become a realistic distribution format.
2. **Agents calling CLIs has become the norm**: the MCP ecosystem is exploding (2026 already has 5,800+ servers and SDK downloads in the 97 million per month range), yet the CLI side lacks an "agent contract" specification.
3. **The official framework is still being rewritten**: the System.CommandLine 2.0 → 3.0 migration window is the moment for third parties to establish differentiation.
4. **Performance infrastructure is reproducible**: BenchmarkDotNet, hyperfine and AOT size diffs can all run in CI, so performance is no longer "verbal faith".

---

## 11. Recommended positioning and narrative

### 11.1 One sentence

> **Treat the CLI as a compiler problem: one declaration compiles into a parser, help, completions, docs, schema and MCP tools; zero reflection at runtime, zero third-party dependencies, zero allocations on the hot path.**

English:
> **A compile-time CLI compiler for .NET: declare once, get parser, help, completions, docs, schema and MCP tools — zero reflection, zero dependencies, zero allocations on the hot path.**

### 11.2 Elevator pitch (30 seconds)

- **For humans**: define a class, get a complete CLI, and help that always matches the code.
- **For scripts**: stable exit codes, stdout/stderr separation, completion, man page.
- **For agents**: automatic structured output when non-TTY, errors with next steps, destructive operations that do not hang, `schema` self-description, optional MCP.
- **For operations**: AOT single file, 3 ms-level startup (target), no third-party dependencies, a budgeted binary size.

### 11.3 Why not X

| Comparison | Answer |
|---|---|
| Why not System.CommandLine? | It is a general-purpose parsing library, not a complete "declarative + artifacts + agent contract" solution; and its API is still evolving. |
| Why not ConsoleAppFramework? | Its performance is excellent, but class-first, machine-readable help, completion/docs/MCP and the agent contract are all empty. |
| Why not CliFx? | Good class-first experience, but it lacks full compile-time generation, an artifact system and the agent contract. |
| Why not Spectre.Console.Cli? | It is strong at rendering; we are strong at parsing/binding/artifacts/contracts, and can be combined with Spectre (output is pluggable). |

---

## 12. Success metrics

### 12.1 Adoption metrics (lagging)

| Metric | 12-month target (assumption) | 36-month target (assumption) |
|---|---:|---:|
| NuGet total downloads | 200 thousand | 2 million |
| GitHub Stars | 1,500 | 6,000 |
| Real dependent projects (GitHub dependency graph) | 50 | 500 |
| External contributors | 10 | 40 |
| Production cases (publicly shareable) | 5 | 30 |

> Targets are inferred from the scale of comparable projects and are **not commitments**; they must be recalibrated after M0.

### 12.2 Quality metrics (leading)

| Metric | Target |
|---|---|
| Parsing hot-path allocations | 0 B (5-option benchmark) |
| Help-path allocations | ≤ 64 B |
| Additional AOT startup overhead | ≤ 1 ms (relative to an empty app) |
| AOT binary size delta | ≤ 100 KB |
| Generator no-op incremental time | ≤ 30 ms (50 commands) |
| AOT/Trim warnings | 0 |
| Public API destructive changes | 0 (within the same major version) |
| Documentation example compilation pass rate | 100% |

### 12.3 Community metrics

- First issue response ≤ 72 hours; first PR review ≤ 7 days.
- Monthly release cadence (patches anytime, minor releases monthly).
- A public benchmark report every quarter (hardware, method, raw data).

---

## 13. Risks and antifragility

| Risk | Probability | Impact | Mitigation |
|---|---|---|---|
| Being perceived as "yet another framework" | High | Fatal | Use the triple differentiation of "compile-time + artifacts + agent" and avoid feature-by-feature comparison |
| Scope spiraling (wanting TUI/DI/serialization too) | High | High | Explicit non-goals; phasing; check scope in every PR |
| Generator complexity dragging down the IDE | Medium | High | Incremental generation, caching, generator benchmarks, snapshot tests |
| AOT edge cases | Medium | Medium | AOT matrix + diagnostics + documentation |
| Performance unable to surpass CAF | Medium | Medium | Do not promise "absolute first"; promise "same order of magnitude + more artifacts/contracts" |
| Solo maintainer burnout | Medium | High | Clear architecture, sufficient tests, sponsorship, co-maintainers |
| The official framework absorbing the features | Medium | Medium | Keep the speed of "combinatorial innovation"; do the "artifacts + agent" work the official team is unwilling to do |
| Brand/naming conflict | Medium | Low | The initial choice RunesHela is done; run trademark/NuGet/domain searches before release |
| Supply chain attack | Low | High | Zero dependencies, locked restore, SBOM, signing, 2FA |

---

## 14. Fact check: three premises that need correction

1. **"Cysharp has nearly 50k stars" → that is organization-level, not a single library.**
   ConsoleAppFramework alone has **2,246 stars**; the claim that the Cysharp organization accumulates tens of thousands of stars is consistent in magnitude, but it cannot be used directly as evidence that "a CLI framework can get 50k stars".

2. **"Performance obsession has a huge market" → there is a market, but not a star market.**
   CAF has 4.31 million downloads vs 2,246 stars, showing that performance wins **adoption** but not necessarily **propagation**. To earn stars, "visible elegance" and a "shareable narrative" must also be solved.

3. **"CliFx represents elegance" → it is one version of elegance, not the endpoint.**
   CliFx requires `partial`, `ICommand` and attribute annotations; a more elegant form is "plain POCO + conventions + doc comments + compile-time diagnostics", which is precisely RunesHela's entry point.

---

## 15. References

> Data read on 2026-11-26. GitHub/NuGet data was read directly from APIs; relative performance conclusions come from the official ConsoleAppFramework design article.

- [ConsoleAppFramework v5 design article (neue.cc)](https://neue.cc/2024/06/13_ConsoleAppFramework_v5.html)
- [Cysharp/ConsoleAppFramework (GitHub)](https://github.com/Cysharp/ConsoleAppFramework)
- [Tyrrrz/CliFx (GitHub)](https://github.com/Tyrrrz/CliFx)
- [Spectre.Console (GitHub)](https://github.com/spectreconsole/spectre.console)
- [System.CommandLine documentation (Microsoft Learn)](https://learn.microsoft.com/dotnet/standard/commandline/)
- [System.CommandLine 2.0.0-beta5 migration guide](https://learn.microsoft.com/dotnet/standard/commandline/migration-guide-2.0.0-beta5)
- [NuGet: ConsoleAppFramework](https://www.nuget.org/packages/ConsoleAppFramework)
- [NuGet: CliFx](https://www.nuget.org/packages/CliFx)
- [NuGet: Spectre.Console](https://www.nuget.org/packages/Spectre.Console)
- [NuGet: System.CommandLine](https://www.nuget.org/packages/System.CommandLine)
- [NuGet: CommandLineParser](https://www.nuget.org/packages/CommandLineParser)
- [NuGet: Cocona](https://www.nuget.org/packages/Cocona)
- [NuGet: DotMake.CommandLine](https://www.nuget.org/packages/DotMake.CommandLine)
- [AI Agents Are Your New Users — A 2026 CLI Checklist](https://garbas.si/posts/ai-agents-are-your-new-users-cli/)
- [MCP vs CLI for AI Agents (arXiv preprint)](https://arxiv.org/pdf/2610.30157.pdf)
- [clap (crates.io data)](https://depdelta.com/cargo/clap)
- [Typer (PyPI)](https://pypi.org/project/typer/)
