---
name: instrument-project-dotnet
description: Install the deterministic instrumentation layer in a .NET repository so an AI coding agent cannot ship work that breaks the team's rules — SDK pin (global.json), strict build with analyzers (Directory.Build.props), verifiable style (.editorconfig), a single entry point (Makefile), pre-commit/pre-push gates (Lefthook), secret scanning (gitleaks), architecture fitness functions (ArchUnitNET), and a CI pipeline (GitHub Actions or Azure DevOps). Every gate is proven to fail before the run ends. Invoke with `/arkandia:instrument-project-dotnet`.
disable-model-invocation: true
---

# instrument-project-dotnet — Turn the Repo Into Its Own Reviewer

You are installing the **deterministic instrumentation** layer of the Arkandia Method: everything a
machine can verify on its own, in milliseconds, with no ambiguity.

The point is not tidiness. Agents write code faster than a team can review it, so verification has
to become mechanical. Each control you install is a sensor the agent hits by itself — **before any
human reads the diff**.

You install eight controls, prove each one fails when it should, and record them in `AGENTS.md` so
the agent knows they exist.

| # | Control | Artifact | What it prevents |
|---|---------|----------|------------------|
| 1 | Reproducible inputs | `global.json`, `Directory.Packages.props`, lock files | Two machines resolving a different SDK or a different dependency tree |
| 2 | Strict build | `Directory.Build.props` | A warning reaching `main` |
| 3 | Style | `.editorconfig` | Formatting noise in every diff |
| 4 | Entry point | `Makefile` | Nobody knowing how the repo is verified |
| 5 | Shift-left | `lefthook.yml` | Errors surfacing at review time |
| 6 | Secrets | `gitleaks` | A credential reaching the history |
| 7 | Architecture tests | Arch-test project | The dependency rule silently breaking |
| 8 | CI | Workflow / pipeline | Local gates being skipped |

## Philosophy (hold these throughout)

- **Never hardcode a version.** Read the SDK from the machine, the target framework from the
  projects, and let NuGet resolve packages. A templated version number is a bug.
- **Encode what the repo already does, not what it should do.** Every architecture rule you
  write must pass the moment you write it. A suite that goes red on install is a refactoring
  proposal, not a sensor.
- **Discover before you write.** Layout varies: tests under `tests/` or `src/Tests/`, one solution
  or several, VSTest or Microsoft.Testing.Platform. Assume nothing.
- **Merge, never clobber.** Existing config files carry sections you did not write.
- **An exception without a comment is invisible debt.** Every `NoWarn`, `severity = none`, and
  `NuGetAuditSuppress` carries a reason and an owner.
- **One definition of "the code is fine."** CI invokes the Makefile; it does not restate its steps.
- **A gate nobody saw fail is not a gate.** Verification means breaking each control on purpose.
- **Fail fast, with a clear message.** A 30-second pre-commit gets uninstalled within a week.
- **Everything you write is in English.** Config files, code comments, `fail_text` strings, Make
  target descriptions, CI step names, commit-free progress output — all English, regardless of the
  language you are conversing in. The one exception is **prose documentation that already exists**:
  editing a Spanish `AGENTS.md` in English breaks the document. Match the language of the file you
  are editing; write every file you create in English.
- **Say where you are, in words the user can act on.** Announce each phase and each control as you
  reach it, in English. Name the artifact and what it buys, not the internal label: not
  `Control 4 — entry point`, but `Control 4/8 — Makefile: one 'make check' that runs every gate`.
  A run takes fifteen minutes or more; silence is indistinguishable from a hang.
- **Plain words, no insider vocabulary.** Say "move the property to Directory.Build.props", not
  "hoist it"; "connect the gate", not "wire it up". The user is deciding, not reading your notes.
- **Every error code gets a sentence.** `CS0219`, `MSB4181`, `NU1004` mean nothing to most readers.
  Name it and say what it is: "CS0219 — a variable is assigned but never used; the strict build
  turns that from a warning into an error."
- **Never commit.** Not at the end, not between phases, not to "checkpoint" your work. The user
  reviews the diff and commits when they decide to. The only `git` writes you make are the
  break-and-restore of Phase 5, and those are undone before the phase ends.

