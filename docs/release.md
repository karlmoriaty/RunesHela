# Release process

RunesHela ships as a single NuGet package: the runtime library plus the generator
under `analyzers/dotnet/cs`. A symbol package (`.snupkg`) and an SPDX SBOM are
produced for every release.

## Versioning

- SemVer 2.0: `MAJOR.MINOR.PATCH`, optional pre-release suffix.
- Public API: attributes, runtime types, MSBuild properties, and generated
  `--help`/JSON Schema/MCP fields.
- API changes must update `tests/RunesHela.Tests/PublicApiTests.approved.txt` and
  `CHANGELOG.md`.
- Breaking changes require a major version and a deprecation window of at least
  two minor releases.

## Prerequisites (one-time)

1. NuGet.org account with trusted publishing configured for this repository and
   the `release` GitHub environment.
2. Repository secret `NUGET_USER` containing the NuGet.org username.
3. GitHub environment `release` with required reviewers.
4. Branch protection on `main`: required review, status checks (`build-test`,
   `pack`, `perf`, `trimmed`, `aot`), and 2FA for maintainers.

Publishing uses short-lived OIDC credentials (`NuGet/login@v1`); no long-lived
NuGet API key is stored.

## Dry run

Run the `release` workflow with:

- `version`: the intended version, for example `1.0.0`
- `dry_run`: `true`

The workflow builds, runs all test suites, packs the package and symbols,
generates the SBOM, and uploads artifacts without publishing. Download the
package artifact and verify it with the consumer smoke test from CI if needed.

## Publishing

1. Merge the release changes and confirm CI is green on `main`.
2. Update `CHANGELOG.md` with the release date.
3. Run the `release` workflow with `dry_run: false`.
4. Approve the `release` environment when prompted.
5. Verify NuGet.org shows the package, symbols, and the GitHub release with the
   SBOM attachment.
6. Validate a clean install:

   ```text
   dotnet new console -n Smoke
   cd Smoke
   dotnet add package RunesHela --version 1.0.0
   dotnet build -c Release
   ```

## Rollback

- **Unlist** the affected version on NuGet.org (does not delete it). Users who
  already restored it keep working; new installs fall back to the previous
  version.
- Publish a patched `PATCH` release with the fix. Do not reuse a version number.
- If the package is compromised, follow `SECURITY.md`, yank the version, and
  publish an advisory with the replacement version.

## Supply chain

- `dotnet build` is deterministic and SourceLink embeds repository metadata.
- CI verifies the package contains the runtime, XML docs, analyzer, README, and
  NOTICE before uploading.
- `build/generate-sbom.py` emits SPDX 2.3 for the package contents; attach it to
  the GitHub release.
- NuGet.org applies repository signing on publish. Optional author signing can be
  added with `dotnet nuget sign` and an organization certificate.
- Reviews use `CODEOWNERS`; every commit requires a DCO `Signed-off-by` trailer
  (`git commit -s`), checked by the `dco` workflow.
