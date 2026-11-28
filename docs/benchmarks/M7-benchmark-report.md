# M7 Benchmark Report (MCP)

> Project: **RunesHela**
> Phase: M7 (T-050 ~ T-051, P2 optional)
> Date: 2026-11-28
> Verdict: **The M7 exit criteria are met.** `app mcp --manifest` is consistent with the command model; `app mcp --stdio` can be initialized by a JSON-RPC client, list tools, and invoke commands, with destructive confirmation and CLI errors mapped as-is. Zero third-party dependencies. **CI is fully green (run 46151302094); the AOT size delta is 605.0 KB and the startup delta is 0.04 ms.**

---

## 1. Scope and Status

| Task | Status | Notes |
|---|---|---|
| T-050 MCP Manifest | ✅ | Tool name/description/inputSchema/annotations, sourced from the same place as `describe` |
| T-051 stdio server | ✅ | JSON-RPC 2.0 line-by-line protocol; initialize / tools/list / tools/call / ping |

---

## 2. Manifest

```json
{
  "schemaVersion": "1.0",
  "kind": "mcp",
  "application": "AotCli",
  "tools": [
    {
      "name": "greet",
      "description": "Print an AOT-friendly greeting.",
      "inputSchema": {
        "type": "object",
        "properties": {
          "name": { "type": "string", "x-runeshela-position": 0 },
          "text": { "type": "string", "default": "Hello", "x-runeshela-option": "text" }
        },
        "required": [],
        "additionalProperties": false
      },
      "annotations": {
        "readOnlyHint": false,
        "destructiveHint": false,
        "idempotentHint": true,
        "openWorldHint": true
      },
      "x-runeshela-path": "greet"
    }
  ]
}
```

- Only **executable** commands (with a handler) that are not hidden are included; nested paths become `parent_child_leaf`.
- Argument property names default to lower-case kebab; when they collide with option names, an `arg-`/`opt-` prefix is added automatically, and the CLI mapping is guaranteed by the extension fields.
- Destructive commands additionally provide `yes: boolean`, mapped to `--yes`.
- `[Secret]` does not output default values; hidden options do not enter the schema.

---

## 3. stdio server

| Method | Behavior |
|---|---|
| `initialize` | Echoes the client `protocolVersion` (default `2024-11-05`), declares the `tools` capability and `serverInfo` |
| `notifications/initialized` | No response |
| `ping` | Empty result |
| `tools/list` | Returns the tool array from the manifest |
| `tools/call` | Maps arguments to CLI argv, executes the command, and captures stdout/stderr |

Example:

```text
→ {"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"greet","arguments":{"name":"Ada","count":2}}}
← {"jsonrpc":"2.0","id":3,"result":{"content":[{"type":"text","text":"Hello, Ada!\nHello, Ada!\n"}],"isError":false,"structuredContent":{"exitCode":0}}}
```

- Argument mapping: positional arguments are ordered by `x-runeshela-position`; options by `x-runeshela-option`; a boolean false turns into `--no-x` on a negatable flag, otherwise it is omitted.
- Arrays: JSON arrays are passed item by item; objects/nested arrays return `-32602`.
- Execution: `RunesHelaConsole` and `Console.Out/Error` are temporarily replaced to capture output; the interactive console is marked as input-redirected, so destructive commands must explicitly pass `yes: true` and never hang.
- Result: `content` text items + `isError` + `structuredContent.exitCode`; CLI errors (missing required, validation, confirmation) are not turned into protocol errors.
- Protocol errors: `-32700` parse failure, `-32600` invalid request, `-32601` unknown method, `-32602` unknown tool/argument.

---

## 4. Consistency with the CLI

Integration test `McpManifestMatchesDescribeModel`:

1. The tool-name set contains `greet`/`deploy`/`parent_child_leaf` and does not contain `pending`, which has no handler.
2. `greet.count`, positional arguments, arrays, and boolean types and default values come from the same model.
3. `deploy.environment` is `required`.
4. `purge` has `destructiveHint=true` and `idempotentHint=false`; ordinary commands are the opposite.

`McpStdio*` tests cover: initialization, no response to notifications, tool listing, successful invocation, destructive rejection/confirmation, missing required, unknown tool/argument/method, invalid JSON.

---

## 5. Tests

| Suite | Count | New in M7 |
|---|---:|---|
| Runtime | 22 | — |
| Generator | 64 | Manifest generation assertions (+1) |
| Integration | 65 | Manifest consistency + stdio end-to-end (+4) |
| Performance | 7 | — |
| **Total** | **158** | All passing |

---

## 6. AOT / Size

- The AOT smoke set gains `AotCli mcp --manifest` (three-platform matrix); CI passes.
- Measured (linux-x64):

| Metric | M6 | M7 | Budget | Verdict |
|---|---:|---:|---:|---|
| Startup delta p50 | 0.28 ms | **0.04 ms** | ≤ 1 ms | ✅ |
| Size delta | 560.7 KB | **605.0 KB (619,512 B)** | target ≤ 100 KB; failure threshold > 150 KB | ❌ exceeds threshold |

- A **44.3 KB** increase relative to M6, coming from the MCP manifest constants and the stdio server code.
- Known technical debt continues: M8 converges the size uniformly (configuration parsing and the MCP server are both candidates for optional trimming).

---

## 7. Differences from the Design

1. **Built in rather than a separate package**: design §4.1 reserved an optional `RunesHela.Mcp` package; M7 implements it as a built-in command of the generation entry point (like `describe`/`schema`), keeping a single package, zero dependencies, and artifacts from the same source. If the size needs to be reduced later, it can be trimmed via an application switch.
2. **Single-threaded sequential processing**: execution is currently line by line in order; concurrent calls and `notifications/cancelled` interruption are not supported; regular serial calls from MCP clients are unaffected.
3. **Conservative annotation defaults**: `CommandAttribute` has no `IsReadOnly`/`IsIdempotent` yet, so `readOnlyHint` is fixed to false and `idempotentHint` takes `!IsDestructive`; the metadata can be extended later.

---

## 8. Follow-up (M8)

- Release packaging, ApiCompat, SBOM/signing, and the documentation site.
- Converge the AOT size (configuration parsing and the MCP server are candidates for optional trimming).
