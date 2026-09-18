# Benchmark suites

Both projects are built by Promissio.slnx. Their overlapping fixtures are retained for continuity; their measurements are not interchangeable.

| Suite | Scope |
|---|---|
| Promissio.Domain.Benchmarks | Separate day-count and complete interest-calculation cases, including monthly segments |
| Promissio.Benchmarks | Earlier mixed day-count/interest cases and Money operations |

From the repository root:

```powershell
dotnet run --project benchmarks/Promissio.Domain.Benchmarks -c Release -- --list flat
dotnet run --project benchmarks/Promissio.Benchmarks -c Release -- --list flat
dotnet run --project benchmarks/Promissio.Domain.Benchmarks -c Release -- --filter '*'
```

BenchmarkSwitcher forwards command-line filters and discovery options. A build or list command verifies harness availability only. Full measurements are opt-in and write ignored BenchmarkDotNet.Artifacts output; copy reviewed reports to results with the commit, SDK, hardware and command recorded before using them as performance evidence.

The earlier mixed suite combines operations of different kinds. Do not interpret its relative ranking as equivalent workloads. Consolidation into a single suite can follow a deliberate comparison of fixtures; no benchmark cases were deleted during structure alignment.
