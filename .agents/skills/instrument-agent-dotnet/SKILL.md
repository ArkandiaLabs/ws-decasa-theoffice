---
name: instrument-agent-dotnet
description: Install the non-deterministic instrumentation layer in a .NET repository so the coding agent works with the team's tools and inside the team's limits — project-scoped MCP servers in .mcp.json, and a catalogue of Claude Code hooks in .claude/settings.json backed by portable shell scripts. The catalogue covers a secret read-guard, scoped auto-formatting, a dangerous-command blocker, a session-start advisory sweep, an audit log, and guards for central package management and generated files. Every hook is proven to fire before the run ends. Invoke with `/arkandia:instrument-agent-dotnet`.
disable-model-invocation: true
---

# instrument-agent-dotnet — Give the Agent Tools, and Limits

You are installing the **non-deterministic instrumentation** layer of the Arkandia Method: the
sensors whose engine is inference rather than computation.

Its sibling, `instrument-project-dotnet`, installs the deterministic half — the controls that
decide, in milliseconds and with no ambiguity, whether the code is fine. This skill installs the
half that applies **the team's judgement to the work that actually requires judgement**: which
systems the agent may reach, which files it may open, what happens to a file the moment it is
written.

Two artifacts, in this order:

| Order | Artifact | What it changes |
|---|---|---|
| 1 | `.mcp.json` | What the agent can **reach** — the team's trackers, docs and data, queried directly instead of pasted into chat |
| 2 | `.claude/settings.json` + `scripts/agent-hooks/*.sh` | What the agent **cannot get past** — checks that run whether or not it thought to run them |

MCP first, hooks second. MCP only adds capability; hooks take it away. Installing the additive
half first means that when a hook starts refusing things, the user knows which half to look at.

## The catalogue

Seven hooks. **1 and 2 are the default**; 3 to 7 are offered, and 6 and 7 only when the repository
actually contains the artifact they protect.

| # | Hook | Event | Blocks | Default |
|---|---|---|---|---|
| 1 | Secret read-guard | `PreToolUse` | **yes** | on |
| 2 | Format on edit | `PostToolUse` | no | on |
| 3 | Dangerous-command blocker | `PreToolUse: Bash` | **yes** | offered |
| 4 | Dependency sweep | `SessionStart` | no — reports only | offered |
| 5 | Audit log | `PreToolUse` (async) | no | offered |
| 6 | Package-version guard | `PostToolUse` | no — warns | offered **if** `Directory.Packages.props` exists |
| 7 | Generated-file guard | `PreToolUse` | **yes** | offered **if** lock files or `Migrations/` exist |

`references/hook-catalog.md` carries the full reasoning for each one. Read it before Phase 3.

## Philosophy (hold these throughout)

- **Resolve every version, then pin it.** Read the formatter, the solution path and the audit
  command out of the repo — never copy them from this file. The same rule decides the MCP
  packages, and it lands on `npx -y <package>@<resolved version>`: resolve with
  `npm view <package> version` when you write `.mcp.json`, then write that number down. A bare
  `npx -y <package>` re-resolves on every session, so a committed `.mcp.json` runs code tomorrow
  that nobody reviewed today, with the user's own permissions and no record of the change. This is
  the same reasoning the sibling skill applies to the gitleaks binary in CI: resolving from the
  source of truth is what "never hardcode" means, and it happens **when the file is written**, not
  on every run. Bump it like any other dependency — and re-check the flags when you do, since that
  is when they change.
- **Encode what the repo already does.** A hook that fires on a legitimate, everyday action is not
  a sensor — it is a bug with a policy attached, and the team disables the whole set to get past
  it. Hook 6 is the clearest case: without central package management, a `Version` attribute is
  the correct way to declare a package, so the hook must not exist there.
- **Discover before you write.** The formatter, the solution file, the audit target, the default
  branch, the tracker, the database provider — all of it is in the repository. Ask only what is
  genuinely not.
- **Merge, never clobber.** This matters more here than it does for the sibling: `settings.json`
  and `.mcp.json` routinely already exist and hold work that is not yours.
