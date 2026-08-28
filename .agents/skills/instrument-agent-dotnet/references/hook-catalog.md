# The hook catalogue

> Called from **Phase 3** and **Phase 4** of `SKILL.md`.

Seven hooks. Each entry says what it does, why it is worth its cost, what it deliberately does not
cover, and what it costs to get wrong. **1 and 2 are the default. 3 to 7 are offered.**

Read this before presenting the menu. The reason each hook exists is the thing the user is
actually deciding about; the event name is not.

---

## What every one of these scripts has in common

They all share `_lib.sh` and they are all written to the same three constraints, which are not
stylistic:

- **bash 3.2** — the version macOS ships. No associative arrays, no `mapfile`.
- **no `jq`** — absent from a default macOS and a default Windows. Field extraction is awk plus
  sed, and it handles exactly the flat string fields these hooks need.
- **Git Bash on Windows** — where `tool_input.file_path` arrives with **backslashes**, even though
  `$PWD` looks like `/c/project`. `json_path` normalises them; `json_raw` does not. Anything
  path-shaped goes through `json_path`.

And one rule that is easy to get wrong and impossible to notice:

> **`set -u`, never `set -o pipefail`.**
>
> **And normalise before you split.** A multi-line Bash command arrives as the two characters `\`
> and `n`, not as a newline. Strip backslashes first and `cat .env\necho done` fuses into the
> single token `.envnecho`, which every anchored pattern misses — and Claude Code emits multi-line
> Bash constantly, so that is the normal case, not an exotic one. `hook_read_stdin` folds
> structural newlines; each guard converts `\n`/`\t`/`\r` to separators before it removes
> quoting.
>
> **Two portability traps in `sed`, both silent.** Inside a bracket expression, POSIX reads
> `[;|&\n]` as the characters `;` `|` `&` `\` and **`n`** — every letter n becomes a separator.
> And BSD `sed` does not expand `\n` in the *replacement* either: on macOS `s/;/\n/` inserts the
> letter n. Normalise to a plain character with `sed`, then let `tr` make the newlines.

A guard reads a value, filters it, and matches. With `pipefail`, a filter that short-circuits —
`grep -q`, `grep -m1`, `head -1` — raises SIGPIPE in the upstream process, the pipeline reports
failure, and the guard falls through to allow. It exits 0. Nothing is blocked. Nothing is logged.
It looks exactly like a guard that found nothing, and it stays that way until somebody tests it.
This is the single most likely way for this skill to ship a hook that does not work.

---

## ⭐ Hook 1 — Secret read-guard

`PreToolUse`, matcher `Bash|PowerShell|Read`. **Blocks.**

**What it does.** Reads the target out of the tool call — `file_path` for `Read`, the tokenised
`command` for `Bash` — and denies when it names a credential file: `.env`, `*.pem`/`pfx`/`p12`,
`id_rsa`/`id_ed25519`, `secrets.json`, `appsettings.Secrets.json`, `.ssh/`, `.aws/credentials`.

**Why it is the one to lead with.** It blocks, and the block is visible: the agent says out loud
that it was refused and why. And it pairs with the deterministic half — gitleaks catches a
credential before it reaches a commit; this catches it before the agent has read it at all. Same
concern, two moments, two engines.

**The implementation trap.** The obvious version greps the raw stdin JSON for `.env`. It also
denies `git status`, `cat .gitignore`, and every command whose text merely mentions the word. The
team turns it off inside a day, and the one hook that actually blocked something is gone. Match
the field, and tokenise the Bash command — whole-string matching cannot tell `cat .env` from
`cat .env.example`, because the second contains the first.

**And tokenising on whitespace is only half of it.** Every word of the command becomes a candidate
path, sentences included, so `echo "=== LEDGER .env ==="`, `git commit -m "chore: ignore .env"` and
`gh pr create --body "... .env ..."` are all denied — prose, opening nothing. That kind of denial
looks like the guard working, so it gets worked around rather than reported, and the workaround
ends at turning the hook off. The shipped tokeniser drops what nothing opens: the operands of
`echo`/`printf`, heredoc bodies, the value of `--message`/`--body`/`--title`, and the pattern of a
`grep` or `sed`. A quoted argument holding spaces is kept whole rather than split, so a sentence
cannot match while `cat "my dir/.env"` still does — unless a shell is what runs it, and
`bash -c "cat .env"` or `ssh host "cat .env"` gets re-scanned as the command it is. Runtime-built
paths (`cat "$SECRET_FILE"`) stay invisible to any of this.

**Each of those drops is a hole if it is drawn one inch too wide.** They shipped
together and were found together, so they are worth reading as one lesson: a rule that drops a
token has to be narrower than the thing it is protecting.

| The drop | Drawn too wide | Where it is drawn now |
|---|---|---|
| Operands of `echo`/`printf` | The whole token went, command substitution included: `echo "$(cat .env)"` ran `cat` and the guard never saw it | Every token is scanned for substitutions *before* any rule gets to drop it — a message, a heredoc word, an assignment, an `echo` operand. The text inside `$(…)` or backticks only, never the whole token, or the surrounding prose becomes operands again |
| The value of a message flag | `-t` and `-b` were on the list, so `cat -t .env` and `sort -b .env` skipped the file as if it were a message | Long forms unconditionally; `-m`/`-b`/`-t` only when the command is `git`/`gh`/`hub`/`glab` |
| A `-c` argument re-scanned as a command | Gated on the argument containing a space, so anything that fits in one word passed: `python -c "open('.env')"` | Any quoted argument to a `-c` (or an interpreter's `-e`), spaces or not |
| The heredoc body | An unterminated `<<EOF` ran the skip to end of input, so one truncated command switched the guard off for everything after it | Fails closed: with no delimiter the body is rewound and tokenised as ordinary commands. A substitution in a body still runs and is still scanned, unless a quoted delimiter (`<<'EOF'`) turned expansion off |
| The command name | Read off the first word, so a wrapper answered for the command it runs: `sudo git commit -m "…"` never saw a `git`, never skipped the `-m`, and re-scanned the message as a command | Wrappers (`sudo`, `env`, `timeout`, `nohup`, `xargs`, `nice`, …) are walked past — their flags, a duration, an assignment — and the word they actually run is what classifies the segment |
| The bound on re-scanning | A count of four calls, spent by four ordinary substitutions (`cd "$(git rev-parse --show-toplevel)"` and friends); everything after them went unscanned, in silence | Budgeted in bytes appended. Still finite, no longer exhausted by commands an agent writes all day |

**Known false positive, shipped on purpose.** `cp .env.example .env` is denied: one of its
arguments is a bare `.env`. Creating a credential file is a step for a human, so the guard is
right to stop the agent doing it unprompted.

**This belongs in the Phase 6 report, not in the Phase 3 menu.** A caveat in a menu option is
noise at the moment the user is comparing choices — they cannot act on it yet, and it makes the
option read as defective. After the hook exists, the same sentence is the thing that stops them
filing a bug. Same for every other caveat in this document.

**This guard collides with the skill's own later phases, every single run.** Not occasionally —
by construction, because both phases have to handle strings the guard matches:

- **Phase 5** must create, read and then delete a credential-shaped probe. Only the read should be
  denied; the create and the delete are denied too, because both take the probe as an operand.
  `verification.md` has the recipe: a throwaway directory, written with `Write`, removed with
  `rm -rf <dir>` so no command ever names the file.
- **Phase 6** documents what this guard blocks, so that prose contains `.env` and `id_rsa`. The
  tokeniser skips heredoc bodies and `echo` operands, so that prose passes — but write it with
  `Edit` or `Write`, which are outside the matcher and do not depend on the tokeniser being right.

Both are called out in `SKILL.md` rather than left to be discovered, because the natural reading
of either failure is "the guard is broken" — which is exactly backwards.

**What it does not cover.**
- **`@`-referenced files.** Claude Code inlines those while building the prompt, with no tool
  call, so **no `PreToolUse` hook fires** — including this one. That gap closes with a `Read` deny
  rule in permissions, not with a hook. Say so.
- **`dotnet user-secrets`.** It is the recommended way to hold local secrets in .NET. Blocking it
  breaks real work to prevent nothing.
- **`Grep` and `Glob`.** They can surface a secret's contents. Add them to the matcher if the team
  wants that, and accept false positives on repo-wide searches.

**Widening it to writes** is one word in the matcher: add `Edit|Write|MultiEdit`. The `file_path`
branch already handles them. That also stops the agent authoring a `.env`, which is usually right.

---

## ⭐ Hook 2 — Format on edit

`PostToolUse`, matcher `Edit|Write|MultiEdit`. Does not block.

**What it does.** Runs `dotnet format <solution> --include <the file that changed> --no-restore`.

**Why teams keep it.** It takes the whole class of style argument out of the loop. The agent stops
spending turns on indentation and sorted usings, and the diff the human reviews carries only the
change.

**Why it does not call `make format`.** `make format` runs over the whole solution: tens of
seconds, rewriting files nobody touched, on **every edit**. Scoped with `--include` it is one to
two seconds. This is the only place in the Arkandia skills where a Makefile target exists and is
deliberately bypassed — say so in the report, or the next reader will "fix" it.

**Why it still loads the solution.** `--include` narrows which files get written, not which
project gets loaded, so the MSBuild workspace still comes up. That workspace is what makes style
rules evaluable at all. `dotnet format whitespace --folder` skips it and looks much faster; it
also silently ignores those rules and resolves `--include` relative to the folder, so repo-root
paths match nothing and it exits 0 having done nothing.

**Why it never blocks.** `PostToolUse` runs after the tool succeeded; there is nothing left to
stop. The common failure is boring and real — the file does not compile yet because the agent is
mid-refactor. Blocking on that fights the agent instead of helping it.

**Verify it against the repo's gate, never against a list of expected fixes.** "Sorted usings" is
only true where `.editorconfig` turns that rule on; in a repo whose own gate does not sort them,
asserting it sends you hunting a bug that is not there. The claim this hook makes is exactly one:
the file it touched passes `dotnet format --verify-no-changes`.

**The demo warning.** When this hook works, nothing visible happens. It is never the closing
beat. `statusMessage` gives it a line in the spinner, which is the most it will ever show.

---

## Hook 3 — Dangerous-command blocker

`PreToolUse`, matcher `Bash`. **Blocks.**

**Why `Bash` alone, when hook 1 also matches `PowerShell`.** The two hooks read the same field and
mean different things by it. Hook 1 looks for a *path* — `.env`, a `.pem`, `secrets.json` — and a
path is spelled the same in either shell, so widening its matcher costs nothing and catches more.
This hook parses *shell syntax*: it splits on `;`, `&&` and `|`, drops leading `VAR=value`
assignments, and dispatches on the command word against `rm`, `git`, `dotnet`, `nuget`, `sudo`.
None of that describes PowerShell. `Remove-Item -Recurse -Force C:\` walks straight through every
rule and exits 0 — a matcher that claims coverage it does not have, which is the one thing a guard
must never do. If a team genuinely runs PowerShell here, add `PowerShell` back *together with*
`Remove-Item`/`rd`/`del` rules; never on its own.

**What it does.** Six patterns, each unrecoverable or outward-facing:

**Why it parses subcommands instead of grepping the whole string.** `grep 'git reset .*--hard'`
also fires on `echo "never run git reset --hard"` — and on the heredoc that writes that sentence
into `README.md`, which is a step Phase 6 of this skill requires. A blocker that stops you
documenting it is a blocker the team removes. Each rule matches on the **command word**, never on
text that happens to sit in an argument.

**And why the `rm` rule asks "is the target outside the repo?" instead of listing system paths.**
Enumerating `/`, `~` and `$HOME` leaves `rm -rf /usr/local` and `rm -rf /etc` allowed, which are no
less final. Flag order defeats a single regex the same way it defeats one for force-push:
`rm -f -r /` and `rm --recursive --force /` are the same command. Parse the flags, then ask one
question about each target.

| Pattern | Why it is on the list |
|---|---|
| `rm -rf` on `/`, `~`, `$HOME` | No undo, no trash. Working tree and git objects go together |
| `sudo` / `doas` | Nothing in a coding task needs root, and a hook cannot undo what root did |
| force-push to a protected branch, **or to none** | Rewrites history other people already pulled. `git push -f` and `--force origin HEAD` name no destination and push whatever is checked out, which is the common shape of this accident |
| `git reset --hard` | Discards uncommitted work with no reflog entry for the working tree |
| `dotnet nuget push` | Publishes. On nuget.org, not truly deletable |
| `rm` of `packages.lock.json`, `Directory.Packages.props`, `global.json` | Silently undoes reproducibility. Regenerating is not restoring |

**Why the list is short.** A long blocklist gives a false sense of security and generates false
positives, and a hook that cries wolf is switched off within a week — taking the patterns that
mattered with it. If you add an entry, add its sentence too.

**Why the force-push rule is three matches and not one regex.** A single expression spanning the
whole command has to guess at flag order and backtrack across `.*`; `git push -f origin main`
slips past a pattern tuned for `git push --force origin main`. Three independent checks — it is a
push, it forces, it names a protected branch — do not care about order. The branch pattern is
anchored so `feature/main-refactor` does not match.

**It is not a security boundary.** Hooks run with the user's shell and permissions, and these are
text matches, not parsing: `eval $(echo cm0gLXJm | base64 -d)` walks straight past. It is a
guardrail against a plausible mistake. For a real boundary, use permission deny rules. **Put this
sentence in the report** — a team that believes it has a sandbox will use it as one.

---

## Hook 4 — Dependency sweep

`SessionStart`, matcher `startup|resume`. Reports only.

**What it does.** Runs `make audit` — or `dotnet list package --vulnerable --include-transitive`
when there is no such target — and puts the findings in the agent's context **before the first
prompt**, so it knows which packages are compromised before it writes an integration against one.

**Why stdout and not JSON.** For `SessionStart`, Claude Code treats a hook's stdout as plain text
and adds it to context. That removes the only place in this hook set where a script would have had
to *build* JSON around arbitrary content — escaping quotes and newlines out of an MSBuild log is
precisely where a bash hook breaks.

**Why it never fails.** Same rule as `NuGetAudit` in the deterministic half: an advisory published
overnight is not a reason for the repo to stop working in the morning. It reports; the human
decides.

**Why silence on a clean repo.** `dotnet list package --vulnerable` exits 0 either way, so the
exit code says nothing — the findings are the lines carrying a severity. A hook that prints
"everything is fine" at every session start trains people to skip its output, and then they skip
it on the day it says something.

**Why `--outdated` is left out.** It needs a network round-trip per package and returns a list
nobody acts on at session start. Vulnerable is actionable; outdated is backlog grooming.

**The cost.** This runs before the first prompt of every session, and a slow `SessionStart` is
indistinguishable from a tool that starts badly. Measured on a small solution it is about two
seconds. If it is regularly slower, move it to CI and say so — the `timeout` is a backstop, not a
plan.

---

## Hook 5 — Audit log

`PreToolUse`, no matcher, `async`. Does not block.

**What it does.** Appends one tab-separated line per tool request to `logs/audit.log`: timestamp,
session id, event, tool name, and the **full `tool_input`**.

**Who it is for.** This is the hook that interests whoever is accountable for the work rather than
doing it. It answers "what did the agent actually run, unattended" with a file rather than a
recollection.

**Why the full `tool_input`.** Without it the file is a counter, not a trail: it would say a Bash
call happened without saying what it ran. With it, the log records the command that was about to
run and the content that was about to be written — **including anything sensitive that passed
through a tool, and including a credential hook 1 did not recognise.**

Two consequences, both mandatory:

1. **`logs/` goes into `.gitignore` before this script exists.** Reversed, the user's next
   `git add -A` publishes it.
2. **The Phase 6 report says out loud what the file holds.** A log nobody knows captures tool
   input is worse than no log.

**Why `PreToolUse` and not `PostToolUse`.** Pre records every *request*, including the ones a
guard denied — which is the interesting half. Register the same script under `PostToolUse` as well
to record outcomes; `hook_event_name` is already the third column, so both fit one file and
`grep PostToolUse` separates them.

**Why `async`.** A log write must never sit between the agent and its tool call. Detached, it
cannot block and it cannot deny.

**Rotation.** One generation at 5 MiB. This is a local trail, not an archive.

---

## Hook 6 — Central package management guard

`PostToolUse`, matcher `Edit|Write|MultiEdit`. Warns.

**Only when `Directory.Packages.props` exists.** Without central package management, a `Version`
attribute on a `PackageReference` is the normal and correct way to declare a dependency, and this
hook fires on every `dotnet add package`. That is the fastest available route to getting the whole
set switched off.

**What it does.** After a write to a `.csproj`, reads the file and warns when a `PackageReference`
carries a `Version` attribute, naming the project and the count.

**Why it matters.** With CPM in force this does not simply error out. NuGet raises NU1008 in most
configurations, but under a lock file — or a `Directory.Packages.props` that also lists the
package — the build can go green while two projects resolve different versions. That drift is the
exact thing central package management was installed to end, and it is invisible.

**Why it reads the file instead of the payload.** An `Edit` delivers a diff, not the result. The
payload cannot answer "does this project now carry a `Version` attribute".

**Why it warns instead of blocking.** `PostToolUse` runs after the write; there is nothing left to
stop, and the fix is one attribute the agent can make on its next turn once it is told.

---

## Hook 7 — Generated-file guard

`PreToolUse`, matcher `Edit|Write|MultiEdit`. **Blocks.**

Two independent branches. **Install each only when its artifact exists.**

**`packages.lock.json`** — the output of restore. A hand edit makes `--locked-mode` fail in CI with
an error pointing at a package rather than at the edit. The denial names the fix: change the
version in `Directory.Packages.props` and run `dotnet restore`, or `--force-evaluate` for a
deliberate re-resolution.

**`Migrations/`** — EF Core's. A hand-written migration desynchronises `ModelSnapshot.cs` from the
migration list, and the next `migrations add` then generates a diff against a model state that
never existed. The denial names `dotnet ef migrations add` and `remove`.

**Path matching is segment-anchored**, so `docs/migrations-guide.md` and `src/MigrationsHelper.cs`
are not caught, and a Windows path is normalised before matching.

**Neither branch depends on the deterministic instrumentation having run.** It depends on the file
being in the repository — which is a Phase 1 question and nothing more.

---

## Hook 8 — `Stop` → `make check`: deliberately not in this skill

It is the hook that most obviously joins the two layers, and it is left out of v1 on purpose.
`Stop` fires **at the end of every response**, not at the end of the session. On a repo where
`make check` takes forty seconds, every single turn carries forty seconds. Teams disable it in a
day and conclude that hooks make the agent slow.

If a team asks for it, the honest version is `make lint` alone — the cheapest gate — and the
number measured on their repository, quoted before they decide.
