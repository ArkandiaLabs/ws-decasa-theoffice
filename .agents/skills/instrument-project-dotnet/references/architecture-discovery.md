# Deriving architecture rules from the repository

> Called from **Phase 1** (classify the shape) and **Phase 4** (derive the rules) of `SKILL.md`.

Control 7 fails in one specific way: assuming an architecture the repo does not have. Rules copied
from a Clean Architecture template land red on day one in a repo organised by feature, and the
team learns that arch-tests are noise.

**The rule: encode what the repository already does, not what it should do.**

Every rule you write must pass the moment you write it. An arch-test suite that goes green on
install and red on the next violation is a working sensor. One that goes red on install is a
refactoring proposal wearing a test's clothes — and nobody asked for it.

---

## 1. Build the dependency graph

Read every `<ProjectReference>` in the solution. That graph is the architecture as actually built,
regardless of what the docs claim.

```bash
dotnet sln <solution> list
grep -r "ProjectReference" --include="*.csproj" .
```

Record, per project: who it references, who references it, and whether it is a test project,
a library, or an entry point (`Microsoft.NET.Sdk.Web`, `<OutputType>Exe</OutputType>`).

**Fix the direction of the graph once, and use it for the rest of the run.** An edge runs *from*
the project that declares the `ProjectReference` *to* the project it names — so edges point from
the host inwards. Both ends are then unambiguous, and the two words mean one thing each:

- **Root — no outgoing references.** It depends on nothing else in the solution. In a layered
  design this is the domain; in a feature-organised design there is usually a small shared kernel
  instead.
- **Leaf — no incoming references.** Nothing in the solution depends on it. This is normally the
  entry point (`Api`, `Web`, `Worker`). A second leaf that is *not* an entry point and *not* a test
  project is usually dead code, and worth reporting as a finding.

## 2. Read what the repo says about itself

- `docs/architecture.md` or `ARCHITECTURE.md`
- `docs/adrs/` — an ADR that names a pattern is the strongest signal there is
- `AGENTS.md` — a "non-obvious rules" section often states the layering in one line
- The README

**When the docs and the graph disagree, the graph wins** — and the disagreement is a finding worth
reporting. Documented-but-violated is exactly the drift these tests exist to stop.

## 3. Classify the shape

| Shape | Signals | Rules that fit |
|---|---|---|
| **Layered** (Clean / Onion / Hexagonal) | Project or folder names like `Domain`, `Core`, `Application`, `Infrastructure`, `Persistence`, `Adapters`, `Api`, `Web`; a project that references nothing | Dependency direction between layers; the core references no infrastructure; the app layer does not reference the ORM |
| **Vertical slices / feature folders** | `Features/`, `Modules/`, `Slices/`; namespaces like `App.Orders.*`, `App.Billing.*`; few projects, many folders | Features do not reference each other; everything may reference a shared kernel; no cycles between features |
| **Modular monolith** | One project per module plus a `Shared`/`Common`; module projects with no references between them | Modules talk only through contracts or the shared project; no module-to-module references |
| **N-tier / classic** | `Web` → `BLL` → `DAL`; names ending in `Business`, `Data`, `Services` | Strict tier direction; the web tier does not reference the data tier |
| **Flat / no discernible structure** | One or two projects, no layering, no feature grouping | Only the universal rules below |

Do not force a repo into a shape. If the signals are mixed or weak, say so and fall back.

## 4. Universal rules — safe in any shape

These hold regardless of architecture and are the floor when nothing else is detectable:

- **No cycles** between projects or namespace slices. A cycle makes the design unlearnable and
  breaks incremental builds.
- **Nothing references the entry point.** The host project (`Api`, `Web`, `Worker`) has no
  incoming references — the leaf, in the direction fixed in step 1.
- **Production code does not reference test projects.**
- **No dependency on a project that is not in the solution's reference graph** — catches stray
  assembly references.

Even a flat repo gets value from these, and they can never be wrong.

## 5. Verify before proposing

For each candidate rule, **evaluate it against the current code before writing it into the test
project.** Three outcomes:

| Result | Action |
|---|---|
| Passes | Write it. This is the rule you wanted |
| Fails with 1–2 violations | Report the violations to the user and ask: fix the code now, or skip the rule? Do not decide alone |
| Fails broadly | Do not write it. Report it as a finding: "the docs say Application must not use the ORM; 14 types violate it." That is a conversation, not a test |

This step is what keeps the suite green on install.

## 6. Confirm with the user

Present the detected shape, the evidence, and the proposed rules — then get agreement before
writing the project. Format:

> Detected shape: **layered**, 5 projects.
> Evidence: `TheOffice.Domain` references nothing; `Application` references only `Domain`;
> `Persistence` and `Adapters` reference `Application`; `Api` references all three.
> `docs/architecture.md` §2 states the same rule.
>
> Proposed rules (all pass against the current code):
> 1. Domain depends on no other layer
> 2. Application does not depend on Persistence, Adapters or Api
> 3. Application does not reference EF Core
> 4. No cycles between layers
>
> Not proposed: "services return `Result`" — ADR-0002 states it, but `ClientService.GetByPublicId`
> returns a nullable, so the rule would fail. Worth a conversation.

Then write the rules using the patterns in `arch-tests.md`, substituting the real project and
namespace names.

## 7. When there is no architecture to protect

A repo with two projects and no layering does not need six arch rules. Write the universal ones,
say plainly that the shape does not support more, and move on. Padding the suite with rules that
assert nothing is worse than a small suite — it teaches the team that the tests are decoration.
