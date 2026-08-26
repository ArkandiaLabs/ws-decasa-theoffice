# Discovery checklist

> Called from **Phase 1** of `SKILL.md`. Silent. Glob, Grep, Read and read-only Bash only.

Everything the later phases need is in the repository. The point of this pass is to arrive at
Phase 3 with three or four real questions instead of fifteen, and to arrive at Phase 4 with no
value left to invent.

---

## 1. Confirm this is a .NET repository

`*.sln`, `*.slnx`, `*.csproj`, `*.fsproj`, `global.json`, `Directory.Build.props`.

If nothing matches, stop and say so. Write nothing.

## 2. The solution and the projects

```bash
ls *.sln *.slnx 2>/dev/null; find . -name '*.sln' -o -name '*.slnx' | grep -v /obj/
```

Record the **solution path relative to the repo root** — hook 2 needs it, and `dotnet format`
resolves `--include` against the solution's directory, so an absolute path or a path from the
wrong root silently matches nothing.

`.slnx` has been supported since SDK 9.0.200 and .NET 10 made it the default for `dotnet new sln`.
Both are current; use whichever the repo has. **If there is no solution file at all**, hook 2
falls back to the nearest `.csproj` walking up from the edited file — note it now, and say so in
the report.

## 3. The existing `.claude/` directory

```bash
ls -la .claude/ 2>/dev/null
```

Three separate questions, and confusing them is the most damaging mistake available in this skill:

- **`.claude/settings.json`** — committed, shared, and the only file you write. Read its `hooks`
  key. For each event already present, record the matcher and the script each handler points at.
- **`.claude/settings.local.json`** — personal and gitignored. **Read it if you must understand
  the setup, but never write to it.**
- **The `permissions` key**, in either file — the user's, not yours. You add exactly one key to
  settings.json: `hooks`.

Also look for scripts a previous hook set left behind: `.claude/hooks/`, `scripts/agent-hooks/`,
`scripts/hooks/`. If the repo already has a home for hook scripts, use it rather than opening a
second one.

## 4. `.mcp.json`

```bash
cat .mcp.json 2>/dev/null
```

Record every server already registered, and whether any credential is written **literally** rather
than as `${VAR}`. A literal token is a finding for the report, not something to fix silently — it
is already in the history and removing it from the file does not remove it from there.

Check `.claude/settings.json` for `enableAllProjectMcpServers` and `enabledMcpjsonServers` too:
they say which project servers the team has already approved.

## 5. The `Makefile`

```bash
grep -E '^[a-zA-Z_-]+:' Makefile 2>/dev/null
```

Do not assume the Arkandia target set. Record which of `format`, `lint`, `audit`, `check`, `test`
actually exist. `make audit` is what hook 4 should call when it is there; without it, fall back to
`dotnet list package --vulnerable --include-transitive` against the solution.

**Read the body of the target, not only its name.** A `format` target that runs something other
than `dotnet format` changes what hook 2 has to do.

Record the audit command as `{{SWEEP_COMMAND}}` for hook 4: `make audit` when the target exists,
otherwise `dotnet list package --vulnerable --include-transitive` against the solution path from
step 2. Run it once and time it — this hook fires before the first prompt of every session, and a
command that takes ten seconds is a finding, not a default.

## 6. The formatter and the style configuration

`.editorconfig` at the root and anywhere below it. Hook 2 is only worth installing when there is a
style to enforce: without `.editorconfig`, `dotnet format` applies compiler defaults and rewrites
files to a convention nobody agreed to. **If `.editorconfig` is absent, say so and recommend
`/arkandia:instrument-project-dotnet` first.**

Check whether the repo is currently clean against its own rules:

```bash
dotnet format <solution> --verify-no-changes 2>&1 | grep -cE 'error (WHITESPACE|IMPORTSSORTING|FINALNEWLINE|IDE0055)' || true
```

Append `|| true` and read the number: `grep -c` exits 1 on zero matches, which is the clean case.
A repo with hundreds of findings will see hook 2 rewrite unrelated files on the first few edits —
worth saying out loud before installing it.

**Then time the scoped command on one real file.** This is the number Phase 3 offers hook 2 on:

```bash
time dotnet format <solution> --include <one real .cs, repo-relative> --no-restore
```

