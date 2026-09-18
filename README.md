# Promissio

An open-source loan servicing platform for .NET, with planned AI support for credit operations.

## Current status

The domain library implements interest calculations, day-count conventions, payment schedules, dated APRC and the loan lifecycle aggregate. Phase 3 now has database-verified idempotent loan creation/replay plus persisted activation and aging workflows with optimistic concurrency. Aging covers grace, past-due, default, cure and the documented Active no-op. Phase 2 is complete: implementation and automated gates pass, and the owner signed off the financial contracts on 2026-09-18. This product acceptance is not legal or regulatory certification. HTTP operations, daily batch jobs, MCP tools and AI agents remain scaffolding or planned work.

See [delivery status](docs/status.md), [current architecture](docs/architecture/current-state.md) and the [development roadmap](docs/plan/README.md). Implemented scope and future scope are recorded separately.

## Get started

Use the .NET 9 SDK with solution XML support (9.0.200 or later), PowerShell 7 and a working Docker endpoint. Docker is required by the PostgreSQL integration tests. From the repository root:

```powershell
pwsh ./tools/verify.ps1
```

This restores and builds the solution, runs available tests, checks formatting in the maintained scope, validates local documentation links and discovers benchmark cases. See [verification](docs/verification/README.md) for limitations and optional checks.

The Testcontainers suite starts its own PostgreSQL container. To start the broader optional local dependencies manually:

```powershell
docker compose up -d
```

Compose starts PostgreSQL, Qdrant and Jaeger. It does not start the application hosts or Langfuse. The origination and servicing hosts have no business routes or Scalar UI yet. The batch executable starts a generic host with no jobs. MCP client setup will be documented when a transport and authorized tools exist; see [MCP status](docs/mcp/README.md).

## Structure

| Location | Responsibility |
|---|---|
| src/Promissio.Domain | Pure financial domain library, NodaTime only |
| src/Promissio.Application | Loan creation, activation and aging orchestration plus persistence ports |
| src/Promissio.Infrastructure | Marten loan event persistence and integration scaffolding |
| src/Promissio.Api.Origination and src/Promissio.Api.Servicing | HTTP host shells |
| src/Promissio.BatchProcessor | Generic host executable |
| src/Promissio.AI and src/Promissio.AI.McpServer | Reserved web host boundaries |
| tests/ | Six test projects; five currently contain tests |
| benchmarks/ | Two build-checked BenchmarkDotNet suites |
| docs/ | Canonical roadmap, architecture, decisions, domain contracts and evidence |

The locked stack and coding rules are in [AGENTS.md](AGENTS.md). Domain and Application remain independent of Infrastructure and transports. [Target architecture](docs/architecture/target-state.md) describes future composition without claiming completed integrations.

## Documentation and contributing

Start with the [documentation index](docs/README.md). [Architecture decisions](docs/adr/README.md) distinguish accepted decisions from proposals awaiting review. Earlier audits are preserved as [historical evidence](docs/audits/README.md).

Follow [CONTRIBUTING.md](CONTRIBUTING.md) and [AGENTS.md](AGENTS.md). Financial changes require authoritative reference cases. Generated documentation needs a human edit before merge.

Promissio is intended for engineering and banking-domain learning. It is not a complete core banking system, a money movement service or a multi-tenant SaaS. See [product scope](docs/plan/00-core.md) for goals and non-goals.

## License

[MIT](LICENSE). There is no warranty; using this code in production remains the operator's responsibility.
