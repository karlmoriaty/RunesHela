# 05 Architecture Decision Records

> Status legend: **Accepted** = confirmed; **Proposed** = suggested proposal; **Superseded** = replaced.
> All 16 ADRs were moved to **Accepted** on **2026-11-26**, together with resolutions D-01~D-14; the project entered M0 on **2026-11-27**.
> Related: [02 Technical Design](./02-technical-design.md) · [06 Decision Log](./06-decisions.md)

---

## ADR-001 Package shape: minimal runtime + generator

**Status**: Accepted (2026-11-26; corresponds to D-03)

### Context

ConsoleAppFramework proved that a "pure analyzer" can achieve zero assembly dependencies, zero package size, and minimal AOT incremental cost. But RunesHela's class-first approach needs public attributes, entry types, an error model, a testing console, and optional MCP; a pure analyzer leads to bloated generated code, hard-to-fix defects, and optional capabilities that cannot be shared.

### Options

| Option | Advantages | Disadvantages |
|---|---|---|
| A. Pure analyzer | Zero assemblies; smallest AOT incremental cost; strong narrative | Attributes must be generated; code bloat; hard to fix; weak advanced capabilities |
| B. Minimal runtime + generator | Stable API; composable; small generated code; fixable | One extra assembly; "zero dependency" needs explaining |
| C. Hybrid | Best of both | Two code paths; double the cost |

### Decision

Adopt **B**: a single NuGet package `RunesHela` (runtime assembly + the `RunesHela.Generator` analyzer assembly, with BCL-only dependencies); method-first ships separately as the pure analyzer package `RunesHela.Script`.

### Rationale

1. Class-first is the main API and needs stable public types.
2. Optional capabilities (Testing/MCP/Spectre adapter) should be composable and must not be injected into the user's assembly.
3. Generator defects can be fixed by upgrading the package instead of asking users to regenerate.
4. We can still claim "zero third-party runtime dependencies", which is the part enterprises care about most.

### Consequences

- Package size: runtime target ≤ 60 KB/TFM.
- Dependencies: the `RunesHela` dependency graph contains only the BCL.
- Documentation: state explicitly that "zero third-party dependencies ≠ zero assemblies".
- Failure mode: generator and runtime versions must match; locked within the package.

### Risks and Mitigation

| Risk | Mitigation |
|---|---|
| Users misread "zero dependencies" | State the definition clearly on the first screen of the README |
| Runtime bloat | Strict budget + trimming + no LINQ/reflection |
| Version mismatch | The `RunesHela` package bundles a same-version generator; MSBuild check |

### Related

ADR-011 (versioning), D-03.

---

## ADR-002 API axis: class-first + method-first (phased)

**Status**: Accepted (2026-11-26; corresponds to D-04)

### Context

Class-first suits real projects; method-first suits micro-commands and virality. Both share parsing/binding, but the entry-point shape differs. Method-first needs a parser generated at the call site; since C# does not support cross-assembly partial types, if the runtime library already defines `RunesHela.Run`, the generator cannot add arbitrary lambda overloads to it.

### Options

| Option | Advantages | Disadvantages |
|---|---|---|
| A. Class-first only | Simple and uniform | Lacks the "one-line CLI" hook |
| B. Method-first only | Minimal, strong performance narrative | Weak for complex projects |
| C. Both, phased | Broad coverage, isolated risk | Two APIs/two documentations |

### Decision

Adopt **C**:
- **V0.1**: class-first (main API).
- **V0.2**: `RunesHela.Script` (pure analyzer) provides `RunesHelaScript.Run(args, lambda)`, generating a separate entry type to avoid conflicts with the runtime `RunesHelaApp`.

### Rationale

1. Class-first is the engineering mainstream; build the verifiable core first.
2. Method-first carries high technical risk (call-site analysis, incremental caching, overload resolution), so it is isolated in a separate package.
3. We do not promise full functional equivalence: script mode does not support complex DI/deep nesting.

### Consequences

- Documentation must state which scenarios each entry point suits.
- The generator must support two discovery modes while sharing the model/parsing/binding.
- API naming: `RunesHelaApp` (class-first)/`RunesHelaScript` (method-first).

### Risks and Mitigation

