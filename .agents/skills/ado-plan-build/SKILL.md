---
name: ado-plan-build
description: >
  Take an Azure Boards work item from "read it" to "PR open, Pipelines green, review
  comments addressed, work item updated" with maximum autonomy. Reads the work item
  and its discussion through either the Azure DevOps MCP server or the `az` CLI;
  grills you on the design decisions the item left open; explores the repo; drafts a
  plan and puts it through a three-lens adversarial review; asks for your approval
  through plan mode when the change warrants it and skips it when it doesn't; then
  implements test-first, runs your repo's own gates, opens an Azure Repos PR, and
  babysits it to green. Stack-agnostic — assumes no particular architecture.
  Invoke with `/arkandia:ado-plan-build [work item id | URL] [skip-checkpoint]`.
argument-hint: "[work item id (2, #2, or its URL)] [skip-checkpoint]"
disable-model-invocation: true
allowed-tools: Read, Glob, Grep, Edit, Write, AskUserQuestion, Agent, Skill, TaskCreate, TaskUpdate, TaskList, TaskGet, EnterPlanMode, ExitPlanMode, Monitor, ScheduleWakeup, Bash(git status*), Bash(git diff*), Bash(git add*), Bash(git commit*), Bash(git log*), Bash(git rev-parse*), Bash(git symbolic-ref*), Bash(git fetch*), Bash(git checkout*), Bash(git switch*), Bash(git pull*), Bash(git push*), Bash(az boards*), Bash(az repos*), Bash(az pipelines*), Bash(az devops*), Bash(az account show*), Bash(az extension list*), Bash(make *), Bash(npm *), Bash(npx *), Bash(pnpm *), Bash(yarn *), Bash(pytest*), Bash(python *), Bash(python3 *), Bash(uv *), Bash(go *), Bash(cargo *), Bash(dotnet *), Bash(mvn *), Bash(gradle *), Bash(./gradlew*), Bash(bundle *), Bash(rake *), Bash(composer *), Bash(php *), mcp__azure-devops__wit_get_work_item, mcp__azure-devops__wit_list_work_item_comments
---

# Azure Boards work item → shipped feature

Take an Azure Boards work item all the way to **PR open, Pipelines green, review
comments addressed, and the work item updated**. There is exactly **one** explicit
checkpoint — plan approval — and even that is conditional: routine changes run
straight through.

It is **stack-agnostic** — .NET repositories are one case it handles, not what it
assumes.

**Read `references/build-loop.md` now.** It is the body of this skill, not optional
background. This file supplies the Azure DevOps bindings it asks for.

## Autonomy contract

- **Act and self-verify by default.** No option menus, no "should I proceed?" on green.
- **Work-item writes on this item are pre-authorized** — its state and its discussion
  comments. Never ask permission for those, and never write anything else in Boards.
- **This skill pushes branches and opens pull requests without asking.** It never
  completes a PR, never sets auto-complete, never bypasses a branch policy, and never
  deploys.
- **Escalate only** for the cases listed in `references/build-loop.md` § Escalation.

## Arguments

Parse `$ARGUMENTS`:

- **Work item id** — bare digits (`2`), digits with a leading `#` (`#2`), or an Azure
  DevOps work item URL (`.../_workitems/edit/2`). Extract the integer id. If absent,
  ask for one.
- **`skip-checkpoint`** — or freeform "skip the plan checkpoint" / "run straight to
  PR". Forces the Step E skip for routine items. Honor it only when explicitly given,
  and never over a user who asked to see a plan.

There is no brief-file or inline-description path. This skill starts from a work item.

## Phase 0 — Resolve the access path

Azure DevOps is reachable two ways, and this skill supports both. Detect in order:

1. **MCP** — the session exposes `mcp__azure-devops__*` tools.
2. **CLI** — otherwise, `az` is installed, the `azure-devops` extension is present,
   and `az account show` succeeds.
3. **Neither** — stop and report both setup options.

**Say which path you took**, and use it for the whole run. `references/ado-access.md`
holds the operation-by-operation mapping, the discovery steps for the two operations
the CLI has no first-class command for, and the auth troubleshooting for both paths.
**Read it before your first Azure DevOps call.**

## Phase 1 — Read the work item

Fetch the item and its discussion. The project is **never hardcoded**: use the one the
caller names, the one configured for the repo, or the one auto-detected from the git
remote — and if you can't determine it, ask before fetching.

Read `System.Title`, `System.Description`, `System.State`, `System.WorkItemType`, and
`System.Tags`, then the comments — **requirements are often negotiated in the
discussion rather than written in a field.**

Two things that bite, both worth getting right:

- **Acceptance criteria live in different places per process.** The **Basic** process
  `Issue` type has **no** acceptance-criteria field — the whole requirement is in
  `System.Description`. Only Agile/Scrum types (`User Story`, `Product Backlog Item`)
  define `Microsoft.VSTS.Common.AcceptanceCriteria`. Read `System.WorkItemType` first
  and ask for that field **only when the type defines it**. Never assume it exists,
  and never treat its absence as an empty requirement.
- **`System.Description` comes back as HTML**, not Markdown — and so do acceptance
  criteria where present. Render to text before reasoning over them, and don't let
  stray tags leak into the plan.

Two failure modes to handle rather than paper over. If the fetch fails, **stop and
report the error verbatim** — do not invent a requirement from the id, and do not
proceed on a partially-read item. If the description is empty (and there's no
acceptance-criteria field, or it's empty too), say so and ask: an id is a pointer, not
a specification.

## Phase 2 — Prepare the git environment

1. Default branch: `git symbolic-ref refs/remotes/origin/HEAD | sed 's@^refs/remotes/origin/@@'`.
2. `git fetch origin`.
3. **If the working tree is dirty, stop and report.** Do not stash, do not discard.
4. `git checkout <default-branch> && git pull --ff-only origin <default-branch>`.
5. Create `feature/<id>-<short-slug>`. The branch name **must** contain the work item
   id. If you are already on that branch with prior work on it, stay on it.

## Phase 3 — Present the work item summary

Print a concise summary: title, type, state, assignee, area and iteration path, tags,
branch name, linked and child items, and the decisions buried in the discussion.

**Board state is not authoritative.** Flag every linked blocker that isn't done, and
before treating a dependency as met, confirm it against `git log` and the code rather
than against a column on a board.

## Phase 4 — Run the build loop

Follow `references/build-loop.md`, Steps A → J, with these bindings. Exact commands
per access path are in `references/ado-access.md`.

| Binding | Azure DevOps |
|---|---|
| `TICKET` | the work item, plus any child items you work |
| `STATUS→IN-PROGRESS` | set the item's in-progress state — see **States** below |
| `STATUS→IN-REVIEW` | set the item's review state — see **States** below |
| `COMMENT` | add to the item's discussion |
| `BRANCH` | the Phase 2 branch |
| `LINK-TOKEN` | **`AB#<id>`** in the commit message — that exact syntax is what makes Boards attach the commit; a bare `#2` does nothing |
| `OPEN-PR` | `az repos pr create … --work-items <id>`, or the MCP equivalent |
| `CI` | Azure Pipelines runs for the branch — `az pipelines runs list --branch <b>`, then `az pipelines runs show --id <run>` |
| `PR-COMMENTS` | the PR's comment threads — `az repos pr show`, plus thread discovery per `references/ado-access.md` |

**States are per-process, so read them, don't assume.** Basic uses
`To Do` / `Doing` / `Done`; Agile uses `New` / `Active` / `Resolved` / `Closed`; Scrum
uses `New` / `Approved` / `Committed` / `Done`. Read the item's current
`System.State` and its type, pick the state that actually means in-progress or in
review for that process, and name the one you picked. A state write is never worth
blocking the work over — if nothing fits, say so and carry on.

**Branch policies matter here.** An Azure Repos PR often can't complete without CI
green plus a reviewer approval. That's the point: this skill drives CI to green and
addresses the review comments, then leaves the completion to a human.

## Notes

- **What this skill is pre-approved to do.** Read and write files in the repo, run the
  repo's own build/test commands, write to this work item, push its branch, and open a
  PR. It will not complete a PR, bypass a policy, deploy, or touch anything else in
  Boards. If that is more autonomy than you want on a given item, run it without
  `skip-checkpoint` and stop it at the Step E checkpoint.
- **No architecture is assumed.** This skill does not check for, recommend, or plan
  against Clean Architecture, hexagonal, MVC, or any other named pattern. Step B reads
  what the repository actually does and the plan follows it.
- **Migrations.** If the change needs a schema migration, use the repo's own migration
  command — whatever `AGENTS.md`, the `Makefile`, or the toolchain defines. Don't
  assume migrations run on startup.
- **Keep secrets out of the shell and the commit.** Don't stage `.env` files, keys, or
  tokens, and never echo a PAT into a command, a commit message, or a PR body.
- **Gate commands.** The mainstream runners (`make`, `npm`/`pnpm`/`yarn`, `pytest`,
  `go`, `cargo`, `dotnet`, `mvn`/`gradle`, `bundle`, `composer`) are pre-approved. If
  your repo's gate isn't among them, run it and approve the prompt — never skip or
  fake a gate to avoid a permission dialog.
