# Security policy

## Supported versions

| Version | Supported |
|---|---|
| 1.x | Yes |
| Pre-release | Best effort |

## Reporting a vulnerability

Please do **not** open a public issue for security problems. Use GitHub's private
vulnerability reporting:

1. Open the repository's **Security** tab.
2. Select **Advisories** and create a draft advisory.
3. Include affected versions, a minimal reproduction, and the impact.

You can expect an acknowledgement within 24 hours. We will confirm the issue,
prepare a fix on a private branch, and publish an advisory together with the
patched release. Please allow up to 90 days before public disclosure.

## Scope

- Command-line parsing and binding in generated code.
- Secret redaction in help, errors, `describe`, configuration, and MCP output.
- Configuration file handling, including permission warnings.
- MCP stdio server input handling.
- Supply chain: package contents, SBOM, signing, and provenance.

Runtime code depends on the BCL only; third-party test and build packages are
not shipped in the `RunesHela` package.