| Risk | Mitigation |
|---|---|
| Two divergent behaviors | Shared snapshot test matrix |
| Users confuse the entry points | Documentation decision tree + templates |
| Method-first generator complexity | Support simple types/no DI first, then extend gradually |

### Related

ADR-001, ADR-013, D-04.

---

## ADR-003 Help text source: attributes > doc comments > conventions

**Status**: Accepted (2026-11-26; corresponds to D-08)

### Context

Help text is expensive to maintain and prone to drift. Doc comments come naturally; attributes are explicit; conventions are zero-config.

### Options

| Option | Advantages | Disadvantages |
|---|---|---|
| A. Doc comments first | Clean, same source | Depends on comment parsing; local functions unsupported |
| B. Attributes first | Stable, explicit | Noisy |
| C. Dual track (attributes override) | Flexible | Requires defined precedence |

### Decision

Adopt **C**, with precedence: **attributes > doc comments > conventions**. On conflict, emit an informational `RunesHela` diagnostic (not an error).

### Rationale

1. Zero-config by default: naming conventions + comments are enough.
2. When precise control is needed, attributes override (alias, short name, ENV, Secret, hidden).
3. Migration-friendly: when migrating from other frameworks, attribute descriptions can be kept initially.

### Consequences

- The generator must read `ISymbol.GetDocumentationCommentXml()`.
- `<summary>`/`<param>`/`<example>`/`<remarks>` must be handled.
- Missing doc comments → `RHL005` informational message.

### Risks and Mitigation

| Risk | Mitigation |
|---|---|
| Comment parsing affected by compiler configuration | Test across multiple IDEs/CI; degrade when missing |
| Attribute/comment conflicts | Explicit precedence + diagnostic |
| Local functions have no comments | Method-first mode uses attributes/parameter names |

### Related

ADR-002, D-08.

---

## ADR-004 Syntax strategy: single pass, predictable, ambiguity rejected

**Status**: Accepted (2026-11-26)

### Context

POSIX/GNU carries a large amount of legacy syntax (`-x` vs `-X`, short option bundling `-fdx`, optional argument values, prefix matching). Full compatibility would significantly increase parsing complexity and cold-start overhead.

### Options

| Option | Advantages | Disadvantages |
|---|---|---|
| A. Full POSIX/GNU compatibility | Familiar to users | Complex, slow, ambiguity-rich |
| B. Explicit subset | Fast, predictable | A few habits unsupported |
| C. Configurable syntax | Flexible | Combinatorial explosion, hard to document |

### Decision

Adopt **B**:
- Supported: `--long`, `--long=value`, `-s value`, the `--` terminator, repeated options, negated flags, negative numbers as values, case-insensitive matching (lowercase preferred).
- Not supported: short option bundling (`-abc`), prefix abbreviations, `-x`/`-X` distinction, optional-value ambiguity.
- Command names are case-sensitive.

### Rationale

1. Single-pass parsing pays off most in cold-start scenarios.
2. Ambiguous syntax breeds errors and security problems.
3. Explicit rules are easier to document and test than "compatible where possible".

### Consequences

- A few user habits need adjustment; migration notes are provided.
- Error messages must explicitly state that "bundled short options are not supported".

### Risks and Mitigation

| Risk | Mitigation |
|---|---|
| Users complain about incompatibility | Clear documentation + `--` pass-through as a fallback |
| Conflicts with scripting habits | Provide a migration guide and diagnostic hints |

### Related

02 §6.4, 02 §8.

---

## ADR-005 Configuration: CLI > ENV > JSON > defaults

**Status**: Accepted (2026-11-26; corresponds to D-05)

### Context

Real CLIs need multi-source configuration; Microsoft.Extensions.Configuration is powerful but has many dependencies, and its binding relies on reflection with heavy allocations. We need a zero-reflection, predictable design that supports redaction.

### Options

| Option | Advantages | Disadvantages |
|---|---|---|
| A. CLI only | Simplest | Does not match engineering conventions |
| B. CLI + ENV | Lightweight | No file configuration |
| C. CLI + ENV + JSON | Complete, controllable | Requires precedence/validation/schema implementation |
| D. Integrate MEDI Configuration | Ecosystem compatible | Heavy dependencies, reflection, AOT risk |

