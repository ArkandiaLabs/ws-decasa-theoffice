---
name: linear-plan-build
description: >
  Take a Linear issue from "read it" to "PR open, CI green, review comments
  addressed, Linear updated" with maximum autonomy. Reads the issue, its subissues
  and its discussion; grills you on the design decisions the ticket left open;
  explores the repo; drafts a plan and puts it through a three-lens adversarial
  review; asks for your approval through plan mode when the change warrants it and
  skips it when it doesn't; then implements test-first, runs your repo's gates, opens
  the PR, and babysits it to green. Stack-agnostic — assumes no particular
  architecture.
  Invoke with `/arkandia:linear-plan-build [ABC-123 | issue URL] [skip-checkpoint]`.
argument-hint: "[ABC-123 | Linear issue URL] [skip-checkpoint]"
disable-model-invocation: true
allowed-tools: Read, Glob, Grep, Edit, Write, AskUserQuestion, Agent, Skill, TaskCreate, TaskUpdate, TaskList, TaskGet, EnterPlanMode, ExitPlanMode, Monitor, ScheduleWakeup, Bash(git status*), Bash(git diff*), Bash(git add*), Bash(git commit*), Bash(git log*), Bash(git rev-parse*), Bash(git symbolic-ref*), Bash(git remote*), Bash(git fetch*), Bash(git checkout*), Bash(git switch*), Bash(git pull*), Bash(git push*), Bash(gh --version*), Bash(gh auth status*), Bash(gh pr create*), Bash(gh pr view*), Bash(gh pr diff*), Bash(gh pr checks*), Bash(gh pr comment*), Bash(gh api repos/*/pulls/*/comments*), Bash(gh run view*), Bash(gh run list*), Bash(gh run rerun*), Bash(make *), Bash(npm test*), Bash(npm run *), Bash(npm ci*), Bash(npm install*), Bash(npx *), Bash(pnpm test*), Bash(pnpm run *), Bash(pnpm install*), Bash(yarn test*), Bash(yarn run *), Bash(yarn install*), Bash(pytest*), Bash(python *), Bash(python3 *), Bash(uv run *), Bash(uv sync*), Bash(go test*), Bash(go build*), Bash(go vet*), Bash(cargo test*), Bash(cargo build*), Bash(cargo clippy*), Bash(cargo fmt*), Bash(dotnet test*), Bash(dotnet build*), Bash(dotnet restore*), Bash(dotnet format*), Bash(mvn test*), Bash(mvn verify*), Bash(mvn package*), Bash(gradle test*), Bash(gradle build*), Bash(gradle check*), Bash(./gradlew test*), Bash(./gradlew build*), Bash(./gradlew check*), Bash(bundle exec *), Bash(bundle install*), Bash(rake *), Bash(composer install*), Bash(composer run *), Bash(php *), mcp__linear__get_issue, mcp__linear__list_issues, mcp__linear__list_comments, mcp__linear__get_project, mcp__linear__list_cycles, mcp__linear__get_team, mcp__linear__list_issue_statuses, mcp__linear__save_issue, mcp__linear__save_comment, mcp__linear-server__get_issue, mcp__linear-server__list_issues, mcp__linear-server__list_comments, mcp__linear-server__get_project, mcp__linear-server__list_cycles, mcp__linear-server__get_team, mcp__linear-server__list_issue_statuses, mcp__linear-server__save_issue, mcp__linear-server__save_comment
---

# Linear issue → shipped feature

Take a Linear issue all the way to **PR open, CI green, review comments addressed, and
Linear updated**. There is exactly **one** explicit checkpoint — plan approval — and
even that is conditional: routine changes run straight through.

The phases below gather the Linear context and prepare git. The actual work — grilling
you on the open design decisions, exploring, planning, reviewing, implementing,
gating, shipping — lives in the shared build loop.

**Read `references/build-loop.md` now.** It is the body of this skill, not optional
background. This file supplies the Linear bindings it asks for.

## Philosophy (hold these throughout)

- **A ticket is a pointer, not a specification.** Requirements get negotiated in comments as
  often as they are written in the description. Read those first, then grill the user on what is
  still open — a wrong assumption costs one question here and an implementation at Step F.
- **Never invent a requirement.** An issue that fails to fetch, a description that is empty, an
  attachment nobody attached — name it and ask. An issue key is not a specification either.
- **The repository is the system of record.** A Linear status, a label, a blocker marked done —
  none of those are evidence. Confirm a dependency against `git log` and the code.
