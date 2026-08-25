# Discovery checklist

> Called from **Phase 1** of `SKILL.md`.

Read real files. Where a fact is not readable, ask in Phase 3 — never guess. Everything here is
read-only.

---

## 1. Confirm this is a .NET repository

Glob for `*.sln`, `*.slnx`, `*.csproj`, `*.fsproj`, `*.vbproj`, `global.json`,
`Directory.Build.props`.

Also check for **file-based apps** (.NET 10+): standalone `.cs` files carrying `#:package`,
`#:sdk`, `#:project` or `#:property` directives, or a `#!/usr/bin/env dotnet` shebang. These have
no project file and a `*.csproj` glob misses them. A file-based app cannot carry most of these
controls — say so and stop.

If nothing matches, tell the user this skill only applies to .NET repositories. Write nothing.

## 2. Solution and project graph

```bash
# -prune skips obj/ and bin/ instead of filtering their output afterwards, and -print0 keeps
# paths with spaces intact — repository paths under "Documents" or "My Projects" are common.
find . \( -name obj -o -name bin -o -name .git \) -prune -o \
     \( -name '*.slnx' -o -name '*.sln' \) -print0 | xargs -0 -n1 echo
dotnet sln <solution> list
```

Quote every path you pass onward. A solution at `/Users/me/My Repos/App/src/App.slnx` breaks any
command that interpolates it unquoted, and it lands in the Makefile, `lefthook.yml` and CI.

Record:

- **Solution path** — it drives every command in the Makefile, extension included. It is often
  `src/<Name>.slnx` or `src/<Name>.sln`, not the repo root.
- **Root namespace prefix** — e.g. `TheOffice.`. The arch-test rules key off this.
- **Layer names** — the actual project names, not the ones you expect. `Persistence` vs `Data` vs
  `Infrastructure` matters when writing namespace regexes.
- **Project reference graph** — read the `<ProjectReference>` elements. This tells you the layering
  the repo *currently* has, which is what the arch-tests must encode.
- **Multiple solutions** — if there is more than one, ask which is authoritative.
- **Both `.sln` and `.slnx` for the same solution** — a half-finished migration, not two
  solutions. Do not ask which is authoritative: point out that the two files will drift the
  moment someone adds a project, name the one you will wire into the Makefile and CI, and
  suggest deleting the other. `.slnx` has been supported since SDK 9.0.200 and is the default
  for `dotnet new sln` from .NET 10 on, so it is normally the one to keep.

## 3. Target frameworks

Grep `<TargetFramework>` and `<TargetFrameworks>` across all project files.

- If every project agrees, that TFM goes into `Directory.Build.props`.
- If they diverge, this is a Phase 3 question. Do not normalise silently — a project on an older
  TFM usually has a reason.
- Multi-targeting (`<TargetFrameworks>`) means the TFM must **not** be moved into
  `Directory.Build.props`. Leave those projects alone.

## 4. Test setup

Find test projects and read them.

| Signal | Framework | Runner |
|---|---|---|
| `xunit` 2.x + `xunit.runner.visualstudio` | xUnit v2 | VSTest |
| `xunit.v3` / `xunit.v3.mtp-*` | xUnit v3 | Microsoft.Testing.Platform |
| `NUnit` + `NUnit3TestAdapter` | NUnit | VSTest |
| `MSTest.TestFramework` | MSTest | either |
| `TUnit` | TUnit | Microsoft.Testing.Platform |
| `global.json` with `"test": { "runner": "Microsoft.Testing.Platform" }` | — | Microsoft.Testing.Platform |
| `<OutputType>Exe</OutputType>` in a test project | — | Microsoft.Testing.Platform |

Record the framework's **major version**, not just its name — MSTest 2 and MSTest 4 need different
ArchUnitNET extension packages, and so do xUnit v2 and v3. The mapping lives in `arch-tests.md`.

This decides how the arch-test project is created. **The arch-test project must match the rest of
the repo** — mixing runners in one solution makes `dotnet test` behave inconsistently.

Also record where tests live: `tests/` at the repo root and `src/Tests/` are both common. Do not
assume.

## 4b. Existing code style

The `.editorconfig` has to encode what the code already does, so measure it rather than assuming
the language default. Indentation is the one that reformats the whole repository when guessed
wrong:

```bash
# Modal leading-space count across the .cs files — the repo's real indent unit.
# Walk from the root, not from `src tests`: a repo that lays its projects out any other way would
# measure nothing, and you would write an .editorconfig with no evidence behind it — which is
# exactly what this section exists to prevent. If the command finds no lines, say so and ask;
# do not fall back to the language default in silence.
# Spaces: the modal leading-space count is the repo's indent width.
find . -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' -not -path './.git/*' \
  -exec grep -hoE '^ +' {} + \
  | awk '{print length($0)}' | sort -n | uniq -c | sort -rn | head -5

# Tabs: one number, to compare against the space total above.
find . -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' -not -path './.git/*' \
  -exec grep -hcE '^\t' {} + | awk '{n+=$0} END {print n+0, "tab-indented lines"}'
```