### Decision

Adopt **C**, implemented in V1.0:
- Precedence: CLI > ENV > file > defaults.
- File format: JSON (`JsonDocument`, no reflection, AOT-safe).
- Key path: `section.key`; `[Option(Config = "deploy.environment")]`.
- Generate a configuration schema; invalid configuration → exit code 3.

### Rationale

1. JSON is the .NET ecosystem default, ships with the BCL, and is AOT-safe.
2. Zero-reflection binding matches the project's positioning.
3. Explicit precedence avoids "configuration hell".
4. No MEDI dependency is introduced; users can bridge it themselves when needed.

### Consequences

- Configuration keys become a public contract (and must be versioned).
- Redaction rules must cover file/ENV/CLI.
- Schema generation adds an artifact.

### Risks and Mitigation

| Risk | Mitigation |
|---|---|
| Users want YAML/TOML | Extend via `IConfigSource` in V1.x |
| Overlaps with MEDI configuration | Provide a bridging example, do not build it in |
| Misunderstood precedence | Show the source in help + `--verbose` |

### Related

02 §9, D-05.

---

## ADR-006 DI: generated constructor injection, no built-in container

**Status**: Accepted (2026-11-26)

### Context

Class-first needs constructor injection; a built-in container would re-implement MEDI and increase AOT risk; reflection-based activation would break zero reflection.

### Options

| Option | Advantages | Disadvantages |
|---|---|---|
| A. No DI | Simplest | Unsuitable for complex projects |
| B. Generated constructor injection + user `IServiceProvider` | Zero reflection, AOT-friendly, composable | Users must register services |
| C. Built-in container | Works out of the box | Scope explosion, AOT risk |

### Decision

Adopt **B**:
- Generated code directly calls `new Command(services.GetService(typeof(ILogger))...)`.
- Without a provider, only parameterless constructors are supported; otherwise a clear error is raised.
- No assembly scanning, no property injection.

### Rationale

1. A constructor is an explicit dependency and is easy to test.
2. `IServiceProvider` is a BCL abstraction, so MEDI or a home-grown container both work.
3. Avoid reflection-based activation and lifecycle magic.

### Consequences

- Users must understand that "commands are activated by the generator".
- Service resolution failure → a clear error (including the missing type).
- Under AOT, explicit factory registration is recommended.

### Risks and Mitigation

| Risk | Mitigation |
|---|---|
| Users expect automatic scanning | Documentation states "explicit registration" |
| AOT issues with open generic services | Diagnostics + documentation guidance |

### Related

02 §13.2, D-04.

---

## ADR-007 Rendering: zero-dependency core, optional adapter layer

**Status**: Accepted (2026-11-26; corresponds to D-09)

### Context

Spectre.Console is the de facto standard for rich terminals in .NET, but it has heavy dependencies, is still 0.x, and needs AOT care; binding the core to it would violate "zero third-party dependencies".

### Options

| Option | Advantages | Disadvantages |
|---|---|---|
| A. Bundle Spectre | Beautiful, effortless | Heavy dependency, AOT risk |
| B. In-house minimal + adapter layer | Light, controllable, composable | Must maintain the basic renderer |
| C. Plain text only | Lightest | Weak experience |

### Decision

Adopt **B**:
- The core ships text/markdown/json/table (minimal).
- The optional `RunesHela.Rendering.Spectre` package adapts `IResult`.
- Help rendering stays in-house (width/CJK/no box-drawing lines).

### Rationale

1. The core must have zero third-party dependencies.
2. Most CLIs only need tables/lists/JSON, not a full TUI.
3. When aesthetics matter, Spectre can be composed in without holding users hostage.

### Consequences

- Must implement CJK width, color capability, and table alignment.
- The adapter layer must track Spectre versions.

### Risks and Mitigation

| Risk | Mitigation |
|---|---|
| In-house rendering is not pretty enough | Minimal style + optional Spectre |
| Table edge cases | Snapshot tests + 80/120 columns |

### Related

ADR-001, D-09.

---

## ADR-008 Agent contract: on by default, overridable

**Status**: Accepted (2026-11-26; corresponds to D-05)

### Context

Agents have become an important consumer of CLIs; existing frameworks default to human-oriented text, forcing agents into repeated trial and error. We need a set of default behaviors that does not disturb human users.

