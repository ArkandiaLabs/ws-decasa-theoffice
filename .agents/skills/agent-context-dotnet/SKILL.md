---
name: agent-context-dotnet
description: Generate a documentation pack for a .NET repository so AI coding agents can reason about it — AGENTS.md, architecture, ADRs, data model, infrastructure, plus a `docs/dotnet.md` deep-dive covering the solution/project graph, target frameworks, EF Core data access, DI, configuration & secrets, analyzers, and CI. Invoke with `/arkandia:agent-context-dotnet [en|es]`.
argument-hint: '[en|es]'
disable-model-invocation: true
---

# agent-context-dotnet — Bootstrap .NET Repository Context

You are generating a **context pack**: a small, cross-linked set of Markdown docs that makes an
unfamiliar .NET repository legible to an AI coding agent. It has two halves, produced in one run:

- the **base pack** — `AGENTS.md`, `CLAUDE.md`, and `docs/` (business, architecture, data model,
  infrastructure, ADRs);
- the **.NET deep-dive** — `docs/dotnet.md`: the solution/project graph, target frameworks,
  package management, EF Core data access, the DI composition root, configuration & secrets,
  analyzer posture, the UI/API surface, packaging, and CI.

You MUST NOT write application code, install packages, or run destructive commands. Your only
outputs are Markdown files at the repo root and under `docs/`.

## Philosophy (hold these in mind throughout)

- **AGENTS.md is a table of contents, not an encyclopedia.** Keep it under ~80 lines.
- **The repository is the system of record.** Anything not in the repo is invisible to the agent.
- **Context is a scarce resource.** Every line in every doc must earn its place. A deleted section
  beats a section full of TODOs.
- **Progressive disclosure.** AGENTS.md points to specialized docs; each specialized doc delegates
  further.
- **TODOs over fabrication.** Never invent a framework version, NuGet version, or schema detail.
- **No application code.** This skill documents; it does not build.

## Input: language

`$ARGUMENTS` is either `en`, `es`, or empty.

- If `$ARGUMENTS == "es"` → output docs in Spanish. Load templates from `templates/es/`.
- If `$ARGUMENTS == "en"` or empty → output docs in English (default). Load templates from `templates/en/`.

The skill's own instructions (this file) stay in English regardless.

---

## Phase 1 — Discover (silent)

Do this without talking to the user. Use Glob, Grep, and Read.

### 1a. Confirm this is a .NET repo

Look for `*.csproj`, `*.sln`, `*.slnx`, `*.fsproj`, `*.vbproj`, `global.json`, or
`Directory.Build.props`.

Before concluding "not .NET", also check for **file-based apps** (.NET 10+): standalone `.cs`
files carrying `#:package` / `#:sdk` / `#:project` / `#:property` directives, or a
`#!/usr/bin/env dotnet` shebang. These have no project file and a `*.csproj` glob will miss them.

If **nothing** matches, stop and tell the user this skill only applies to .NET repositories.
Write no files.

### 1b. Detect prior context → augment mode

If ANY of these exist, switch to **augment mode**:

- `AGENTS.md`, `CLAUDE.md` at repo root
- `docs/` directory with `.md` files
- `ARCHITECTURE.md`, `ARCHITECTURE.rst`
- `ADR/`, `adrs/`, `decisions/`, `doc/adr/`

In augment mode: read what exists, report it to the user in Phase 2, and only create **missing**
docs. Never overwrite.

**A `docs/` tree is not necessarily yours.** Many repos ship their own documentation
(`docs/architecture/`, design notes, database dumps) that this skill did not create. Those are
**not** yours to edit — but DO cross-link them from `docs/dotnet.md` ("Related docs") and from
AGENTS.md, so the generated context points at what already exists instead of ignoring or
duplicating it.

When pre-existing docs are in one language, prefer matching it in Phase 2's language question.

### 1c. Deep .NET discovery

Run the full checklist in `references/dotnet-inspection.md`. It covers the solution/project graph,
target frameworks, package management (including central package
management), Aspire orchestration, data access, DI, configuration & secrets, build/run/test
including the test-platform split, quality gates, the UI/API surface, deployment & packaging,
cross-cutting concerns, the C# language posture, and hotspots.

