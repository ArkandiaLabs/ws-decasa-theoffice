# The build loop — ticket to green PR

This is the body of the delivery skills. The calling `SKILL.md` reads the ticket and
supplies the **bindings**; everything below is tracker-agnostic and runs the same way
whatever tracker the ticket came from.

> This procedure is intentionally duplicated in every delivery skill (skills ship
> independently). If you change it here, mirror the change in the other skill's copy.

**Bindings** the calling skill defines, referenced below by name:

| Binding | What it names |
|---|---|
| `TICKET` | the work item / issue you are implementing |
| `SUB-TICKETS` | the child items of `TICKET`, if it has any — how to list them, comment on one, and set one's status |
| `STATUS→IN-PROGRESS` | how to move `TICKET` to its in-progress state |
| `STATUS→IN-REVIEW` | how to move `TICKET` to its in-review state |
| `STATUS→DONE` | how to move a **sub-ticket** to its completed state |
| `COMMENT` | how to post a comment on `TICKET` |
| `BRANCH` | the feature branch name |
| `LINK-TOKEN` | the string that makes the tracker attach a commit to `TICKET` |
| `OPEN-PR` | the command that opens the pull request |
| `CI` | how to read check status, failing logs, and rerun a job |
| `PR-COMMENTS` | how to read and reply to review comments |

Run steps A → J in order. Only Step E pauses for the user; everything else is
**act and self-verify.** See **Escalation** at the end for the complete list of
things that stop you.

---

## Standing rule — say what you are doing, in plain language

This run is long and mostly unattended. Between Step A and Step J the only window the
user has into it is what you print, so print deliberately rather than leaving them to
infer progress from tool output.

- **One line when a step starts, one when it closes** — which step, which item of the
  work list, and the result. `Step F · ABC-124 "Export endpoint" · 3 of 5 · tests
  green` is worth more than either silence or a wall of raw output.
- **Announce every change the user would otherwise find out about later** — the branch
  created, each tracker status written (and which state you picked), each commit and
  push, the PR URL, the CI run you are waiting on and roughly how long it takes.
- **Write for the person who wrote the ticket, not for the person who will review the
  diff.** Plain sentences, no unexplained jargon: "rerunning the failing test job once
  in case it was flaky" beats "rerun --failed on job 42". Same rule for tracker
  comments and the PR body.
- **Never announce work you have not done.** A line printed ahead of the fact is a
  plan, not a report — say what happened, after it happened.

## Step A — Grill the user

A ticket is a pointer, not a specification. Before you explore, close the design
questions the ticket left open — a wrong assumption discovered at Step F costs an
implementation; discovered here it costs one question.

**First, settle what you are building.** If `TICKET` has `SUB-TICKETS`, ask which of
them this run covers: one `AskUserQuestion` with `multiSelect: true`, listing each child
by key, title and current status, with **"All of them"** first and marked
`(Recommended)`. The selected children become the **work list**, ordered by their
dependencies; the ones left out are named in the Step J report so nobody assumes they
shipped. If `TICKET` has no children, the work list is the ticket itself. Do this before
the design questions — what is worth asking depends on what you are actually building.

**Two things are settled before that question is asked, not after:**

