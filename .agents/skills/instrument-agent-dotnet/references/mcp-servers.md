# MCP servers: deriving the menu, and writing the file

> Called from **Phase 1**, **Phase 3** and **Phase 4** of `SKILL.md`.

MCP is Principle 4 of the Arkandia Method — direct access to tools — and the method calls it the
one without which the others stay a demo. A repository where the agent must be handed a work item
by copy-paste has an agent that works from a description of the system instead of the system.

**Do not present a fixed list.** Every candidate below is offered only when the repository gives a
signal for it, the same way the sibling skill derives architecture rules from the reference graph
instead of assuming Clean Architecture. A menu of eight servers for a repo that needs two is a
menu nobody reads.

---

## The detection table

| Server | Offer when Phase 1 found | Gives the agent |
|---|---|---|
| **Azure DevOps** | `.azdevops/`, `azure-pipelines.yml`, or a remote on `dev.azure.com` / `visualstudio.com` | Work items, repos, pipelines, wiki |
| **GitHub** | a remote on `github.com`, or `.github/workflows/` | Issues, pull requests, Actions runs |
| **Linear** | `linear` in `AGENTS.md`, `README.md` or `docs/`; or branch names carrying an issue prefix | Issues and projects |
| **Microsoft Learn** | always, in a .NET repository | Current official .NET / EF Core / Azure documentation |
| **Context7** | always | Up-to-date library documentation, version-aware |
| **DBHub** | a connection string or an EF Core provider call in the code | Read access to the actual schema and data |
| **Playwright** | a web project — Blazor, MVC, Razor Pages, an SPA under the repo | Driving a browser: navigation, forms, assertions |
| **Chrome DevTools** | the same signal as Playwright | The live page: console, network, performance, DOM |

**Microsoft Learn and Context7 are the two worth arguing for in a .NET repository**, and they
argue for themselves: they are the answer to a model writing an API that was renamed two releases
ago. Microsoft Learn is HTTP and needs no credential and no install, which makes it the cheapest
thing on this list.

**Playwright and Chrome DevTools are not alternatives.** Playwright drives the browser — it
navigates, fills, clicks, asserts. Chrome DevTools inspects the one that is already open — console
errors, failed requests, layout. Offer both when there is a web project; a team debugging a page
wants the second, a team writing end-to-end tests wants the first.

---

## The rules that do not vary

**No literal secrets. Ever.** Every credential is `${ENV_VAR}`. This file is committed — that is
the entire point of project scope — so a token written here is a token in the history, and
deleting it from the file later does not remove it from there. Put the variable *name* in
`README.md` so a teammate knows what to export.

**Every version resolved at write time, then pinned.** `npx -y <package>@<resolved version>`,
never a bare `npx -y <package>`. Resolve with `npm view <package> version` in the same run that
writes the file, and never copy a number out of this reference — it is wrong the week after it is
written, which is exactly why it is resolved rather than transcribed.

The bare form is worse than stale, not better than it. `.mcp.json` is committed and read on every
session start, so an unpinned entry executes whatever npm published since — under the user's own
permissions, on every machine that cloned the repo, with nothing in the history recording that the
code changed. A pin makes the bump a reviewable line in a diff. It is the same rule the sibling
skill applies to the gitleaks binary in CI, and for the same reason: "resolve, never hardcode"
means resolve **when the file is written**, not on every run.

The cost is honest and it is small: the pin goes stale, and a stale pin is a dependency bump —
re-resolve, re-run the flag check below (a bump is exactly when a flag disappears), commit both
together. Report every resolved version alongside the file, the way the sibling skill reports
resolved package versions.

**`${CLAUDE_PROJECT_DIR}` does not give you an absolute path here.** This is the trap that looks
solved and is not. Claude Code sets that variable in the **server's** environment, not in its own,
so in a project-scoped `.mcp.json` there is nothing to expand from at read time. The default is
mandatory — without it the entry is left unexpanded — but it is also the value you always get:

```
sqlite://${CLAUDE_PROJECT_DIR:-.}/src/Api/app.db   →   sqlite://./src/Api/app.db
```

