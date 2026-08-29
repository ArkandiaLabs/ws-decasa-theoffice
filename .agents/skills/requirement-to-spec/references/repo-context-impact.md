# Repo context and consumer impact — generic, not .NET-first

This is the step that reads the target repository the way the delivery skills
(`linear-plan-build`/`ado-plan-build`) explore before planning — along the repo's own seams, no
architecture assumed — plus one thing they never have to do: deciding whether the change this
document describes is allowed to break something nobody mentioned.

## Reading context (Phase 1)

`AGENTS.md`, `CLAUDE.md`, `docs/` (including ADRs) at the root of the target repo. If none exist,
infer what you can from the code and mark it explicitly as a **provisional inference**, not a fact
— the same rule the delivery skills use when a repo documents nothing. This step never fails the
run; a repo with no context docs
still gets a spec, just one with fewer facts to lean on.

## Detecting a public contract (requirement, always active)

**Scope: public contracts the change plausibly touches, not "any business impact anywhere."** The
general ambiguity sweep in `references/interview.md` already covers business impact broadly; this
step is narrower and mechanical — it looks for a concrete, nameable interface.

Detect, stack-agnostically:

- **OpenAPI/Swagger** — `openapi.yaml`/`openapi.json` anywhere in the repo, or a controller/handler
  layer that would generate one (ASP.NET Core controllers, Express routes, FastAPI routers, Spring
  `@RestController`, and equivalents — read what the repo actually has, don't assume a framework).
- **GraphQL** — a `.graphql` schema file, or resolver definitions.
- **`.proto`** files — gRPC or any protobuf-defined contract.
- **Exported symbols of a library/package** — a public API surface meant for other code to import,
  not an internal module.

For each contract the document's described change would plausibly touch:

1. **List who consumes it** — other projects in the same repo that reference it, generated clients,
   `docs/` pages that mention it by name.
2. **Ask, explicitly, whether to break it or keep it compatible** — this question is not
   conditional on the document itself raising the topic. A document that describes changing a
   response shape without mentioning existing callers is exactly the case this exists to catch.

**If the repo exposes no recognizable public contract at all, do not ask a generic version of this
question.** Report "no public contract detected — step skipped" and move on. A question with no
concrete interface to name is not something the user can act on, and inventing a stand-in question
just to have asked something is worse than skipping it — the same principle the delivery skills
apply to a gate a repo does not define: name the omission, don't manufacture a substitute.

## Detecting the documentation the change will invalidate (requirement, always active)

The contract scan above asks "what breaks for another *program*". This one asks "what becomes
**false** for another *person*" — and it is the step whose absence is easiest to miss, because the
run looks complete without it. A requirement that changes a database schema is understood as code
changes and stops there; meanwhile `docs/database.md` still describes the old columns, and the next
person to read it acts on a document that is now wrong.

**This skill never edits those documents.** That is the delivery skills' job, and the whole point of
the pipeline. What this step produces is a **documentation task per affected document**, each its
own item, placed by the rule `interview.md` already carries: **after the functional work it
describes, blocking nothing.** A page is rewritten to match what was built, so it cannot be built
first — and a code task marked blocked by a documentation task stalls the delivery on prose.

### The inventory (Phase 1)

Read what the repo actually has; assume none of it exists. Stack-agnostic, by role rather than by
filename:

| Role | Typical homes | Goes stale when |
|---|---|---|
| Architecture / design | `docs/architecture*.md`, `docs/design*.md`, C4 or diagram files | A component, boundary or dependency moves |
| Data model / schema | `docs/database*.md`, `docs/data-model*.md`, ERDs, a documented migration history | A table, column, index or relation changes |
| API / integration | `docs/api*.md`, a hand-written endpoint list, Postman/Insomnia collections | A route, payload or status code changes |
| Decisions | `docs/adr/`, `docs/decisions/` | A prior decision is superseded — **a new ADR, never an edit to the old one** |
| Agent context | `AGENTS.md`, `CLAUDE.md` | A convention, command or boundary the agent relies on changes |
| Onboarding / operations | `README.md`, runbooks, `docs/operations*.md` | Setup, a command, or a failure procedure changes |
| Domain language | a glossary, a ubiquitous-language doc | A term the requirement renames or redefines |

### The rule that keeps this honest: evidence, not category

**Never list a document because its category sounds related.** List it because you opened it and
found a specific line the change makes false — and **quote that line** in the report and in the
task. "`docs/database.md` documents the `stock` table, which this change adds two columns to" is
actionable. "The architecture doc may need review" is noise, and noise here is expensive: it fills
the breakdown with tasks nobody can close.

The mechanic is a grep, not a guess. Take the concrete things the requirement touches — table
names, endpoint paths, module names, domain terms — and search the doc set for each. A hit is
evidence. No hit is a real answer: **say the document exists and appears unaffected**, rather than
adding it "just in case".

A repo with no documentation at all is a valid outcome. Report "no project documentation found —
nothing to invalidate", and do **not** propose creating a doc set; that is `agent-context-dotnet`'s
job, and proposing it here quietly turns a spec into a documentation project.

### Turning it into questions and tasks (Phase 3)

Present the affected documents **as a list with the evidence attached**, and ask in a single
`AskUserQuestion` which ones get updated in this pass. This is a scope decision, not a derivation:
four documentation tasks bolted onto a two-task change is a real widening of scope, and the skill
does not get to make it. Whatever the user excludes goes in the report's **Out of scope** section,
named — a document known to be going stale and deliberately left alone is a decision worth being
able to find later.

**Confirmed is not promoted.** A document the user keeps becomes one ordinary task after the
functional work, never a prerequisite of it and never a blocker of anything. The single exception
is a document the code is written *against* — a contract or schema declaration, an ADR recording
the decision a task implements — and it has to name the functional task that cannot start without
it.

ADRs are the one asymmetric case: a superseded decision is **never** edited. The task is "write a
new ADR that supersedes `ADR-00N`", and it says so.

## Cross-checking a tabular attachment against real data

**When this runs: Phase 3, before the ambiguity sweep** — it is requirement 2 in
`references/interview.md`, not an optional extra. Phase 1 only *detects* the server; the query
itself belongs here, where a discrepancy can still become a question the user answers. A check
deferred past the interview never runs at all.

If Phase 1 detected a database MCP server — by the **shape** of its tools (something that looks
like `query`/`execute`/`list_tables`, whatever the server happens to be named; never hardcode a
specific server's name) — and the document or one of its attachments describes data that should
exist in that database (a catalog, a price list, an inventory count), query the real numbers and
compare.

**Report discrepancies with the concrete numbers on both sides**, never in the abstract. "The
database and the attachment disagree" tells the user nothing they can act on; "the database has 25
units of X, the attachment lists 11" does.

If no database MCP server is connected, say so in the report and move on — this is not a blocking
step. A spec that could not cross-check its data is still useful; one that silently skipped the
check without saying so is not.

## Fan-out

Fan out `Explore` subagents along the repo's own module/package/service boundaries only when the
repo is large enough that reading it serially would be slow — the same judgment call the delivery
skills make. For a single contract file or a small repo, read it inline; spinning up agents to read
one file is waste, and the discipline is saying so rather than doing it anyway.
