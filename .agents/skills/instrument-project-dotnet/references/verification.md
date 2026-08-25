# Verify by breaking

> Called from **Phase 5** of `SKILL.md`.

Installing a gate proves nothing. Making it fail proves it is wired.

Run this for every control installed in this session. Introduce the violation, confirm the gate
catches it, revert. Capture the real output — you will report it in Phase 6.

**The clean-tree check belongs to Phase 4, not here.** Before writing the first file, confirm the
working tree is clean; if it is not, stop and tell the user — you cannot tell your edits from
theirs. By the time you reach this phase the tree is dirty *by design*: Phase 4 wrote eight files
and the skill does not commit.

**That is why the revert here is never `git checkout`.** Most of the files you are about to break
are files you just created or merged. `git checkout -- global.json` either fails (the file is
untracked) or restores the version from **before** the install — silently undoing your own work
and leaving a half-instrumented repo that still reports success.

---

## Procedure

For each control:

1. Note the exact file and the exact edit you are about to make.
2. **Snapshot the file first** — copy it somewhere outside the repo (`cp <file> <tmp>/<file>.bak`).
   One file, one break, one restore: never break two gates at once.
3. Make the edit.
4. Run the gate's command.
5. Confirm it **fails**, and that the message names the problem.
6. **Restore from the snapshot** (`cp <tmp>/<file>.bak <file>`), and confirm the gate passes again.

`git checkout -- <file>` is safe for exactly one case: a **pre-existing, committed source file**
you edited to trigger a break — the unused local of control 2, the reordered `using` directives of
control 3, the forbidden dependency of control 7. Everything the install produced is restored from
the snapshot.

**Restoring the file is not the whole restore.** Two breaks reach past the working tree:

- **Controls 5 and 6 stage their file** (`git add`). Putting the original content back leaves the
  broken version — a fake credential, in control 6's case — sitting in the index, where the user's
  next commit picks it up. Always finish with `git restore --staged <file>`.
- **Control 7 runs `dotnet add package`**, which edits the project file, may add a
  `<PackageVersion>` to `Directory.Packages.props`, and — with `RestorePackagesWithLockFile`
  enabled, which this skill installs — rewrites `packages.lock.json`. Restore all of them, then
  `dotnet restore` and confirm the lock files match the projects again.

After every break, `git status` must look exactly as it did before it: same files, same staged
state.

If a gate does **not** fail, it is misconfigured. Fix it before continuing — a silent gate is worse
than no gate, because the team believes it is covered.

---

## Control 1 — SDK pin

**Break:** in `global.json`, raise `sdk.version` one feature band above what is installed — with
10.0.400 present, use `"10.0.500"`. A plausible-but-absent version reproduces the real failure (a
teammate who has not updated); a fantasy version like `9.9.999` proves the same thing but reads as
a trick.

```bash
dotnet build <solution>
```

**Expect:** the SDK refuses to run. The three lines that matter:

```
Requested SDK version: 10.0.500
global.json file: <path>
Installed SDKs: 10.0.400
```

What was asked, who asked, what is available. Any `dotnet` command in the repo fails the same way.

Use `dotnet build`, not `dotnet --version`: the message then reads as "I tried to build and the
repo refused" instead of leading with `The application 'version' does not exist`.

## Control 1b — Lock files

**Break:** bump a version in `Directory.Packages.props` (or a `.csproj`) without restoring.

```bash
dotnet restore <solution> --locked-mode
```

**Expect:** restore fails, naming the package whose resolved version no longer matches the lock
file. Revert, then `dotnet restore` to confirm it is clean again.

If lock files were skipped, note it and move on.

## Control 2 — Strict build

**Break:** add an unused local to any source file: `var unused = 1;`

```bash
dotnet build <solution>
```

**Expect:** a build **error** (not a warning) with the analyzer's code.

If it only warns, `TreatWarningsAsErrors` is not reaching that project — check whether the project
overrides it locally.

## Control 3 — Style

**Break:** reorder the `using` directives in a file, or change its indentation.

```bash
make lint
```

**Expect:** `dotnet format --verify-no-changes` fails, naming the file and line.

Confirm the fix path too: `make format` repairs it and `make lint` goes green.

## Control 4 — Entry point

No break needed. Verify each target resolves and `make check` chains them:

```bash
make help
make check
```

**Expect:** `help` lists every target with its description; `check` runs restore, lint, build and
test in that order.

## Control 5 — Shift-left

**Break:** stage a badly formatted file and try to commit.

**Prove it by the commit that did not happen, not by the message you read.** Hook output carries
ANSI colour codes that can swallow the reason, and a non-zero exit is easy to misattribute. The
only fact that matters is whether history moved:

**Supply an identity on the command line, and disable signing.** Reading the absence of a commit
is the right test, but it cannot tell you *why* history did not move. A repo with no `user.name`
or `user.email` configured — a fresh container, a CI checkout, a machine the user just set up —
fails before the hook ever runs, and so does a `commit.gpgSign=true` with no key present. Either
one prints `BLOCKED` and proves nothing. `-c` scopes the identity to this one command and changes
nothing in the user's config.