The checklist is conditional: inspect only what the repo actually signals, and carry that
conditionality into the doc — delete `docs/dotnet.md` sections that don't apply.

Read real files. Where a fact isn't readable, you'll leave a TODO — do not guess.

### 1d. Adjacent signals

A .NET repo is rarely only .NET. Glob for these, since they feed `architecture.md` and
`infrastructure.md`:

| Signal | Infer |
|---|---|
| `Dockerfile`, `docker-compose*.yml`, `compose.yaml` | Containerization (but see the checklist — the SDK can build images with no Dockerfile) |
| `Chart.yaml`, `values.yaml`, `k8s/`, `kustomization.yaml` | Kubernetes / Helm |
| `*.tf`, `terraform.tfvars`, `*.bicep`, `cdk.json`, `serverless.yml`, `template.yaml` | Infrastructure as Code |
| `azure-pipelines*.yml`, `.github/workflows/`, `Jenkinsfile`, `.gitlab-ci.yml` | CI/CD |
| `*.wsdl`, `*.xsd`, `*.proto`, `openapi.yaml`, `swagger.json` | API contract style (SOAP, gRPC, REST) |
| `package.json`, `pnpm-workspace.yaml`, `angular.json`, `vite.config.*` | JS/TS frontend alongside the .NET backend |
| `sonar-project.properties`, `.editorconfig` | Quality gates (details in the checklist) |

### 1e. Read the README

Read `README.md` if present. Use it to seed the one-line project summary. Do NOT copy large
chunks — just extract the purpose.

### 1f. Scan for obvious domain cues

Grep the entity / model / `DbContext` classes. Note dominant domain nouns (e.g. `Order`,
`Invoice`, `Patient`, `Course`). Use them only as prompts for your Phase 2 interview — don't
hallucinate a domain you can't verify.

---

## Phase 2 — Interview

**Around ten questions is the norm here, and asking more is fine when the repo genuinely left a
load-bearing gap.** What keeps that from being tedious is the skip rule, which is absolute:
**never ask what Phase 1 already read.** On a well-documented repo you may end up asking three
questions; on a bare legacy solution, twelve. Both are correct.

`AskUserQuestion` caps at 4 questions per call and 4 options per question, so the structured set
needs two batched calls. Long-form answers don't fit it at all — ask those in plain chat.

### 2a. Batch A — scope and disambiguation (one `AskUserQuestion`)

1. **Output language** — only if `$ARGUMENTS` was empty. Options: `English (default)`, `Spanish`.
2. **Optional docs** — "Generate also `target-user.md` and/or `design.md`?" `multiSelect: true`.
   Options: `target-user.md`, `design.md`.
3. **Augment-mode confirmation** — only if Phase 1b found existing docs: "Existing docs detected:
   [list]. Only generate missing ones?" Options: `Yes (augment only)`, `Overwrite matching docs`,
   `Cancel`.
4. **Phase-1 ambiguity** — the one thing discovery could not settle. Usually the DB provider
   (when the `DbContext` and the package list disagree) or the primary target framework (when
   projects differ). Offer the top candidates **you actually read**, not generic ones.

### 2b. Batch B — facts that live outside the repo (second `AskUserQuestion`)

5. **Production deployment target** — rarely readable from source, and `infrastructure.md` needs
   it. Options: `Azure App Service`, `Azure Container Apps / AKS`, `IIS on VM / on-prem`, `Other`.
6. **Production secrets source** — Options: `Azure Key Vault`, `Environment variables`,
   `User-secrets only (dev)`, `Other`.
7. **Auth / identity model** — only if ambiguous from the packages. Options:
   `Entra ID (Microsoft.Identity.Web)`, `ASP.NET Core Identity`, `IdentityServer / Duende`,
   `Other`.
8. **Path to production** — how a merge reaches the deployment target: `CI deploys on merge to
   main`, `Tag / release triggers deploy`, `Manual release`, `Other`. Feeds `infrastructure.md`;
   the pipeline file often shows the build but not the promotion path.

### 2c. Free-text answers — ask in plain chat

9. **Business context** — "In one or two sentences: what does this product do, and who pays for
   it?"
