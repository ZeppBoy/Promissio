# Promissio

**An open-source loan servicing platform for .NET, with built-in AI augmentation for credit operations.**

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Status](https://img.shields.io/badge/status-early%20development-orange)](#status)
[![Build](https://img.shields.io/badge/build-pending-lightgrey)]()
[![Code style](https://img.shields.io/badge/code%20style-dotnet%20format-blue)]()

---

## What is Promissio?

Promissio implements the full lifecycle of consumer and commercial lending on a modern .NET stack: from application origination through servicing, daily batch processing, delinquency management, and AI-assisted operations.

It is built to demonstrate what production-grade loan servicing looks like in 2026 — transparent, testable, regulator-aware, and AI-native — beyond the typical "calculate annuity payment" tutorial.

### Why this project exists

Most open-source lending repositories on GitHub stop at simple annuity arithmetic. Real-world loan servicing involves:

- Multiple interest rate types (fixed, floating with reference rate, tiered, grace, penalty).
- Multiple day-count conventions (Actual/360, Actual/365, 30/360, Actual/Actual).
- Multiple amortization profiles (annuity, differentiated, bullet, custom).
- End-of-day batch operations that must be idempotent, auditable, and resumable.
- State transitions tied to days past due, with IFRS 9 staging.
- Effective rate (APRC) calculation per the EU Consumer Credit Directive.
- Event-sourced audit trails for regulator-facing scenarios.
- AI agents operating under compliance constraints (no improper communications, no third-party data leakage, full audit trail).

Promissio addresses all of these.

### Who this is for

- **Engineering managers and senior engineers** evaluating modern .NET architecture in a regulated domain.
- **Banking domain practitioners** who recognize correct day-count conventions, sensible IFRS 9 staging, and authentic state transitions.
- **AI engineers** building agentic systems in regulated industries.
- **The .NET community** interested in real domain depth beyond the typical e-commerce demo.

---

## Status

**This project is in early development. Phase 0 (foundation) is in progress.**

Promissio is being built in public over approximately 22 weeks, in deliberate phases. See [`developers_plan.md`](./developers_plan.md) for the complete roadmap.

| Phase | Focus | Status |
|---|---|---|
| 0 | Foundation, project skeleton, tooling | 🚧 In progress |
| 1 | Domain core: interest engine, day-count conventions | ⏳ Planned |
| 2 | Payment schedule generators, APRC calculator | ⏳ Planned |
| 3 | Loan aggregate, state machine, event sourcing | ⏳ Planned |
| 4 | Application services, HTTP APIs | ⏳ Planned |
| 5 | Daily batch processor, IFRS 9 staging | ⏳ Planned |
| 6 | MCP server, banker tooling | ⏳ Planned |
| 7 | AI agents (credit decisioning, early warning, collections) | ⏳ Planned |
| 8 | Evaluation framework, observability | ⏳ Planned |
| 9 | Performance, reliability, polish | ⏳ Planned |
| 10 | Public launch | ⏳ Planned |

Progress is shared regularly on [LinkedIn](https://www.linkedin.com/in/zeppboy/) and through commit history.

---

## Architecture at a glance

```
+-------------------------------------------------------------------+
|                       Promissio Platform                          |
+-------------------------------------------------------------------+
|  +-------------------+        +--------------------+              |
|  |  Origination API  |        |   Servicing API    |              |
|  |  (minimal APIs)   |        |   (minimal APIs)   |              |
|  +---------+---------+        +----------+---------+              |
|            |                             |                        |
|            v                             v                        |
|  +---------------------------------------------------+            |
|  |              Application Layer                    |            |
|  |  (MediatR handlers, validation, mapping)          |            |
|  +---------------------------------------------------+            |
|                          |                                        |
|                          v                                        |
|  +---------------------------------------------------+            |
|  |              Domain Core                          |            |
|  |  Aggregates: Loan, Application, Schedule, Payment |            |
|  |  ValueObjects: Money, InterestRate, LoanTerm      |            |
|  |  Services: InterestCalculator, ScheduleGenerator  |            |
|  +---------------------------------------------------+            |
|                          |                                        |
|                          v                                        |
|  +---------------------------------------------------+            |
|  |     PostgreSQL + Marten (event sourcing)          |            |
|  +---------------------------------------------------+            |
|                          |                                        |
|                          v                                        |
|  +---------------------------------------------------+            |
|  |   Daily Batch Processor (hosted service)          |            |
|  |   Interest accrual, IFRS 9 staging, provisioning  |            |
|  +---------------------------------------------------+            |
+-------------------------------------------------------------------+
|                       AI Operations Layer                         |
|  +-------------------+        +--------------------+              |
|  |   MCP Server      |        |  Agent Runtime     |              |
|  |   (loan tools)    |        |  (SK + Claude)     |              |
|  +-------------------+        +--------------------+              |
|                                                                   |
|   - Credit decisioning copilot                                    |
|   - Document analysis (income proof, identity)                    |
|   - Early warning signal generator                                |
|   - Collections conversation agent                                |
+-------------------------------------------------------------------+
|                       Observability                               |
|   - OpenTelemetry traces and metrics                              |
|   - Langfuse for LLM observability                                |
|   - Evaluation suites integrated into CI                          |
+-------------------------------------------------------------------+
```

For detailed C4 diagrams, see [`docs/architecture/`](./docs/architecture/) (populated in Phase 9).

---

## Key features (planned)

### Domain depth

- **Five day-count conventions** with reference test cases sourced from ISDA documentation.
- **Four schedule generation strategies**: annuity, differentiated, bullet, and custom.
- **Five interest rate types**: fixed, floating with reference rate, tiered, grace, penalty.
- **APRC calculator** implementing the EU Consumer Credit Directive iterative solver.
- **IFRS 9 staging** with configurable triggers for Stage 1, 2, and 3 transitions.
- **Loan state machine** with explicit transitions, event sourcing, and time-travel queries.

### Engineering

- Event sourcing via Marten on PostgreSQL.
- Idempotent daily batch processing with full audit trail.
- Property-based testing for value objects (CsCheck).
- Mutation testing for financial logic (Stryker.NET).
- Reference values for every calculation against authoritative sources.
- BenchmarkDotNet results checked in and tracked.

### AI augmentation

- **MCP server** exposing loan operations as tools for any compatible AI client (Claude Desktop, Cursor, etc.).
- **Credit decisioning copilot** with RAG over internal credit policies.
- **Early warning agent** combining hard data and soft signals.
- **Collections conversation agent** with compliance constraints baked in.
- **Evaluation framework** running on every PR that touches AI code.
- **Full LLM observability** via Langfuse: cost, latency, quality scores, prompt versions.

---

## Technology stack

### Core

| Component | Choice |
|---|---|
| Runtime | .NET 9 (planned migration to .NET 10) |
| Web framework | ASP.NET Core minimal APIs |
| ORM | EF Core 9 |
| Event sourcing | Marten |
| Date/time | NodaTime (no `System.DateTime` in domain) |
| Mediation | MediatR |
| Validation | FluentValidation |
| Mapping | Mapster |

### Infrastructure

| Component | Choice |
|---|---|
| Database | PostgreSQL 16 |
| Vector store | pgvector / Qdrant |
| Local orchestration | .NET Aspire |
| Background jobs | Hangfire or hosted services |
| Caching | Redis (where justified) |

### AI layer

| Component | Choice |
|---|---|
| Abstraction | Microsoft.Extensions.AI |
| Primary model | Anthropic Claude Sonnet 4.7 / Haiku 4.5 |
| Fallback model | OpenAI GPT-5 |
| Agent framework | Semantic Kernel |
| MCP | ModelContextProtocol C# SDK |
| LLM observability | Langfuse (self-hosted) |

### Testing

| Component | Choice |
|---|---|
| Framework | xUnit + FluentAssertions |
| Integration | Testcontainers.NET |
| Property-based | CsCheck |
| Mutation testing | Stryker.NET |
| Load testing | NBomber |
| Snapshot | Verify.Xunit |

---

## Getting started

> **Note:** Getting-started instructions will become functional as Phase 0 completes. Right now this section describes the intended developer experience.

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download) (or .NET 10 preview)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) or compatible container runtime
- [JetBrains Rider](https://www.jetbrains.com/rider/) recommended; VS Code or Visual Studio 2022+ also work

### Clone and run

```bash
git clone https://github.com/ZeppBoy/Promissio.git
cd Promissio
docker compose up -d
dotnet build
dotnet test
```

That should bring up:

- PostgreSQL 16 on port 5432
- Qdrant on port 6333
- (Langfuse — see docs/mcp/ for self-hosting setup)
- Jaeger UI on port 16686

Then start one of the services:

```bash
dotnet run --project src/Promissio.Api.Origination
dotnet run --project src/Promissio.Api.Servicing
dotnet run --project src/Promissio.BatchProcessor
```

### API documentation

Once running, OpenAPI documentation is available at:

- Origination API: `http://localhost:5001/scalar`
- Servicing API: `http://localhost:5002/scalar`

### Connecting Claude Desktop to the MCP server

Once the MCP server is implemented (Phase 6), see [`docs/mcp/setup.md`](./docs/mcp/setup.md) for instructions on connecting Claude Desktop to the Promissio MCP server.

---

## Project structure

```
.
├── src/
│   ├── Promissio.Domain/              # Pure domain, NodaTime only
│   ├── Promissio.Application/         # MediatR handlers, application services
│   ├── Promissio.Infrastructure/      # EF Core, Marten, external integrations
│   ├── Promissio.Api.Origination/     # Origination HTTP API
│   ├── Promissio.Api.Servicing/       # Servicing HTTP API
│   ├── Promissio.BatchProcessor/      # Daily batch worker
│   ├── Promissio.AI/                  # AI orchestration, agents
│   └── Promissio.AI.McpServer/        # Standalone MCP server
├── tests/
│   ├── Promissio.Domain.Tests/
│   ├── Promissio.Application.Tests/
│   ├── Promissio.Integration.Tests/
│   └── Promissio.AI.Evals/
├── docs/
│   ├── adr/                           # Architecture Decision Records
│   ├── domain/                        # Banking concepts, formulas
│   ├── architecture/                  # C4 diagrams
│   └── mcp/                           # MCP server documentation
├── benchmarks/                        # BenchmarkDotNet results
├── seed-data/                         # Demo portfolio data
├── docker-compose.yml
├── README.md                          # This file
├── developers_plan.md                 # Full roadmap
├── FRONTEND_PLAN.md                   # UI strategy
├── AGENTS.md                          # AI agent operating manual
└── CLAUDE.md                          # Claude Code specifics
```

---

## Documentation

| Document | Purpose |
|---|---|
| [`developers_plan.md`](./developers_plan.md) | Complete roadmap, all phases, acceptance criteria |
| [`FRONTEND_PLAN.md`](./FRONTEND_PLAN.md) | Frontend strategy: Claude Desktop + AI Workspace |
| [`AGENTS.md`](./AGENTS.md) | Operating manual for any AI coding agent |
| [`CLAUDE.md`](./CLAUDE.md) | Claude Code specific guidance |
| `docs/adr/` | Architecture Decision Records |
| `docs/domain/` | Day-count conventions, IFRS 9 staging, APRC formula |
| `docs/architecture/` | C4 diagrams, system overview |
| `docs/mcp/` | MCP server setup, tools reference, usage patterns |

---

## Design principles

These principles guide every decision in the codebase:

1. **Correctness over speed in financial logic.** A slow calculation is fixable; a wrong one may be irrecoverable.
2. **Domain authenticity.** Every concept that exists in real loan servicing must exist in the code with the right semantics.
3. **Make decisions visible.** Important choices live in ADRs, not memory.
4. **Tests are not optional.** Domain logic has unit tests. Workflows have integration tests. AI has evaluation suites.
5. **Transparency over cleverness.** Code is read more than written. Optimize for the reader.
6. **Modern .NET.** Use .NET 9 features when they improve clarity. Avoid retro patterns.
7. **AI as a first-class layer.** Not a chat bubble bolted on. MCP, agents, evaluations, observability designed from day one.

---

## Non-goals

To keep focus, Promissio explicitly does not aim to be:

- A complete core banking system (no general ledger, no GL postings).
- A real KYC / AML platform (customer model is intentionally minimal).
- A credit bureau integration (scoring is illustrative).
- A multi-tenant SaaS.
- A money movement system (no SWIFT, SEPA, or card processing).
- A customer-facing portal (internal-facing only).

See [`developers_plan.md`](./developers_plan.md) Section 2 for the complete list.

---

## Contributing

This project is currently a solo build-in-public effort. External contributions are not actively solicited at this stage.

That said:

- Bug reports and architectural feedback are welcome via GitHub Issues.
- If you spot a mathematical error in a financial calculation — please open an issue immediately with a reference to the correct value.
- Once the project reaches Phase 9 (Production Polish), a proper `CONTRIBUTING.md` will be added.

For coding standards and conventions, see [`AGENTS.md`](./AGENTS.md).

---

## License

[MIT License](./LICENSE).

You can use this code freely. There is no warranty. If you use Promissio in production to service real loans, you accept full responsibility for the consequences. The author is not a regulated entity and this code is not a substitute for legal, compliance, or financial advice.

---

## Acknowledgments

This project draws on:

- **ISDA** for day-count convention documentation.
- **The European Banking Authority and ESMA** for regulatory guidance.
- **The IFRS Foundation** for IFRS 9 standards.
- **Microsoft .NET team**, particularly David Fowler and Stephen Toub, for the engineering practices that inform modern .NET design.
- **Anthropic** for Claude and the Model Context Protocol.
- **The Latent Space community** for AI engineering practices and inspiration.

---

## Build in public

Progress updates appear on:

- [LinkedIn](https://www.linkedin.com/in/zeppboy/) — weekly summaries
- [GitHub](https://github.com/ZeppBoy/Promissio/commits/main) — daily commits
- Personal blog (link TBD) — monthly long-form posts

If you find this project interesting, follow along. If you spot something wrong, open an issue. If you want to discuss banking domain modeling or AI engineering in regulated industries, reach out on LinkedIn.

---

*Promissio (from Latin: a promise) — the foundational concept of a loan.*