---

## Phase 1 — Discover (silent)

Do this without talking to the user. Use Glob, Grep, Read, and read-only Bash.

Work through `references/inspection.md`. It covers, in order:

1. **Confirm .NET** — `*.sln`, `*.slnx`, `*.csproj`, `*.fsproj`, `global.json`,
   `Directory.Build.props`. If nothing matches, stop and say so. Write nothing.
2. **Solution and project graph** — solution path, project list, layer names, the root namespace
   prefix (e.g. `TheOffice.`), and the project reference graph.
3. **Target frameworks** — the TFM each project uses. Divergence is a question for Phase 3, not a
   guess.
4. **Test setup** — test project locations, framework and major version (xUnit v2/v3, NUnit,
   MSTest, TUnit) and runner (VSTest vs Microsoft.Testing.Platform). This decides how the
   arch-test project is built.
5. **Package management** — whether `Directory.Packages.props` exists (central package management
   changes how you add references), and whether lock files are in use. **Count the lock files
   against the project count**, not merely "are any present": a solution where some projects have
   one and some do not restores half-pinned under `--locked-mode` and still goes green. Phase 3
   question 4 needs that number, not a boolean.
6. **Existing controls** — which of the eight are already present, and what each one contains.
7. **CI** — `.github/workflows/`, `.azdevops/`, `azure-pipelines.yml`, `Jenkinsfile`,
   `.gitlab-ci.yml`.
8. **Context docs** — `AGENTS.md`, `CLAUDE.md`, `README.md`, `docs/`. Read the architecture doc and
   any ADRs: they state rules a compiler cannot enforce, and they are the documents Phase 6 has to
   update.
8b. **Documentation language** — what language are `AGENTS.md`, `README.md` and `docs/` written
   in? Phase 6 edits those files and must not switch their language mid-document. Config files and
   code comments you create are always English, so this only affects the prose edits.
8c. **Commit conventions** — read the last ~20 subjects (`git log --oneline -20`). Do they follow
   Conventional Commits (`feat:`, `fix(scope):`)? This is the only evidence for whether the
   `commit-msg` hook belongs in `lefthook.yml`. Imposing a convention the team does not use is a
   team decision, not an instrumentation fix — if the log says no, drop the block and say why.
9. **Architecture shape** — reconstruct the `<ProjectReference>` graph and classify what the repo
   actually is: layered, vertical slices, modular monolith, n-tier, or flat. Follow
   `references/architecture-discovery.md`. **Do not assume Clean Architecture.** This drives
   control 7 and is the step that decides whether the arch-tests are useful or noise.

Then report the state to the user as a table — control, status (`present` / `partial` / `missing`),
and what you found — plus a short list of environment facts: SDK version installed, solution path,
test location, test runner, CI platform detected.

---

## Phase 2 — Prerequisites

Check the tooling the controls depend on, and report per OS. Do not install anything yourself.

| Tool | Check | macOS | Windows | Linux |
|---|---|---|---|---|
| .NET SDK | `dotnet --version` | — | — | — |
| Lefthook | `lefthook version` | `brew install lefthook` | `winget install evilmartians.lefthook` | `go install github.com/evilmartians/lefthook@latest` |
| `make` | `make --version` | ships with Xcode CLT | `winget install ezwinports.make` | ships with the distro |
| gitleaks (if opted in) | `gitleaks version` | `brew install gitleaks` | `winget install gitleaks` | `apt install gitleaks` on Debian trixie+ / Ubuntu 25.04+; older LTS needs the release binary |

**`make` does not ship with Windows.** If the team is on Windows and `make` is absent, surface the
`winget` command as a documented prerequisite and add it to the repo README — do not silently
switch to another task runner. `make check` is the convention across Arkandia material.

