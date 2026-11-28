# 06 Decision Log

> Project name: **RunesHela** (selected)
> Resolution date: **2026-11-26**
> Status: **Resolved, pending review; the project entered M0 on 2026-11-27**
> Related: [01 PRD](./01-product-requirements.md) · [02 Technical Design](./02-technical-design.md) · [03 Tasks](./03-implementation-plan.md) · [05 ADR](./05-architecture-decisions.md)

---

## 0. Resolution outcome

| ID | Decision | Your choice | Impact on documents |
|---|---|---|---|
| **D-01** | Product direction | **C: dual engine + agent-native** | Keep both entry points (class-first/method-first) + artifacts + Agent contract |
| **D-02** | Target framework | **C: net10.0 single target, C# 14** | TFM changed to `net10.0`; CI uses 1 TFM; no more multi-targeting warnings |
| **D-03** | Package shape | **B: minimal runtime + generator** | A single NuGet package containing the runtime + analyzer |
| **D-04** | API axis | **C: class-first + method-first** | Class-first is the main API; method-first ships as a separate package in V0.2 |
| **D-05** | MVP boundary | **B: standard MVP** | V0.1 scope unchanged (class-first + core + Agent basics) |
| **D-06** | Project name | **RunesHela** | Global rename: packages/namespaces/entry points/diagnostic prefix/URL |
| **D-07** | License | **B: Apache-2.0** | Adds LICENSE/NOTICE/DCO requirements |
| **D-08** | Help source | **C: attributes > doc comments > conventions** | Dual track + informational diagnostic on conflict |
| **D-09** | Output rendering | **B: in-house minimal + adapter layer** | Zero-dependency core; the Spectre adapter is an optional package |
| **D-10** | Documentation language | **B: English first, Chinese on demand** | English is the primary documentation; a Chinese version is optional |
| **D-11** | MCP depth | **C: manifest + stdio server** | Phased in V1.x; the core package introduces no dependencies |
| **D-12** | Internationalization | **C: pluggable resources in V1.x** | The MVP only guarantees that user-facing text can be Chinese |
| **D-13** | Interactive prompts | **B: separate package in V1.x** | The core only defines the input abstraction |
| **D-14** | Telemetry/governance | **A: zero telemetry + sponsorship** | No data collected by default |

**Conclusion**: the design baseline has been updated according to the choices above; **the final freeze awaits your review approval**. Defaults not listed here continue to follow the recommendations in 01/02.

---

## 1. Review notes (D-02 settled; 1.2/1.3 still to execute)

### 1.1 D-02: net10.0 LTS single target (reviewed)

- .NET 10 is the current **LTS**, supported until **2028-11**.
- .NET 9 (STS) went out of support in **2026-05**; .NET 8 LTS went out of support in **2026-11**.
- Therefore we adopt **net10.0 single target + C# 14**, with no multi-targeting.
- Impact: the CI matrix drops from 2 TFMs to 1 TFM; generated code can use C# 14; net8/net9 users must upgrade.
- Conclusion: **no open items**; D-02 is final.

### 1.2 D-06: name search

RunesHela has already been written into the documents as the project name. Before release the following work is still required:
1. NuGet package name search: `RunesHela`, `RunesHela.Cli`;
2. Trademark search (target markets);
3. Domain/social account availability (`runeshela.dev` is currently used as a placeholder in the documents);
4. Research of GitHub projects with the same name.
If there is a conflict, the alternatives are: `Keel`, `Lintel`, `Clio`.

### 1.3 D-07: Apache-2.0 compliance actions

- Repository root: `LICENSE` + `NOTICE`.
- NuGet: `PackageLicenseExpression=Apache-2.0`.
- Contributions: DCO (no CLA).
- Releases: retain copyright/license/NOTICE; modified files must be declared.
- Third-party dependencies (currently zero runtime dependencies) need a license compatibility audit.

---

## 2. Item-by-item decision log

### D-01 Product direction: dual engine + agent-native

**Decision: C**

**Context**: Pure performance would compete head-on with ConsoleAppFramework with weak differentiation; pure elegance would be homogenized with CliFx/Spectre. The intersection of the two (the compile-time model) is an empty space.

