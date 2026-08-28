# Interview — the ambiguity sweep and the task breakdown

Same discipline the delivery skills (`linear-plan-build`/`ado-plan-build`) apply when they grill you
about a ticket — ask only what the source material does not already answer, record answers as quoted
**Decisions** and self-settled calls as written **Assumptions** — but aimed at a business document
read by a business reader instead of a ticket read by an engineer. The categories are generic
analysis-of-requirements
categories that this or that document happens to illustrate well — not a checklist tuned to any one
document. If a category never fires on a given document, that is expected; it exists for the
documents where it does.

## Categories to sweep

- **Implicit scope** — what the document clearly implies but never states outright.
- **Text vs. attachment contradictions** — a number, a name, or a rule in the prose that disagrees
  with what an attachment (spreadsheet, screenshot, prior spec) actually shows.
- **Behavior gaps** — "what happens when…", "what about the ones that don't have…" — cases the
  document's happy path never addresses.
- **Numbers that look inconsistent** — totals that don't sum, a count mentioned twice with two
  different values, a date range that overlaps another.
- **Scope that arrives casually** — an ask folded into a closing paragraph in a conversational tone,
  easy to read past because it doesn't look like a requirement.
- **Undeclared currency or unit** — a price, a weight, a duration with no unit stated, where the
  team's context does not make the default obvious.
- **Validation criteria stated as background** — a rule the business will check the delivered work
  against ("the totals have to match the invoice", "an order without a customer is rejected"),
  written as context rather than as a requirement. See § *Validation criteria* below: it is never
  folded silently into the prose and never promoted to a task on your own.

## The requirements that are always active

Never conditional on the agent "deciding to look" — these run every time, whether or not the
document itself raises them:

1. **Public-contract impact** — resolved in `references/repo-context-impact.md`, turned into a
   question here if a contract was detected.
2. **The tabular cross-check** — if Phase 1 detected a database MCP server and the document or an
   attachment carries data that should exist in that database, run the comparison
   (`references/repo-context-impact.md`) **before** the sweep below, and turn every discrepancy into
   one of its questions, with the concrete numbers on both sides. This is the step that catches "the
   spreadsheet says 11, the database says 25" — it is worthless if it runs after the questions are
   already asked, and dishonest if the report claims it ran when it did not.

   **A database MCP server is detected by shape, so its tools cannot be named in `allowed-tools`
   ahead of time — expect a permission prompt on the first query, and let it happen.** That prompt
   is the run working. What is forbidden is treating a *refused* or unavailable call as "no
   database connected": Phase 1 already recorded that one is. If the query is denied or fails, say
   so in the **Not read** section of the report in those words, naming the tool you tried.
3. **Documentation impact** — resolved in `references/repo-context-impact.md`, turned into one
   question here: which of the documents the change leaves stale get updated in this pass. Ask it
   with the quoted evidence attached, never as a bare list of filenames; "`docs/database.md` line 40
   documents the `stock` table this change adds columns to" is answerable, "the docs may need
   review" is not.
4. **Missing attachments** — resolved in `references/document-conversion.md`. This is a **fact to
   report**, not a question — do not ask the user whether an attachment is missing; you already
   know. State it in the report.
5. **Grouped questions** — `AskUserQuestion`, **at most 4 per call**, recommended option first, no
   jargon without one clause of explanation. The person answering is in the business, more reliably
   than in `instrument-*`'s already-strict rule for that — do not let a technical term slip through
   unexplained.
6. **Validation criteria** — every criterion the sweep surfaces gets the two questions in
   § *Validation criteria* below. Never decide on your own that one is "just context" or that it
   blocks the rest of the work.

## Validation criteria: two questions, not one

A requirement document — or the user, answering a sweep question — routinely carries a
**validation criterion**: the rule the business will check the result against before accepting it.
It arrives phrased as background ("of course the totals have to match the invoice"), and it is
usually real work: a check to implement, a report to reconcile, a test to write.

Two things you never do with one: fold it silently into the spec's prose as if it were context,
or turn it into a task yourself. Ask, in order:

1. **"Does this validation become its own item in the breakdown, or is it only a note in the
   spec?"**
