# M5 Benchmark Report (Completion and Documentation Artifacts)

> Project: **RunesHela**
> Phase: M5 (T-043 ~ T-046)
> Date: 2026-11-28
> Verdict: **AC-11 / AC-12 pass.** The static completion scripts for the four shells, the hidden `__complete` dynamic completion, the Markdown documentation, and the roff man pages are all generated from the same command model; integration tests compare help/describe/completion/docs item by item, with zero artifact drift. **The AOT size delta is 291.6 KB, still above the 150 KB threshold, recorded as technical debt to be resolved before the M8 release.**

---

## 1. Scope and Status

| Task | Status | Notes |
|---|---|---|
| T-043 Static completion | ✅ | bash / zsh / fish / pwsh, candidates generated per command path |
| T-044 Dynamic completion | ✅ | `__complete <words...>`, prefix filtering, silent success when there are no candidates |
| T-045 Man / Docs | ✅ | `man` (roff), `docs` (Markdown) |
| T-046 Artifact consistency | ✅ | Integration tests compare the command and option sets of help/describe/completion/docs |

---

## 2. Built-in Commands

| Command | Output | Notes |
|---|---|---|
| `app completion bash` | bash script | Registered with `complete -F`, candidates selected by path `case` |
| `app completion zsh` | zsh script | `#compdef` + `_describe` |
| `app completion fish` | fish script | `complete -c`, with descriptions and short names |
| `app completion pwsh` | PowerShell script | `Register-ArgumentCompleter -Native` |
| `app __complete ...` | One candidate per line | Hidden; consumed by scripts/Agents |
| `app docs` | Markdown | Usage/arguments/options/default values/ENV/subcommands per command |
| `app man` | roff | `.TH`/`.SH`/`.TP`, viewable with `man -l` |

`describe`/`completion`/`docs`/`man`/`__complete` are all registered as reserved command names; a user command that collides with one of them receives RHL025.

---

## 3. Static Completion Implementation

- `CommandModel` produces a "path → candidates" table: candidates = visible subcommands + visible options (long name/alias/short name) + `--help` (`--version` additionally at the root) + `--yes`/`--no-input` for destructive commands.
- bash/zsh compute "the path preceding the current prefix" from `COMP_CWORD`/`CURRENT` and hand over to native shell matching once a `case` matches; any nesting depth is supported.
- fish uses `__fish_use_subcommand` / `__fish_seen_subcommand_from`; PowerShell computes the path from `CommandElements`.
- Escaping: fish escapes `\` and `'` inside single quotes; PowerShell doubles single quotes; command/option names are constrained by generation-time naming validation.

---

## 4. Dynamic Completion

```text
app __complete ""            # root command and global options
app __complete gre           # root candidates starting with gre
app __complete parent ""     # subcommands and options of parent
app __complete greet --c     # options starting with --c
```

- Implemented as a `switch (path)` in generated code, using generation-time candidate arrays, with no reflection.
- Prefix matching is `OrdinalIgnoreCase`; exit code 0 when there are no candidates.
- The candidate arrays share `CompletionData` with the static scripts, avoiding drift between two implementations.

---

## 5. Man / Markdown

- Markdown: `# App` → `## <path>` per command, argument/option tables, default values redacted (`[Secret]` → `***`), nested commands handled recursively.
- roff: `.TH` header, `.SH NAME/SYNOPSIS/COMMANDS`, a `.TP` and option section per command; renders with `man -l <file>`.
- Both use the same `CommandModel` as help, with consistent field semantics (type, required, default value, ENV, description).

---

## 6. Artifact Consistency (T-046 / AC-11)

Integration test `CompletionDocsAndDescribeStayConsistent`:

1. Parse `describe` and take the declared options of `greet` (excluding the built-in `help/yes/no-input`).
2. Assert that every `--option` appears in both the bash script and the Markdown documentation.
3. Assert that every root command appears in both the bash candidates and the Markdown headings.
4. Existing tests guarantee that the option sets of `describe` and `help --json` are identical.

> `schema` (configuration schema) extends the same comparison once M6 (T-048) lands.

---

## 7. Tests and Performance

| Suite | Count | Notes |
|---|---:|---|
| Runtime | 22 | Parsing/error output |
| Generator | 59 | Diagnostics, binding, snapshots (including completion/documentation artifact snapshots) |
| Integration | 49 | Four shell scripts, dynamic completion, docs/man, consistency |
| Performance | 7 | Allocation and latency smoke tests |
| **Total** | **137** | All passing |

- Completion/documentation are `const string` and static arrays, with zero parsing and zero reflection at run time.
- The existing budgets for help, routing, and binding are unchanged; the new artifacts only write constants when their command is invoked.

---

## 8. AOT

The CI matrix (linux-x64 / win-x64 / osx-arm64 + Trim/SingleFile + R2R) gains new smoke steps:

```text
AotCli completion bash
AotCli docs
AotCli man
```

| Metric | M4 | M5 | Budget | Verdict |
|---|---:|---:|---:|---|
| Startup delta p50 | -0.06 ms | **0.20 ms** | ≤ 1 ms | ✅ |
| Size delta | 279.6 KB | **291.6 KB (298,600 B)** | target ≤ 100 KB; failure threshold > 150 KB | ❌ exceeds threshold |

- Completion/documentation/man pages enter the artifact as constant strings, adding about 12 KB.
- M4 already recorded the size technical debt; before the M8 release it must converge to ≤ 100 KB uniformly (candidates: generate artifacts on demand, compress/split constants, trim via a linker descriptor).

---

## 9. Outstanding / Follow-up

- The non-TTY default Markdown **help** is still not enabled (`docs` already provides a Markdown artifact); M6 can evaluate whether to switch the default rendering.
- `schema` and the configuration schema land with M6 (T-048).
- Completion does not include value enums (for example the candidates of `--mode`); they can be generated from enum members on demand later.
