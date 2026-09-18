# Verification

Run from the repository root with PowerShell 7, a .NET 9 SDK supporting Promissio.slnx, and a working Docker endpoint. Docker is required by the Phase 3 PostgreSQL Testcontainers suite:

```powershell
pwsh ./tools/verify.ps1
```

Use `-NoRestore` only after a successful restore for the same checkout. Local development and CI use the same entry point. The script disables desktop diff launching while preserving snapshot assertions, then restores the previous environment setting. It stops on a failed command and checks:

1. Dependency restore and solution build, including both benchmark projects.
2. Available tests, with TRX results under TestResults.
3. Formatting for Phase 2, the batch host/tests and benchmark harnesses. Legacy formatting elsewhere is not yet enforced by CI.
4. Local Markdown file links in maintained documentation and forwarding pages. Historical bodies, external URLs and anchors are excluded.
5. Benchmark case discovery, without running performance measurements.

These are baseline checks. Empty test projects do not establish coverage. Phase 3 now includes real PostgreSQL integration scenarios for creation, activation and aging; endpoint failure modes, batch idempotency and AI evaluations are not yet enforced by this script. Phase 2 coverage and mutation gates use the separate command below.

## Phase 2 assurance

Restore the pinned Microsoft coverage and Stryker tools, run the clean Domain suite with the built-in collector, report Phase 2 coverage, and enforce the project-wide Domain coverage and mutation gates:

```powershell
pwsh ./tools/verify-phase2.ps1
```

The script enforces at least 90% line coverage across `Promissio.Domain`, reports the Phase 2 subset separately, and retains the configured 80% Stryker break threshold for an unfiltered Domain mutation run. Use `-NoRestore` only after restoring both project dependencies and local tools for the same checkout. The coverage conversion follows the [Microsoft dotnet-coverage guidance](https://learn.microsoft.com/dotnet/core/additional-tools/dotnet-coverage); mutation execution follows the [Stryker.NET configuration](https://stryker-mutator.io/docs/stryker-net/configuration/).

## Optional mutation verification

From the repository root, restore the pinned local tool, then run from the domain test project:

```powershell
dotnet tool restore --tool-manifest ./dotnet-tools.json
Push-Location tests/Promissio.Domain.Tests
try {
    dotnet stryker --config-file ../../stryker-config.json
} finally {
    Pop-Location
}
```

The configuration selects Promissio.Domain.csproj by filename and fails a mutation run below 80%. A configured threshold is not a measured score. This follows the [Stryker configuration reference](https://stryker-mutator.io/docs/stryker-net/configuration/); mutation execution is currently separate from baseline CI.

Performance runs are described in the [benchmark guide](../../benchmarks/README.md). Record environment and commit with any results.

## Evidence

- [2026-09-18 Phase 3 persisted aging](2026-09-18-phase-3-aging.md)
- [2026-09-18 Phase 3 PostgreSQL and activation](2026-09-18-phase-3-activation.md)
- [2026-09-18 Phase 2 completion](2026-09-18-phase-2-completion.md)
- [2026-09-18 current implementation status](2026-09-18-current-status.md)
- [2026-09-16 Phase 3 T2 creation and replay](2026-09-16-phase-3-t2.md)
- [2026-09-10 structure alignment](2026-09-10-structure-alignment.md)
- [Historical audits](../audits/README.md)

Evidence must state date, scope, commands, results and limitations. Human review remains separate from automated verification.
