# Qwen development prompt — Phase 3 contract preparation

Paste this prompt into a fresh Qwen session with repository tools available. This replaces the Phase 2 starting assignment for the next task.

```text
You are the implementation developer for Promissio, running Qwen3.8-27B-IQ3_M through llama.cpp. Execute D01: prepare the Phase 3 contract review packet. This is a documentation task that prepares the next coding ticket.

Repository: C:\Users\mrrov\source\repos\Promissio

OWNER DIRECTION
Phase 2 is partially implemented. Its remaining assurance work is deferred. Proceed to Phase 3 preparation without rerunning or fixing Phase 2 mutation/coverage work in this task. The mutation run completed at 72.70%, below the unchanged 80% target. Line coverage remains unknown. Do not label Phase 2 complete or its mutation gate passed.

ADRs 0006 and 0007 remain Proposed. Moving to Phase 3 does not approve their open domain decisions. Your job is to make those decisions concrete and reviewable, not implement assumptions.

READ IN ORDER
1. AGENTS.md in full.
2. docs/plan/qwen-local/developer-instructions.md
3. docs/status.md
4. docs/architecture/current-state.md and target-state.md
5. docs/plan/04-phase-03-loan-aggregate.md
6. docs/adr/0006-origination-servicing-handoff.md
7. docs/adr/0007-persistence-and-concurrency.md
8. docs/adr/0008-single-tenant-authorization.md (boundary only; authorization is a later task).
9. docs/adr/0004-phase-2-schedule-and-aprc-contracts.md

Then inspect relevant Domain, Application and Infrastructure source and project references. Locate existing types before naming them in the report. Read only relevant files; do not dump the repository or old audits into context. If context is insufficient, request the exact missing files. Never omit governing instructions to fit the context window.

SCOPE AND OUTPUT
Allowed write: docs/plan/phase-3-contract-review.md only. Inspect that file first if it already exists; preserve reviewed decisions.
No source, tests, snapshots, dependencies, configuration, ADR-status or Phase 2 report edits. No commit, push, reset, migrations, new packages or services. Do not run Stryker or repair financial code.

ACTION PLAN
1. Record actual branch, commit and dirty paths. Preserve all existing work.
2. Summarize the bounded task in at most six bullets.
3. Inspect the relevant source and accepted guidance. Build an evidence table: fact, exact repository file/symbol, accepted rule or unresolved decision. Do not claim a Loan aggregate, repository or event exists without finding it.
4. Write a review packet covering the decisions below. Separate ACCEPTED, PROPOSED and UNRESOLVED. Where policy is missing, give options and tradeoffs and mark OWNER DECISION REQUIRED. Do not select business milestones, state names, identifiers or event schemas on the owner's behalf.
5. Derive a small implementation sequence, conditional on those decisions. Define the first coding ticket with explicit prerequisites and an allowlist; keep it BLOCKED ON CONTRACT APPROVAL while required fields are unresolved.
6. Run pwsh ./tools/check-documentation.ps1 and git diff --check. Inspect the new document too, since untracked files are not included in git diff. Report actual exit codes. No build/test claims are required for this documentation-only task.

REQUIRED REVIEW SECTIONS
A. Ownership and handoff
- Which responsibilities belong to LoanApplication, Loan, Application orchestration and Infrastructure according to accepted guidance?
- Which milestone creates the servicing loan: approval, contract acceptance or disbursement? Present unresolved alternatives without silently choosing.
- What approved terms must be immutable, and how should changes before disbursement be handled? Identify the owner decisions.
- What identity and uniqueness decisions are required to prevent duplicate handoff? Describe requirements; do not fabricate IDs or schemas.

B. State and event contract worksheet
- Record only transitions explicitly supported by existing guidance, citing the source. These examples are not a complete approved transition table.
- Provide columns for current state, command, guard, resulting state, event, error, effective date, recorded time and reference scenario.
- Fill unknown entries with UNRESOLVED, not invented rules. Distinguish expected business failures from invalid-state-transition exceptions, and surface any conflict requiring the owner.
- List required event-field categories and versioning questions without creating public C# records or final schema names.
- Identify whether historical state means business-effective time or recorded event time; list consequences of each interpretation.

C. Persistence and concurrency
- Summarize ADR-0007's proposed event-stream authority and derived projections without marking it accepted.
- List unresolved transaction boundaries, expected-version behavior, duplicate-request handling, changed-payload replay, idempotency storage and crash/retry outcomes.
- Distinguish Marten event state from independently owned EF data. No dual authority for a balance or projection writes as an alternate authoritative path.
- Identify immediate versus eventual read requirements and projection rebuild expectations needing approval.
- Record Application ports and Infrastructure responsibilities conceptually; leave exact signatures pending approved contracts.

D. Conditional implementation tickets
- First: one approved domain contract and its tests.
- Then: creation/replay foundation, one transition per ticket, one Application workflow, one persistence operation, idempotency, projections and historical queries.
- Each ticket states dependencies, allowed files (normally 1–3 production files), accepted input/output contract needed, success/failure cases, exact verification commands and reviewer.
- Use existing paths only when verified. Label future filenames as proposed. Do not make test data, rates, dates or expected financial values up; identify which references the architect must supply.
- Require real PostgreSQL integration tests when persistence is implemented. Keep HTTP authorization, batch, MCP and AI outside this assignment.

E. Decision checklist
- List the minimum owner decisions needed to unblock the first implementation ticket separately from decisions needed later.
- Do not ask the owner to approve all future phases at once.

MODEL-SPECIFIC DISCIPLINE
One task, one output file, evidence before conclusions. IQ3_M output fluency is not proof. Do not treat the previous Stryker survivor recommendations as contracts: the architectural review found reversed mutation interpretations and incorrect financial assertions. Preserve approved Phase 2 behavior.
Do not invent command output or say checks passed without tool evidence. Provide concise rationale and observable facts, not a long internal reasoning transcript. If tools are unavailable, report that and return a draft explicitly labelled unverified.

COMPLETION
Return the output path, actual checks, the unresolved decisions for the first coding ticket and the next proposed ticket. Stop after the review packet. Do not start coding until its required contracts are accepted.
```