- **A child already in a terminal state is not work.** Read each child's status, and
  leave the ones already completed (or cancelled/duplicate/won't-do) **out of "All of
  them"** — list them in the question as context, marked with their state, so the user
  can still select one deliberately to redo it. "All of them" that silently reopens
  finished work is a destructive default.
- **A selected child whose dependency is neither selected nor already complete is a
  question, not a plan.** Dependencies come from the tracker's own relations plus what
  the ticket text says; where a child cannot start until another lands, say so and ask
  before continuing — add the missing dependency to the run, or confirm it is already
  done elsewhere. Never build a child on top of work that is not there: the gates fail
  late, at Step G, on a diff that was never coherent.

**Ask only what nobody has answered yet.** Read `TICKET`, its comments, its parent and
subissues, and the repo's `AGENTS.md` / `CLAUDE.md` / `docs/` pack **first**. A
question whose answer is already written down is noise, and it teaches the user that
your questions aren't worth reading.

Use `AskUserQuestion`: **at most 4 questions per call, 2–4 options each.** Prefer a
single round; two at the absolute most. Put your recommended option first and mark it
`(Recommended)` — you have read the code, so have an opinion.

Sweep these categories and ask about the ones that are genuinely open **and would
change the code**:

- **Scope boundary** — what the ticket implies but never states, and what is
  explicitly out.
- **Data model** — new fields, nullability, defaults, and whether this needs a
  migration.
- **Contract** — the API/CLI/event shape, and whether breaking existing callers is
  acceptable.
- **Failure behavior** — what happens on invalid input, a missing record, a timeout,
  a partial write. Silent skip, error, or retry?
- **Auth and permissions** — who may do this, and what a caller without the right
  sees.
- **Scale** — the expected volume, and whether the obvious implementation survives it.
- **User-visible surface** — copy, states, empty and error cases.
- **Rollout** — behind a flag or straight on; migration of existing data.
- **Test depth** — unit only, or an integration test against the real dependency.

Record the outcome in two explicit lists you carry into the plan:

- **Decisions** — what the user answered. Quote them; do not paraphrase into
  something looser.
- **Assumptions** — what you settled yourself because it was too minor to ask. Write
  each one with its **source** — `file:line` where you read it, `inferred` where you
  deduced it from an indirect signal, `none` where you simply picked what seemed
  reasonable — and one clause naming what would falsify it. Assumptions are exactly
  what the Step E checkpoint exists to catch, so write them down even when you are
  confident. **An assumption with source `none` about a public contract, a data
  schema, auth, or a money path is not an assumption — it is a Step A question you
  did not ask.** Go back and ask it.

Step A is **not** skippable. The `skip-checkpoint` argument governs Step E only.

## Step B — Explore

1. **Conventions first.** Read `AGENTS.md`, `CLAUDE.md`, or a `docs/` context pack —
   that is the fastest path to this repo's patterns, commands, and non-obvious rules.
   If none exists, infer the conventions from the code as you go and treat the
   inference as provisional.

2. **Do not assume an architecture.** Clean Architecture, hexagonal, MVC, layered,
   modular monolith — read what this repo actually does and follow it. Never plan
   against a pattern the repo does not use, and never propose adopting one; that is a
   separate conversation, not a side effect of a ticket.

3. **Fan out `Explore` subagents along the repo's own seams**, all in one message so
   they run concurrently. Pick the partition the codebase already has — modules,
   packages, services, bounded contexts, whatever it is — and scope each agent to the
   area the feature touches. Ask each for the files that exist, the exact symbols
   involved, **the existing tests that assert the current behavior of those symbols**
   (by file and test name), and any edge case visible in its area (an unguarded parse,
   a missing uniqueness check, a swallowed error). Tell each one to **read, not
   judge** — no plans, no fixes. Their combined output is a map, not an opinion.

4. Synthesize one short map yourself. Where two subagents disagree about the same
   file, open it and settle it — do not average their claims. If the feature is a
   single-file change, skip the fan-out; several agents to read one function is waste,
   and saying so out loud is part of the discipline.

5. **Decide the fate of the tests this change invalidates — here, before any code.**
   A test that asserts today's behavior is not a test that will break later; it is a
   decision the plan has to carry now. For every symbol the feature changes, take the
   existing tests over it and give each one a verdict:

   - **update** — the Step A Decisions cover this behavior change. The test moves to
     the new expectation **in the same step that changes the code**, never after.
   - **delete** — the behavior disappears entirely; there is nothing left to assert.
     Say what it used to cover, so the plan shows what stopped being tested.
   - **escalate** — the test asserts something the ticket never mentioned. Stop and
     ask. Either the change breaks more than anyone said, or that test is the only
     place the requirement was ever written down. Both are the user's call, not yours.

   Finding this at Step I, when CI goes red, is exactly the failure mode this step
   prevents — that is where "the test is in the way" turns into loosening it.

## Step C — Draft the plan

Write a step-by-step plan: **small steps**, each naming the exact file(s) and its own
verification, expressed in **the repo's own commands** (from `AGENTS.md`'s "Commands"
section, the `Makefile`, `package.json` scripts, or whatever the repo uses). The
**first implementation step is a failing test** that proves the behavior is missing.