- **Never touch `.claude/settings.local.json`, and never touch `permissions`.** The local file is
  personal and gitignored; the permissions key is the user's decision, not the instrumentation's.
  You write exactly one key: `hooks`.
- **A hook nobody saw fire is not a hook.** Phase 5 triggers every installed hook on purpose and
  reverts. This is not optional and it is not a formality: a guard with a broken regex exits 0 and
  looks exactly like a guard that found nothing.
- **A short blocklist beats a long one.** Every pattern you add is a promise the team will not
  have to argue with it. A list that generates false positives gets the hook switched off, taking
  the patterns that mattered with it.
- **Hooks are not a security boundary.** They run with the user's shell and permissions and they
  match text, not intent. Say this in the report. A team that believes it has a sandbox will use
  it as one.
- **Spell out every acronym the first time, every time you speak to the user.** `CPM`, `DSN`,
  `TFM`, `PAT` are your vocabulary, not theirs, and a menu option is the worst place to learn one.
  Say "the repo's central package-version file (`Directory.Packages.props`)", not "CPM"; "the
  database connection string", not "the DSN"; "a personal access token", not "a PAT". `MCP` is the
  exception — it is the product's name — and it still gets one clause of explanation the first
  time it appears. This applies to menu labels, questions, progress lines and the final report; it
  does not apply to comments inside the files you write, which are read by people editing them.

- **Say what stays tied to Claude Code.** The scripts are plain shell and portable. The
  registration in `.claude/settings.json` is not: no other agent reads it today. State it plainly
  in Phase 6 rather than letting the user discover it on another tool.
- **Say where you are, in words the user can act on.** Announce each phase and each hook by what
  it buys, not by its number: not `Hook 2/7 — PostToolUse`, but
  `Hook 2/7 — auto-format: dotnet format on the file you just edited, about two seconds`.
- **Everything you write is in English** — scripts, comments, JSON, hook messages, progress
  output — regardless of the language of the conversation. The one exception is **prose
  documentation that already exists**: match the language of the file you are editing.
- **Never commit.** Not between phases, not to checkpoint. The only git writes are the
  break-and-restore of Phase 5, undone before the phase ends.

---

## Phase 1 — Discover (silent)

Do this without talking to the user. Glob, Grep, Read and read-only Bash only.

Work through `references/inspection.md`. It covers, in order: confirming this is a .NET
repository; the solution and its projects; the existing `.claude/` and whether `settings.json`
already registers hooks; existing `.mcp.json` servers; the `Makefile` and which targets it really
has; the formatter and the style config; which of the seven hooks' preconditions hold
(`Directory.Packages.props`, `packages.lock.json`, `Migrations/`); the git remote, default branch
and long-lived branches; the database provider; the documentation language; and the team's OS.

Then report the state as a table — artifact, status (`present` / `partial` / `missing`), what you
found — plus the environment facts the later phases depend on: solution path, format command,
audit command, default branch, detected MCP candidates, and whether Git Bash is a concern.

**Two findings change the menu and must be surfaced here, not silently applied:**

- A hook already registered on an event you are about to write to. Name it.
- A precondition that fails. Hooks 6 and 7 are not offered without their artifact — say which and
  why, so nobody is left believing they are covered.

---

## Phase 2 — Prerequisites

Check what the hooks depend on and report per OS. **Install nothing yourself.**

| Tool | Check | macOS | Windows | Linux |
|---|---|---|---|---|
| .NET SDK | `dotnet --version` | — | — | — |
| Git Bash | `bash --version` | ships | **ships with Git for Windows** | ships |

**`npx` is not checked here.** It is needed only by the stdio MCP servers, and which of those the
team wants is a Phase 3 question. Checking a prerequisite for a decision nobody has made yet
produces a warning the user cannot act on. It moves to Phase 3, immediately after the server
selection.

**Git Bash is the whole Windows story.** The hook scripts are bash, written to bash 3.2 with no
`jq`, so they run unchanged on macOS, Linux and Git Bash. Claude Code's shell-form hooks use Git
Bash on Windows when it is present and fall back to PowerShell when it is not — and these scripts
are not PowerShell. Anyone on Windows with Git for Windows installed already has it; anyone
without it needs it before the hooks do anything. Put it in the README prerequisites table.

