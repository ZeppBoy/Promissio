# Phase 3 loan-state projection verification — 2026-10-19

Scope: inline Marten projection of each loan's current state, Application query, rebuild policy, and event compatibility decision in [ADR-0009](../adr/0009-loan-state-projection.md).

## Checks

| Check | Command | Result |
|---|---|---|
| Full solution tests | `dotnet test Promissio.slnx --no-restore -v minimal` | Passed: 566 tests across Domain (528), Application (16), Infrastructure (4), Integration (16), and Batch Processor (2). AI evaluation assembly has no tests. |
| Full solution build | `dotnet build Promissio.slnx --no-restore -v minimal` | Passed with zero warnings and zero errors. |
| Formatting | `dotnet format Promissio.slnx --no-restore --verify-no-changes` | Passed. |
| Documentation links | `pwsh ./tools/check-documentation.ps1` | Passed. |
| Diff whitespace | `git diff --check` | Passed. |

The Integration suite ran against a PostgreSQL 16 Testcontainer using Docker Desktop's 29.8.0 engine. The new scenarios verify immediate reads after creation and each supported state event, unchanged reads after a stale write, missing-loan results, and restoration of a deleted view by rebuilding the named `LoanState` projection.

## Limits

This read model is a single-loan current-state view. It does not implement active/overdue lists, portfolio totals, historical effective-date queries, terminal command handlers, HTTP endpoints, or authorization. Existing installations need a projection rebuild before this query can serve pre-existing streams. Documentation generated in this change requires human editing before merge under `AGENTS.md`.
