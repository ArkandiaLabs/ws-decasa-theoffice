# Verification and the final report

There is no gate to break here — requirement-to-spec writes prose and tracker items, not code. "Verify"
means **rereading what Phase 4 actually wrote**, the same discipline `instrument-*` applies by
introducing a violation, except here the failure mode is a broken link rather than a gate that
passes when it should fail.

## Tracker mode

Re-fetch the parent issue/work item and every child Phase 4 created:

- **Confirm the parent relation actually resolves** — `parentId` on the Linear side,
  `System.Parent` on the Azure DevOps side — not merely that the creation call returned success. A
  create that "succeeds" with an unset relation is exactly the failure this step exists to catch,
  and it looks identical to a working run until someone opens the child and finds it orphaned.
- **Confirm the initial status landed on the state actually picked** — read it back, don't trust the
  write.

If either check fails, **do not report success**. Fix the link/status and re-verify before moving
to Phase 6.

## File mode

- Both `docs/specs/<slug>/spec.md` and `docs/specs/<slug>/tasks.md` exist at the expected path.
- **Both links resolve, in both directions** — the `## Breakdown` heading in `spec.md` pointing at
  `tasks.md`, and the `[← spec](./spec.md)` back-link at the top of `tasks.md`. Both are written by
  `LINK-PARENT` (see `tracker-bindings.md`), so both are real things to check. Open the two files
  and confirm the relative paths are correct, not merely that you wrote what looked like a link.

## The final report — four sections, always, even when one is empty

Say the section is empty rather than omitting it — an omitted section reads as "not applicable",
an empty one reads as "checked, found nothing".

1. **Asked** — every question actually put to the user, in the order asked.
2. **Answered** — the Decisions, quoted verbatim from Phase 3.
3. **Out of scope** — what was explicitly excluded, including any scope-creep item the user chose to
   keep out of this pass (and where they chose to register it, if anywhere), **and every project
   document detected as going stale that the user chose not to update** — named, with the line that
   made it stale. A document knowingly left wrong is a decision; it belongs somewhere findable, not
   dropped because nobody picked it.
4. **Not read** — every document or attachment that failed to convert; any tabular data that could
   not be cross-checked, distinguishing **no database MCP server was connected** from **one was
   connected and the query was refused or failed** — the second names the tool that was tried, and
   is never reported as the first (**data**, never a contract —
   contract detection is a repo scan and does not involve a database at all); anything Phase 1/2
   reported as missing or degraded. If a public contract was detected, it was detected; do not file
   it here because some unrelated cross-check was unavailable.

## Try it — the closing line

One concrete line, derived from what was actually created this run — never a generic template:

- **Tracker mode**: `/arkandia:linear-plan-build <PARENT-KEY>` or `/arkandia:ado-plan-build <id>`,
  with the real key/id substituted in.
- **File mode**: "read `docs/specs/<slug>/`" — plus the explicit caveat that
  `linear-plan-build`/`ado-plan-build` have no file-mode entry point today, so this path does not
  chain automatically into them. That asymmetry is accepted, not a defect this skill can fix on its
  own.

Do not commit. The changes — files, or tracker items — are left for the user to review.
