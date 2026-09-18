# Phase 3 PostgreSQL and activation verification — 2026-09-18

Scope: execute the deferred T2 PostgreSQL scenarios, correct the persistence defect they exposed, and implement the first persisted lifecycle workflow (`Disbursed` to `Active`).

Baseline: `main` at `34c7804`, with the changes described here in the working tree.

## Environment

Docker Desktop 4.91.0 was installed through WinGet and started with its Linux/WSL2 engine. Tests use the pinned `postgres:16-alpine` Testcontainers image. Windows reports a pending restart, but the Docker daemon and test environment operate successfully without it.

## Persistence correction

The first real PostgreSQL run reached the database but all four T2 scenarios failed during replay. PostgreSQL `jsonb` reordered the nested `$rateType` and `$dayCountType` metadata, while Marten's System.Text.Json configuration did not permit out-of-order metadata. Marten is now configured with `AllowOutOfOrderMetadataProperties`; an Infrastructure regression test deliberately moves metadata fields to the end before deserialization.

## Persisted activation workflow

`ActivateLoanCommandHandler` loads the authoritative stream, applies the already accepted activation transition, and appends `LoanActivated`. The repository uses Marten's versioned `Append` overload and returns a typed concurrency-conflict outcome when the loaded stream version is stale. Missing streams return a typed not-found result; prohibited Domain transitions continue to throw `InvalidStateTransitionException` as defined by the accepted state machine.

## Results

| Check | Result |
|---|---|
| Original PostgreSQL creation/replay scenarios | 4 passed |
| Persisted activation and stale-writer scenarios | 2 passed |
| Application activation tests | 4 passed |
| Application line coverage | 81.48% (66/81 executable source lines), above the 80% gate |
| Infrastructure tests | 4 passed, including reordered polymorphic metadata |
| Full solution build | Passed with zero warnings and errors |
| Full solution tests | 546 passed: 528 Domain, 6 Application, 4 Infrastructure, 6 Integration and 2 Batch Processor; AI evaluation assembly currently has no tests |

## Limits

This slice does not implement aging, payment allocation, terminal transitions, projections, historical queries, HTTP exposure, or authorization. ADR-0008 remains proposed and requires explicit security review before operations are exposed over HTTP or MCP.