If something is missing, print the install command and ask whether to continue writing the files
anyway — the configuration is still valid, it simply does not run yet.

---

## Phase 3 — Agree on scope

Ask **only what Phase 1 could not answer**. Use `AskUserQuestion` for the closed questions.

**Write every question for someone who does not know the vocabulary.** Never put an acronym in a
question without spelling it out and saying what it changes for them. State the consequence, then
let them choose.

1. **Which servers to register.** Offer only what Phase 1 derived from the repository — see
   `references/mcp-servers.md` for the detection rules and the config shape of each. For each one
   name the environment variable it needs, because that is the user's follow-up work. Default:
   the servers whose signal is unambiguous.

   Introduce the idea before the first option, in one clause: *these let the agent query your
   systems directly instead of you pasting things into the chat.*

   **`AskUserQuestion` allows at most four options per question, and detection can return eight.**
   Do not silently truncate. Group by what the server is for, ask only the groups that have
   candidates, and skip a group of one by folding it into the nearest neighbour:

   | Question | Candidates |
   |---|---|
   | "Which of your systems should the agent reach?" | Azure DevOps, GitHub, Linear, the database |
   | "Which reference sources?" | Microsoft Learn, Context7 |
   | "Should it be able to drive the browser?" | Playwright, Chrome DevTools |

   Each group is capped at four by construction. If a future candidate breaks that, split the
   group rather than dropping an option.
2. **`npx`, only now** — and only if at least one stdio server was chosen. `npx --version`;
   `brew install node` / `winget install OpenJS.NodeJS` / the distro package. Microsoft Learn is
   HTTP and needs nothing.

   **Resolve each chosen package's version in the same step**, with `npm view <package> version`.
   That number goes into the `args` in Phase 4 and into the report — the entry is
   `<package>@<resolved version>`, never a bare package name. Resolving here rather than in Phase 4
   also means a package that no longer exists under the name in `references/mcp-servers.md` is a
   finding now, while the menu is still open.

3. **Which hooks to install. One hook per option** — the same shape as the server menu above, and
   for the same reason: a reader compares options, not paragraphs. Do not bundle two hooks into
   one choice, and do not put caveats, false positives or design rationale in an option's
   description. Those belong in the Phase 6 report, where the user reads them once the thing
   exists.

   `AskUserQuestion` allows at most four options per question, so this is **two multi-select
   questions**, split by the only distinction the user actually cares about:

   **"Which blocking hooks?"** — these refuse a tool call.

   | Option | Description |
   |---|---|
   | Secret guard *(Recommended)* | Blocks reading `.env`, private keys, `secrets.json`. |
   | Dangerous commands | Blocks `rm -rf ~`, `sudo`, force-push to `<branch>`, `dotnet nuget push`. |
   | Generated files | Blocks hand-editing `packages.lock.json` and `Migrations/`. |

   **"Which reporting hooks?"** — these never refuse anything.

   | Option | Description |
   |---|---|
   | Auto-format *(Recommended)* | Formats the file you just edited. Measured here: `<N>s`. |
   | Dependency sweep | Lists packages with known vulnerabilities when a session starts. Measured here: `<N>s`. |
   | Audit log | Records every tool call to `logs/audit.log`. Not committed. |
   | Package versions | Warns when a project file pins its own package version instead of using the repo's central list. |

   **Each description is one or two plain sentences.** Name what it does and what it costs —
   nothing else.

   **Measure before you offer.** Timing the audit command turns "it is fast" into a number the
   user can weigh, and a number is the difference between an informed choice and one reverted a
   week later. Do the same for the auto-format hook: run the scoped `dotnet format --include`
   against one real `.cs` file in this repository and quote the seconds. A one-project sample and
   a six-project solution are not the same hook.

   Pre-check the secret guard and auto-format. **Hide 6 and 7 when their precondition fails**, and
   say — outside the menu, in prose — which you hid and why.
4. **The audit log, if chosen.** It records the full `tool_input` of every tool request. That is
   what makes it an audit trail rather than a counter, and it means the file can contain anything
   that passed through a tool — including a credential the read-guard did not catch. It is
   gitignored and never leaves the machine. Confirm explicitly; do not assume.