**Alternatives considered**:
- A Pure performance: small scope, but a low star ceiling and easily seen as a CAF shadow.
- B Pure elegance: fast to implement, but lacks hard metrics and a technical moat.
- C Dual engine + agent-native: strongest differentiation, and can be phased.

**Rationale**:
1. The same compile-time model can serve both class-first and method-first, with controllable marginal cost.
2. The Agent contract is a gap newly opened in 2026, with strong virality.
3. Same-source artifacts (help/completion/schema/docs/MCP) form a long-term moat.

**Consequences**: The architecture must revolve around the compile-time model; the MVP needs strict scope control.

**Risks**: Scope creep. Mitigation: V0.1 covers only class-first + core + Agent basics.

---

### D-02 Target framework: net10.0 single target, C# 14

**Decision: C**

**Context**: Zero overhead depends on `ISpanParsable<T>`, static abstract members, and modern language features; net8/net9 are both out of support, so a single target must be determined.

**Alternatives considered**:
- A net8.0 and up: widest coverage, but net8 is out of support.
- B net9.0 and up: covers net9/net10, but net9 is out of support and it increases the matrix.
- C net10.0 only: the only supported LTS, smallest matrix, allows C# 14.

**Rationale**: Per your final choice, net10.0 is the only target, with C# 14; the generator remains `netstandard2.0`.

**Consequences**:
- Runtime and optional packages: `net10.0`.
- CI matrix: 1 TFM × 3 OS.
- Minimum documented requirement: .NET 10 SDK.
- Generated code can use C# 14; no multi-target conditional compilation is needed.

**Risks**: Excludes net8/net9 users. Mitigation: new-project positioning + migration guide; re-evaluate multi-targeting if existing users are truly needed in the future.

---

### D-03 Package shape: minimal runtime + generator

**Decision: B**

**Context**: A pure analyzer can achieve zero assembly dependencies, but class-first needs public attributes/entry points/error model; MCP/Testing need a shared runtime.

**Alternatives considered**:
- A Pure analyzer: smallest package, but code bloat and hard to fix.
- B Minimal runtime + generator: stable API, composable, small generated code.
- C Hybrid: best of both but double the cost.

**Rationale**: A single NuGet package contains the runtime assembly + the `RunesHela.Generator` analyzer; the dependency graph is BCL only.

**Consequences**:
- Package size budget ≤ 60 KB/TFM.
- Generator and runtime are locked to the same version.
- "Zero dependency" means "zero third-party runtime dependencies".

**Risks**: Users misread "zero dependencies". Mitigation: state the definition clearly on the first screen of the README.

---

### D-04 API axis: class-first + method-first

**Decision: C**

**Context**: Class-first suits real projects; method-first is the virality hook for a "one-line CLI", but call-site generation is complex.

**Alternatives considered**:
- A Class-first only: simple and uniform, but lacks a virality hook.
- B Method-first only: minimal, but weak for complex projects.
- C Both, phased: broad coverage, isolated risk.

**Rationale**: V0.1 has class-first as the main API; V0.2 provides method-first through `RunesHela.Script`, with a separate entry point to avoid conflicts with runtime types.

**Consequences**:
- Two entry points: `RunesHelaApp` (class-first)/`RunesHelaScript` (method-first).
- Method-first does not promise complex DI/deep nesting.
- Shared parsing/binding/snapshot tests.

**Risks**: Inconsistent behavior. Mitigation: shared test matrix + explicit scope boundaries.

---

### D-05 MVP boundary: standard MVP

**Decision: B**

**Context**: Scope is the biggest risk; the first release must have enough selling points, but must not be left unfinished.

**Alternatives considered**:
- A Minimal MVP: fast, but lacks completion/schema/configuration, so there are not enough selling points.
- B Standard MVP: class-first + nesting + ENV + validation + filters + DI + `describe` + `--yes` + non-TTY contract + templates.
- C Full first release: complete features, but a long cycle and high quality risk.

**Rationale**: The standard MVP is enough to support the "elegant + agent-native" narrative, and it can be tried out in real projects.

**Consequences**: V0.1 scope is locked; method-first/completion/configuration/man pages/MCP are phased.