**Verified against DBHub: SQLite rejects that.** `sqlite://./x.db` and `sqlite://sub/x.db` both
fail with `unable to open database file`; only `sqlite://` followed by an absolute path connects.
`${CLAUDE_PROJECT_DIR}` is substituted directly in a **plugin-provided** MCP config, which is
where that pattern comes from — a project-scoped file is not that.

**So treat a machine-specific path the way you treat a credential**: an environment variable with
no default, and the export line in `README.md`.

```json
"args": ["-y", "@bytebase/dbhub@{{DBHUB_VERSION}}", "--transport", "stdio", "--dsn", "${APP_DSN}"]
```

```bash
# README.md, alongside the tokens
export APP_DSN="sqlite://$(pwd)/src/Presentation/TheOffice.Api/theoffice.db"
```

`${CLAUDE_PROJECT_DIR:-.}` is still right for a server that accepts a **relative** path. Confirm
that the server does before using it.

**Expansion, verified against the current documentation:**

| Form | Expands to |
|---|---|
| `${VAR}` | the environment variable |
| `${VAR:-default}` | `VAR`, or the default when it is unset |

Expansion works in `command`, `args`, `env`, `url` and `headers`. Nowhere else — not in a server
name, not in a key.

**Merge, never replace.** An existing `mcpServers` object holds the team's work. Add your keys to
it. If a server with the same name already exists, that is a question for the user, not a
silent overwrite.

---

## Writing the file does not connect the servers

The most important sentence in this reference, because it looks like failure and is not.

A newly written `.mcp.json` leaves its servers at **`⏸ Pending approval`**. They connect when the
user trusts the workspace. And `enableAllProjectMcpServers` or `enabledMcpjsonServers` committed
in `.claude/settings.json` are **ignored in an untrusted folder** — a cloned repository cannot
approve its own servers, by design.

This is the same shape as the sibling skill's Azure Pipelines case: the file is correct, the
control is not yet active, and the remaining steps belong to the user. Report it that way:

> `.mcp.json` written with N servers — **pending approval**. To activate: run `claude` in this
> repository and accept the workspace trust dialog, then `/mcp` to confirm each server connects.

Never report an MCP server as installed on the strength of having written the file.

**This also means Phase 5 cannot verify MCP the way it verifies hooks.** Hooks take effect
immediately through the file watcher and can be triggered on the spot. MCP servers cannot. Say
which half of the run was proven and which half was only written — do not let one verification
table imply both.

---

## Config shapes

Take these as skeletons. Resolve the environment variable names against what the team already
uses; if `AZURE_DEVOPS_PAT` is already in their README, do not invent `ADO_PERSONAL_ACCESS_TOKEN`.

**Azure DevOps** — stdio, PAT via environment.

```json
"ark_azdevops": {
  "type": "stdio",
  "command": "npx",
  "args": ["-y", "@azure-devops/mcp@{{AZDEVOPS_MCP_VERSION}}", "${ADO_ORGANIZATION}", "--authentication", "pat"],
  "env": { "PERSONAL_ACCESS_TOKEN": "${ADO_PERSONAL_ACCESS_TOKEN}" }
}
```

**Microsoft Learn** — HTTP, no credential, no install.

```json
"ark_mslearn": { "type": "http", "url": "https://learn.microsoft.com/api/mcp" }
```

**Context7** — stdio. The key is optional; `${CONTEXT7_API_KEY:-}` degrades to the anonymous tier
rather than failing to start.

```json
"ark_context7": {
  "type": "stdio",
  "command": "npx",
  "args": ["-y", "@upstash/context7-mcp@{{CONTEXT7_MCP_VERSION}}"],
  "env": { "CONTEXT7_API_KEY": "${CONTEXT7_API_KEY:-}" }
}
```

**DBHub** — stdio, DSN-driven, one server for SQLite, PostgreSQL, MySQL, MariaDB and SQL Server.

```json
"ark_dbhub": {
  "type": "stdio",
  "command": "npx",
  "args": ["-y", "@bytebase/dbhub@{{DBHUB_VERSION}}", "--transport", "stdio", "--dsn", "${APP_DSN}"]
}
```

