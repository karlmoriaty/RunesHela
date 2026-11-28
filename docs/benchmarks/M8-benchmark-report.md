# M8 Benchmark Report (Release and Community)

> Project: **RunesHela**
> Phase: M8 (T-052 ~ T-055, V1.0.0)
> Date: 2026-11-28
> Verdict: **V1.0.0 has been published to NuGet.org and GitHub Release.** The single package (runtime + generator + symbol package) can be packed and compiled/run by an independent consumer; the public API baseline and the DCO/release workflows are in place; the license, security policy, SBOM, and rollback process are complete. `System.Text.Json` has been removed from the runtime, and the AOT size delta drops from 605.0 KB to **432.7 KB** (−172.3 KB), still above the 150 KB threshold and listed as a V1.1 trimming item. **CI is fully green, the release dry-run passes, and the real release run 46159539773 completed the NuGet push and the GitHub Release attachment update.**

---

## 1. Scope and Status

| Task | Status | Notes |
|---|---|---|
| T-052 Version/compatibility/packaging | ✅ | Single package + analyzer, `.snupkg`, SourceLink, Apache-2.0, public API baseline |
| T-053 Documentation/migration | ✅ | Quickstart, migration guide, release process; M0–M8 report index |
| T-054 Security/governance | ✅ | LICENSE/NOTICE/SECURITY/CODEOWNERS/CONTRIBUTING, DCO, SBOM, OIDC publishing |
| T-055 Release rehearsal | ✅ | dry-run passes; **NuGet.org and GitHub Release v1.0.0 published** (run 46159539773) |

---

## 2. Packaging (T-052)

`dotnet pack src/RunesHela/RunesHela.csproj -c Release` produces:

| File | Size | Notes |
|---|---:|---|
| `RunesHela.1.0.0.nupkg` | 93,657 B | Runtime + documentation + generator + README/NOTICE |
| `RunesHela.1.0.0.snupkg` | 17,903 B | Symbol package |

Package layout:

```text
lib/net10.0/RunesHela.dll
lib/net10.0/RunesHela.xml
analyzers/dotnet/cs/RunesHela.Generator.dll
README.md
NOTICE
```

- `PackageLicenseExpression=Apache-2.0`; `RepositoryUrl`/`PackageProjectUrl` point to GitHub.
- SourceLink 10.0.404 (aligned with SDK 10.0.404) + `EmbedUntrackedSources`; CI uses `ContinuousIntegrationBuild=true`.
- Consumer smoke test: a temporary console project references the local package, and `dotnet run -- greet Ada --loud` outputs `HELLO, ADA!`, proving the analyzer takes effect from the package.
- The CI `pack` job validates the package contents, the SBOM, and the consumer build, and uploads artifacts.

---

## 3. Compatibility and Public API

- SemVer 1.0.0; every generated JSON artifact contains `schemaVersion`.
- New `PublicApiTests`: reflection lists all public types/members and compares them line by line against
  `tests/RunesHela.Tests/PublicApiTests.approved.txt`; changes require an explicit baseline update.
- Generated code and internal types are not part of the baseline; behavior contracts (exit codes, stdout/stderr, ordering) are covered by integration tests.
- Because no version had been published before, the 1.0.0 baseline is the first compatibility baseline.

---

## 4. Security and Governance (T-054)

| Item | Implementation |
|---|---|
| License | Full Apache-2.0 text + NOTICE |
| Vulnerability reporting | `SECURITY.md`: GitHub private reporting, 24h acknowledgement, 90-day disclosure |
| Code ownership | `.github/CODEOWNERS` |
| Contribution process | `CONTRIBUTING.md`: test commands, API baseline, DCO |
| DCO | `.github/workflows/dco.yml` checks the `Signed-off-by` of every PR commit |
| SBOM | SPDX 2.3, `build/generate-sbom.py` generates checksums and purls for the package contents |
| Release | `.github/workflows/release.yml`: dry-run, tests, packaging, SBOM, `NuGet/login@v1` OIDC publishing, GitHub Release |
| Rollback | `docs/release.md`: unlist + patch version + security advisory |
| Repository settings | 2FA, branch protection, and required reviews are recorded as release prerequisites (repository settings are not in code) |

Signing note: NuGet.org applies repository signing at publish time; author signing can be added to the release workflow through a certificate. The SBOM covers the package contents (zero third-party runtime dependencies) and does not include the build-machine toolchain inventory.

---

## 5. AOT Optimization

`System.Text.Json`, introduced by M6/M7, has been removed from the runtime:

- New built-in `JsonParser`/`JsonWriter` ([Json.cs](../../src/RunesHela/Json.cs)), supporting objects/arrays/scalars, escaping, trailing commas, a depth limit, and line/column errors.
- Configuration (M6) and the MCP server (M7) now use the built-in implementation; the runtime no longer references `System.Text.Json`.
- 11 new JSON unit tests (escaping, nesting, trailing commas, invalid input, round-trip).

| Metric | M7 | M8 | Budget | Verdict |
|---|---:|---:|---:|---|
| Startup delta p50 | 0.04 ms | **0.11 ms** | ≤ 1 ms | ✅ |
| Size delta | 605.0 KB | **432.7 KB (443,096 B)** | target ≤ 100 KB; failure threshold > 150 KB | ❌ exceeds threshold |

- A **172.3 KB** reduction relative to M7; Trim analysis shows the trimmed IL is only `RunesHela.dll` 44.5 KB + `AotCli.dll` 28.7 KB, and the remaining delta comes mainly from native code, exception handling, and framework-reachable members rather than string constants.
- Further convergence requires structural trimming: for example, a minimal profile that excludes `describe`/`schema`/`completion`/`docs`/`man`/`mcp`/configuration, or trimming features per application. V1.0 keeps full capability first; size optimization is scheduled for V1.1.

---

## 6. Documentation (T-053)

- [Quickstart](../quickstart.md): installation, declaring commands, running, configuration, MCP, AOT publishing.
- [Migration guide](../migration.md): mapping from System.CommandLine / Spectre.Console.Cli / ConsoleAppFramework / hand-written Main.
- [Release process](../release.md): versions, prerequisites, dry-run, publishing, rollback, supply chain.
- `CHANGELOG.md` records the 1.0.0 capabilities and known limitations; the README is updated for M8.

---

## 7. Tests

| Suite | Count | Changes in M8 |
|---|---:|---|
| Runtime | 34 | JSON parsing/writing +11, public API baseline +1 |
| Generator | 64 | — |
| Integration | 65 | — |
| Performance | 7 | — |
| **Total** | **170** | All passing |

Additional gates: package content validation, consumer compile-and-run, and SBOM generation in the `pack` job; commit signature checks in the `dco` job.

---

## 8. Differences and Follow-up

1. **SBOM scope**: currently a package-content-level SPDX (zero runtime dependencies) that does not include the SDK/toolchain; if a full build SBOM is needed, a dedicated tool can be wired into the release workflow.
2. **Signing**: relies on NuGet.org repository signing; author certificate signing requires a certificate provided by the release environment.
3. **API baseline**: 1.0.0 is the first baseline; subsequent versions must update the approved file in the PR.
4. **AOT size**: reduced to 432.7 KB but still above the 150 KB threshold; V1.1 plans a minimal profile (trimming artifact commands/configuration/MCP) and continues compressing native code.