**Risks**: The scope is still somewhat large. Mitigation: strictly follow the task DAG in 03; nothing outside P0 jumps the queue.

---

### D-06 Project name: RunesHela

**Decision: RunesHela**

**Context**: The name affects NuGet, the repository, the domain, trademarks, search, and community recall.

**Alternatives considered**:
- Truss (early candidate, abandoned): a good structural-support metaphor, but still needs searching.
- Keel / Lintel / Clio / Armature: each has its own metaphor and conflict risk.
- RunesHela (your choice): distinctive, searchable, brandable.

**Rationale**: A distinctive name helps search and trademarks; the "compile-time/runes/structure" imagery supports a brand story.

**Consequences**:
- Packages: `RunesHela`, `RunesHela.Script`, `RunesHela.Mcp`, `RunesHela.Testing`, `RunesHela.Templates`, `RunesHela.Analyzers`.
- Namespace: `RunesHela`.
- Entry points: `RunesHelaApp`, `RunesHelaScript`.
- Diagnostic prefix: `RHL` (compile-time `RHL001`; runtime `RHL_USAGE_*`).
- Template: `dotnet new runeshela-cli`.
- Documentation domain placeholder: `runeshela.dev`.

**Risks**: Name/trademark conflicts. Mitigation: the search checklist in §1.2.

---

### D-07 License: Apache-2.0

**Decision: B**

**Context**: MIT is the most permissive but has no explicit patent grant; Apache-2.0 provides a patent grant and a retaliation clause, and is more familiar to enterprise legal teams.

**Alternatives considered**:
- A MIT: shortest, lowest adoption barrier.
- B Apache-2.0: patent grant, enterprise-friendly.
- C Dual licensing: flexible commercialization, but complex governance.

**Rationale**: An explicit patent grant reduces enterprise concerns; it remains a permissive license; the DCO lowers the contribution barrier.

**Consequences**: LICENSE/NOTICE, package metadata, DCO, release checks.

**Risks**: Missing NOTICE/contributor IP. Mitigation: release checklist + DCO + contribution guide.

---

### D-08 Help source: attributes > doc comments > conventions

**Decision: C**

**Context**: Doc comments come naturally; attributes are explicit; conventions are zero-config. Precedence must be explicit to avoid ambiguity.

**Alternatives considered**:
- A Comments first: clean, but local functions are unsupported.
- B Attributes first: stable, but noisy.
- C Dual track: flexible, but requires defined precedence.

**Rationale**: Precedence is **attributes > doc comments > conventions**; conflicts produce an informational diagnostic, not an error.

**Consequences**: The generator reads `GetDocumentationCommentXml()`; missing comments degrade to the `RHL005` informational message.

**Risks**: Comment parsing is affected by compiler configuration. Mitigation: multi-IDE/CI testing + graceful degradation.

---

### D-09 Output rendering: in-house minimal + adapter layer

**Decision: B**

**Context**: Spectre.Console excels at rendering, but has heavy dependencies, is 0.x, and needs AOT care; the core must have zero third-party dependencies.

**Alternatives considered**:
- A Depend on Spectre: effortless and beautiful, but violates zero dependencies.
- B In-house minimal + adapter layer: light, controllable, composable.
- C Plain text: lightest, but a weak experience.

**Rationale**: The core ships text/markdown/json/table; `RunesHela.Rendering.Spectre` is the optional adapter package.

**Consequences**: Must implement CJK width, color capability, and table alignment; the adapter layer tracks Spectre versions.

**Risks**: In-house rendering is not pretty enough. Mitigation: minimal style + optional Spectre.

---

### D-10 Documentation language: English first, Chinese on demand

**Decision: B**

**Context**: International OSS adoption needs English; as the reviewer you are more comfortable with Chinese.

**Alternatives considered**:
- A Chinese first: fast review, but slow international reach.
- B English first: aids adoption; Chinese provided on demand.
- C Synchronized bilingual: broad coverage, but high maintenance cost.

**Rationale**: English is the primary documentation and README; a Chinese version is an optional translation that does not block releases.

**Consequences**: Documentation is written primarily in English; a Chinese version can follow later; comments in sample code may be Chinese or English as needed.

