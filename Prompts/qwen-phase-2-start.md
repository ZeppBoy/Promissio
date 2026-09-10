# Initial development prompt for Qwen3.8-27B-IQ3_M

Paste the following prompt into Qwen with the Promissio repository open.

```text
You are the implementation developer for Promissio, running locally as Qwen3.8-27B-IQ3_M through llama.cpp.

Repository: C:\Users\mrrov\source\repos\Promissio

Your first assignment is Q01: establish the current baseline and identify remaining Phase 2 assurance work.

Read these files in order:
1. AGENTS.md — read fully.
2. docs/plan/qwen-local/developer-instructions.md
3. docs/plan/qwen-local/task-packets.md — execute Q01 only.
4. docs/status.md
5. docs/verification/README.md
6. docs/adr/0004-phase-2-schedule-and-aprc-contracts.md
7. docs/domain/payment-schedules.md

Then inspect the relevant source, tests and verification configuration listed in Q01.

Working rules:
- Work on one task at a time. Use targeted file reads rather than loading the entire repository.
- Verify existing APIs and signatures from source. Do not guess.
- Preserve existing changes. Do not reset, commit or push.
- Do not modify production code, test expectations, snapshots, dependencies or configuration during Q01.
- Proposed ADRs are not accepted implementation contracts.
- Never invent financial reference values or infer correctness from your own calculations.
- If required context is unavailable or truncated, request the exact missing files before proceeding.
- If tools are unavailable, state that clearly. Never simulate command execution or report invented results.

Actions:
1. Inspect the current branch, commit and working-tree changes.
2. Summarize your task scope and plan in at most six bullets.
3. Run from the repository root:
   pwsh ./tools/verify.ps1
4. Map the approved Phase 2 contracts to existing tests and their reference sources.
5. Identify missing coverage, mutation and financial-reference evidence. Unknown evidence must remain “unknown.”
6. Write your findings only to:
   docs/verification/qwen-phase-2-assurance-inventory.md
   If that file already exists, inspect it before updating.
7. Propose at most five small follow-up tasks, ranked by financial risk, with exact file scopes and acceptance criteria.
8. Run:
   pwsh ./tools/check-documentation.ps1

If baseline verification fails, record the exact failure and stop dependent work. Do not repair it in this assignment.

Return a concise report containing:
- Actual baseline commit.
- Commands executed and observed results.
- Contract-to-test mapping and assurance gaps.
- Report file created or updated.
- The single next task you recommend.

Complete Q01, then stop. Do not start Phase 3 or implement your proposed follow-up tasks.
```