10. **Non-obvious rules** — "List up to 3 invariants or gotchas an AI coding agent must know that
    are NOT enforceable by linters or tests. Examples: *'`Core` must not reference
    `Infrastructure`', 'never bypass the tenant query filter', 'always thread `CancellationToken`',
    'run migrations before starting the API', 'do not touch the legacy `Billing` project'*. If
    none come to mind, reply 'skip'."

### 2d. Conditional extras — ask only when the repo left the gap

11. **Test expectations** — only if Phase 1 found thin or missing test coverage: "What counts as
    done for a change here — unit tests only, integration tests required, or end-to-end?"
12. **Ownership / escalation** — only if there is no `CODEOWNERS` and no obvious maintainer:
    "Who reviews changes to this repo?"

Do not proceed to Phase 3 until the interview is complete.

---

## Phase 3 — Draft

For each doc to generate, read the template at `templates/<lang>/<doc>.md.template`, substitute
the placeholders, and write to the target path. Placeholders use `{{UPPER_SNAKE}}` syntax; each
template declares its own at the top.

Target paths:

- `AGENTS.md` (repo root) — see Phase 4
- `CLAUDE.md` (repo root) — see Phase 4
- `docs/business.md`
- `docs/architecture.md`
- `docs/data-model.md`
- `docs/infrastructure.md`
- `docs/dotnet.md`
- `docs/adrs/README.md` + `docs/adrs/adr-template.md` + `docs/adrs/adr-0001-<slug>.md` (1–3 seed ADRs)
- `docs/target-user.md` (only if opted in)
- `docs/design.md` (only if opted in)

Rules for filling templates:

- Short sentences. Sacrifice grammar for clarity.
- If you don't have info for a section, leave a `<!-- TODO: fill in -->` marker — don't
  hallucinate. If a whole section doesn't apply (no UI, no Aspire, no MAUI), **delete it** rather
  than filling it with TODOs.
- **In augment mode, never clobber user content.** Fill `<!-- TODO -->` slots or append a clearly
  marked subsection; leave everything else alone. Docs the repo already shipped are read-only —
  cross-link them instead.

What each doc must carry from the .NET discovery:

- **`docs/dotnet.md`** — the primary output of Phase 1c. Keep the project table and the reference
  graph concrete (real project names). Cross-link, don't restate, the other docs.
- **`docs/architecture.md`** — name the framework + EF Core explicitly in the stack summary
  (e.g. "ASP.NET Core 10 + SQL Server + EF Core 10"), and add a one-line pointer to
  `docs/dotnet.md` for the project graph and layering.
- **`docs/data-model.md`** — migration tool is EF Core; record the `DbContext` location, the
  provider, and the migrations workflow (`dotnet ef` vs `Migrate()` on startup vs applied in CI).
- **`docs/infrastructure.md`** — the CI system and its pipeline file, the configuration & secrets
  layering, and the deployment/packaging shape (Dockerfile *or* SDK container publishing, AOT /
  trimming / single-file if in play).

ADR seeds — propose 1–3 decisions that were **clearly made**, each with Status, Context (with
alternatives considered), Decision, and Consequences (easier / harder). Good candidates:

- `adr-0001-target-framework.md` — the target framework the solution standardizes on.
- Data access — EF Core (and the provider) as the persistence approach.
- Deployment target, if a Dockerfile, IaC, or SDK container properties were detected.

Never fabricate the rationale for an ADR.

---

## Phase 4 — Wire (AGENTS.md + CLAUDE.md)

Generate `AGENTS.md` strictly as a **table of contents**:

- **Opening:** 2 lines max (project name + one-line purpose).
- **"Where to find things":** a bulleted list of every doc with a one-line description, including
  `docs/dotnet.md` ("deep .NET context: project graph, TFMs, EF Core, DI") and any pre-existing
  repo docs found in Phase 1b.
- **"Commands":** the 3–6 commands a developer actually runs. Take them from the real repo, not
  from habit:
  - When **Aspire** is present the entry point is `aspire run` / `dotnet run --project *.AppHost`,
    **not** each service individually. Getting this wrong sends an agent down the wrong path.
  - Note the test command the repo's runner actually needs (see the test-platform split in the
    checklist), and `dotnet ef` invocation style (global tool vs `dotnet tool run`).