The plan must also carry, verbatim: the **work list** from Step A, the **Decisions** and
**Assumptions** from Step A (each assumption with its source), the **test verdicts** from
Step B.5 — every existing test to update or delete, named by file and test — the
dependencies and risks, and what you deliberately left out of scope.

**Write it to `.claude/plans/<TICKET>.md`** as you draft it, and keep it current as you
work: the work list with each item's state, the steps, and what each one verified. It is
a **working notebook**. It survives a session that dies or gets compacted, so a resumed
run continues instead of re-deriving everything, and the user can read it while you work.

Two rules keep it from becoming noise. **Never stage it** — Step F.6 stages only the
files the change touches, and this is not one of them. And **never treat it as the
permanent record**: that is the PR body and the `COMMENT` on `TICKET`, which is where
someone looks six months from now. Step J asks the user what to do with the file.

## Step D — Adversarial review

Critique the draft **before** any code. Fan out **three `general-purpose` subagents in
one message**, each with a *different lens* rather than three copies of the same
reviewer — redundancy catches less than diversity does. Pass each the ticket, the
Decisions/Assumptions, and the full draft plan:

> **Conventions lens.** Critique this plan against this repo's conventions as
> documented in `AGENTS.md`/`docs/` (or the patterns observed while exploring):
> error-handling style, layering and dependency rules, where validation belongs,
> naming, and any non-obvious rule the docs call out. Flag any step that would violate
> an established pattern or put logic in the wrong place. Judge against what this repo
> does, not against an architecture you would prefer.
>
> **Correctness lens.** Hunt for unhandled edge cases and wrong behavior: inputs the
> plan never validates, states it never reaches, tests that would pass while the bug
> survives. For each, give the concrete input and the wrong result.
>
> **Scope lens.** Find what is missing and what does not belong: steps absent from the
> plan that the acceptance criteria demand, steps present that no criterion asks for,
> and anything requiring a migration or a product decision nobody has answered.
> Challenge the Assumptions list specifically — an assumption that should have been a
> question is a finding.

Each returns a short structured list of concrete issues with a suggested fix, and
**writes no code**.

Fold the three critiques into one revised plan. **Judge, don't tally** — a single
reviewer naming a real convention violation outranks two that found nothing, and a
confidently-argued finding that is simply wrong gets dropped with a reason.
Deduplicate where lenses overlap. State plainly what each lens flagged and how you
resolved it, including what you rejected and why. If a critique surfaced a genuine
product-judgment gap that Step A did not close, that is an escalation — ask.

## Step E — Approval checkpoint (conditional)

This is the **one** place the workflow stops for the user. Whether it stops at all
depends on the plan, and the test is concrete, not a feeling.

**Enter plan mode if ANY of these hold:**

- more than ~3 implementation steps, or more than ~3 files touched;
- it changes a public contract (API, CLI, event, exported symbol), a data schema or
  migration, auth/permissions, or a billing/money path;
- the adversarial review left a product-judgment question unresolved;
- the plan rests on **Assumptions** rather than answered **Decisions**;
- it is hard to reverse — deletes data, rewrites history, changes a production
  default;
- the user asked to see a plan.

To enter: call `EnterPlanMode`, present the vetted plan with a short note on what each
review lens flagged and how you resolved it, then `ExitPlanMode` to submit it. If the
session is **already** in plan mode, do not call `EnterPlanMode` again — go straight
to `ExitPlanMode`. Write no code until it is approved.

**Skip plan mode only if ALL of the inverse hold** — small, reversible, no contract or
schema or auth change, no open questions, everything a Decision. Then print the final
plan inline for the record and keep going.

`skip-checkpoint` in the arguments forces the skip for routine tickets, but it never
overrides a user who asked for approval. Either way, **say which branch you took and
why** in one line.

Set `STATUS→IN-PROGRESS` here (pre-authorized — do not ask).

## Step F — Implement, test-first