Phase 1 found the provider; that decides the DSN shape the README tells the user to export. These
are DBHub's own documented forms — read them back from `--help` rather than from this table if the
package has moved on:

| Provider found in code | `APP_DSN` |
|---|---|
| `UseSqlite` | `sqlite://` + an **absolute** path — `sqlite:///Users/you/repo/src/Api/app.db` |
| `UseNpgsql` | `postgres://user:pass@localhost:5432/db?sslmode=require` |
| `UseSqlServer` | `sqlserver://user:pass@localhost:1433/db?sslmode=disable` |
| `UseMySql` | `mysql://root:pass@localhost:3306/db?sslmode=require` |

SQLite is the one with a trap, and it is the common case in a workshop or a sample repo: the path
must be absolute, so it cannot live in the committed file. See the `${CLAUDE_PROJECT_DIR}` rule
above.

**Do not pass `--readonly`.** DBHub removed it; it now exits with `--readonly flag is no longer
supported` and the server never starts. Read-only is configured through a `dbhub.toml` with a
`[[tools]]` section. **Confirm the current mechanism before offering it** — and either way, say in
the report that an agent with a database connection reads whatever the DSN points at. If that DSN
can reach production, that is the user's decision to make knowingly, not a default to slip in.

**Playwright** and **Chrome DevTools** are stdio servers launched with `npx -y`; resolve their
package **names** as well as their versions at write time, rather than trusting a name written in
this file months ago.

### Confirming a flag

Pinning removes the *drift* — the entry cannot start resolving a different release behind your
back — but it does nothing about the gap between this reference and the version you just resolved.
A flag that worked when this file was written may be gone from the release you are about to pin,
and it is gone again the next time someone bumps the pin. **Check the flags against the exact
version in the args, every time that number changes.** Two ways to check, and one way that wastes
several minutes:

```bash
# usage, or the error that lists it
npx -y @bytebase/dbhub@<the version you resolved> --help < /dev/null 2>&1 | head -40
```

**Always redirect stdin from `/dev/null`.** An stdio MCP server started without it sits waiting
for a JSON-RPC handshake that never comes, produces no output, and hangs until something kills it.
That is not the flag being unsupported; that is the server working correctly.

If `--help` is not implemented, grep the resolved package instead:

```bash
find ~/.npm/_npx -path '*<package>/dist*' -name '*.js' | xargs grep -l -- '--the-flag'
```

Never write a flag into `.mcp.json` that you have not seen the package acknowledge. A wrong flag
does not degrade — the server exits and the whole entry is dead.

---

## Before you finish

- Confirm the file parses: `python3 -c "import json;json.load(open('.mcp.json'))"`.
- **Confirm every stdio server actually starts**, with the DSN or token the README will tell the
  user to export, and with `< /dev/null` so it exits instead of hanging:

  ```bash
  # The assignment goes on its own line. `VAR=x cmd --flag "$VAR"` does NOT work: the shell
  # expands "$VAR" before the assignment takes effect, so --dsn receives an empty value and the
  # server fails for a reason that has nothing to do with the DSN you meant to test.
  APP_DSN="<the real value>"
  # Capture before truncating. Piping straight into `head` hands you HEAD's exit status, so a
  # server that died on startup reports 0 — the same silent-success shape the hook scripts are
  # written against.
  OUT="$(npx -y @bytebase/dbhub@<pinned version> --transport stdio --dsn "$APP_DSN" \
    < /dev/null 2>&1)"; RC=$?
  printf '%s\n' "$OUT" | head -5; echo "exit: $RC"
  ```

  A connected server says so. Anything else — an unknown flag, a malformed DSN — is a finding now
  rather than a mystery when the user restarts Claude Code. This is the closest thing MCP has to
  the hooks' fire-and-revert, and it is worth the thirty seconds.
- Grep your own output for anything that looks like a credential rather than a `${VAR}`.
- List every environment variable you introduced, so Phase 6 can put them in `README.md`.
- If Phase 1 found a **literal** secret already in the file, report it as a finding. It is already
  in the history; removing it from the file does not remove it from there, and rotating the
  credential is the only real fix.