- **No architecture is assumed.** Read what this repo actually does and follow it. Never plan
  against a pattern it does not use, and never propose adopting one; that is a separate
  conversation, not a side effect of a ticket.
- **Evidence beats claims.** You run the gates yourself and paste their real output — a subagent
  reporting "tests pass" is a claim, the gate's own output is evidence. Same for CI: a run that
  never registered is not a green run, and an absent CI is not a green CI.
- **One checkpoint, and it has to be earned.** Step E stops the run only when the change's shape
  demands it. Stopping on a routine fix teaches the user to skim your plans; not stopping on a
  schema migration is how you lose them.
- **Autonomy ends at the PR.** Push it, open it, babysit it to green — never merge, never enable
  auto-complete, never deploy.
- **A silent run is a run nobody trusts.** Say which step you are on, what you just changed, and
  what you are waiting for — in plain sentences the person who wrote the ticket can follow, not in
  raw tool output. See § *Say what you are doing* in `references/build-loop.md`.
- **A finished subissue is marked finished.** Each child that is implemented, gated and pushed
  moves to the team's completed state as it closes. The **parent** is what waits on the PR.
- **Keep the tracker footprint minimal.** Status and comments on the issue you are building, and
  nothing else in Linear, ever.

## Autonomy contract

- **Act and self-verify by default.** No option menus, no "should I proceed?" on green.
- **Linear writes on this issue are pre-authorized** — the status and comments of
  `TICKET` and of any subissue you actively work. Never ask permission for those, and
  never write anything else in Linear.
- **This skill pushes branches and opens pull requests without asking.** It never
  merges a PR, never enables auto-complete, and never deploys.
- **Escalate only** for the cases listed in `references/build-loop.md` § Escalation.
  Otherwise keep going.

## Arguments

Parse `$ARGUMENTS`:

- **Issue identifier** — a Linear key like `ABC-123`, or a
  `linear.app/<workspace>/issue/ABC-123/<slug>` URL (extract the key). If absent, list
  the caller's assigned, unstarted-or-in-progress issues with `list_issues` and ask
  which one via `AskUserQuestion`.
- **`skip-checkpoint`** — or freeform "skip the plan checkpoint" / "run straight to
  PR". The user's opt-in for routine issues: force the Step E skip. Honor it only when
  explicitly given, and never over a user who asked to see a plan.

There is no brief-file or inline-description path. This skill starts from a Linear
issue; if you don't have one, ask for one.

## Phase 0 — Resolve the bindings and check the tooling

Two things have to hold before Phase 1. Both are cheap to check here and expensive to
discover later.

### 0a. The Linear binding

The Linear MCP server is registered under different names in different setups, so the
tool prefix varies: `mcp__linear__*` in some sessions, `mcp__linear-server__*` in
others. Determine which prefix this session actually exposes and use it consistently.
If **neither** is available, stop and say so — point the user at the Linear MCP server
setup. Never infer an issue's content from its key.

Everything below names operations bare (`get_issue`, `save_issue`); prepend the prefix
you resolved.

### 0b. The GitHub path — prerequisites

Steps H and I push a branch, open the PR and read CI through `gh`. Check it **now**: an
unauthenticated `gh` discovered at Step H costs a full implementation before it
surfaces. Report what is missing and **install nothing yourself**.

| Tool | Check | macOS | Windows | Linux |
|---|---|---|---|---|
| `gh` CLI | `gh --version` | `brew install gh` | `winget install GitHub.cli` | the distro's `gh` package, or `cli.github.com` |
| `gh` authentication | `gh auth status` | `gh auth login` | `gh auth login` | `gh auth login` |
| a GitHub `origin` | `git remote get-url origin` | — | — | — |

If `gh` is missing or unauthenticated, say so and ask whether to continue anyway:
everything through Step G still runs, and the work would stop at the push with the
branch intact. If `origin` is not a GitHub remote, name the host and stop — this
skill's `OPEN-PR` and `CI` bindings are GitHub-only, and `ado-plan-build` is the
sibling for Azure Repos.

## Phase 1 — Gather Linear context

Run these in parallel:

1. `get_issue` for the key — include relations (parent, blocking, related). Note the
   team ID and project ID.
2. `list_issues` with `parentId` = the issue ID — the subissues.
3. `list_comments` — prior discussion and decisions. **Requirements are frequently
   negotiated in comments rather than written in the description**; read them before
   concluding anything is unspecified.