1. **Work one item of the work list at a time**, in the plan's order. Set that
   sub-ticket's `STATUS→IN-PROGRESS` when you start it (pre-authorized — do not ask),
   break its steps into 3–8 concrete items with `TaskCreate`, and mark each
   `in_progress` / `completed` as you go. State lives in the task list and the plan
   file, not in memory.

2. **Delegate the writing; keep the judgment.** The main session is the orchestrator —
   it holds the plan, runs the gates and talks to the user — and every file it reads in
   full is context it will not have later in the run. So the default is that a subagent
   does the editing and reports back what it changed, not the file contents. Serial
   does **not** mean "in the main session": steps that must run in order can go to a
   single subagent that runs them in order. Keep in the main session only what needs
   it — the gates (Step G), the pushes, the tracker writes and the user-facing lines.

   **Partition the steps by the files they touch.** This decides what can fan out, and
   it is the whole judgment call:

   - Steps whose file sets are **disjoint** (a new validator in one module, an
     unrelated fix in another, a config change) can run as **parallel subagents**, one
     step each, dispatched in a single message.
   - Steps that **converge on the same file** stay **serial**, in one agent. This is
     the common case: N acceptance criteria usually become N checks in the *same*
     function plus N tests in the *same* test file. Two agents editing that file in
     parallel will overwrite each other — and a format-on-save hook, if the repo has
     one, makes the race worse, not better.

   When steps converge you can still parallelize the *thinking*: fan out one subagent
   per acceptance criterion to **return a proposed test and change as a diff, writing
   nothing**, then apply them yourself, serially, RED → GREEN. You get the breadth
   without the write conflict. Say which mode you chose and why. Only reach for
   `isolation: "worktree"` if agents genuinely must write the same paths concurrently;
   usually they shouldn't, and the merge cost is not worth it.

3. For each step, **RED → GREEN**: write the failing test first, run it and watch it
   fail, then write the minimal code to pass. After each step run the **targeted**
   check (a single-test filter and/or the linter), not the whole suite yet. Subagents
   report results; **you** run the checks, so one agent's green is never taken on
   faith.

4. Keep steps small — split anything past ~8 files / ~200 lines of diff. Follow the
   repo's own file-size and module-splitting conventions if it has any; do not impose
   a limit it never asked for.

   **Comment the way this repo already comments.** Match the surrounding density,
   which usually means writing none: a comment earns its place when a reader would
   otherwise ask *why* — a workaround, a constraint from outside the code, a rule that
   looks wrong until you know the reason — never to restate what the line says, and
   never as narration of your own change ("added validation here"). The same restraint
   applies to config, YAML and project files: comment the value that would surprise
   someone, not every key. **Write comments in the language the repo's code already
   uses** — English unless the surrounding code says otherwise, whatever language this
   conversation is in.

5. On **3 consecutive failures of the same check**, stop iterating blindly. Classify
   the cause — test, code, environment, or plan drift — fix it at the source, and
   resume. If you can't confidently classify it, that's an escalation.

6. **Close each item before starting the next.** Once its steps are done and its
   targeted checks pass:

   1. Stage **only the files that item touched** — never `git add -A`, never the plan
      file, never anything secret-like (`.env`, keys, tokens), and never echo a secret
      value into a command or a commit message.
   2. Commit with a message carrying `LINK-TOKEN` for **that sub-ticket**, so the
      tracker attaches the commit to the child and not only to the parent. **When the
      work list is `TICKET` itself** — no children, per Step A — there is no child key
      to use: the parent's is the one that goes in every commit, here and in the fixes
      made at Step G and Step I.
   3. Push `BRANCH`. Everything lands on the same branch — **one branch for the whole
      `TICKET`**, and one PR at the end.
   4. `COMMENT` on the sub-ticket with what changed and the checks that verified it,
      then set its `STATUS→DONE` (both pre-authorized). A child that is implemented,
      gated, committed and pushed is finished as a unit of work; leaving it in progress
      makes the board claim there is work left that nobody is doing. **The parent is
      the one that waits** — it moves to `STATUS→IN-REVIEW` at Step J and stays there
      until someone merges the PR, which is never you. A child you had to abandon or
      escalate is the exception: leave it in progress and say so.

      **`STATUS→DONE` is for children only.** If `TICKET` has no children, the work list
      is the ticket itself (Step A) — it is the parent, so **comment here and leave the
      status alone**. Step J moves it to `STATUS→IN-REVIEW`. Closing it here would put
      the board in Done before a PR exists and then walk it *backwards* into In Review,
      which is the one thing a tracker must never show.
   5. Update the plan file, then start the next item.

   **Keep the plan file current *inside* the item too, not only when it closes.** Tick
   each step as its targeted checks pass, and record a decision or a deviation when it
   happens. An item can take many turns and a session can be interrupted in any of them;
   a plan file written only at close-out loses everything since the last item, and the
   resumed run rebuilds it from the diff — or from guesswork.

   A mid-branch push is a **checkpoint, not a delivery**: the targeted checks of F.3 are
   what gate it, and the full gate set runs once at Step G over the complete diff. If
   the repo's whole gate takes seconds, run it here too — cheap insurance.

