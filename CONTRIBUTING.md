# Contributing

Thanks for helping improve RunesHela.

## Development

```text
dotnet restore RunesHela.slnx
dotnet build RunesHela.slnx -c Release -m:1
dotnet test tests/RunesHela.Tests -c Release
dotnet test tests/RunesHela.Generator.Tests -c Release
dotnet test tests/RunesHela.Integration.Tests -c Release
dotnet test tests/RunesHela.Performance.Tests -c Release
```

- Target framework: `net10.0` only. The generator targets `netstandard2.0`.
- Runtime and generated code must stay reflection-free, allocation-free on the
  binding hot path, and dependent on the BCL only.
- Code comments and XML documentation are written in English.
- Keep public API changes deliberate: update the API baseline in
  `tests/RunesHela.Tests/PublicApiTests.approved.txt` and the changelog.

## Pull requests

1. Add tests for behavior changes. Snapshot changes must include the received
   file in the PR description.
2. Run the full solution build and all test suites locally.
3. Sign off every commit (Developer Certificate of Origin):

   ```text
   git commit -s -m "feat: ..."
   ```

   The CI checks that each commit contains a `Signed-off-by:` trailer matching
   the author.

## Release

See `docs/release.md`. Releases are produced from tags through the
`release` workflow and require the `release` environment approval.