If a prerequisite is missing, print the install command and ask whether to continue installing the
files anyway (the config is still valid; only the local hooks won't run until it is installed).

---

## Phase 3 — Agree on scope

Ask **only what Phase 1 could not answer**. Use `AskUserQuestion` for the closed questions.

**Write every question for someone who does not know the jargon.** Never put an acronym in a
question without spelling it out and saying what it changes for them. "Migrate to CPM?" is a bad
question; "Central Package Management moves every package version out of the 10 project files into
one file at the root, so two projects can't drift onto different versions of the same package —
migrate?" is a good one. Same for TFM (target framework), lock files, and analyzers. State the
consequence and the cost in files touched, then let them choose.

1. **Which controls to install** — default is all eight. Offer to skip the ones already present.
2. **How tightly to pin the .NET SDK** — `global.json` fixes which SDK version builds this repo.
   `Permissive (recommended)`: the lowest feature band of the installed major.minor with
   `rollForward: latestFeature`, so a teammate whose SDK is slightly older still builds. `Strict`:
   the exact installed version, for teams whose SDK comes from a devcontainer or a pinned CI image
   — anyone on a different one is locked out until they update. List what `dotnet --list-sdks`
   found when you ask.
3. **Central Package Management** — only if `Directory.Packages.props` is absent. Today each
   project declares its own package versions, which is how two projects end up on different
   versions of the same package. This moves every version into one file at the repo root; the
   projects then say only *which* packages they use, not which version. **It edits every `.csproj`
   in the solution** — state the exact count ("this edits 10 project files") and report the version
   drift you already measured. Offer `Migrate now`, `Skip — keep versions in each project`.
   Recommend migrating: the drift is usually already there and the change is mechanical.
4. **Lock files** — **count them per project first**, because the answer is rarely all-or-nothing:
   `find . -name packages.lock.json -not -path '*/obj/*' | wc -l` against the project count.
   - **Every project has one** — the control is present. Skip the question.
   - **None do** — ask.
   - **Some do.** This is the `partial` case, and it is the one that matters: `--locked-mode`
     restores the projects that have a lock and resolves the rest fresh, so CI passes while half
     the solution is unpinned and nobody can tell from a green build. Do not ask a generic
     yes/no here. Show which projects have one and which do not, and offer `Complete the set
     (generates N lock files)` or `Remove the ones that exist — a half-pinned solution reads as
     pinned`. Leaving it as it is, is the one answer to argue against.

   Then the question itself. Right now a restore picks whatever version the feed offers today, so
   a machine that restores next month can get a different dependency tree from the same commit; a
   lock file records the tree that was actually resolved, and CI restores in `--locked-mode`,
   which fails when the lock and the projects disagree. The cost is real and ongoing: **one
   `packages.lock.json` per project, all of them committed**, and every deliberate version change
   now needs a `dotnet restore` in the same commit or the build goes red. Offer
   `Generate lock files`, `Skip — restore resolves fresh each time`.

   **This does not depend on question 3.** Lock files work with versions in ten `.csproj` files
   exactly as they do under central package management — only the file you edit to change a
   version differs. Recommending it alongside CPM is fine; *requiring* CPM first is not, and a
   repo that declined the migration is precisely the one where a resolved tree drifts unnoticed.

5. **CI platform** — only if Phase 1 found none or found both. `GitHub Actions`, `Azure DevOps`,
   `Skip CI for now`.
6. **Secret scanning** — gitleaks blocks a commit that contains something shaped like a credential,
   and scans history in CI. Off by default, because the first week costs curation: sample
   connection strings and test GUIDs trip it, and each false positive needs an entry in
   `.gitleaksignore`. Offer `Yes — pre-commit and CI`, `CI only`, `Skip`.
7. **Pipeline scope** — `CI only (quality gates)` or `CI + deploy`. Default to CI only; how this
   repo deploys is a separate conversation.
8. **Warnings as errors** — only if the repo has never built with `TreatWarningsAsErrors`. Turning
   it on means today's warnings stop the build. Offer `On, strict` or `On, with a documented list
   of exceptions to work through over time` — the second for any repo with history, and the
   exception list becomes the visible record of the debt. Say how many warnings the current build
   produces.
9. **One-time reformat** — `.editorconfig` plus `EnforceCodeStyleInBuild` makes the build enforce
   formatting, and existing files that never followed those rules start failing. Fixing that is a
   single `dotnet format` pass, which rewrites **source files** — a real diff, not config.

   You cannot measure this before the file exists, so **probe for it**: write the `.editorconfig`
   you intend to install, run the check, delete it again, and only then ask. Count only genuine
   formatting failures, never output lines:

   ```bash
   # Findings, and the distinct files they land in. Report both; they are different numbers.
   dotnet format <solution> --verify-no-changes 2>&1 \
     | grep -E 'error (WHITESPACE|IMPORTSSORTING|FINALNEWLINE|IDE0055)' > /tmp/fmt.txt || true
   wc -l < /tmp/fmt.txt                                    # findings
   grep -oE '^[^(]+\.cs' /tmp/fmt.txt | sort -u | wc -l    # files that would be rewritten
   ```

   Two traps here. **`grep -c` exits 1 when the count is zero**, so a clean repo looks like a
   failed command — always append `|| true` and read the number. And **one file produces many
   findings**, so counting findings and calling them files overstates the change by a wide margin.
   Tell the user the file count, since that is the size of the diff they will review.

   Offer `Reformat now`, `Start style rules at suggestion severity instead`, or `Skip — just report
   the failing files`. Never reformat the repository as a side effect of installing a config file.
10. **Target framework** — the TFM (`net10.0` and friends) says which .NET each project builds for.
    Three cases, and only when they arise:
    - **Projects disagree.** Ask which is authoritative. Do not normalise silently — a project left
      behind usually has a reason.
    - **Centralising it.** Moving `<TargetFramework>` into `Directory.Build.props` means deleting it
      from every `.csproj`; otherwise the projects keep overriding it. Same shape as question 3:
      off by default, offered with the file count. See **Where the target framework lives** in
      Phase 4.
    - **It is already in `Directory.Build.props` but every project overrides it.** The property is
      inert: the file claims the framework is centralised and nothing honours it. This is the
      `partial` case from Phase 1, and it is worse than absent because the next reader edits the
      wrong file. Show the evidence — the property and the projects that override it — and offer
      `Remove the inert property` or `Finish the migration (edits N project files)`. Never leave it
      as it is.

**A control that exists but does nothing follows the same rule.** Whenever Phase 1 marked a control
`partial`, say what is inert and offer both exits — complete it, or remove it. Silence leaves the
team believing they are covered.

Do not ask about anything already visible in the repo.

---

## Phase 4 — Apply

**Before writing anything, confirm the working tree is clean** (`git status`). If it is not, stop
and tell the user. This is the phase the check belongs to: from here on the tree is dirty by
design, and Phase 5 can no longer tell your edits from theirs.

**Ignore the agent's own footprint.** A repo the skill was installed into locally almost always
carries untracked `.claude/`, `.codex/`, `skills-lock.json` or similar. These are tooling, not the
user's work — exclude them from the check rather than stopping on them, and say which ones you
excluded. Everything else counts:

```bash
git status --porcelain | grep -vE '^\?\? (\.claude/|\.codex/|skills-lock\.json)' | wc -l
```

**Read the count, never the exit code.** `grep` exits 1 when nothing matches, which is exactly the
clean case — treating that as failure inverts the check. `0` means clean; anything else means stop
and show the user the lines.

Read each template from `templates/`, **adapt it to this repository**, and write it. The templates
are skeletons, not literals: solution path, root namespace, layer names, test location and runner
all change. Every template opens with a header of instructions — read it, follow it, and delete it
before writing the file.

| Template | Becomes |
|---|---|
| `templates/global.json.template` | `global.json` |
| `templates/Directory.Packages.props.template` | `Directory.Packages.props` |
| `templates/Directory.Build.props.template` | `Directory.Build.props` |
| `templates/editorconfig.template` | `.editorconfig` |
| `templates/Makefile.template` | `Makefile` |
| `templates/lefthook.yml.template` | `lefthook.yml` |
| `templates/ci/github-actions.yml.template` | `.github/workflows/ci.yml` |
| `templates/ci/azure-pipelines.yml.template` | `.azdevops/azure-pipelines.yml` |

There is no template for the arch-test project: it is generated from the repo's own test project.
See `references/arch-tests.md`.

### Version resolution (mandatory)

| Value | Source |
|---|---|
| `global.json` SDK version | See **Pinning the SDK** below. Not simply `dotnet --version` |
| `rollForward` | `latestFeature` by default. See **Pinning the SDK** |
| `TargetFramework` | The TFM the projects already use — and it stays there. See **Where the target framework lives** |
| `AnalysisLevel` | `latest` — it tracks the SDK |
| New packages | `dotnet add package <name>` **without `--version`**, then read back the resolved version and report it |
| Arch-test framework | Whatever the repo's other test projects use |
| GitHub Actions majors | `gh api repos/<owner>/<action>/releases/latest --jq .tag_name`, truncated to the major. Never the value written in the template |
| `{{GITLEAKS_VERSION}}` (CI) | `gh release view --repo gitleaks/gitleaks --json tagName --jq '.tagName' \| tr -d v`. Written into the pipeline as a **pinned** version with a checksum check, not re-resolved on every run — see below |
| `.editorconfig` indentation | The modal indent of the existing `.cs` files. See `references/inspection.md` §4b — never the language default |

Under central package management, `<PackageReference>` must carry no `Version`; add a
`<PackageVersion>` to `Directory.Packages.props` instead.

**"Resolve, never hardcode" means resolve when you write the file — not on every CI run.** The two
are opposites for a downloaded binary. A pipeline that fetches `releases/latest` runs a different
gitleaks tomorrow than it ran today, with nothing recording the change, and a broken or tampered
release lands inside the gate meant to catch problems. So the CI templates resolve the current
gitleaks version here, **pin it**, and verify the download against the project's published
`checksums.txt` before extracting. The team bumps it like any other dependency. Say in the report
which version you pinned, so they know what to bump.

### Where the target framework lives

**Do not move `<TargetFramework>` into `Directory.Build.props` by default.** MSBuild imports that
file *before* the body of the `.csproj`, so any project that declares its own TFM overrides it. In
a repo where every project already sets one — which is every repo created from a template — the
property is inert: the file reads as though the target framework is centralised, it is not, and the
next reader edits the wrong file.

Centralising it for real means deleting `<TargetFramework>` from every `.csproj`. That is the same
class of change as central package management, so treat it the same way: **off by default, offered
explicitly with the file count**, never a side effect of this run. If the user declines — the
default — say so in the report: the TFM stays in the projects.

Two cases where the question does not arise at all:

- **Any project multi-targets** (`<TargetFrameworks>`). Leave every project alone.
- **Projects disagree on the TFM.** That is a Phase 3 question first; centralising a disagreement
  just hides it.

### Pinning the SDK

**`rollForward` only moves forward, never backward.** Pinning the version installed on the machine
that runs this skill locks out every teammate on a lower feature band — and the pin gets deleted
within a week, which is worse than not having one.

| `rollForward` | With `version: 10.0.400`, accepts |
|---|---|
| `disable` | 10.0.400 only |
| `latestPatch` | 10.0.4xx — **fails on 10.0.300** |
| `latestFeature` | 10.0.4xx and higher feature bands within 10.0 — **also fails on 10.0.300** |
| `latestMinor` | any higher 10.x |

**Default: pin the lowest feature band of the installed major.minor, with `latestFeature`.** With
10.0.400 installed, that is `"version": "10.0.100"` — every 10.0.x on the team satisfies it, and the
major.minor is still locked.

Ask for the strict variant only when the team's SDK is controlled centrally (a devcontainer, a
pinned CI image, a managed fleet). Then `dotnet --version` exactly, with `latestPatch`.

If the repo already pins a version, **do not lower it silently** — report the mismatch and ask.

### Order and rules

Install in this order — each control builds on the previous one:

1. **Reproducible inputs** — three artifacts, in this order:

   - **`global.json`** — if the file exists, **merge**: add the `sdk` block, preserve `test`,
     `msbuild-sdks`, and anything else. Do not rewrite the file.
   - **`Directory.Packages.props`** (if the user opted in) — this is a migration, not a file drop:
     1. collect every `<PackageReference Include="X" Version="Y" />` across the solution;
     2. where the same package appears at different versions, **ask** which one wins — do not pick
        silently. Report the drift you found;
     3. write one `<PackageVersion Include="X" Version="Y" />` per package;
     4. edit every `.csproj` to remove the `Version` attribute, leaving the rest of the element
        (`PrivateAssets`, `IncludeAssets`) untouched;
     5. **delete every `obj/` directory first**, then run `dotnet restore` and confirm it succeeds
        before continuing. The `project.assets.json` left over from the pre-CPM state makes restore
        fail with errors that point at packages, not at the migration — the wrong trail entirely.
        A half-migrated solution does not build.
   - **Lock files** (**only if the user accepted question 4** — never as a side effect of the CPM
     migration, which is a separate decision they may well have answered the other way) — add
     `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>` to `Directory.Build.props`
     and run `dotnet restore`. The generated `packages.lock.json` files belong in version control —
     leave them in the working tree, list them in the report, and tell the user they must be
     committed for CI to work. You do not commit them yourself. CI restores in locked mode, which
     fails when the locks and the projects disagree.

     **If they declined, do not write the property**, and say so where it matters: `make ci` and
     both CI templates pass `--locked-mode`, which fails outright with no lock file present. Drop
     that flag from the Makefile and the pipeline in the same run, and record the pair in the
     report — a restore flag that outlives the artifact it depends on is the sort of breakage that
     surfaces first in someone else's pull request.

2. **`Directory.Build.props`** — repo root. If one exists, merge property groups; never drop
   properties you did not add.
3. **`.editorconfig`** — start style rules at `suggestion` severity when the repo has debt.
   Under `TreatWarningsAsErrors`, anything at `warning` breaks the build.
4. **`Makefile`** — `make check` is the single confidence signal; `make ci` is what the pipeline
   calls. Keep both in one file.
5. **`lefthook.yml`** — pre-commit touches only staged files; pre-push runs `make check`.
6. **Secret scanning** (if opted in) — `gitleaks protect --staged` in pre-commit, `gitleaks detect`
   in CI. Create an empty `.gitleaksignore` with a comment explaining what belongs in it. Run
   `gitleaks detect` once against the working tree and **report what it finds before wiring the
   gate**: a repo with existing findings needs those triaged first, or the gate blocks every commit
   from day one.
7. **Arch-tests** — use the shape detected in Phase 1 step 9. Follow
   `references/architecture-discovery.md` to derive the candidate rules and
   `references/arch-tests.md` to build the project. Evaluate every candidate against the current
   code **before** writing it: rules that pass go in, rules that fail broadly become findings, not
   tests. Add the project to the solution with `dotnet sln add`.
8. **CI** — `templates/ci/github-actions.yml.template` or
   `templates/ci/azure-pipelines.yml.template`. The pipeline pins the SDK from `global.json` and
   runs `make ci`. It does not restate the steps.

   **The pipeline runs on every push to every branch**, not only on `main`. A gate that fires
   after the merge tells the team about breakage once it is already shared; on a feature branch it
   tells the person who wrote it, while they still have the context. Two things keep that
   affordable on GitHub Actions, and both are in that template: a second push to a branch cancels
   the first, and a pull request inside the repository does not trigger the same run twice.
   **Azure Pipelines has neither.** It offers no equivalent of `cancel-in-progress`, so pushes
   queue; if the organisation is on a single parallel job, add `batch: true` to the trigger, which
   waits for the running build and then queues one build covering every change since. That trades
   away per-commit results — a failure no longer points at a single commit — so only reach for it
   when the queue is a real problem. Mention the cost when you report: hosted runners bill by the
   minute, and Azure DevOps free tiers ship one parallel job.

   **Writing the file is not the same as having a pipeline.** GitHub Actions picks up anything in
   `.github/workflows/` automatically; **Azure DevOps does not** — a YAML file at any path is inert
   until somebody creates a pipeline in the web UI pointing at it (Pipelines → New pipeline → Azure
   Repos Git → Existing Azure Pipelines YAML file), and it only becomes a gate once it is added to
   the branch policy. Report control 8 as **written but not yet active** for Azure DevOps, with
   those two steps spelled out as the user's to-do. The same applies to GitHub branch protection.

**Never touch a `.csproj`** for anything that can live in `Directory.Build.props`. The only new
project file is the arch-test one.

After each control, run its command once and confirm it passes before moving on. A broken control
compounds.

**After writing either `.props` file, confirm MSBuild can load it** before doing anything else:

```bash
dotnet msbuild <any-project> -getProperty:TargetFramework
```

It is one second, and it converts the worst error in this whole run — `MSB4181`, which names no
file and no line — into a message that points at the offending line. See **Troubleshooting**.

---

## Phase 5 — Verify by breaking

Mandatory. Follow `references/verification.md`. Installing a gate proves nothing; making it fail
does.

For each installed control: snapshot the file, introduce the violation, confirm the gate catches
it, restore.

**Restore from the snapshot, not with `git checkout`.** Most of these files are files Phase 4 just
wrote: `git checkout -- global.json` either fails because the file is untracked, or restores the
version from *before* the install — silently undoing your own work. `git checkout` is safe only on
a pre-existing, committed source file you edited to trigger a break (controls 2, 3 and 7).

| # | Control | Violation | Expected |
|---|---|---|---|
| 1 | SDK pin | Set `version` one feature band above what is installed (10.0.400 → `10.0.500`) | `dotnet build` fails, naming the requested version, the `global.json` that asked for it, and the installed SDKs |
| 1b | Lock files *(only if accepted in Phase 3)* | Bump a version **wherever this repo declares it** — `Directory.Packages.props` under central package management, the `.csproj` otherwise — without restoring | `dotnet restore --locked-mode` fails, naming the project whose lock no longer matches |
| 2 | Strict build | Add an unused local variable | `dotnet build` fails |
| 3 | Style | Reorder the `using` directives in a file | `make lint` fails, naming the file |
| 4 | Entry point | No break needed | `make help` lists every target; `make check` chains them |
| 5 | Lefthook | Stage a badly formatted file and commit | The hook blocks the commit |
| 6 | Secrets | Stage a **non-allowlisted** fake credential — not `AKIAIOSFODNN7EXAMPLE`, which gitleaks ignores by design | `gitleaks protect --staged` blocks the commit |
| 7 | Arch-tests | Add the ORM `<PackageReference>` to the Application project, then a `using` in a service | `make test` fails, naming the rule |
| 8 | CI | Cannot be broken locally — verify by inspection | Installs the SDK from `global.json`, calls `make ci`, triggers on PRs |

Controls 5 and 6 are verified by attempting a **real commit**. If the hook does not fire, the
commit succeeds and the repo now has a commit the user never asked for. Undo it immediately with
`git reset --soft HEAD~1`, then fix the hook — and say what happened. Never leave a commit behind.

Restore every change. Then run `make check` and capture the real output.

**Do not report success with a gate in the red.** Fix it first.

---

## Phase 6 — Document and report

Installing eight controls makes parts of the repo's documentation wrong. Follow
`references/documentation.md`: it maps which document goes stale and which section to edit.

**Update what exists; do not create the doc pack.** If a document is missing, report the gap and
point at `/arkandia:agent-context-dotnet`.

**One fact, one home.** Every fact belongs in exactly one document; the others link to it. Copying
the CI steps into three files means two of them are wrong within a month, and an agent reading the
stale copy acts on it. `references/documentation.md` carries the canonical-home table — follow it,
and when you catch yourself writing something a second time, write a link instead.

Required, in order:

1. **`AGENTS.md`** (or `CLAUDE.md`) — setup with `make hooks`, a **Checks to run** section, the
   **change-based check matrix**, the layering rules now enforced, and the CI paragraph. A gate the
   agent does not know about does not get used. This step is not optional.
2. **`README.md`** — the per-OS prerequisites table (Lefthook, and `make`, which does not ship with
   Windows), `make hooks` in setup, and `make check` as the verification step.
3. **The rest, if present** — `docs/infrastructure.md` (prerequisites, CI/CD), `docs/dotnet.md`
   (SDK pin, build props, quality-controls table, test organisation), `docs/architecture.md` (the
   dependency rule is now enforced), `docs/claims-ledger.md` (new claims).

Then report:

- tree of files created and modified, with config and documentation listed separately;
- resolved package versions — what NuGet actually picked;
- the real `make check` output, green;
- every suppressed warning and its reason;
- architecture rules **not** written, and why (a rule the current code violates is a finding, not a
  test);
- what was deliberately left out.

**Restate the migrations the team declined**, if any: central package management, lock files, or
centralising the target framework. Each is pure determinism and each touches every `.csproj` —
which is exactly why they are offered rather than applied. Restate them with the evidence this run
produced (the version drift you measured, the projects that disagree), not as a generic pitch. If
the team accepted all three in Phase 3, this section is empty — say nothing.

Do not commit. Leave the changes for the user to review.

## Troubleshooting

Symptoms whose error message does not name the real cause. Check these before investigating
anything else.

| Symptom | Almost always | Confirm with |
|---|---|---|
| `MSB4181: The "RestoreTask" task returned false but did not log an error` — no file, no line | A `--` inside an XML comment in `Directory.Build.props` or `Directory.Packages.props`. XML forbids it, so MSBuild cannot load the file | `dotnet msbuild <any-project> -getProperty:TargetFramework` — it names the file and line |
| A property in `Directory.Build.props` has no effect and the build reports nothing | A second `Directory.Build.props` in a subdirectory shadows it — MSBuild stops at the first one it finds walking up | `dotnet msbuild <project> -getProperty:<TheProperty>` shows the value actually in force. See `references/inspection.md` §6b |
| Restore fails on packages immediately after the CPM migration | Stale `project.assets.json` from the pre-CPM state | Delete every `obj/`, restore again |
| `dotnet build` fails on files nobody touched, right after installing `.editorconfig` | The repo was never formatted to these rules; `EnforceCodeStyleInBuild` now enforces them | `dotnet format --verify-no-changes` for the file count. This is a Phase 3 question, not a surprise |
| `gitleaks protect` passes on your test secret | The string is allowlisted — `AKIAIOSFODNN7EXAMPLE` and other published examples are ignored by design | Retry with a fake key that is not a documentation sample |
| The arch-test break does not compile | Application has no reference to the ORM, which is exactly what the rule asserts. A `using` alone cannot work | Add the package reference first, then the `using` |
| `CS0118: 'Architecture' is a namespace but is used like a type` in the arch-test project | The project's own namespace contains a segment that shadows ArchUnitNET's `Architecture` type — `<Root>.Architecture.Tests` is the usual culprit | Qualify it: `ArchUnitNET.Domain.Architecture`. The same collision hits any namespace segment that matches a type you import |
| The pre-commit hook does not fire | `lefthook install` was never run | `ls .git/hooks` — all `.sample` means no hooks are installed. Then `git reset --soft HEAD~1` to undo the commit that went through |

## References

This list is where the phase numbering lives. The reference files are named for what they contain,
not for where they are called from — several are used by more than one phase.

| Reference | Used by | What it covers |
|---|---|---|
| `inspection.md` | Phase 1 | The discovery checklist, step by step |
| `architecture-discovery.md` | Phase 1, Phase 4 | Detect the real architecture, then derive rules that pass |
| `arch-tests.md` | Phase 4 (control 7) | Arch-test project setup and the rule catalogue |
| `verification.md` | Phase 5 | The break-and-revert procedure, per control |
| `documentation.md` | Phase 6 | Which documents the install invalidates, and their canonical homes |
| `templates/` | Phase 4 | The file skeletons |

## Rules

- Do NOT hardcode SDK, framework, or package versions. Resolve them.
- Do NOT overwrite an existing config file without reading and merging it.
- Do NOT modify `.csproj` files for settings that belong in `Directory.Build.props`.
- Do NOT write arch-test rules the repository does not actually follow, and do NOT assume
  Clean Architecture — derive the shape from the reference graph.
- Do NOT report success until `make check` is green and every gate has been proven to fail.
- Do NOT commit or push.
- DO leave every exception commented with a reason and an owner.
- DO update the documentation the install invalidated — starting with `AGENTS.md` — and keep
  each fact in exactly one document, linked from the others.
- DO tell the user what you skipped and why.