### Decision

Adopt the following contract:
1. **TTY detection**: on non-TTY, drop color, spinner, and interaction; data goes to stdout, diagnostics to stderr.
2. **Structured errors**: `code`/`message`/`hint`/`nextSteps[]`/`docsUrl`/`exitCode`.
3. **Fixed exit codes**: 0/1/2/3/4/5/6/7/130.
4. **Destructive guard**: non-TTY without `--yes` → fail immediately (exit code 2), never hang.
5. **Self-description**: `describe`/`schema`/`--help --json`.
6. **Explicit overrides**: `--format`/`--plain`/`--json` can override the automatic behavior.

### Rationale

1. Correct by default > making users configure it.
2. Only a stable contract can be relied on by scripts/agents.
3. The human experience is not harmed: interactive TTYs still get friendly text/color.

### Consequences

- Output format becomes a public API (and must be versioned).
- All commands inherit the global options by default.
- Tests must cover both TTY and non-TTY environments.

### Risks and Mitigation

| Risk | Mitigation |
|---|---|
| Breaks existing scripts | An explicit `--format text` restores the old behavior |
| Agents misjudge the output | `describe` provides the authoritative model |
| Disputes over exit codes | Documented + customizable via `RunesHelaError.ExitCode` |

### Related

02 §10, D-05.

---

## ADR-009 MCP: optional package, same-source generation, phased

**Status**: Accepted (2026-11-26; corresponds to D-11)

### Context

The MCP ecosystem is growing fast, but the specification is still evolving; the core package should not bind external dependencies.

### Options

| Option | Advantages | Disadvantages |
|---|---|---|
| A. Do nothing | Small scope | Misses the ecosystem |
| B. Generate the manifest only | Zero dependencies | Needs an external host |
| C. Manifest + stdio server | End-to-end | Dependency/specification risk |

### Decision

Adopt **C**, but in phases:
- V1.x starts with `mcp --manifest` (generating the tool list from the same model).
- Once stable, the optional `RunesHela.Mcp` package implements the stdio server.
- The core package introduces no MCP dependencies.

### Rationale

1. Same-source generation guarantees that the CLI and the MCP tools stay consistent.
2. An optional package isolates the risk of specification changes.
3. The manifest already satisfies most host integrations.

### Consequences

- Tool annotations (read-only/destructive/idempotent) come from `[Command]`.
- JSON-RPC lifecycle/cancellation/error mapping is required.
- Tests must cover tool schema consistency.

### Risks and Mitigation

| Risk | Mitigation |
|---|---|
| MCP specification changes | Adapter layer + versioning |
| Tools and CLI behave differently | Same execution pipeline |
| Security (agents call automatically) | Destructive guard + dry-run |

### Related

02 §11.5, D-11.

---

## ADR-010 Target framework: net10.0 single target, C# 14

**Status**: Accepted (2026-11-26; corresponds to D-02=C)

### Context

Zero overhead depends on `ISpanParsable<T>`, static abstract members, and modern language features. Both net8/net9 are out of support, so continued multi-targeting would only add build/test cost; net10 is the current LTS (supported until 2028-11).

### Decision

- Runtime: `net10.0` single target.
- Generator: `netstandard2.0`.
- Language: C# 14.
- No support for .NET Framework / netstandard2.0 runtime / Unity.

### Rationale

1. Follows D-02=C: use only a supported LTS and avoid EOL runtimes.
2. Modern APIs and C# 14 are enough to achieve zero overhead and the best experience.
3. Minimal CI matrix (1 TFM), reducing conditional compilation and release combinations.
4. Give up legacy platforms in exchange for architectural simplicity and performance.

### Consequences

- CI matrix of 1 TFM × 3 OS.
- No multi-target conditional compilation; generated code can use C# 14.
- Minimum documented requirement: .NET 10 SDK.
- net8/net9 users must upgrade or stay on older versions.

### Risks and Mitigation

| Risk | Mitigation |
|---|---|
| Excludes existing net9/net8 users | New-project positioning; provide a migration guide; re-evaluate multi-targeting if necessary |
| Users' SDKs are not upgraded | Explicit minimum requirement + install check + documentation guidance |
| C# 14 feature compatibility | Keep generated code readable; use only stable features |

