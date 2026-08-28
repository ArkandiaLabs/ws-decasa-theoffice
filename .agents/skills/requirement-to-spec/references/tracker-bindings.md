# Tracker bindings — detection, the destination prompt, and creation

This skill **creates** issues/work items, where `linear-plan-build`/`ado-plan-build` only ever read
and update ones that already exist. That difference drives everything below: nothing here is
pre-verified the way `save_issue`/`save_comment` are in the delivery skills, so every creation path
gets verified against the server's or CLI's own `--help`/tool listing before it is trusted.

## Detection (Phase 1, silent)

Same order the delivery skills use, MCP before CLI:

1. **Linear** — the session exposes `mcp__linear__*` or `mcp__linear-server__*` tools. Resolve which
   prefix is actually present; do not assume one.
2. **Azure DevOps** — `mcp__azure-devops__*` tools, or, failing that, `az` installed, the
   `azure-devops` extension present (`az extension list`), and `az account show` succeeding.
3. Detecting is silent. **Never ask which one to use here** — that is Phase 3, after both detections
   have run.

## The destination prompt (Phase 3, mandatory every run)

`AskUserQuestion`, options = **exactly what Phase 1 detected** (0, 1, or 2 trackers) plus
**"Local file"**, always in that order, never auto-selected — even when exactly one tracker was
found, and even on a repeat run against the same repo. This is a hard rule for this skill
specifically: a team with Linear wired up may still want a given spec to live as a reviewable file,
and the skill does not get to decide that for them.

- **0 trackers detected** — the prompt still appears. `AskUserQuestion` needs at least two options,
  and "Local file" alone is not a question, so the second option is the honest other answer:
  **"Stop here — I'll wire up a tracker first."** Word the question so the user can see *why* there
  is no tracker on the list ("No Linear or Azure DevOps connection was detected in this session"),
  rather than having it silently assumed. If they pick "Stop here", say what to connect (the Linear
  or Azure DevOps MCP server, or `az login` + the `azure-devops` extension) and end the run without
  writing anything — the interview's Decisions are still worth reporting, and a re-run picks up
  from the same document.
- **1 tracker** — that tracker + "Local file".
- **2 trackers** — both + "Local file", in the order they were listed above (never alphabetical
  preference).

**If the destination question is declined, cancelled, or never gets an answer, nothing is written.**
No default, not even file mode, and not even when the whole spec is ready and only one option was
on offer. Report what was decided in the interview, say the destination was never chosen, and stop.

## The binding table

| Binding | Linear | Azure DevOps | Local file |
|---|---|---|---|
| `CREATE-PARENT` | Create an issue in the resolved team/project | Create a work item (type per the process — Issue/PBI/User Story) | `docs/specs/<slug>/spec.md` |
| `CREATE-CHILD` | Create an issue with `parentId` set | Create a work item, then a `System.Parent` relation | A row in `docs/specs/<slug>/tasks.md` |
| `LINK-PARENT` | `parentId` at creation time | A `parent` relation — see **Azure DevOps: linking**, below | Both directions: a `## Breakdown` heading in `spec.md` linking to `tasks.md`, **and** a back-link line at the top of `tasks.md` pointing to `spec.md` (`[← spec](./spec.md)`) |
| `SET-INITIAL-STATUS` | The team's first `unstarted`/backlog-type state, via `list_issue_statuses` | The process's first state, resolved by category — see **Azure DevOps: initial status**, below | N/A — a file has no status |

Never hardcode a Linear workspace/team or an Azure DevOps project. Use what the caller names, what
resolves from context, or ask if neither applies — the same rule `ado-plan-build` Phase 1 uses for
its project.

## Linear: the creation tool is not attested — verify before calling it

`linear-plan-build` only attests `get_issue`, `list_issues`, `list_comments`, `get_project`,
`list_cycles`, `get_team`, `list_issue_statuses`, `save_issue`, `save_comment` — a shape built
around reading and updating an issue that already exists. **Whether `save_issue` also creates one
when called with no id is not verified anywhere in this repo.** Before calling anything to create
the parent or a child issue:

1. List the server's actual tools (whichever prefix Phase 1 resolved).
2. Look for the real creation tool by shape, not by a guessed name — something that accepts a
   title/description and a team, and either returns a new id or explicitly documents upsert
   behavior on `save_issue`.
3. Use the name you actually found. Report which tool you used in the final report — this is new
   ground for this skill family, and the next person maintaining it should not have to rediscover
   it.

`get_team` and `list_issue_statuses` are attested reads and safe to call directly.

## Azure DevOps: initial status — new technique, not a precedent already proven

`ado-plan-build` resolves in-progress/in-review states, but only for an item that **already
exists**: it reads the item's current `System.State` and maps it. It has never had to derive the
**full list of states for a work-item type** in order to pick the first one, because it never
creates anything. That derivation is new here — build it and prove it, don't assume it behaves like
the existing in-progress/in-review resolution.

**Do not hardcode a state name.** Azure DevOps ships three process templates and each names the
"just created" state differently: Basic uses `To Do`, Agile uses `New`, Scrum uses `New` with a
different flow behind it. Resolve by category (the state whose semantic type is "not started"),
never by matching a literal string.

**Two paths, and the detection already told you which one you are on.** Phase 1 prefers MCP over the
CLI; the recipe has to follow that preference, not assume the CLI. **Never mix them silently** — the
same guard `ado-access.md` carries. That file belongs to the sibling skill —
`arkandia/skills/ado-plan-build/references/ado-access.md`, not this skill's `references/` — and
every recipe needed here is inlined below, so open it only to check the original. Pick the path
Phase 1 resolved, say in the report which one you used, and if it fails, say that too before
switching.

### MCP path (when `mcp__azure-devops__*` is present)

There is no attested state-listing tool: `ado-access.md` attests only `wit_get_work_item` and
`wit_list_work_item_comments`, neither of which enumerates a type's states.

1. List the server's actual tools and look **by shape** for one that returns work-item types or
   their states for a project/process (something like `wit_list_work_item_types`,
   `wit_get_work_item_type`, `core_*` process queries). Do not guess a name.
2. Found one → call it, pick the state whose category/semantic type is "not started", and report
   which tool answered — this is new ground, and the next maintainer should not rediscover it.
3. **Nothing of that shape exists** → do not fall through to `az`. Ask the user which state new
   items should start in, offering the three process defaults as options (`To Do` for Basic, `New`
   for Agile, `New` for Scrum) plus whatever the organization may have customized. An unanswerable
   lookup becomes a question, never a hardcoded guess.

The `az` CLI is **not** a prerequisite on this path — if MCP is present, the run never needs `az`
installed, and Phase 2 does not report it as missing.

### CLI path (when only `az` is present)

**Verified this build: `az boards work-item-type show` does not exist.** Running
`az boards work-item-type show --help` returns `'work-item-type' is misspelled or not recognized by
the system`, and `az boards --help` lists only `area`, `iteration`, and `work-item` as subgroups —
no `work-item-type` group at all. The plausible-looking command is wrong; **do not write it into a
run**. The verified path to a work-item type's states is the REST API via `az devops invoke`,
following the exact discovery recipe `ado-access.md` already documents for reading comments and PR
threads:

```bash
# discover the wit resource that lists a process's work item types / their states
az devops invoke --org <url> --query "[?area=='wit'].resourceName" -o tsv

# then call the resource you found, e.g. (resource name confirmed by the discovery step above)
az devops invoke --org <url> --area wit --resource <resource-from-discovery> \
  --route-parameters project=<project> type=<work-item-type> --api-version 7.1-preview
```

This has **not** been exercised against a live organization in this build — there is no test org
available. Treat it the same way `ado-access.md` treats every "discover" row: run the discovery
step for real before trusting the result, and if it 404s or returns nothing usable, say so and fall
back to asking the user which state to use (list the process's known states from the table above as
options). **Do not paste a REST call you have not seen succeed.**