**Count the tabs separately, and report both.** `^ +` matches spaces only, so a tab-indented repo
measures nothing at all and lands in the same place as a repo with no evidence — which, per the
paragraph above, means the answer becomes a question rather than a guess, but a needless one. If
the tab count dominates, the answer is `indent_style = tab` and there is no width to infer. If
both are non-trivial the repo is mixed, and that is a finding to report before writing anything.

A dominant count of 2 means the repo indents with 2 spaces, whatever the .NET default is. Check
`AGENTS.md` and any style section in the docs too — when the docs state a convention and the code
follows it, that is the answer. When they disagree, the code wins and the disagreement is a
finding.

Also record: tabs or spaces, brace placement (same line vs. own line), and whether `using`
directives are grouped with blank lines between groups. Each is a rule you are about to enforce.

## 5. Package management

- **Central package management** — does `Directory.Packages.props` exist? If so,
  `<PackageReference>` elements must carry no `Version`; versions go in `<PackageVersion>`.
- **Lock files** — is `RestorePackagesWithLockFile` set, and are `packages.lock.json` files
  committed? If yes, restore must run with `--locked-mode` in CI, and adding a package regenerates
  the lock file.
- **Version drift** — grep for the same package at different versions across projects. Worth
  reporting as a finding even though fixing it is out of scope.
- **NuGet sources** — does a `NuGet.config` exist, and does it clear inherited sources?

## 6. Existing controls

For each of the eight, record `present` / `partial` / `missing` **and what it contains**:

| Control | Look for | "Partial" looks like |
|---|---|---|
| SDK pin | `global.json` | File exists but has no `sdk` block — common when it only sets the test runner |
| Strict build | `Directory.Build.props` | Exists but no `TreatWarningsAsErrors`, analyzers off, or **a second copy in a subdirectory that shadows the root one** |
| Style | `.editorconfig` | Formatting only, no diagnostic severities or naming rules |
| Entry point | `Makefile`, `justfile`, `*.ps1`, `package.json` scripts | A Makefile without a `check` target |
| Shift-left | `lefthook.yml`, `.husky/`, `.pre-commit-config.yaml` | Hooks configured but never installed (`.git/hooks` still all `.sample`) |
| Secrets | `.gitleaks.toml`, `.gitleaksignore`, gitleaks in a hook or a pipeline | Gitleaks in CI only, so the secret is already in the history by the time it fires |
| Architecture | Test project referencing `ArchUnitNET`, `NetArchTest`, `NsDepCop` | Project exists with no rules, or rules that are skipped |
| CI | `.github/workflows/`, `.azdevops/`, `azure-pipelines.yml` | Pipeline that builds but runs no gates |

**A partial control is more dangerous than a missing one** — the team believes it is covered. Call
these out explicitly in the report.

## 6b. Where MSBuild actually reads properties from

```bash
find . -name 'Directory.Build.props' -o -name 'Directory.Build.targets' | grep -v -E 'obj/|bin/'
```

**MSBuild walks up from each project and stops at the first `Directory.Build.props` it finds.** One
at `src/Directory.Build.props` therefore makes a root-level file dead for every project under
`src/` — which, in a repo laid out as `src/<Layer>/<Project>`, is all of them. Writing a new root
file in that situation installs a control that never runs, and the build shows no error at all.

If a nested file exists, you have two honest options: **merge into the nested file** instead of
writing at the root, or add an explicit import to it so the root file is chained:

```xml
<Import Project="$([MSBuild]::GetPathOfFileAbove($(MSBuildThisFileName)$(MSBuildThisFileExtension), $(MSBuildThisFileDirectory)..))" />
```

Confirm whichever you choose actually took effect:

```bash
dotnet msbuild <any-project> -getProperty:TreatWarningsAsErrors
```

The same precedence applies to `Directory.Packages.props` and `.editorconfig` — for `.editorconfig`
the nearest file wins per setting, and `root = true` stops the search.

## 7. CI platform

```bash
ls .github/workflows/ .azdevops/ 2>/dev/null
find . -maxdepth 2 -name "azure-pipelines*.yml" -o -maxdepth 2 -name "Jenkinsfile" -o -maxdepth 2 -name ".gitlab-ci.yml"
```

If exactly one platform is present, use it. If none or several, ask in Phase 3.

For an existing pipeline, read it: does it already run gates, and does it duplicate what the
Makefile will do? Converging it to `make ci` is part of the work.

## 8. Context documents

Read, don't just detect:

- **`AGENTS.md` / `CLAUDE.md`** — is there a "Checks to run" section? Phase 6 updates it.
- **`docs/architecture.md`** — the layering rules stated here are the arch-test candidates.
- **`docs/adrs/`** — decisions like "services return `Result` instead of throwing" or "never expose
  the internal `Id`" are conventions a compiler cannot enforce. They are the second round of
  arch-tests.
- **`README.md`** — the setup steps Phase 6 has to extend.

**The rule for arch-tests:** any rule written in these docs that an agent could violate is a
candidate. A rule that is documented but not verified is a suggestion.

## 9. Environment facts

```bash
dotnet --version
dotnet --list-sdks
uname -s        # Darwin / Linux; on Windows check $OS or PowerShell
make --version
lefthook version
git config core.hooksPath
```

Report the SDK version installed — it becomes the pin — and flag any mismatch with an existing
`global.json`.