### Related

02 §4.2, D-02.

---

## ADR-011 Versioning and packages: SemVer, same-version locking, ApiCompat

**Status**: Accepted (2026-11-26)

### Context

The generator and runtime are tightly coupled; public APIs and generated code need different compatibility strategies.

### Decision

- Package version: SemVer 2.0; `RunesHela` and `RunesHela.Generator` are released and locked at the same version.
- Public API: attributes, runtime types, MSBuild properties, JSON Schema fields, stable fields in help output.
- Non-public: generated code naming/layout, internal types, constants.
- Deprecation: `[Obsolete]` + diagnostic + at least 2 minor versions.
- Gates: ApiCompat, generation snapshots, schema compatibility, behavior compatibility.

### Rationale

1. Users depend on "behavior + public API", not on generated code.
2. Same-version locking prevents "generator/runtime mismatch".
3. Compatibility gates keep breaking changes out of CI.

### Consequences

- The release process needs a baseline package and a compatibility report.
- Major versions may break, but require a migration guide.

### Risks and Mitigation

| Risk | Mitigation |
|---|---|
| Generated code referenced directly by users | Forbidden by documentation; namespace isolation |
| False positives in compatibility gates | Allowlist + review |

### Related

ADR-001, 02 §17.

---

## ADR-012 Performance: budgeted, reproducible, CI-gated

**Status**: Accepted (2026-11-26)

### Context

"Extreme performance" that cannot be reproduced is just marketing. Performance must become an engineering constraint.

### Decision

1. Micro-benchmarks (BenchmarkDotNet) + cold start (hyperfine) + size (publish diff) + generator (driver benchmarks).
2. Budgets are written into the PRD/technical design; PRs exceeding thresholds fail.
3. Every release stores baseline JSON and hardware information.
4. Publish benchmark reports and allow third parties to reproduce them.
5. Hot paths forbid LINQ/regex/`Split`/boxing/exceptions.

### Rationale

1. Performance without gates degrades quietly.
2. Reproducibility builds trust.
3. Budgets make trade-offs discussible (e.g., "is this feature worth 200B?").

### Consequences

- CI needs dedicated runners/tolerances.
- Feature development must assess budgets.
- The release process gains a benchmarking step.

### Risks and Mitigation

| Risk | Mitigation |
|---|---|
| CI noise causes false positives | Multiple samples + tolerance + dedicated runner |
| Budgets block features | Tiered budgets (core/optional) |

### Related

02 §12, 04 checklist.

---

## ADR-013 Generator: incremental, call-site analysis, graceful failure

**Status**: Accepted (2026-11-26)

### Context

The generator is at the heart of performance and experience, and also the part most likely to drag down the IDE. Method-first needs call-site analysis; class-first needs symbol modeling.

### Decision

- Use `IIncrementalGenerator` + `ForAttributeWithMetadataName`.
- Models are immutable records + `EquatableArray`; never capture `Compilation`/`ISymbol`.
- Method-first uses `CreateSyntaxProvider` to match call sites and analyze lambda parameters.
- Each command generates its own file; routing/entry point is generated separately.
- Internal exceptions are captured as `RHL999`, a fallback entry point is generated, and the build does not blow up.
- The generator ships its own performance benchmarks and snapshot tests.

### Rationale

1. Incremental caching determines the IDE experience.
2. Separate files make navigation and diffs easier.
3. Graceful failure avoids "the compiler plugin crashes and the whole project is wrecked".

### Consequences

- Generator code needs strict equality design.
- Snapshot tests cover all artifacts.
- Generated code readability becomes an acceptance item.

### Risks and Mitigation

| Risk | Mitigation |
|---|---|
| Cache invalidation causes full re-runs | Equality tests + benchmarks |
| Many call-site analysis edge cases | Support simple lambdas first; diagnose unsupported shapes |
| Generated code bloat | Generate modules on demand (help/artifacts separated) |

### Related

ADR-002, 02 §6.

---

## ADR-014 Error model: structured, stable, renderable

**Status**: Accepted (2026-11-26)

### Context

Errors are the part of a CLI most often consumed by scripts/agents; string errors cannot be parsed reliably.