`az devops invoke` is a generic REST invoker — it can reach every area of the Azure DevOps API, not
just work items. This skill uses it for **exactly two things**: the `wit` discovery query above, and
a `--area wit` call against the resource that discovery returned. No other area, no write verbs, no
"while I'm here" call to graph, git, release, or security. If what you need is not in `wit`, it is
not something this skill does.

The skill's `allowed-tools` encodes exactly those two shapes —
`Bash(az devops invoke --org * --query *)` and `Bash(az devops invoke --org * --area wit *)` — so
**the flag order in the two snippets above is load-bearing**. They are prefix globs: `--org` first,
then `--query` or `--area wit`. Move a flag and the command is still correct but no longer
pre-authorized, and the run stops on a permission prompt mid-phase. Copy the shape, then fill in the
values.

## Azure DevOps: linking

`az boards work-item relation add --relation-type parent --id <child> --target-id <parent>` is
**verified against `--help`** in this build (arguments: `--id`, `--relation-type`, `--target-id`/
`--target-url`, `--org`) and is the plausible correct call — but, like the state lookup above, it
has not been run against a live work item here. Run it for real on the first live use and confirm
the relation actually resolves in Phase 5 before trusting it on the next run.

On the MCP path, list the server's real tools before calling anything beyond what
`ado-access.md` already attests (`wit_get_work_item`, `wit_list_work_item_comments`) — neither of
those creates or links anything.

### MCP path: creating and linking, end to end

The binding table promises `CREATE-PARENT` and `CREATE-CHILD`. On the CLI path the calls above
deliver them. On the MCP path nothing is attested past the two readers, so **resolve the whole
path before the destination prompt commits the user to it** — a run that offers Azure DevOps and
then cannot write has already spent the entire interview.

1. **List the server's real tools.** Match **by shape**, never by a name from this page:
   - creation — takes a project, a work-item type and field values (`wit_create_work_item`-like);
   - update — takes an id and field values (`wit_update_work_item`-like);
   - linking — either a dedicated child/parent call (`wit_add_child_work_items`-like) or a generic
     relation call taking a link type plus two ids (`wit_work_items_link`-like).
2. **Report which tool answered for each of the three**, in the Phase 2 table. This is new ground;
   the next maintainer should not have to rediscover the spelling.
3. **A shape with no match is a question, not a guess.** If creation is missing, Azure DevOps is
   not a viable destination on this path — say so *in the destination prompt*, so the user picks
   Linear or the local file with that fact in hand, rather than discovering it after Phase 3. If
   only *linking* is missing, offer the fallback explicitly: create the items unlinked and report
   the parent relation as not established, naming each child. Never create children whose parent
   link silently never happened.
4. **Set the parent at creation if the creation tool accepts it** (a `parent`/`parentId` argument,
   or a `System.Parent` field in its value map) — one call cannot half-succeed the way create-then-
   link can. Fall back to the linking tool only when it does not.
5. **Verify in Phase 5 by reading the child back** with the attested `wit_get_work_item` and
   confirming the parent relation resolves to the id you set. A creation call that returned success
   is a claim; the read-back is the evidence, and it is the one check that costs nothing because
   the tool for it is already attested.

## Azure DevOps: Basic process has no acceptance-criteria field

If the resolved process is Basic, the full spec text goes into `System.Description` — same as
Linear's `description` field. Nothing is lost, it just has one home instead of two. Say which case
applied in the report, the same caveat `ado-plan-build` Phase 1 already carries for reading an
item.

## Prerequisite for the CLI path

Only when the CLI is the path Phase 1 resolved — if the Azure DevOps MCP server is present, `az` is
irrelevant to this run and is neither checked nor reported as missing.

Before relying on `az`: `az account show` succeeds (logged in), and
`az extension list --query "[?name=='azure-devops']"` is non-empty. If either is missing, report the
install/login command — never authenticate on the user's behalf — and either fall back to the MCP
path if it is available, or drop Azure DevOps from the destination prompt entirely for this run.
