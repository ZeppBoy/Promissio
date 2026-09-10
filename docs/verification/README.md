# Verification

Run from the repository root with PowerShell 7 and a .NET 9 SDK supporting Promissio.slnx:

```powershell
pwsh ./tools/verify.ps1
```

Use `-NoRestore` only after a successful restore for the same checkout. Local development and CI use the same entry point. The script disables desktop diff launching while preserving snapshot assertions, then restores the previous environment setting. It stops on a failed command and checks:

1. Dependency restore and solution build, including both benchmark projects.
2. Available tests, with TRX results under TestResults.
3. Formatting for Phase 2, the batch host/tests and benchmark harnesses. Legacy formatting elsewhere is not yet enforced by CI.
4. Local Markdown file links in maintained documentation and forwarding pages. Historical bodies, external URLs and anchors are excluded.
5. Benchmark case discovery, without running performance measurements.

These are baseline checks. Empty test projects do not establish coverage. Real database integration, endpoint failure modes, batch idempotency, AI evaluations, coverage targets and mutation scores are not yet enforced by this script.

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

- [2026-09-10 structure alignment](2026-09-10-structure-alignment.md)
- [Historical audits](../audits/README.md)

Evidence must state date, scope, commands, results and limitations. Human review remains separate from automated verification.