4. `get_project`, if the issue belongs to one — goals and scope.
5. `list_cycles` for the team with `type: "current"` — what else is in flight.

**Resolve the team's real workflow states with `list_issue_statuses` before any status
write.** "In Progress", "In Review" and "Done" are conventions, not guarantees — teams
rename and reorder them. Resolve **three** states by *type*, not by name: the
`started` one for in-progress, the team's review state (typically the last `started`
or `unstarted` state before completion) for in-review, and the `completed` one that
subissues land in when they close. Say which state you picked for each. If nothing
plausibly matches, pick the closest by type, name it, and continue — a status write is
never worth blocking the work.

## Phase 2 — Prepare the git environment

1. Default branch: `git symbolic-ref refs/remotes/origin/HEAD | sed 's@^refs/remotes/origin/@@'`.
2. `git fetch origin`.
3. **If the working tree is dirty, stop and report.** Do not stash, do not discard.
4. `git checkout <default-branch> && git pull --ff-only origin <default-branch>`.
5. Create the feature branch. Prefer the `branchName` Linear itself suggests on the
   issue; otherwise `feature/<ISSUE-KEY>-<short-slug>`. The branch name **must**
   contain the Linear key. If you are already on that branch with prior work on it,
   stay on it and continue rather than recreating it.

## Phase 3 — Present the issue summary

Print a concise summary: title, status, priority, assignee, project, current cycle,
branch name, subissues (key / title / status), blocking and related issues, and the
decisions buried in the comments.

**Print the subissues even when there is only one** — that list is what Step A asks the
user to choose from, and it is the work list for the rest of the run.

**Linear status is not authoritative.** Flag every blocker that isn't done, and before
treating a dependency as met, confirm it against `git log` and the code rather than
against a green label.

## Phase 4 — Run the build loop

Follow `references/build-loop.md`, Steps A → J, with these bindings.

| Binding | Linear / GitHub |
|---|---|
| `TICKET` | the Linear issue — the parent |
| `SUB-TICKETS` | its subissues — `list_issues` with `parentId` to list, `save_comment` / `save_issue` against the child's id |
| `STATUS→IN-PROGRESS` | `save_issue` with the team's `started`-type state (Phase 1) |
| `STATUS→IN-REVIEW` | `save_issue` with the team's review state (Phase 1) — used on the **parent**, which waits on the PR |
| `STATUS→DONE` | `save_issue` with the team's `completed`-type state (Phase 1) — used on a **subissue** once it is implemented, gated and pushed |
| `COMMENT` | `save_comment` on the issue |
| `BRANCH` | the Phase 2 branch |
| `LINK-TOKEN` | the issue key (`ABC-123`) — this is what Linear's GitHub integration matches on. Each Step F.6 commit carries **the sub-issue's own key**; the PR title carries the parent's. **With no subissues the work list is `TICKET` itself, so its key is the one in every commit as well** |
| `OPEN-PR` | `gh pr create --title "<key>: <title>" --body "<body>"` |
| `CI` | `gh pr checks <pr> --watch` to wait; `gh run view <run-id> --log-failed` for logs; `gh run rerun <run-id> --failed` to retry a flaky job |
| `PR-COMMENTS` | `gh pr view <pr> --comments` for the conversation; `gh api repos/{owner}/{repo}/pulls/{n}/comments` for inline threads; reply with `gh pr comment` or the API |

`gh pr checks --watch` blocks until the checks settle, which is the right way to wait —
it beats polling. If the repo has no CI configured at all, say so and skip straight to
the review-comment half of Step I; an absent CI is not a green CI.

## Notes

- **What this skill is pre-approved to do.** Read and write files in the repo, run the
  repo's own build/test commands, write to this Linear issue, push its branch, and
  open a PR. It will not merge, deploy, or touch anything else in Linear. If that is
  more autonomy than you want on a given ticket, run it without `skip-checkpoint` and
  stop it at the Step E checkpoint.
- **The plan file.** Step C writes `.claude/plans/<TICKET>.md` and keeps it current
  while the work runs — a working notebook, never staged and never committed on the
  skill's initiative. Step J asks what to do with it.
- **Keep secrets out of the shell and the commit.** Don't stage `.env` files, keys, or
  tokens, and don't echo secret values into commands, commit messages, or PR bodies.
