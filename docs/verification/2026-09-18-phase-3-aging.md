# Phase 3 persisted aging verification — 2026-09-18

Scope: implement the persisted `ApplyAging` workflow over the accepted loan state machine, verify it against PostgreSQL, and retain optimistic stream concurrency.

Baseline: `main` at `57a93bd`, with the changes described here in the working tree.

## Implemented workflow

`ApplyLoanAgingCommandHandler` loads the authoritative stream and delegates all transition decisions to `Loan.ApplyAging`. It appends the resulting event at the loaded stream version, reports missing streams and stale writers as typed outcomes, and does not call persistence for the documented Active-to-Active zero-days no-op. A concurrency conflict omits the attempted state because it is not authoritative.

The persisted scenarios cover Active to InGrace, InGrace to PastDue, PastDue to Active cure, Active to Defaulted, Active with zero days, and competing aging writers. Application tests also cover missing streams and invalid aging from Disbursed without saving.

## Dependency security

A fresh restore reported critical SQL-injection advisory [GHSA-rfx3-98h7-v3xp](https://github.com/advisories/GHSA-rfx3-98h7-v3xp) against Marten 9.0.0. `Marten` and `Marten.NodaTime` were upgraded together to the patched 9.13.0 release. A transitive audit then found two high-severity SSH.NET advisories through Testcontainers 4.1.0; upgrading both Testcontainers packages to 4.15.0 resolves SSH.NET 2026.0.0. The full build and PostgreSQL suite verify compatibility; no warning is suppressed.

## Results

| Check | Result |
|---|---|
| Application aging tests | 8 aging cases passed; 14 Application tests total |
| Application line coverage | 86.86% (119/137 executable source lines), above the 80% gate |
| PostgreSQL Testcontainers scenarios | 9 passed, including three persisted aging scenarios |
| Full solution build | Passed with zero warnings and errors |
| Full solution tests | 557 passed: 528 Domain, 14 Application, 4 Infrastructure, 9 Integration and 2 Batch Processor; AI evaluation assembly currently has no tests |
| Formatting | Whole solution passed `dotnet format --verify-no-changes` |
| Dependency audit | No known vulnerable direct or transitive packages reported by NuGet |

## Limits

This slice does not add terminal-transition handlers, projections, historical queries, HTTP exposure, payment allocation, or authorization. The aging thresholds and state transitions are the already accepted Domain contract; this slice does not change their semantics. Projection lifecycle and event-schema evolution decisions remain prerequisites for the next projection ticket.
