# Verify by firing

> Called from **Phase 5** of `SKILL.md`. Mandatory.

Installing a hook proves nothing. Making it fire does.

**This phase carries more weight here than in the deterministic half**, because a hook's failure
mode is silence. A build gate with a broken configuration fails the build; a guard with a broken
pattern **exits 0** and is indistinguishable from a guard that looked and found nothing. Nobody
discovers it until the day it was supposed to stop something.

Hook changes take effect immediately — Claude Code's file watcher picks up edits to
`settings.json` without a restart — so everything Phase 4 wrote is live in this session and you
can trigger it yourself.

---

## The procedure

For each installed hook:

1. Note the exact file you are about to create or change.
2. **Snapshot anything pre-existing** you are about to edit (`cp <file> <tmp>/<file>.bak`). One
   hook, one trigger, one restore.
3. Trigger it, **through the real tool call** wherever possible — a `Read`, a `Write`, a `Bash`.
   Running the script by hand with a synthetic payload proves the script works; it does not prove
   the registration in `settings.json` is right, and a wrong matcher is the more common mistake.
4. Confirm the expected outcome, and that the message names the problem.
5. Restore.

**Restore from the snapshot, not with `git checkout`.** Nearly everything here is a file this run
just created: `git checkout -- scripts/agent-hooks/secret-read-guard.sh` either fails because the
file is untracked, or restores a version from before the install and quietly undoes your work.
`git checkout` is safe for exactly one case — a pre-existing, committed file you edited to trigger
a break, which in this skill is only the `.csproj` of hook 6.

After every trigger, `git status` must look exactly as it did before it.

**Three traps in this skill specifically, all from hook 1, all guaranteed rather than occasional.**

1. **Reading the probe is denied — to you.** That is the proof, not an obstacle. Do not disable
   the hook to get past it.
2. **Creating and deleting the probe are denied too**, because the guard tokenises Bash commands
   and cannot tell a path from a mention of one. The recipe below sidesteps this; follow it
   literally rather than improvising a workaround mid-phase.
3. **The probe files are new and untracked**, so restoring means deleting them. Check
   `git status --porcelain` for leftovers before reporting.

---

## Hook 1 — Secret read-guard

**Put the probe inside a throwaway directory, not in the repo root.** Follow this recipe
literally — it is the only shape where all four steps are possible once the guard is live.

```
1.  Edit  .gitignore          →  append a line: hooktest/
2.  Write hooktest/.env       →  API_KEY=not-a-real-key
3.  Read  hooktest/.env       →  must be DENIED. This is the proof.
4.  Bash  rm -rf hooktest     →  allowed
5.  Edit  .gitignore          →  remove the hooktest/ line
```

**Why the directory.** The guard tokenises Bash commands and cannot tell a path from a mention of
one, so **every command that names the probe file is denied — including the ones that clean it
up.** `rm -f .env.hooktest` is refused. So is `git check-ignore -v .env.hooktest`. A probe in the
root leaves you solving a puzzle in the middle of a mandatory phase, and the obvious conclusion —
that the guard is broken — is the opposite of the truth. With the probe one level down, no
cleanup command ever contains `.env`: `rm -rf hooktest` and `git check-ignore -v hooktest/` both
pass, and the `Read` in step 3 is still denied because the path contains `/.env`.

**Why `Write` and not a shell redirect.** `printf 'API_KEY=...' > hooktest/.env` is denied for the
same reason. `Write` is outside the matcher (`Bash|PowerShell|Read`) by design, so it goes
through. If the team widened the matcher to include `Edit|Write|MultiEdit`, create the probe
**before** writing `settings.json` in Phase 4 instead.

**Why `.gitignore` first.** Step 2 puts a file shaped like a credential in a repository that may
have no `.env` entry at all — check with `grep -i env .gitignore`. Until step 4 removes it, a
`git add -A` in that window stages it. In a repo running gitleaks the pre-commit hook would catch
it, but that is the second line of defence, not the first. This is the same reflex the skill
already applies to `logs/` before the audit log exists; the probe deserves it too.

**If `grep -i env .gitignore` comes back empty, that is a finding for the report** — a .NET repo
with no `.env` entry is one `git add -A` away from committing one.

**Expect:** step 3 is **denied**, and the reason names credential files and points at the
`.example` alternative. If the guard allows it, the pattern is wrong and that is a real finding.