- **"Non-obvious rules":** the user's Phase 2 answers, each as a bullet with a short rationale.
  Add the mechanical rules discovery turned up, which agents reliably get wrong:
  - central package management — when `Directory.Packages.props` manages versions, a
    `<PackageReference>` in a `.csproj` **must not carry `Version`**;
  - any AOT / trimming constraint (no unbounded reflection, no reflection-based serialization);
  - the project-layering rule, if one exists.
- **"Testing"** and **"Code style":** one paragraph each, naming the frameworks and analyzers
  detected in Phase 1.
- **"Security":** no secrets committed, `.env` / user-secrets not in VCS, don't log PII.

Enforce the ~80-line ceiling. If you exceed it, move detail into `docs/dotnet.md`.

`CLAUDE.md` is one line: `@AGENTS.md`, with a comment explaining that it delegates.

---

## Phase 5 — Validate claims (Claimify-inspired)

Generated docs hallucinate. Before finishing, surface the load-bearing factual claims you wrote
and confirm the uncertain ones with the user. This step is adapted from Microsoft Research's
**Claimify** — extract atomic, self-contained, verifiable claims, and **flag ambiguity instead of
guessing**. Follow `references/claim-validation.md` in full. In short:

1. **Select** the verifiable, load-bearing claims from the docs you just wrote: target frameworks,
   the persistence provider and migration workflow, the deployment
   target, the CI system, key entities, the commands, package versions, DI lifetimes, and the
   user's non-obvious rules. Skip TODOs, boilerplate, and opinions.
2. **Atomize + tag provenance.** One self-contained statement each, with a source ref
   (`file:line` or `inferred`) and a confidence: `high` (read from a file), `medium` (one weak
   signal), `low` (guessed / unverified).
3. **Flag ambiguity.** Mark any claim with more than one plausible reading or no clear source
   (e.g. two projects pinning different target frameworks). Never silently keep a low-confidence
   claim.
4. **Verify with the user.** Present a compact ledger; confirm or correct the `medium` / `low` /
   ambiguous claims (use `AskUserQuestion` for the top binary confirmations, plain chat for the
   rest). `high`-confidence claims with a concrete source are shown but not blocking.
5. **Apply.** Write corrections into the docs. Downgrade any unconfirmed `low`-confidence claim to
   `<!-- TODO: verify -->` rather than asserting it.
6. **Persist** the ledger to `docs/claims-ledger.md` (format in the reference) as an audit trail.

---

## Phase 6 — Verify

1. Print a tree of files written (or augmented).
2. Check that every link in `AGENTS.md` and `docs/dotnet.md` resolves to a file that exists
   (use Read).
3. Remind the user:
   - Commit: `git add AGENTS.md CLAUDE.md docs/ && git commit -m "docs: bootstrap .NET context pack for AI coding agents"`
   - Fill in the `<!-- TODO -->` markers, review the ADRs, and skim `docs/claims-ledger.md` for
     anything still unverified.
   - If quality gates were absent, consider adopting `.editorconfig` + analyzers
     (`StyleCop.Analyzers`, `Microsoft.CodeAnalysis.NetAnalyzers`) and an arch-linting tool
     (`NsDepCop` / `ArchUnitNET`) to enforce the layering the docs describe.
   - Re-run `/arkandia:agent-context-dotnet` later; it will augment, not overwrite.

---

## Reference

- `references/dotnet-inspection.md` — the full .NET discovery checklist (Phase 1c).
- `references/claim-validation.md` — the Claimify-inspired claim-validation procedure (Phase 5).
- `templates/en/` and `templates/es/` — the doc skeletons.

## Rules

- Do NOT write application code.
- Do NOT overwrite existing docs without explicit user opt-in; enrich by filling TODOs or
  appending clearly marked sections.
- Do NOT fabricate framework or package versions, providers, endpoint names, or schema you
  haven't read.
- DO leave `<!-- TODO -->` markers where human input is needed, and delete sections that don't
  apply rather than padding them.
- DO keep every doc focused: each has one job, delegated from AGENTS.md.