### Decision

- Unified `RunesHelaError`: `code`/`message`/`hint`/`nextSteps`/`docsUrl`/`exitCode`/`commandPath`/`details`.
- Error code naming: `RHL_<category>_<reason>` (e.g., `RHL_USAGE_UNKNOWN_OPTION`).
- The normal path throws no exceptions; errors are returned as values.
- Three renderings — human/Markdown/JSON — from the same data source.
- Unknown exception → exit code 1; stack traces only with `--verbose`/`RHL_DEBUG=1`.

### Rationale

1. Structured errors are a prerequisite for agent self-healing.
2. Stable error codes can be relied on by scripts.
3. Avoids the performance cost and noise of exception-based control flow.

### Consequences

- Error codes become a public API.
- All built-in errors need documentation pages.
- User-defined errors must follow the model.

### Risks and Mitigation

| Risk | Mitigation |
|---|---|
| Error code proliferation | Categories + review |
| Uneven `nextSteps` quality | Templates + review |

### Related

02 §7.6, ADR-008.

---

## ADR-015 Completion: static scripts + hidden dynamic protocol

**Status**: Accepted (2026-11-26)

### Context

Completion is an important part of the CLI experience; dynamic completion requires cooperation between the shell and the process; it must not pollute help or the Agent contract.

### Decision

- `completion <shell>` outputs a static script (commands/options).
- The hidden command `__complete` provides dynamic candidates (files/context).
- Four shells: bash/zsh/fish/pwsh; CI smoke tests.
- `__complete` is not exposed in `--help` and is not part of the documented main flow.

### Rationale

1. Static scripts cover 90% of scenarios at zero runtime cost.
2. The dynamic protocol isolates complexity and security risk.
3. Four shells cover mainstream environments.

### Consequences

- Completion scripts need escaping and snapshots.
- Dynamic completion must handle timeouts/errors (silent failure).
- Isolated from the Agent contract.

### Risks and Mitigation

| Risk | Mitigation |
|---|---|
| shell differences | Integration tests for all four shells |
| Dynamic completion injection | Strict escaping + never execute input |
| Maintenance cost | Generate rather than hand-write |

### Related

02 §11.3, D-05.

---

## ADR-016 License: Apache-2.0

**Status**: Accepted (2026-11-26; corresponds to D-07=B)

### Context

The license affects enterprise adoption, willingness to contribute, and patent risk. MIT is the most permissive but lacks an explicit patent grant; Apache-2.0 stays permissive while providing a patent grant and a patent retaliation clause, and is more familiar to enterprise legal teams.

### Options

| Option | Advantages | Disadvantages |
|---|---|---|
| A. MIT | Shortest, most permissive | No explicit patent grant |
| B. Apache-2.0 | Patent grant; enterprise-friendly; still permissive | Longer text; requires NOTICE |
| C. Dual licensing | Open source + commercial | Complex governance; community sensitive |

### Decision

Adopt **B: Apache-2.0**.

Specific requirements:
- The repository root contains `LICENSE` and `NOTICE`.
- NuGet package metadata declares `PackageLicenseExpression=Apache-2.0`.
- Source files are not required to carry an SPDX header, but releases must include the license and NOTICE.
- Contributions use the **DCO** (Developer Certificate of Origin); no CLA is required.
- Modifications/redistribution must retain the copyright, license, and NOTICE statements.

### Rationale

1. An explicit patent grant reduces enterprise adoption concerns.
2. Compatible with the mainstream licenses of the .NET OSS ecosystem.
3. Still a permissive license that does not hinder commercial use and redistribution.
4. The DCO lowers the contribution barrier and avoids CLA friction.

### Consequences

- The release checklist gains LICENSE/NOTICE checks.
- Third-party dependencies (currently zero runtime dependencies) need a license compatibility audit.
- Documentation and package metadata must uniformly declare Apache-2.0.

### Risks and Mitigation

| Risk | Mitigation |
|---|---|
| Omitted NOTICE | Release checklist + automated checks |
| Contributor employer IP disputes | DCO sign-off + explanation in the contribution guide |
| Combined use with GPL projects | Documentation explains compatibility boundaries; no legal advice provided |

### Related

D-07, 04 §11 release checklist.