**Also confirm a true negative**, because a guard that denies everything is not a guard: read
`.gitignore` in the same session and confirm it goes through.

**Restore:** steps 4 and 5 above — `rm -rf hooktest`, then take the `hooktest/` line back out of
`.gitignore`. Confirm with `git status --porcelain`, which is allowed.

## Hook 2 — Format on edit

**Trigger.** `Write` a file the repo's own style rules reject — unsorted `using` directives and
wrong indentation are enough, and both are things `.editorconfig` covers:

```csharp
using System.Text;
using System;

namespace <RootNamespace>;
public class HookProbe {
        public string Go() {
   return new StringBuilder().ToString();
}
}
```

**The success criterion is parity with the repo's own gate, not a fixed list of fixes.**

```bash
dotnet format <solution> --include <the probe, repo-relative> --verify-no-changes; echo "exit=$?"
```

**Expect `exit=0`:** the file, as the hook left it, satisfies the same check `make lint` runs. That
is the whole claim the hook makes.

**Do not assert that specific things were fixed.** "Sorted usings" is only true where
`.editorconfig` turns that rule on. A repo whose own gate does not sort usings will not have them
sorted — the hook is correct and the criterion is wrong, and the reference then sends you hunting
three things that are all fine. `--verify-no-changes` asks the repo what clean means instead of
assuming.

**Also quote the time**, measured here, not from this document:

```bash
time dotnet format <solution> --include <file> --no-restore
```

That number is what Phase 3 offered the hook on, and what tells the user whether it stays.

**If `--verify-no-changes` fails**, check in order: `{{SOLUTION_PATH}}` resolves to a solution that
actually contains this project; the `--include` path is **repo-relative**, not absolute; and
`.editorconfig` covers `*.cs`.

**Restore:** delete the probe file.

## Hook 3 — Dangerous-command blocker

**Trigger.** The safest of the six to fire deliberately is one that is blocked *before* it runs:

```
Bash: git reset --hard HEAD
```

**Expect:** denied, with the reason naming `git stash` as the alternative.

**Do not trigger this one with `rm -rf ~`.** If the hook is misconfigured, the command runs. Use a
pattern whose failure mode is survivable, and read the others from the script.

**Also confirm a true negative:** `git status` and `dotnet restore` go through untouched.

**Restore:** nothing to restore — the command never ran.

## Hook 4 — Dependency sweep

`SessionStart` cannot be triggered mid-session, so this is the one hook verified by running the
script directly:

```bash
printf '{"hook_event_name":"SessionStart","source":"startup"}' \
  | bash scripts/agent-hooks/dependency-sweep.sh; echo "exit=$?"
```

**Expect:** either the advisory lines on stdout, or **nothing at all** on a clean repository — and
`exit=0` in both cases. A non-zero exit is a bug: this hook never fails a session.

**Silence is not enough to pass.** A clean repo and a broken audit command produce the same empty
stdout and the same `exit=0`. Prove the command itself runs before you accept the silence:

```bash
{{SWEEP_COMMAND}}; echo "sweep command exit=$?"
```

`exit=0` there means the silence is real. Anything else means the hook is reporting nothing
because it cannot run — a missing Makefile target, a solution that will not restore — and the
script now says so on stderr rather than passing quietly. Check that stderr line is there.

Say in the report that this hook was verified by direct invocation rather than through a real
session start, and that it will first take effect on the user's next session.

## Hook 5 — Audit log

**Trigger.** Any tool call at all, then:

```bash
tail -3 logs/audit.log
```

**Expect:** tab-separated lines — timestamp, session id, event, tool name, full `tool_input`.

**Check the content, not only the shape.** A payload the extractor could not read logs a `-` in
the last column, and the column count is still 5 — so counting fields signs off on an empty log:

```bash
awk -F'\t' '{print NF}' logs/audit.log | sort -u          # must be exactly 5
awk -F'\t' '$5 == "-" {n++} END {print n+0 " rows with no tool_input"}' logs/audit.log
```

The second number must be **0** for rows whose tool genuinely had input. A `-` on every row means
the extractor is failing, not that the tools took no arguments.

**Then confirm the gitignore**, which matters more than the log itself:

```bash
git check-ignore -v logs/audit.log
```

