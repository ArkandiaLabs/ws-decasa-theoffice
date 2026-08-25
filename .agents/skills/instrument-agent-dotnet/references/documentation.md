# Documentation the install invalidates

> Called from **Phase 6** of `SKILL.md`.

Installing this layer makes parts of the repository's documentation wrong in a particular way: the
setup instructions still work, but they no longer describe what happens. A new teammate follows
the README, gets a denial from a hook nobody mentioned, and concludes the tooling is broken.

**Update what exists. Do not create the doc pack** — that is `agent-context-dotnet`'s job. If a
document below is absent, say so in the report and move on.

---

## One fact, one home

Same rule as the sibling, and the same failure mode: not omission, but copying. The list of hooks
gets pasted into `AGENTS.md`, `README.md` and `docs/infrastructure.md` because each felt right. A
month later two are wrong, and whoever lands on a stale copy acts on it. For an agent, which copy
it read is arbitrary.

| Fact | Canonical home | Everyone else |
|---|---|---|
| Which hooks exist, what each blocks, which script implements it | `AGENTS.md` | link |
| Which MCP servers exist and what each gives access to | `AGENTS.md` | link |
| The environment variables the MCP servers need | `README.md` | `AGENTS.md` names the variable only |
| How to get Git Bash and Node (per OS) | `README.md` | link |
| The workspace-trust step that activates `.mcp.json` | `README.md` | link |
| That the audit log records full tool input | `README.md` | `AGENTS.md` states the one-line rule |
| That hooks run under Claude Code only | `README.md` | link |
| MCP server configuration detail, transports, auth | `docs/infrastructure.md` | link |
| Which generated files must not be hand-edited | `docs/dotnet.md` | `AGENTS.md` states it in one line; hook 7 enforces it |

Two tests before writing a paragraph. **Have I written this already in this run?** Replace it with
a link. **Does it already exist in the repo?** Link to that, even if the wording is not yours.

## The map

| Document | What goes stale | Priority |
|---|---|---|
| `AGENTS.md` / `CLAUDE.md` | The agent does not know the hooks or the servers exist | **Required** |
| `README.md` | Prerequisites, environment variables, workspace trust, what the log holds | **Required** |
| `docs/infrastructure.md` | MCP servers, transports, credentials | High |
| `docs/dotnet.md` | Generated files, the central package-version rule now enforced at edit time | Medium — **skipped entirely if hooks 6 and 7 were both declined** |
| `docs/claims-ledger.md` | New verifiable claims | Medium |
| `.gitignore` | `logs/` — and this one is written in Phase 4, not here | If hook 5 |

**Write all of it with `Edit` and `Write`, never a Bash heredoc.** These documents describe what
the secret guard blocks, so they contain `.env`, `id_rsa` and `secrets.json`. The guard tokenises
Bash commands and cannot tell a path from a mention of one, so a `cat <<EOF > AGENTS.md` carrying
that paragraph is denied. `Edit` and `Write` are outside its matcher, deliberately.

---

## `AGENTS.md` — required

The single most important edit, and for a reason specific to this layer: **a guard the agent does
not know about turns into a retry loop.** It attempts the denied action, reads a refusal, tries a
variation, and burns the turn. One paragraph converts that into picking the right path first.

Two sections. Keep both short — if `AGENTS.md` is a table of contents, it holds the rule and the
pointer, not the explanation.

```markdown
## Agent hooks

`.claude/settings.json` registers N hooks, implemented in `scripts/agent-hooks/`. They run under
Claude Code only.

- **secret-read-guard** — blocks reading .env, private keys, secrets.json. Use the committed
  `.example` file, or ask.
- **format-on-edit** — runs `dotnet format` on each .cs file after it is written. Do not spend
  turns on indentation or using order.
- **generated-files-guard** — blocks editing `packages.lock.json` and `Migrations/`. Change the
  source and regenerate.
```

```markdown
## MCP

`.mcp.json` registers <server> (<what it reaches>), authenticated with `$VAR` from the
environment. The token is never committed. See README.md for setup.
```

**Name the script for each hook.** The next person to debug a refusal needs the file, and the
agent reading `AGENTS.md` needs to know the refusal is policy rather than a bug.

If the file already has these sections — Phase 1 checked — **update them rather than appending a
second copy**.

