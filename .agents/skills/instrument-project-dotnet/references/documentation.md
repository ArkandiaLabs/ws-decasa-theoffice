# Documentation the install invalidates

> Called from **Phase 6** of `SKILL.md`.

Installing eight controls makes parts of the repo's documentation wrong. A setup section that
skips `make hooks`, a prerequisites table with no `make` in it, a quality-controls table that says
"missing" for things you just installed — each one sends the next reader, human or agent, down a
path that no longer exists.

**Update what exists. Do not create the doc pack** — that is `agent-context-dotnet`'s job. If a
document below is absent, say so in the report and move on.

The map comes from a repository that went through this end to end: seven files needed edits.

---

## One fact, one home

The failure mode here is not omission — it is copying. The CI steps get pasted into `AGENTS.md`,
`README.md` and `docs/infrastructure.md` because each felt like the right place. A month later two
of the three are wrong, and the reader who lands on a stale copy acts on it. For an agent, which
copy it read is arbitrary.

**Every fact lives in exactly one document. The others link to it.**

| Fact | Canonical home | Everyone else |
|---|---|---|
| How to install the tools (per OS) | `README.md` | link |
| How to set up the repo the first time | `README.md` | `AGENTS.md` names the command only |
| Which command verifies the repo (`make check`) | `AGENTS.md` | `README.md` links to it as the last setup step |
| Which command for which kind of change | `AGENTS.md` | nowhere else |
| Rules an agent must not break | `AGENTS.md` | the arch-test file names them in comments |
| CI platform, steps, runner, branch policy | `docs/infrastructure.md` | `AGENTS.md` gives one line + link |
| Configuration, secrets, deployment target | `docs/infrastructure.md` | link |
| SDK pin, build properties, analyzer posture | `docs/dotnet.md` | link |
| Quality-controls table (what is installed) | `docs/dotnet.md` | link |
| Test projects and what each covers | `docs/dotnet.md` | `AGENTS.md` names the command only |
| Layering and the dependency rule | `docs/architecture.md` | `AGENTS.md` states it in one line; the arch-tests enforce it |
| What was verified vs. still open | `docs/claims-ledger.md` | nowhere else |

Two tests before writing a paragraph:

1. **Have I written this already in this run?** If yes, replace it with a link.
2. **Does it already exist somewhere in the repo?** If yes, link to that — even if the wording is
   not yours.

The exception is a **command**, which may appear in more than one document when each audience needs
it in place: `make check` belongs in `AGENTS.md` as the contract and in `README.md` as the last
setup step. A command is one line and is verified by running it. A *description* of what CI does is
prose that rots — that one goes in a single place.

When `AGENTS.md` is a table of contents (it should be), it almost never holds the explanation. It
holds the pointer and the one-line rule.

## The map

| Document | What goes stale | Priority |
|---|---|---|
| `AGENTS.md` / `CLAUDE.md` | Setup, checks to run, check matrix, CI, layering rules | **Required** |
| `README.md` | Prerequisites table, setup steps, how to verify | **Required** |
| `docs/infrastructure.md` | Prerequisites, CI/CD section | High |
| `docs/dotnet.md` | SDK pin, build props, quality-controls table, test organisation | High |
| `docs/architecture.md` | The dependency rule is now enforced, not just described | Medium |
| `docs/claims-ledger.md` | New verifiable claims | Medium |
| `docs/adrs/` | One ADR for the instrumentation decision | Optional |
| `.gitleaksignore` | Header comment explaining what belongs in it (fingerprints, never paths) | If installed |

---

## `AGENTS.md` — required

The single most important edit. **A gate the agent does not know about does not get used.**

Four sections:

1. **Setup** — add `make hooks` as the first step, before restore.

2. **Checks to run** — one command, stated as the contract:

   ````markdown
   ## Checks to run

   ```bash
   make check    # single confidence signal: lint + build + test
   ```
   ````

3. **Change-based check matrix** — which command for which kind of change. This is what lets an
   agent pick the cheap check instead of always running everything:

   ```markdown
   ## Change-based check matrix

   - **Any `.cs` change:** `make check`
   - **New or changed test:** `make test`
   - **Style / imports:** `make lint`
   - **Layer or architecture change:** `make test` (enforced by the arch-tests)
   - **NuGet dependency:** `make build` (NuGet audit runs automatically)
   ```

