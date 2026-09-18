# Phase 2 completion verification — 2026-09-18

Scope: all production files under `Promissio.Domain/ScheduleGeneration` plus `HolidayCalendar`, as defined by ADR-0004 and the Phase 2 plan.

Baseline: `main` at `c0096ac`, with the changes described here in the working tree.

## Correctness repair

Remote merge `075840d` had replaced the stabilized bullet and custom strategies with older implementations. The replacement made the solution fail compilation because both classes lacked the optional first-payment parameter. It also violated accepted contracts: bullet principal amortized before maturity and custom inputs were ignored. The two files were restored to the reviewed `2fe5ded` implementations without changing any approved financial formula.

## Results

| Check | Result |
|---|---|
| Solution build after strategy repair | Passed with zero warnings and errors |
| Domain tests | 528 passed, including 29 Phase 2 boundary/failure-contract cases and 22 whole-Domain contract cases |
| Phase 2 line coverage | 98.51% (330/335 executable source lines), above the 90% gate |
| Whole-Domain line coverage | 96.48% (1014/1051 executable source lines), above the 90% gate |
| Whole-Domain mutation score | 85.58%: 628 killed, 95 survived, 7 timed out and 12 had no coverage, above the 80% gate |
| Other executable non-Docker tests | 2 Application, 3 Infrastructure and 2 Batch Processor tests passed; 535 total with Domain |
| Full solution build | Passed with zero warnings and errors after the complete change set |
| Formatting and documentation links | Passed |

Line coverage was collected with the SDK `Code Coverage` data collector and converted to Cobertura with pinned `dotnet-coverage` 18.11.0. The calculation unions executable line numbers by source file so compiler-generated nested classes do not double-count lines.

Mutation testing used pinned Stryker.NET 4.14.2 with the existing thresholds and no source filters. Additional tests exercise existing result, serialization, dependency-validation, value-object boundary, and interest-rate equality contracts; no financial formula was changed to reach the threshold.

The repeatable `pwsh ./tools/verify-phase2.ps1 -NoRestore` run completed successfully with 96.48% whole-Domain coverage, 98.51% Phase 2 coverage and an 85.58% whole-Domain mutation score.

The initial direct `dotnet-coverage collect` attempt produced instrumentation-only `TypeLoadException` failures in Phase 3 Loan types and is excluded. Rebuilding restored clean binaries; the built-in collector then ran all 528 tests successfully before report conversion.

## Limits

Automated Phase 2 implementation gates, including the whole-Domain quality thresholds required by the project manual, are satisfied. Human review of the financial contracts, published reference interpretation and regulatory scope remains separate. The calculator still intentionally excludes legal-disclosure rounding, multiple drawdowns, open-ended-product assumptions and unknown future charges, as recorded in ADR-0004.