**Risks**: Onboarding cost for Chinese users. Mitigation: keep a Chinese Quickstart (optional).

---

### D-11 MCP: manifest + stdio server (phased)

**Decision: C**

**Context**: The MCP ecosystem is growing fast, but the specification is still evolving; the core package should not bind external dependencies.

**Alternatives considered**:
- A Do nothing: smallest scope.
- B Manifest generation only: zero dependencies, but needs an external host.
- C Manifest + stdio server: usable end-to-end.

**Rationale**: V1.x starts with `mcp --manifest`; once stable, the optional `RunesHela.Mcp` package implements the stdio server.

**Consequences**: Tool annotations come from `[Command]`; JSON-RPC lifecycle/cancellation/error mapping is required.

**Risks**: Specification changes. Mitigation: adapter-layer isolation + optional package.

---

### D-12 Internationalization: pluggable resources in V1.x

**Decision: C**

**Context**: The MVP does not need framework message internationalization; but it may be needed in the future.

**Alternatives considered**:
- A English only: simplest.
- B Built-in Chinese and English: good experience, double the maintenance.
- C Pluggable resources: English by default, user-replaceable.

**Rationale**: The MVP only guarantees that user-facing text can freely use Chinese; turning framework messages into resources is deferred to V1.x.

**Consequences**: A resource interface must be designed; not done now.

**Risks**: Retrofit cost later. Mitigation: manage messages centrally, no scattered string literals.

---

### D-13 Interactive prompts: separate package in V1.x

**Decision: B**

**Context**: Interactive prompts conflict with AOT/the non-interactive contract, so the core should not build them in.

**Alternatives considered**:
- A Do nothing: safest, but no wizard experience.
- B Separate package: does not pollute the core.
- C Built-in: works out of the box, but complex.

**Rationale**: The core only defines `IInput` and the `--no-input` semantics; `RunesHela.Prompts` is provided later.

**Consequences**: No prompts in the MVP; destructive operations are guarded by `--yes`.

**Risks**: Users expect built-in prompts. Mitigation: documentation states the phasing clearly.

---

### D-14 Telemetry and governance: zero telemetry + sponsorship

**Decision: A**

**Context**: Telemetry brings trust and compliance costs; the project needs trust most in its early days.

**Alternatives considered**:
- A Zero telemetry + sponsorship: highest trust.
- B Opt-in telemetry: data-driven, but requires disclosure.
- C Open core/commercial edition: revenue, but risk of community division.

**Rationale**: No data is collected by default; support comes from GitHub Sponsors/consulting/training.

**Consequences**: No telemetry code; a simple privacy policy; releases contain no network reporting.

**Risks**: Limited revenue. Mitigation: prioritize sponsorship and consulting; do not cripple features.

---

## 3. Document change list for this round

| Document | Change |
|---|---|
| README | Name/status; the 14-decision table; D-02 re-reviewed as net10.0 single target; Apache-2.0; 16 ADRs |
| 00 | Name; license (Apache-2.0); naming risk update |
| 01 | Name; TFM changed to `net10.0`; C# 14; NFR-09; AS-01; license constraints; resolved-decision table |
| 02 | Name/package names/type names/diagnostic prefix; TFM table; net10.0 single-target note; environment variables/generated identifiers |
| 03 | Name; template name; T-052 license; T-053 English first; T-054 DCO |
| 04 | Name; net10.0 acceptance; license compliance items; release checklist |
| 05 | All ADRs moved to Accepted; ADR-010 updated to net10.0 single target; ADR-016 license added |
| 06 | This document rewritten as a decision log; D-02 finally changed to C |

---

## 4. Next steps

1. The documents have been reviewed and approved; **M0 is complete (2026-11-27)**, see the [M0 benchmark report](./benchmarks/M0-benchmark-report.md).
2. Move on to **M1**: model IR, diagnostics, type system, incremental pipeline, help/JSON snapshots.
3. The name search (§1.2) and Apache-2.0 compliance (§1.3) remain pre-release items.

---

## 5. Sign-off

```text
Review conclusion: approve / conditionally approve / do not approve
Reviewer:
Date:
Items requiring changes:
Approved to start development: yes / no
```