## Step G — Gates

Run this **once, over the complete branch diff**, after the last item of the work list
is closed — not per sub-ticket. Step F.6 already gated each push with targeted checks;
this is the full sweep that stands behind the PR.

**Resolve the repo's gate commands in this order**, and use the first that answers:

1. A "Gates", "Commands", or "Build & Test" section in `CLAUDE.md` / `AGENTS.md` —
   where a repository is supposed to declare this. Claude Code reads `CLAUDE.md`; many
   repos put the detail in `AGENTS.md` and delegate to it from there, so check both.
2. Fallback detection from the manifest — `Makefile`, `package.json`,
   `pyproject.toml`, `go.mod`, `Cargo.toml`, `*.sln`, `pom.xml`, `build.gradle`,
   `composer.json`, `Gemfile`. Read the actual scripts/targets; don't guess that
   `npm test` exists because `package.json` does.
3. If neither answers and the repo clearly has checks you can't identify, **ask** —
   one `AskUserQuestion` naming the candidates you found beats inventing a command or
   declaring an untested repo green.

Run every gate that resolves: lint, type-check, build, tests, and any architecture or
dependency lint the repo defines. **A gate that does not exist degrades to green — but
name the ones you skipped**, so "green" is never mistaken for "complete".

Then run **`/code-review`** on the diff at medium effort, review only (no `--fix`).
Treat correctness and security findings as red. Run **`/security-review`** as well
when the diff touches auth, secrets, input parsing, or external I/O.

**The gate is never delegated to a subagent.** Run it yourself and paste the real
output. A subagent reporting "tests pass" is a claim; the gate's own output is
evidence.

**Never proceed on red.** Fix and re-run this step. On an *ambiguous* failure — flaky
versus real, unclear error, possibly pre-existing — escalate rather than guess.

**Fixes here follow the same close-out as F.6.** Stage only the files you touched,
commit with the `LINK-TOKEN` of the sub-ticket the fix belongs to, push `BRANCH`. Never
`git add -A`, never the plan file, never anything secret-like. The commit/push rules live
in F.6.1–F.6.3 and they apply to every commit on this branch, not only the ones made
inside the loop.

**A red gate on work already marked done sends that item back.** F.6.4 closes a child on
its *targeted* checks; this step is the first time the full suite, `/code-review` and
`/security-review` see the complete diff. If a finding lands in a sub-ticket already at
`STATUS→DONE`, move it back to in progress, `COMMENT` what the gate found, fix it, and
close it again through F.6.4. A tracker left asserting a verification that later failed
is worse than one that never claimed it.

## Step H — Open the PR

The commits and pushes already happened, one per item, in Step F.6. What is left is the
delivery.

1. **Confirm the work list is complete**: every selected sub-ticket implemented, its
   checks green, its commit pushed, and `git status` clean apart from the plan file. If
   it is not clean, a fix from Step G was left uncommitted — commit it under F.6.1–F.6.3
   rather than improvising a commit here.