**No output means the file is not ignored** — stop and fix `.gitignore` before going further. This
is the one verification step in this skill where failing to check has consequences beyond the repo.

**Restore:** nothing. The log is meant to accumulate.

## Hook 6 — Central package management guard

**Before triggering, confirm the repo is clean**, or this hook fires on every turn for something
the agent never wrote:

```bash
grep -rlE '<PackageReference[^>]*[[:space:]]Version[[:space:]]*=' --include='*.csproj' . || true
```

Any hit is a finding for Phase 6, not something to work around: report the count and either fix
those projects first or leave the hook out. A control that installs red is a refactoring proposal,
not a sensor.

**Trigger.** Add a `Version` attribute to an existing `PackageReference` in any `.csproj` —
snapshot it first, this is a committed file. **Also trigger the multi-line form**, which is what
Visual Studio writes and what a line-oriented check misses:

```xml
<PackageReference Include="Serilog"
                  Version="4.2.0" />
```

**Expect:** a warning after the write, naming the project file and the count of offending
references.

**Restore:** `cp <tmp>/<name>.csproj.bak <path>`, or `git checkout -- <path>` — this is the one
pre-existing committed file in the whole phase, so `git checkout` is safe here.

## Hook 7 — Generated-file guard

**Trigger.** Attempt an `Edit` to a `packages.lock.json`, or to any file under `Migrations/`.

**Expect:** denied, with the reason naming `dotnet restore` or `dotnet ef migrations add`.

**The migrations branch only fires for EF Core**, and that is deliberate: it requires a
`*ModelSnapshot.cs` beside the file. DbUp and FluentMigrator use the same directory name for
migrations written by hand, so blocking those would stop normal work and hand the team advice for
a tool they do not use. If the trigger is allowed, check for the snapshot file before calling it a
bug.

**Also confirm a true negative:** editing `docs/` prose or a normal `.cs` file goes through. The
path patterns are segment-anchored precisely so `src/MigrationsHelper.cs` is not caught — verify
that, because it is the failure the team will hit first.

**Restore:** nothing — the edit never happened.

---

## What MCP verification can and cannot be

**It cannot be a trigger.** A newly written `.mcp.json` leaves its servers at `⏸ Pending approval`
until the user trusts the workspace, and a committed `enableAllProjectMcpServers` is ignored until
then. There is no equivalent of firing a hook.

So verify what is verifiable, and be precise about the rest:

```bash
python3 -c "import json;d=json.load(open('.mcp.json'));print(list(d['mcpServers']))"
grep -nE '"(env|headers)"' -A3 .mcp.json    # every value must be ${VAR}, never a literal
```

**And start each stdio server once**, with the value the README will tell the user to export, and
with `< /dev/null` so it exits instead of waiting for a handshake:

```bash
# The assignment goes on its own line: `VAR=x cmd --flag "$VAR"` expands "$VAR" before the
# assignment takes effect, so --dsn would receive an empty value. And capture before truncating:
# piping into `head` hands you head's exit status, so a server that died reports 0.
APP_DSN="<the real value>"
OUT="$(npx -y @bytebase/dbhub@<pinned version> --transport stdio --dsn "$APP_DSN" \
  < /dev/null 2>&1)"; RC=$?
printf '%s\n' "$OUT" | head -5; echo "exit: $RC"
```

A connected server says so. An unsupported flag or a malformed DSN fails here, in thirty seconds,
instead of silently when the user next restarts Claude Code. **Two real failures this catches**:
DBHub's `--readonly`, which it no longer supports and which stops the server from starting at all;
and a SQLite DSN built from `${CLAUDE_PROJECT_DIR:-.}`, which resolves to a relative path that
SQLite rejects. Neither shows up in a JSON parse.

Report MCP as **written, pending approval**, with the activation steps. Never let one verification
table imply that both halves of the run were proven — one was fired, the other was written.

---

## Before you report

- `git status` matches its pre-phase state, with no leftover probe files.
- Every installed hook fired, and **every hook was also confirmed not to fire** on a benign case.
  A guard that denies everything passes a one-sided test.
- `logs/` is genuinely ignored, if hook 5 was installed.
- `.claude/settings.json` and `.mcp.json` both still parse.

**Do not report success with a hook that did not fire.** Fix it, or remove it and say so.