5. **Protected branches**, only if hook 3 was chosen and the repository has more than one
   long-lived branch. Default to the detected default branch.
6. **An existing hook on the same event**, only when Phase 1 found one. Default is to append —
   two matcher groups on one event both fire and there is nothing to resolve. Ask only when a
   handler already points at a script with the same name, which means it is the same hook, not a
   second one.

Do not ask about anything already visible in the repository.

---

## Phase 4 — Apply

**Before writing anything, confirm the working tree is clean** (`git status`). If it is not, stop
and tell the user. From here the tree is dirty by design, and Phase 5 can no longer tell your
edits from theirs.

Ignore the agent's own footprint — an untracked `.claude/`, `.codex/` or `skills-lock.json` is
tooling, not the user's work. Exclude those and say which you excluded:

```bash
git status --porcelain | grep -vE '^\?\? (\.claude/|\.codex/|skills-lock\.json)' | wc -l
```

**Read the count, never the exit code.** `grep` exits 1 when nothing matches, which is exactly the
clean case; treating that as failure inverts the check.

Write in this order.

### 1. `.mcp.json`

Follow `references/mcp-servers.md`. Merge into the existing `mcpServers` object; never replace it.
Every credential is an `${ENV_VAR}` reference — **a token written into this file is a token in the
history**. **An absolute path is not solved by `${CLAUDE_PROJECT_DIR}`** — Claude Code sets that
variable in the *server's* environment, not its own, so in a project-scoped file it always falls
back to its default. `${CLAUDE_PROJECT_DIR:-.}` yields `.`, a relative path, which a SQLite DSN
rejects. Treat a machine-specific path like a credential: an environment variable with no default,
and the export line in `README.md`.

**Never give a credential a default.** `${SOME_TOKEN:-}` converts "not set" into "empty token
supplied", moving a clear startup error into an authentication failure the user has to chase. A
bare `${VAR}` is right for anything the user must export; `:-` is for values with a genuinely
sensible fallback.

If the team chose no servers, still write `{"mcpServers": {}}` when the file is absent. The empty
skeleton is the committed, discoverable place the next server goes.

**Expect the editor to flag the file, and say so before the user sees it.** VS Code reads
`${APP_DSN}` as one of its own variables, does not find it, and underlines the line —
`Variable APP_DSN not found`. The file is correct; the variable is simply not exported yet, and
the warning goes away once it is. This is the **third** way "working" looks like "broken" in this
run, after a silent hook and a pending server. Name all three in the report.

**Writing the file does not connect the servers.** They stay at `⏸ Pending approval` until the
user trusts the workspace, and `enableAllProjectMcpServers` / `enabledMcpjsonServers` committed in
`.claude/settings.json` are **ignored in an untrusted folder** — a cloned repository cannot
approve its own servers. This is the MCP equivalent of the sibling's "written but not yet active"
for Azure Pipelines. Report it that way and give the user the two steps: run `claude` in the repo
and accept the trust dialog, then `/mcp` to confirm.

### 2. The hook scripts

Write `scripts/agent-hooks/`, starting with `_lib.sh` — every other script sources it.

| Template | Becomes | Only when |
|---|---|---|
| `templates/hooks/_lib.sh.template` | `scripts/agent-hooks/_lib.sh` | always |
| `templates/hooks/secret-read-guard.sh.template` | `…/secret-read-guard.sh` | hook 1 |
| `templates/hooks/format-on-edit.sh.template` | `…/format-on-edit.sh` | hook 2 |
| `templates/hooks/block-dangerous-bash.sh.template` | `…/block-dangerous-bash.sh` | hook 3 |
| `templates/hooks/dependency-sweep.sh.template` | `…/dependency-sweep.sh` | hook 4 |
| `templates/hooks/audit-log.sh.template` | `…/audit-log.sh` | hook 5 |
| `templates/hooks/cpm-guard.sh.template` | `…/cpm-guard.sh` | hook 6 |
| `templates/hooks/generated-files-guard.sh.template` | `…/generated-files-guard.sh` | hook 7 |

Each template opens with a header of instructions and its `{{PLACEHOLDER}}` list. Read it, resolve
the placeholders from Phase 1, **and delete the header before writing the file**.