```bash
make hooks                                    # install the hooks first
GC='git -c user.name=arkandia-check -c user.email=check@example.invalid -c commit.gpgSign=false'
HEAD_BEFORE=$(git rev-parse HEAD)
git add <file>
$GC commit -m "test: hook check"              # expected to fail
[ "$HEAD_BEFORE" = "$(git rev-parse HEAD)" ] \
  && echo "BLOCKED — no commit was written" \
  || { echo "NOT BLOCKED — undoing"; git reset --soft "$HEAD_BEFORE"; }
```

**Expect:** `BLOCKED`. The same guard applies to control 6.

**Before you trust a `BLOCKED`, prove the commit path works at all.** Run the same `$GC commit` on
a trivial staged change with the hooks bypassed — `LEFTHOOK=0 $GC commit -m "test: baseline"` —
and confirm history *does* move, then `git reset --soft` it. A gate that reports `BLOCKED` on a
repo where nothing can commit is the most convincing false positive in this file.

If it prints `NOT BLOCKED`, the reset above has already undone the commit — now fix the hook. Most
often `lefthook install` was never run, so `.git/hooks` still holds only `.sample` files. Report
both the failure and the reset.

Also confirm the escape hatch works, and mention it in the report: `LEFTHOOK=0 git commit …`. A
gate with no emergency exit gets uninstalled instead of skipped.

## Control 6 — Secrets

**Break:** stage a line matching a credential pattern. **Do not use `AKIAIOSFODNN7EXAMPLE`** — it
is AWS's own documentation example and gitleaks allowlists it, so the scan passes and the gate
looks broken when it is working correctly. Use a fake key that is not a published example, e.g.
`AKIA4SFODNN7QWERTZXC`, and verify it trips before you conclude anything.

If gitleaks passes on your test string, suspect the string before the gate: run
`gitleaks detect --no-banner` on a file containing it to confirm.

```bash
# Same temporary identity as control 5 — without it a repo with no user.email fails before
# gitleaks runs, and the gate looks like it blocked something it never saw.
git add <file>
git -c user.name=arkandia-check -c user.email=check@example.invalid -c commit.gpgSign=false \
  commit -m "test: secret check"
```

**Expect:** `gitleaks protect --staged` blocks the commit before it exists, with the rule name and
the redacted match.

Then confirm the escape path is documented: a genuine false positive is silenced by adding its
**fingerprint** to `.gitleaksignore` — never by adding the file path, which blinds the scanner to
everything in it.

If gitleaks was skipped, note it and move on.

## Control 7 — Architecture tests

**Break:** add a forbidden dependency — the ORM inside an application-layer service is the clearest
case. **A `using` alone does not compile**: in a repo where the rule passes, Application has no
reference to the ORM, so the compiler stops you before the arch-test ever runs. That failure proves
nothing — it is the project system refusing, not your sensor.

Two steps, in order:

```bash
dotnet add <application-project> package <the ORM>   # or add a <ProjectReference> to Persistence
# then add `using Microsoft.EntityFrameworkCore;` and a real use of a type from it in a service
make test
```

**Expect:** the solution **compiles**, and the arch-test fails, naming the rule and the offending
type. That is the whole point: the compiler was happy and the repository refused anyway.

Restore **four** things, not one: the source file, the project file, the `<PackageVersion>` entry
`dotnet add package` may have written to `Directory.Packages.props`, and the `packages.lock.json`
files it regenerated. Then run `dotnet restore` and confirm locked-mode restore is clean —
otherwise the lock files stay drifted and CI fails on a change nobody made.

## Control 8 — CI

Cannot be broken locally. Verify by inspection instead:

- the pipeline installs the SDK **from `global.json`**, not from a hardcoded version;
- it calls `make ci` rather than restating the steps;
- it triggers on **every push, on every branch** — not only the default one;
- on GitHub Actions, a second push to the same branch cancels the first instead of queueing
  behind it — Azure Pipelines cannot do this, so do not claim it there;
- a pull request inside the repository does not run the whole pipeline twice;
- test results are published, and `make test` actually writes the file they publish;
- for Azure Repos, there is no `pr:` block — PR validation comes from the branch policy;
- every marketplace action is on its **current major**, resolved during this run, not a version
  copied from the template. A stale major is not a build failure — it is a deprecation warning
  that goes unread until the runtime it depends on is withdrawn.

Tell the user the pipeline is unverified until it runs once, and that branch protection (required
status check) is what makes it a gate rather than a report.

---

## Closing

After reverting everything:

```bash
git status          # only the files the install produced — nothing else added or modified
git diff            # confirm no break survived in a pre-existing file
make check
```

Compare `git status` against the file tree you wrote in Phase 4. An extra modified file means a
break was not reverted; a **missing** one means a `git checkout` wiped part of the install.

Capture that output. **Do not report success while any gate is red.**
