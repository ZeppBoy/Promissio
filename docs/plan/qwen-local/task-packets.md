# Task packets for the local developer

The architect fills the template. Qwen executes exactly one packet. Paths below are relative to the repository root. They become absolute when supplied to a client that requires absolute paths.

## Required implementation packet

```text
Task ID and single behavior:
Baseline branch/commit and permitted existing changes:
Prerequisites with accepted ADR identifiers:
Read first (exact files and relevant symbols):
Allowed production files (normally 1–3):
Allowed test and documentation files:
Existing signatures to reuse (copy from source):
Approved new public signatures/event schema (if any):
Inputs, outputs and units:
Invariants:
Expected errors and exact failure behavior:
Approved reference inputs/outputs with source:
Acceptance cases: success, boundary, invalid input, regression:
Persistence/concurrency/idempotency contract (if relevant):
Explicit non-goals:
Focused verification commands:
Full verification: pwsh ./tools/verify.ps1
Formatting paths beyond the shared script:
Required reviewer and completion evidence:
```

Unfilled behavior/schema/reference fields block dependent implementation. Do not ask Qwen to complete them creatively.

## Ready task Q01 — establish the baseline and assurance inventory

Objective: identify the remaining Phase 2 assurance work without changing production code or test expectations.

Read: AGENTS.md; docs/status.md; docs/verification/README.md; docs/adr/0004-phase-2-schedule-and-aprc-contracts.md; docs/domain/payment-schedules.md; the domain test project and its ScheduleGeneration/Calculations tests; stryker-config.json; tools/verify.ps1; Directory.Packages.props.

Expected baseline: main at 8b3c314 or a verified descendant. Record actual commit and dirty paths; never reset unrelated changes.

Allowed output: one new report, docs/verification/qwen-phase-2-assurance-inventory.md. No production, test, package or configuration edits.

Steps:

1. Record the actual environment and run `pwsh ./tools/verify.ps1`. Report failures as failures. Do not fix them in this ticket.
2. Map each approved Phase 2 contract to existing test names and source references. Separate externally sourced expectations, independent calculations, algebraic properties and residual/self-consistency checks.
3. List whether line coverage and mutation evidence exist for this checkout. Missing evidence means unknown, not zero. The prior 405-test result does not establish a score.
4. Propose at most five follow-up tickets ranked by financial risk, each with a bounded file scope and a missing assertion/reference/tooling requirement. Do not invent expected numbers.
5. Run `pwsh ./tools/check-documentation.ps1` after writing the report.

Acceptance: report includes commit, actual commands/results, contract-to-test table, gaps and next tickets. No financial source/test changed. Empty test projects remain explicitly identified. A failed baseline produces a blocked report, not a passing claim.

## Task Q02 — measure coverage (architect must complete tooling choice)

Goal: produce Domain line coverage for the current checkout, with the >=90% target visible. First inspect centrally declared packages and existing collectors. The architect supplies the compatible collector/tool version and exact command before implementation; no guessed package version.

Allowlist after review: test collector configuration/project reference if needed, Directory.Packages.props if needed, one verification script/document. Domain source and tests' assertions are excluded.

Acceptance: deterministic reproduction command, report location, line numerator/denominator and percentage; generated code treatment documented; financial code not excluded; report retained as an artifact, not fabricated in Markdown. This ticket measures the result; fixing coverage is separate.

## Task Q03 — measure mutations

Goal: run existing Stryker configuration and classify surviving mutants. Read docs/verification/README.md and stryker-config.json. Restore the pinned tool, then invoke it from tests/Promissio.Domain.Tests as documented. Do not change thresholds, mutate filters or assertions.

Allowed output: dated report under docs/verification. Record source commit, tool version, command, scope, score, killed/survived/timed-out counts as reported, and artifact location. If execution fails or cannot finish, record the failure and missing score. Never infer a score from a partial run.

Acceptance: every suggested test addresses a specific survivor and independent expected behavior. Equivalent-mutant classifications require review. Full execution can be long; keep logs and report progress without repeatedly restarting it.

## Future example Q07 — one state transition (blocked until contract supplied)

The architect must provide: accepted ADR; source/target state; triggering command; guard conditions; effective date and recorded time semantics; event fields/version; exact invalid-transition exception contract; deterministic input examples; approved diagram; and allowed files.

Implement only that transition and its tests. Verify valid transition emits exactly the approved event, invalid transition changes no state and emits no event, boundary cases match the guard table, and replay reproduces the state. Do not add adjacent transitions or infer payment accounting. If any required field is absent, return the missing field and stop dependent work.

## Review checklist for the architect

Check the diff against the packet, not just the model's report. Independently verify financial expectations and state diagrams. Inspect negative tests for meaningful assertions. Run the commands, confirm no hidden excludes or skipped tests, and distinguish implementation verification from domain/security review. A second answer from the same IQ3_M model is not independent assurance.