## `README.md` — required

Written for a human on day one, so it carries what `AGENTS.md` does not: how to make any of this
work.

- **Prerequisites table**, per OS. **Git Bash is the entry that matters**, because it is the one
  that is not obvious:

  | Tool | macOS | Windows | Linux |
  |---|---|---|---|
  | Git Bash | ships | **ships with Git for Windows** | ships |
  | Node / `npx` (stdio MCP servers) | `brew install node` | `winget install OpenJS.NodeJS` | distro package |

  Say why: the hook scripts are bash, and on Windows Claude Code falls back to PowerShell when Git
  Bash is absent — at which point the hooks silently do nothing.

- **Environment variables** — one row per variable an MCP server expects, what it is, and where to
  get it. Never the value.

- **Activating the MCP servers** — the step that is easy to miss and looks like a failure: run
  `claude` in the repository, accept the workspace trust dialog, then `/mcp` to confirm. Say that
  a freshly cloned repository cannot approve its own servers.

- **The audit log**, if installed — where it is, that it is gitignored, and **that it records the
  full input of every tool call, which can include sensitive content**. One sentence, in the
  README, where a human reads it.

- **That the hooks are Claude Code's.** The scripts are portable shell; the registration is not.
  On Codex, Cursor or Copilot they do not run. State it here rather than letting somebody find out.

- **A "Try it" table** — one line per installed hook and per registered server, with the outcome
  to expect. This is the only part of the report worth repeating in two places. A teammate who was
  not in the session has no other way to tell a hook that is working silently from one that never
  fires, and both look identical. Keep it to one line each and list only what was installed:

  | Installed | Ask the agent / run | You should see |
  |---|---|---|
  | Secret guard | "read `.env`" | A refusal naming credential files |
  | Auto-format | Ask for a method added to a `.cs` file | It comes back formatted; `make lint` still passes |
  | Audit log | Any request, then `tail -3 logs/audit.log` | One line per tool call |
  | Any MCP server | `/mcp` | Connected, not `⏸ Pending approval` |

  Add the row for the environment variable each server needs, so the reader can see why a server
  is not connecting.

## `docs/infrastructure.md`

The MCP servers belong here in detail: each server, its transport (stdio via `npx`, or HTTP), what
it authenticates with, and what it can reach. Add the asymmetry explicitly — **which controls are
local-only**. The hooks run on a developer's machine and nowhere else; nothing in CI enforces
them. Six months later nobody remembers that.

## `docs/dotnet.md`

**Skip this file entirely unless hook 6 or hook 7 was installed.** Both of its additions describe a
check that now exists; with neither hook in place there is no change to record, and writing "these
files are generated" into a document that already says so is the copying this reference exists to
prevent. Say in the report that `docs/dotnet.md` was left untouched and why — a document listed in
the map and then silently skipped reads as an oversight.

Otherwise, one short addition per installed hook, if the file exists:

- **Generated files** *(hook 7 only)* — `packages.lock.json` and `Migrations/` are regenerated,
  not edited, and the guard now blocks hand-editing them. Name the commands that produce them.
- **Central package management** *(hook 6 only)* — the rule that a `PackageReference` must not
  carry `Version` is now checked at edit time rather than discovered at build time.

## `docs/claims-ledger.md`

If the repo has one, append the new claims with source and confidence: which hooks are installed,
which MCP servers are registered, that each hook was seen to fire. These are `high` confidence —
you built them and watched them work. **Record the MCP servers as written-and-pending, not as
verified**; that distinction is the whole reason the ledger exists.

---

## Rules

- **Update, do not rewrite.** These documents have authors. Edit the sections the install
  invalidated and leave the rest alone.
- **Do not create documents that do not exist.** Report the gap and suggest
  `/arkandia:agent-context-dotnet`.
- **Match the document's language.** These are the only files where you do not write English. A
  Spanish `AGENTS.md` edited in English becomes two documents in one. Write Spanish **with correct
  diacritics** — `versión`, `código`, `línea`, `configuración`. Accent-stripped Spanish reads as
  broken text, and these files go to a client.
- **Every command you document must be one you ran.** No aspirational instructions.
- **Report the documentation edits separately** from the configuration files, so the user can
  review them as prose.