- **Deleting the plan file is the one step that asks.** `allowed-tools` is static and
  `<TICKET>` is not, so no grant can say "this ticket's plan and no other" — a
  `Bash(rm .claude/plans/*)` would authorise deleting every other ticket's plan too. The
  grant is therefore absent on purpose: Step J's "Delete it" runs
  `rm .claude/plans/<TICKET>.md`, exactly that path, and costs one permission prompt.
  One prompt for the only destructive step in the run, right after the user chose it, is
  the correct trade.
- **The Bash allowlist is narrowed to the subcommands this run actually uses**, so a
  package publish, an arbitrary GitHub API mutation or an unrelated tool prompts instead
  of running silently. `gh api` is scoped to the inline-review-comment path — the one
  place the skill needs it. Two grants stay wide on purpose and are worth knowing about:
  `git add`/`git push` and `npx`. A glob cannot express "not `-A`" or "not `--force`",
  and the gate a repo defines may legitimately be `npx <anything>`; the controls there
  are the textual rules in `references/build-loop.md` (stage only what the item touched,
  never `git add -A`, never force-push, never merge). A team that wants a hard boundary
  rather than an instruction should add `permissions.deny` rules in `settings.json` —
  deny wins over any allowlist, including this one.
- **Gate commands.** The mainstream runners (`make`, `npm`/`pnpm`/`yarn`, `pytest`,
  `go`, `cargo`, `dotnet`, `mvn`/`gradle`, `bundle`, `composer`) are pre-approved. If
  your repo's gate isn't among them, run it and approve the prompt — never skip or
  fake a gate to avoid a permission dialog.

## References

| Reference | Used by | What it covers |
|---|---|---|
| `build-loop.md` | Phase 4 | The body of the skill: Steps A → J — grilling the user, exploring, drafting the plan, the three-lens adversarial review, the conditional approval checkpoint, test-first implementation, the repo's gates, commit/push/PR, the CI-and-review-comments watch loop, and the complete Escalation list |

## Troubleshooting

| Symptom | Almost always | Confirm with |
|---|---|---|
| Neither `mcp__linear__*` nor `mcp__linear-server__*` is exposed | The Linear MCP server is not registered, or the workspace is not trusted so a project-scoped server never loaded | `/mcp`. A pending-approval server behaves exactly like an absent one |
| `gh` fails to authenticate at Step H, after the whole build | Phase 0b was skipped | `gh auth status` before Phase 1 — that is what 0b is for. Recover with `gh auth login` and resume from Step H; the branch and commits survive |
| `gh pr checks --watch` returns instantly with no checks | Either the repo has no CI, or the first run has not registered yet on a just-pushed branch | `gh run list --branch <branch>`. If there genuinely is no CI, say so and skip to the review-comment half of Step I — an absent CI is **not** a green CI, and Step J must name it as a skipped gate |
| The status write lands in the wrong column | The state was matched by name. Teams rename and reorder states, so "In Progress" is a convention, not a guarantee | `list_issue_statuses` and match by state *type* (`started`, the team's review state, `completed`). Name the state you picked |
| Subissues sit in progress after their work shipped | Step F.6.4 was skipped, or the `completed` state was never resolved in Phase 1 | Each child closes into `STATUS→DONE` as Step F.6 finishes it; only the parent stays in review, waiting on the PR |
| Linear never links the commit or the PR to the issue | A key is missing, or the wrong one is used. **Branch and PR title carry the PARENT key; each Step F.6 commit carries its own sub-issue's key** — one key in all three is impossible once a run covers several sub-issues, and a commit under the parent key leaves the child with nothing attached | Branch name and PR title: parent key. Each commit: the key of the sub-issue it closes. Linear's GitHub integration matches on whichever key it finds |
| Phase 2 stops on a dirty working tree | By design. Stashing someone's uncommitted work is not this skill's call | `git status`. Commit or stash it yourself, then re-run |
| Two subagents overwrite each other's edits in Step F | Steps that converge on the same file were dispatched in parallel | Step F.2: converging steps stay **serial** in one agent. Parallelize the thinking (subagents return diffs, you apply them), not the writes |
| `EnterPlanMode` errors, or the plan is presented twice | The session was already in plan mode | Step E: when already in plan mode, go straight to `ExitPlanMode` |
| The same gate fails three times in a row | Not a reason to keep iterating — classify it: test, code, environment, or plan drift | Step F.5, and § Escalation in `build-loop.md`. An ambiguous failure is an escalation, never a guess |