Then `chmod +x scripts/agent-hooks/*.sh`, and check each one parses:

```bash
for f in scripts/agent-hooks/*.sh; do bash -n "$f" || echo "SYNTAX ERROR: $f"; done
```

**If hook 5 was chosen, add `logs/` to `.gitignore` before writing `audit-log.sh`.** In the other
order, the user's next `git add -A` publishes a file full of tool input.

### 3. `.claude/settings.json`

`templates/settings.json.template`, carrying only the handlers the team chose. Merge: keep every
key you did not add, keep every event you did not add, and **append** your matcher group to an
existing event's array rather than replacing it.

Then confirm the file still parses. A malformed `settings.json` does not fail loudly — it drops
the hooks:

```bash
node -e "JSON.parse(require('fs').readFileSync('.claude/settings.json'));console.log('settings.json parses')"
```

### Rules for this phase

- **Resolve, then pin** — `npx -y <package>@<resolved version>` from `npm view <package> version`,
  and every command read out of the repo. Report the resolved versions with the file.
- **Never write a secret**, in either file.
- **`make format` is deliberately not used by hook 2.** It formats the whole solution; the hook
  formats the file that changed. This is the only place in the Arkandia skills where a Makefile
  target exists and is intentionally bypassed, so put it in the report or the next reader will
  "fix" it.
- After writing, run each script once against a synthetic payload and confirm it exits 0 on the
  benign case. A guard that crashes on every tool call is worse than no guard.

---

## Phase 5 — Verify by breaking

Mandatory. Follow `references/verification.md`.

Installing a hook proves nothing; making it fire does. **This is the phase that matters most in
this skill**, because a hook's failure mode is silence: a guard with a broken pattern exits 0 and
is indistinguishable from a guard that found nothing.

Hook changes take effect immediately — Claude Code's file watcher picks up edits to
`settings.json` without a restart — so the hooks you just wrote are live in this session and you
can trigger them yourself.

| # | Hook | Trigger | Expected |
|---|---|---|---|
| 1 | Secret read-guard | `Read` a probe at `hooktest/.env` — **follow the five-step recipe in `verification.md`**, because creating *and* deleting the probe are denied too | Denied, with the reason naming credential files |
| 2 | Format on edit | `Write` a `.cs` file with unsorted usings and wrong indentation | The file comes back formatted |
| 3 | Dangerous-command blocker | `Bash: git reset --hard HEAD` | Denied |
| 4 | Dependency sweep | Run the script directly with a synthetic `SessionStart` payload | Advisories on stdout, or silence on a clean repo — never a failure |
| 5 | Audit log | Any tool call | A new line in `logs/audit.log` with the tool and its input |
| 6 | Package-version guard | Add `Version="1.0.0"` to a `<PackageReference>` | A warning naming the project and the count |
| 7 | Generated-file guard | `Edit` a `packages.lock.json` or a file under `Migrations/` | Denied, naming the command to use instead |

**Every trigger must be reverted**, and reverted from a snapshot rather than with `git checkout` —
most of these files did not exist before this run. Delete the test `.env` and the test `.cs`,
restore the `.csproj`, and finish with `git status` showing exactly what it showed before.

**Three traps specific to this skill, all from hook 1.** Reading the probe is denied *to you* —
that is the proof, not an obstacle, so do not disable the hook to get past it. **Creating and
deleting it are also denied**, because those commands take the probe as an operand and an operand
that names a credential file is exactly what the guard blocks: `rm -f .env.hooktest` is refused,
and so is `git check-ignore` on it. That one is the guard working, not a false positive. And
the probe is a credential-shaped file in a repo that may have no `.env` entry in `.gitignore` at
all. `verification.md` has the five-step recipe that handles all three — a throwaway directory,
gitignored first and removed whole. Follow it literally rather than improvising mid-phase.

**Do not report success with a hook that did not fire.** Fix it first.

---

## Phase 6 — Document and report

Follow `references/documentation.md`. **Update what exists; do not create the doc pack** — that is
`/arkandia:agent-context-dotnet`'s job. If a document is missing, report the gap and point there.

