# Loan summary verification — 2026-10-20

Baseline: `main` at `10fd369`, plus the working-tree changes described here. The owner approved this bounded projection slice and the proposed consistency, rebuild and event compatibility policies on 2026-10-20.

## Implemented scope

Application owns `LoanSummary`, `ILoanSummaryReader` and the MediatR query by loan ID. Infrastructure registers a single-stream inline projection, document reader and offline maintenance rebuilder. The view records identity, terms version, original principal, disbursement/first-payment dates, lifecycle state, last recorded days past due and stream version. It follows append order, including backdated events. It does not calculate balances or historical state. Domain formulas, transitions and existing event payloads are unchanged.

[ADR-0009](../adr/0009-loan-summary-projection.md) documents the approved transaction, maintenance/backfill and compatibility policies. Generated documentation still needs a human edit before merge.

## Verification

| Command / check | Result |
|---|---|
| `dotnet build Promissio.slnx --no-restore --verbosity quiet` | Passed, zero warnings/errors |
| `dotnet test tests/Promissio.Application.Tests --no-build --no-restore --collect:"Code Coverage"` | 17 passed; Application line coverage 82.91% (131/158) |
| `dotnet test tests/Promissio.Integration.Tests --no-build --no-restore` | 18 PostgreSQL Testcontainers cases passed |
| `pwsh ./tools/verify.ps1 -NoRestore` | Passed: build, 569 tests (528 Domain, 17 Application, 4 Infrastructure, 18 Integration, 2 Batch), maintained formatting, documentation links and benchmark discovery; AI evaluation assembly has no tests |
| `dotnet format Promissio.slnx --no-restore --verify-no-changes --verbosity quiet` | Passed for the whole solution |
| `git diff --check` | Passed |

Coverage was collected with the SDK Code Coverage collector, converted to Cobertura with pinned `dotnet-coverage` 18.11.0, and calculated by unioning executable source-line numbers per Application file. Domain coverage/mutation values remain the historical Phase 2 evidence; this slice changes no Domain files and does not rerun mutation testing.

Docker initially had no reachable engine. A hung Desktop frontend prevented its normal restart; relaunching the task-started frontend outside the sandbox restored Docker Engine 29.8.0. Tests use disposable `postgres:16-alpine` containers. The first database run found missing `AddLogging` registration in the expanded test fixture's MediatR setup; that fixture setup was corrected, and the rerun passed all cases.

The new integration cases verify creation visibility through MediatR, identical retry behavior, missing summaries, activation/grace/past-due/re-aging/cure/default updates, the Active no-op, stale-writer rollback, all three terminal events, and reserved payment-event version tracking without changing original principal. The rebuild test writes legacy streams with projections disabled, backfills them through Marten's daemon, compares inline and rebuilt results including backdated append order, checks stream versions and the preserved handoff outcome, repeats the rebuild, then resumes inline writes.

## Limits

This is the first projection slice. State-filtered lists, portfolio summaries, business-effective historical queries, terminal Application handlers, payment allocation, authorization and HTTP/MCP exposure remain outside scope. Offline rebuilds require stopping readers and writers until successful completion; no production database was modified by these checks.