4. **Non-obvious rules** — add the layering rules now enforced, pointing at the test project by
   name. Also add the mechanical traps the agent will otherwise hit: warnings are errors, style is
   verified in the build, hooks run on commit.

5. **CI** — one paragraph: which platform, which file, that it mirrors `make ci`, and what the
   branch protection requires.

Respect the file's ceiling. If `AGENTS.md` is a table of contents (it should be), push detail into
`docs/` and link.

## `README.md` — required

Written for a human on day one, so it carries what `AGENTS.md` does not: **how to get the tools**.

- **Prerequisites table**, per OS. Real example from the reference repo:

  | Tool | macOS | Windows |
  |---|---|---|
  | `make` | ships with Xcode CLT | `winget install ezwinports.make` |
  | `lefthook` | `brew install lefthook` | `winget install evilmartians.lefthook` |
  | `gitleaks` | `brew install gitleaks` | `winget install gitleaks` |

- **Setup steps** — `make hooks` first, then restore, then run.
- **A verification step** — "check everything is in order: `make check`".

## `docs/infrastructure.md`

- **Prerequisites** — add Lefthook, `make`, and gitleaks if installed, alongside the SDK and
  tooling already listed.
- **CI/CD** — replace or write the section: platform, pipeline file, trigger, the steps in order,
  the agent/runner, and the branch policy that turns the pipeline into a gate. State explicitly
  which gates run **only locally** (design-system checks, for instance) — that asymmetry is the
  kind of thing nobody remembers six months later.

## `docs/dotnet.md`

Three places, if the file exists:

- **Target frameworks / language posture** — the SDK is now pinned (give the version and
  `rollForward` value), and `Directory.Build.props` centralises the TFM, nullable, analyzers,
  warnings-as-errors and NuGet audit.
- **Package management** — if central package management was installed, this is the biggest change
  to how the repo is edited day to day: **a `<PackageReference>` must not carry `Version` any
  more**. Agents get this wrong constantly. State it here and add the one-line rule to
  `AGENTS.md`'s non-obvious rules. Note the lock files and that CI restores with `--locked-mode`.
- **Quality controls table** — the format that repo uses is exactly right: one row per control,
  status, and a note naming the file that implements it. Update every row you changed.
- **Test organisation** — the arch-test project is new; name it, say what it enforces, and note
  that `dotnet test` runs it with the rest.
- **Traps / hotspots** — anything the install surfaced: suppressed warnings, version drift,
  a rule that could not be enforced.

## `docs/architecture.md`

Usually one sentence, but a load-bearing one: the dependency rule described here is now **enforced
by `<project>.ArchitectureTests`**, not only by project references. Link the test project.

If Phase 1 found the docs and the graph disagreeing, this is where the correction goes.

## `docs/claims-ledger.md`

If the repo has one, append the new verifiable claims with source and confidence: the SDK version
pinned, the CI platform and file, the number of test projects, the controls installed. Everything
you write here is `high` confidence — you just built it and watched it fail.

## `docs/adrs/`

Optional, and only offer it — do not write it unsolicited. If the team records decisions as ADRs,
"adopt deterministic instrumentation" is a real decision with real consequences (slower commits,
a warning-debt list to drain, a new required check on PRs). Context, decision, consequences —
easier and harder.

---

## Rules

- **Update, do not rewrite.** These documents have authors. Edit the sections the install
  invalidated and leave the rest alone.
- **Do not create documents that do not exist.** Report the gap and suggest
  `/arkandia:agent-context-dotnet`.
- **Match the document's language.** These are the only files where you do not write English: a
  Spanish `AGENTS.md` edited in English becomes two documents in one. Write Spanish **with correct
  diacritics** — `diseño`, `versión`, `código`, `línea`, `organización`. Accent-stripped Spanish
  reads as broken text, and these files go to a client. Code fences and file names inside the prose
  stay as they are.
- **Every command you document must be one you ran.** No aspirational instructions.
- **Report the doc edits separately** from the config files, so the user can review them as prose.