**Write this prose with Edit and Write.** This phase documents what the secret guard blocks, so
the text you are writing contains the strings `.env`, `id_rsa`, `secrets.json`. The guard skips a
heredoc body and the operands of an `echo`, so prose that merely names those files is no longer
denied — but the file a redirection opens is still checked, so is every operand that names a
credential, so is anything inside a `$(…)`, and a heredoc whose delimiter never arrives is scanned
rather than skipped. `Edit` and `Write` are outside the matcher entirely, deliberately: they do not depend
on the tokeniser being right.

Required, in order:

1. **`AGENTS.md`** — an `Agent hooks` section listing each installed hook, what it blocks and
   which script implements it, and an `MCP` section naming each server, what it gives access to,
   and the environment variable it needs. A guard the agent does not know about turns into a
   retry loop against a wall.
2. **`README.md`** — Git Bash in the per-OS prerequisites table, the environment variables the
   MCP servers expect, and the workspace-trust step that turns `.mcp.json` from written into
   active.
3. **The rest, if present** — `docs/infrastructure.md` (MCP servers, environment variables),
   `docs/dotnet.md` (the hooks that protect central package management and the lock files),
   `docs/claims-ledger.md` (the new claims).

### Give the user a way to try it

Everything installed here is invisible until it does something, and two of the three failure modes
— a hook that never fires, a server that never connects — look exactly like everything working.
So end the run with a short **Try it** section: one concrete line per installed hook and per
registered server, phrased as something to ask the agent or type, with the outcome to expect.

Keep it to one line each. Derive them from what was actually installed — never list a hook the
team declined:

| Installed | Ask the agent / run | You should see |
|---|---|---|
| Secret guard | "read `.env`" (create one first with Write) | A refusal naming credential files |
| Auto-format | Ask it to add a method to a `.cs` file with sloppy indentation | The file comes back formatted; `make lint` still passes |
| Dangerous commands | "run `git reset --hard HEAD`" | A refusal pointing at `git stash` |
| Dependency sweep | Start a new session | Vulnerable packages listed before your first prompt, or nothing on a clean repo |
| Audit log | Any request, then `tail -3 logs/audit.log` | One line per tool call, with its input |
| Package-version guard | Ask it to add a package version to a project file | A warning naming the project |
| Generated-file guard | Ask it to edit a file under `Migrations/` | A refusal naming `dotnet ef migrations add` |
| Any MCP server | `/mcp` | The server connected — not `⏸ Pending approval` |
| Microsoft Learn | "what does `dotnet list package --vulnerable` do, per the official docs?" | An answer citing learn.microsoft.com |
| DBHub | "how many rows are in each table?" | Real counts from the database |

**This section is mandatory and it goes in two places**: `README.md`, for the teammate who was not
in this session, **and as the last thing in your final message**, after everything else. Not a
mention that you wrote it into the README — the table itself, in the message. Everything above it
is reference the user reads once; this is the thing they do in the next two minutes, and a report
that ends on caveats leaves them with nothing to try.

Report the rest first, then close with it:

- files created and modified, configuration and documentation listed separately;
- **the audit log's contents**, if hook 5 was installed: it records full tool input, it is
  gitignored, and it can hold anything that passed through a tool;
- the hooks **not** installed and why — especially a precondition that failed, so nobody believes
  they are covered;
- the MCP servers as **written, pending approval**, with the two steps to activate them, and
  **the version pinned for each stdio package** — that number is a dependency the team now
  owns, and a pin nobody knows about is a pin nobody bumps;
- the known false positives you shipped, hook by hook — starting with `cp .env.example .env`
  being denied by hook 1;
- **what stays tied to Claude Code**: the scripts in `scripts/agent-hooks/` are plain shell and
  portable, but `.claude/settings.json` is Claude Code's alone. No other agent reads it today, so
  on Codex, Cursor or Copilot these hooks do not run. `.mcp.json`'s format is more widely shared,
  but approval and scoping are per-tool. Say it explicitly; do not let the user find out on
  another tool.

**Then the Try it table, last.** It is the closing beat of the run.

Do not commit. Leave the changes for the user to review.

## Troubleshooting