2. **Only if it becomes an item: "Does the rest of the work wait for this validation, or can it be
   built alongside?"** — a blocking criterion goes ahead of the items it gates, with the dependency
   written out in the breakdown; a non-blocking one is an ordinary item in the normal order.

Q2 only makes sense once Q1 came back "its own item", so it goes in a **later `AskUserQuestion`
call**, batched with the next round — the same shape as scope creep below. There is **no default**
for either question: it is decided per criterion, every time, never reused from a previous
criterion or a previous run.

The result must match what was chosen. "Note in the spec" ends as a line in the spec and nothing
else; "its own item" ends as a real item, and only an answer of "the rest waits" ever makes it a
blocker.

## Scope creep: two questions, not one

When a category-sweep item turns out to be new scope arriving mid-document (the casual-ask case
above), it is **two separate questions**, asked in order:

1. **"Does this belong in this change, or is it separate?"** — a scope decision.
2. **Only if separate: "Should I just note it in the spec, or make it its own item in the
   breakdown?"** — there is **no default** for this. It is decided per case, every time; do not
   reuse the answer from a previous run or a previous item in the same run.

Q2 only makes sense once Q1 has been answered "separate", so it goes in a **later
`AskUserQuestion` call** — never bundled with Q1, which would ask the follow-up before knowing
whether it applies. Batch it with the next round's questions.

**Phrase Q2 in terms of the breakdown, not the tracker.** The destination question has not been
asked yet at this point in the phase, and it may well come back "Local file" — an option that says
"register it in the tracker" writes a promise the run may not be able to keep. "Its own item in the
breakdown" is true in both modes: in tracker mode that item becomes a real subissue/work item, in
file mode a row in `tasks.md` — the destination chosen later decides which, and neither is the
skill deciding scope on its own.

The result must match what was actually chosen: "note in the spec" ends as a line in the spec;
"its own item" ends as a real item in the breakdown, never folded into another one regardless of
how minor it looks.

## Recording the outcome

Two lists, carried forward into the spec exactly as the delivery skills carry them into a plan:

- **Decisions** — quoted, not paraphrased. What the user actually answered.
- **Assumptions** — what was resolved without asking, because it was minor. Write these down even
  when confident; they are what Phase 5/6 and the user's own read of the spec are there to catch.

## Deriving the task breakdown

This is its own activity once the Decisions are closed — not something that falls out of the
interview as a side effect.

1. **The functional tasks carry the change; documentation follows it.** A document that describes
   the system is rewritten to match what was built, so it cannot come first: putting a
   "update `docs/database.md`" task at the head of the breakdown, or marking a code task as blocked
   by it, inverts the real dependency and stalls the delivery on prose. **Every document confirmed
   in requirement 3 becomes a task placed after the functional work it describes, and blocks
   nothing.** Each still gets **its own subissue/work item** — never folded as a note inside a
   functional task — and each still names three things or it is not actionable: **which file**,
   **what in it is now false** (the quoted line from Phase 1), and **what it should say instead**
   once the functional work lands. "Update the docs" is the failure this step exists to prevent.
   For a superseded ADR the task is "write a new ADR superseding `ADR-00N`" — never "edit
   `ADR-00N`".
2. **A documentation task goes first only when the code is written *against* it**, and only when
   you can say which functional task cannot start without it — a typed contract or schema
   declaration the implementation consumes, or an ADR recording a decision the work implements.
   That is the whole exception, it is argued case by case in the task itself, and it never extends
   to "we should document this before we change it".
3. **A validation criterion is a blocker only if the user said so** (§ *Validation criteria*).
   Otherwise it is an ordinary item in the normal order.
4. **Write dependencies out, don't imply them by ordering.** Wherever one item genuinely waits on
   another, the breakdown says which and why; every item with nothing written is free to be picked
   up on its own.
5. Everything else follows in the order the spec's own sections present it, grouped so that a
   `linear-plan-build`/`ado-plan-build` run against any single item has what it needs without
   reading the others first.

Batch the ambiguity questions across rounds of ≤4; there is no cap on the number of rounds, only on
the size of each `AskUserQuestion` call.