2. **If anything on the work list was not implemented** — blocked, escalated, or
   abandoned — **do not open the PR.** Report what is missing and why, `COMMENT` the
   same on `TICKET`, and **leave `BRANCH` pushed**: nothing that was built is lost and
   the run can be resumed. This is the one place a run ends without a PR, and it ends
   there deliberately.
3. Open the PR with `OPEN-PR`, **once, for the whole `TICKET`**. Its title carries
   `LINK-TOKEN`. Its body links `TICKET`, lists each sub-ticket with what it changed,
   and states **the verification commands you actually ran** with their results — not
   the ones you intended to run.

**Never merge the PR** and never enable auto-complete. Opening it is where your
authority ends.

## Step I — Watch CI to green, then address review comments

Loop until the PR is **green with no unaddressed comments**. This runs **in the main
conversation, not a fork** — it pushes commits and needs Bash approvals a subagent
cannot get. No one should need to watch the session while it runs.

**Do not tight-poll.** Prefer the platform's own blocking watch (see `CI`). Failing
that, pace the waits with the `Monitor` tool or `ScheduleWakeup` if this session
offers them; otherwise poll on a bounded schedule with real gaps between checks.

On red:

1. Pull the failing job's logs via `CI`.
2. Classify **flaky vs. real**. Rerun a plausibly-flaky job **once**. If it fails
   again, it is real.
3. Fix a real failure **at the source** — not by loosening the test. Re-run Step G
   locally, then commit and push under the F.6.1–F.6.3 rules — staged narrowly, with the
   `LINK-TOKEN` of the sub-ticket the fix belongs to. Same for a code change made in
   response to a review comment below.
4. **Convergence guard:** three fix attempts on the same failing job and you stop and
   escalate. A loop that isn't converging is a signal, not a reason to keep going.

On green, read the review comments via `PR-COMMENTS` and address each one: change the
code, or reply explaining why not — never silently ignore one. Re-push, resolve the
threads the platform lets you resolve, and go back to watching CI. A comment that asks
for a product judgment nobody has answered is an escalation, not a code change.

## Step J — Wrap up

1. Post the summary via `COMMENT` on `TICKET` — the parent — and set its
   `STATUS→IN-REVIEW` (both pre-authorized — never ask). The parent is what waits on
   the PR; the sub-tickets were already commented and moved to their completed state
   as each one closed, in Step F.6. The summary must reflect the
   **final** state: the PR URL, the CI result, and that review comments were addressed.
   **Report CI as green only if CI exists and passed.** A repo with no pipeline gets
   "no CI configured — the gates that ran were <the ones from Step G>", never silence
   and never "green": the reader cannot tell a passing suite from an absent one, and the
   PR body is where that difference gets decided.
2. **Ask what to do with `.claude/plans/<TICKET>.md`** — one `AskUserQuestion`, three
   options, **Delete it** first: delete, leave it in place, or move it into the repo's
   own documentation if the team keeps plans there. Never commit it on your own
   initiative. Deleting is `rm .claude/plans/<TICKET>.md` and nothing wider. It **will ask
   for permission** — `allowed-tools` is static and `<TICKET>` is not, so no grant can be
   written that covers this plan and not every other ticket's. One prompt, for the only
   destructive step in the run, immediately after the user chose it.
3. Report back in the session: PR URL, CI status — green, red, or **not configured**,
   named as such — tracker status, **which sub-tickets were built and which were left out
   of this run** (and which were already complete before it started), what the watch loop
   changed after the first push, which comments you addressed, which gates were skipped
   because the repo does not define them, and anything deliberately left for a
   follow-up.

---

## Escalation — stop and ask ONLY for

- **Production writes, deploys, or destructive / irreversible actions.**
- **Customer-facing sends** — real outreach to real recipients.
- **An ambiguous gate or CI failure** — a check fails in a way you cannot confidently
  resolve: flaky vs. real, unclear error, environment gap, possibly pre-existing.
- **A non-converging watch loop** — CI still red after 3 fix attempts on the same job.
- **A product-judgment call with no source-of-truth answer** that Step A did not close.
- **A missing credential or permission** you would have to work around.

For everything else — branching, planning, implementing, fixing your own gate
failures, and tracker writes on `TICKET` itself — **decide and proceed. Do not check
in.**