A one-project sample and a six-project solution are not the same hook. Quote the seconds you
measured, the way step 5 quotes the audit command's — a measured cost is the difference between an
informed choice and one the team reverts in a week.

## 7. Hook preconditions

This is what decides which options Phase 3 shows.

| Hook | Precondition | Check |
|---|---|---|
| 3 | none | — |
| 4 | a solution that restores | `make audit` exists, or `dotnet list package` works |
| 5 | none, but `.gitignore` must be writable | `ls .gitignore` |
| 6 | central package management | `ls Directory.Packages.props` |
| 7a | lock files | `find . -name packages.lock.json -not -path '*/obj/*'` |
| 7b | EF Core migrations | `find . -type d -name Migrations -not -path '*/obj/*'` |

**A failed precondition is reported, not worked around.** Hook 6 in a repository without CPM would
fire on every legitimate `dotnet add package`; hook 7's lock branch without lock files guards a
file that does not exist. Say which you are hiding and why — silence leaves the team believing
they are covered.

## 8. Git facts

```bash
git remote -v
git symbolic-ref --short HEAD 2>/dev/null
git branch -r --format='%(refname:short)' | head -20
```

- **The remote host** drives the MCP menu: `github.com` → GitHub, `dev.azure.com` or
  `visualstudio.com` → Azure DevOps.
- **The default branch** is hook 3's `{{PROTECTED_BRANCHES}}`. Take it from
  `git symbolic-ref refs/remotes/origin/HEAD` when it is set, not from an assumption that it is
  `main`.
- **Long-lived branches** (`develop`, `release/*`) are the only reason to ask the user about
  protected branches at all.

## 9. The database provider

Hook-irrelevant, MCP-relevant. Find the connection string and the EF Core provider package:

```bash
grep -rl 'UseSqlite\|UseSqlServer\|UseNpgsql\|UseMySql' --include=*.cs . | head
grep -rh 'ConnectionStrings' --include=appsettings*.json . | head
```

**All four providers are detected, not just SQLite.** The `Use*` call names the engine, and the
engine decides the DSN shape the README will tell the user to export:

| Found | Engine | DSN shape |
|---|---|---|
| `UseSqlite` | SQLite | `sqlite://` + an **absolute** path |
| `UseNpgsql` | PostgreSQL | `postgres://user:pass@host:5432/db` |
| `UseSqlServer` | SQL Server | `sqlserver://user:pass@host:1433/db` |
| `UseMySql` | MySQL / MariaDB | `mysql://user:pass@host:3306/db` |

More than one is normal — SQLite for local development, something else in production. **Report
every one you found and ask which the agent should reach**, rather than picking the first match.
Pointing a database MCP server at production is a decision, not a detection.

SQLite is the one with a trap: the path must be absolute, which means it cannot be committed. See
`references/mcp-servers.md`.

## 10. Documentation and its language

`AGENTS.md`, `CLAUDE.md`, `README.md`, `docs/`. Phase 6 edits these and **must not switch their
language mid-document**. Record which language they are in. Everything you *create* is English;
this only affects the prose you edit.

Note whether `AGENTS.md` already has `Agent hooks` or `MCP` sections — if it does, Phase 6 updates
them rather than appending duplicates.

## 11. The team's operating system

Ask the user only if the repository gives no signal. What you are really establishing is whether
anyone is on Windows, because that makes Git Bash a hard prerequisite rather than a footnote.
`lefthook.yml`, CI runner images and a `.gitattributes` with line-ending rules all hint at it.

---

## What to report

A table — artifact, status, what you found:

| Artifact | Status | Found |
|---|---|---|
| `.mcp.json` | present / missing | which servers, whether any secret is literal |
| `.claude/settings.json` | present / missing | which events already have hooks, pointing where |
| `scripts/agent-hooks/` | present / missing | which scripts |
| `.editorconfig` | present / missing | current clean/dirty count |
| `Directory.Packages.props` | present / missing | → hook 6 offered or hidden |
| `packages.lock.json` | present / missing | → hook 7a offered or hidden |
| `Migrations/` | present / missing | → hook 7b offered or hidden |
| `.gitignore` | present / missing | whether `logs/` is already in it |

Plus the flat facts Phase 4 consumes: solution path, format command, audit command, default
branch, protected branches, MCP candidates with the signal that produced each, documentation
language, and whether Windows is in play.