| Symptom | Almost always | Confirm with |
|---|---|---|
| A guard never fires, and never errors | `set -o pipefail` in the script. A filter that short-circuits (`grep -q`, `head -1`) raises SIGPIPE, the pipeline reports failure, and the guard falls through to allow — silently | Remove `pipefail`; the scripts use `set -u` alone for exactly this reason |
| A guard fires on things it should not | The pattern is matched against the whole stdin payload instead of a field — or against every word of `tool_input.command`, which denies prose: a commit message, a PR body, an `echo` that names the file | Match `tool_input.file_path`. For Bash, tokenise `tool_input.command` **and drop what nothing opens** — `echo`/`printf` operands, heredoc bodies, the value of `--message`/`--body`/`--title`, a `grep` pattern. Draw each drop narrowly, or it becomes a bypass: `hook-catalog.md` has the four that were once too wide. `secret-read-guard.sh` is the worked example |
| A path check never matches on Windows | `tool_input.file_path` arrives with backslashes, even under Git Bash | `json_path` in `_lib.sh` normalises them; use it instead of `json_raw` for anything path-shaped |
| Hooks do nothing at all on Windows | Git Bash is absent, so Claude Code fell back to PowerShell and the `.sh` never ran | `bash --version` in the user's shell |
| `settings.json` edits appear to be ignored | The file no longer parses. Malformed JSON drops the hooks rather than reporting an error | `node -e "JSON.parse(require('fs').readFileSync('.claude/settings.json'))"` |
| An MCP server stays at `⏸ Pending approval` | The workspace is not trusted. `enableAllProjectMcpServers` in a committed settings file is ignored until it is | Run `claude` in the repo, accept the trust dialog, then `/mcp` |
| `${CLAUDE_PROJECT_DIR}` expands to nothing in `.mcp.json` | It is set in the server's environment, not Claude Code's, so a project-scoped file always takes the default | Only `${CLAUDE_PROJECT_DIR:-.}` expands at all, and it gives `.`. For an absolute path, use a dedicated env var |
| A SQLite DSN fails with `unable to open database file` | The path is relative. `sqlite://./x.db` and `sqlite://sub/x.db` both fail; only an absolute path connects | `sqlite://` + an absolute path, supplied through an env var |
| An MCP server never starts, and nothing in `.mcp.json` looks wrong | A flag the pinned version does not accept — most often after someone bumped the pin without re-reading `--help` | `npx -y <package>@<pinned version> --help < /dev/null` — and always redirect stdin, or the server hangs waiting for a handshake |
| Hook 6 fires on every `dotnet add package` | It was installed in a repository without central package management, where a `Version` attribute is correct | Remove it. The precondition is `Directory.Packages.props` |
| The formatter takes tens of seconds per edit | It is running over the solution instead of the edited file | `--include <relative-path>` — and the path must be repo-relative |

## References

| Reference | Used by | What it covers |
|---|---|---|
| `inspection.md` | Phase 1 | The discovery checklist, step by step |
| `hook-catalog.md` | Phase 3, Phase 4 | The seven hooks: what each does, why, what it costs, what it misses |
| `mcp-servers.md` | Phase 1, Phase 3, Phase 4 | Deriving the server menu from the repo, and the config shape of each |
| `verification.md` | Phase 5 | The trigger-and-revert procedure, hook by hook |
| `documentation.md` | Phase 6 | Which documents the install invalidates, and their canonical homes |
| `templates/` | Phase 4 | The file skeletons |

## Rules

- Do NOT write to `.claude/settings.local.json`, and do NOT touch the `permissions` key anywhere.
- Do NOT replace an existing `hooks` or `mcpServers` block. Append.
- Do NOT write a credential into `.mcp.json`. Use `${ENV_VAR}`.
- Do NOT install a hook whose precondition the repository does not meet.
- Do NOT hardcode a package version, a solution path, a formatter or a branch name. Read them.
- Do NOT report success until every installed hook has been seen to fire and every trigger has
  been reverted.
- Do NOT commit or push.
- DO write `logs/` into `.gitignore` before the audit log exists, not after.
- DO say which hooks you did not install, and why.
- DO say, in the final report, that the hooks run only under Claude Code.
