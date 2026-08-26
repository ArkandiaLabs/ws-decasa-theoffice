# Architecture Decision Records

This directory holds **ADRs** — short, dated records of architecturally significant decisions. They explain *why* the code looks the way it does, which is harder to derive from reading the source than *what* it does.

## When to write one

Write an ADR when you:
- Choose between two or more viable options (language, framework, pattern, tool).
- Accept a trade-off that future contributors will want to revisit.
- Adopt a convention that isn't enforced by tooling.

Don't write one for reversible implementation details.

## Format

Use `adr-template.md` as the starting point. Each ADR has four sections:

1. **Status** — Proposed / Accepted / Superseded.
2. **Context** — the problem, the options considered, and the constraints.
3. **Decision** — what we chose.
4. **Consequences** — what becomes easier, what becomes harder.

Keep ADRs short (1 page). Name them `adr-NNNN-short-slug.md` with a zero-padded counter.

## Index

<!-- TODO: list ADRs as they are created. -->

- `adr-0001-...` — <!-- title -->
