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
allowed-tools: Read, Glob, Grep, Edit, Write, AskUserQuestion, Agent, Skill, TaskCreate, TaskUpdate, TaskList, TaskGet, EnterPlanMode, ExitPlanMode, Monitor, ScheduleWakeup, Bash(git status*), Bash(git diff*), Bash(git add*), Bash(git commit*), Bash(git log*), Bash(git rev-parse*), Bash(git symbolic-ref*), Bash(git fetch*), Bash(git checkout*), Bash(git switch*), Bash(git pull*), Bash(git push*), Bash(gh pr create*), Bash(gh pr view*), Bash(gh pr diff*), Bash(gh pr checks*), Bash(gh pr comment*), Bash(gh api*), Bash(gh run view*), Bash(gh run list*), Bash(gh run rerun*), Bash(make *), Bash(npm *), Bash(npx *), Bash(pnpm *), Bash(yarn *), Bash(pytest*), Bash(python *), Bash(python3 *), Bash(uv *), Bash(go *), Bash(cargo *), Bash(dotnet *), Bash(mvn *), Bash(gradle *), Bash(./gradlew*), Bash(bundle *), Bash(rake *), Bash(composer *), Bash(php *), mcp__linear__get_issue, mcp__linear__list_issues, mcp__linear__list_comments, mcp__linear__get_project, mcp__linear__list_cycles, mcp__linear__get_team, mcp__linear__list_issue_statuses, mcp__linear__save_issue, mcp__linear__save_comment, mcp__linear-server__get_issue, mcp__linear-server__list_issues, mcp__linear-server__list_comments, mcp__linear-server__get_project, mcp__linear-server__list_cycles, mcp__linear-server__get_team, mcp__linear-server__list_issue_statuses, mcp__linear-server__save_issue, mcp__linear-server__save_comment
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

## Phase 0 — Resolve the Linear binding

The Linear MCP server is registered under different names in different setups, so the
tool prefix varies: `mcp__linear__*` in some sessions, `mcp__linear-server__*` in
others. Determine which prefix this session actually exposes and use it consistently.
If **neither** is available, stop and say so — point the user at the Linear MCP server
setup. Never infer an issue's content from its key.

Everything below names operations bare (`get_issue`, `save_issue`); prepend the prefix
you resolved.

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
write.** "In Progress" and "In Review" are conventions, not guarantees — teams rename
and reorder them. Match by state *type* (`started` for in-progress, `completed`'s
predecessor / the team's review state for in-review), and say which state you picked.
If nothing plausibly matches, pick the closest by type, name it, and continue — a
status write is never worth blocking the work.

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

**Linear status is not authoritative.** Flag every blocker that isn't done, and before
treating a dependency as met, confirm it against `git log` and the code rather than
against a green label.

## Phase 4 — Run the build loop

Follow `references/build-loop.md`, Steps A → J, with these bindings.

| Binding | Linear / GitHub |
|---|---|
| `TICKET` | the Linear issue, plus the subissues you work |
| `STATUS→IN-PROGRESS` | `save_issue` with the team's `started`-type state (Phase 1) |
| `STATUS→IN-REVIEW` | `save_issue` with the team's review state (Phase 1) |
| `COMMENT` | `save_comment` on the issue |
| `BRANCH` | the Phase 2 branch |
| `LINK-TOKEN` | the issue key (`ABC-123`) in the commit message and the PR title — this is what Linear's GitHub integration matches on |
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
- **Keep secrets out of the shell and the commit.** Don't stage `.env` files, keys, or
  tokens, and don't echo secret values into commands, commit messages, or PR bodies.
- **Gate commands.** The mainstream runners (`make`, `npm`/`pnpm`/`yarn`, `pytest`,
  `go`, `cargo`, `dotnet`, `mvn`/`gradle`, `bundle`, `composer`) are pre-approved. If
  your repo's gate isn't among them, run it and approve the prompt — never skip or
  fake a gate to avoid a permission dialog.
