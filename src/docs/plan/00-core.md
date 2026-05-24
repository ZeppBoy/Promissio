# Promissio — Core (всегда подгружать)

> **Источник:** разделы 1–6, 8, 9 и Appendix B из `developers_plan.md`.
> **Правило:** этот файл — инвариантный контекст. Подгружается в каждую сессию агента.
> **Размер:** ≈3.5k токенов.

---

## 1. Project Mission

**Promissio** is an open-source loan servicing platform for .NET, with built-in AI augmentation for credit operations.

The project demonstrates how modern .NET 9 / 10 systems can implement the full lifecycle of consumer and commercial lending — from application origination through servicing, daily batch processing, delinquency management, and AI-assisted operations — while remaining transparent, testable, and aligned with regulatory expectations (Basel III provisioning logic, IFRS 9 staging, EU Consumer Credit Directive APRC calculation, GDPR-aware data handling).

This is not a toy. The goal is production-grade engineering throughout. Even where shortcuts are taken (mock external dependencies, simplified KYC, illustrative scoring), they are explicit and documented.

### Why this project exists

Most open-source lending repositories on GitHub stop at "calculate annuity payment for a loan." Real-world loan servicing is dramatically more complex:

- Multiple interest rate types (fixed, floating with reference rate, tiered, grace, penalty).
- Multiple day-count conventions (Actual/360, Actual/365, 30/360, Actual/Actual).
- Multiple amortization profiles (annuity, differentiated, bullet, custom).
- End-of-day batch operations that must be idempotent, auditable, and resumable.
- State transitions tied to days past due, with IFRS 9 staging.
- Effective annual percentage rate calculation per regulatory formulas.
- Event-sourced audit trails for regulator-facing scenarios.
- AI agents operating under compliance constraints (no improper communication patterns, no third-party data leakage, full audit trail).

Promissio addresses all of these.

---

## 2. Goals and Non-Goals

### Goals

- **Authentic domain modeling.** Every concept that exists in real loan servicing — accrual periods, day-count conventions, schedule strategies, IFRS 9 stages — must exist in the code with the correct semantics.
- **Production-grade engineering.** Event sourcing where it earns its keep, proper concurrency control, idempotent operations, observability from day one, comprehensive testing including property-based and mutation testing.
- **Modern .NET.** Native AOT-friendly where reasonable, source generators where they help, minimal APIs, .NET 9 / 10 idioms throughout.
- **AI as a first-class layer, not a bolt-on.** MCP server for tool exposure, agents using Semantic Kernel with proper guardrails, evaluation suites running in CI, full LLM observability.
- **One-command startup.** `docker compose up` should produce a working system in under five minutes.
- **Documentation as a deliverable.** ADRs for every meaningful decision, mathematical formulas explained in `/docs/domain/`, architectural diagrams in C4 format.
- **Reproducible benchmarks.** BenchmarkDotNet results checked into the repository, regression detection in CI.

### Non-Goals

- **Not a complete core banking system.** No general ledger, no GL postings, no multi-product platform. Focused exclusively on loans.
- **Not a real KYC / AML system.** Customer model is intentionally minimal. KYC integration is mocked.
- **Not a credit bureau integration.** Scoring is illustrative, not connected to real bureaus.
- **Not a multi-tenant SaaS.** Single-tenant deployment model.
- **Not a UI project.** Frontend may exist as a developer console but is not the focus.
- **No real money movement.** No SWIFT, no SEPA, no card processing.

---

## 3. Target Audience

1. **Engineering managers and senior engineers at fintechs** evaluating the project owner's portfolio. The code should answer "can this person operate at Staff Engineer level in our context?" with a clear yes.
2. **Banking domain practitioners** who can recognize correct day-count conventions, proper APRC implementation, sensible IFRS 9 staging logic, and authentic state transitions.
3. **AI engineers** building agentic systems in regulated industries who can learn from the patterns demonstrated here: MCP server design, evaluation suites, guardrails, observability.
4. **The .NET community** interested in seeing modern .NET 9 / 10 used for real domain problems beyond the typical e-commerce demo.

---

## 4. Technology Stack

### Core platform

- **.NET 9** (current), with planned migration to .NET 10 upon GA release in November 2026.
- **C# 13** language features where they improve clarity.
- **ASP.NET Core minimal APIs** for HTTP surface.
- **EF Core 9** with PostgreSQL provider.
- **Marten** for event sourcing on PostgreSQL.
- **NodaTime** for all date arithmetic — `System.DateTime` is banned in domain code.
- **MediatR** for command and query handling.
- **FluentValidation** for input validation.
- **Mapster** for object mapping (preferred over AutoMapper for performance).

### Infrastructure

- **PostgreSQL 16** as primary database.
- **Qdrant** or **pgvector** for vector storage (regulatory documents, customer communications).
- **Hangfire** or custom hosted service for scheduled batch operations.
- **Redis** for caching where justified by load.
- **.NET Aspire** for local development orchestration.

### AI layer

- **Microsoft.Extensions.AI** as the unified abstraction.
- **Anthropic Claude API** as the primary model provider (Sonnet 4.7 for quality, Haiku 4.5 for cost-optimized operations).
- **OpenAI GPT-5** as a fallback provider, with the abstraction allowing seamless switching.
- **Semantic Kernel** for agent orchestration.
- **ModelContextProtocol C# SDK** for the MCP server.
- **Langfuse** (self-hosted in Docker Compose) for LLM observability.

### Testing

- **xUnit** as the primary test framework.
- **FluentAssertions** for readable assertions.
- **Testcontainers.NET** for integration tests against real PostgreSQL.
- **NBomber** for load testing.
- **Stryker.NET** for mutation testing of critical financial logic.
- **CsCheck** or **FsCheck** for property-based testing of value objects.
- **Bogus** for test data generation.
- **Verify.Xunit** for snapshot testing of generated schedules.

### Developer experience

- **JetBrains Rider** as the primary IDE on macOS.
- **Claude Code** and **Cursor** for AI-assisted development.
- **Scalar** for OpenAPI documentation (modern alternative to Swagger UI).
- **GitHub Actions** for CI/CD.
- **Renovate** for dependency updates.
- **Conventional Commits** with commitlint enforcement.

---

## 5. High-Level Architecture

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
|  |     Infrastructure                                |            |
|  |     PostgreSQL + Marten event store               |            |
|  +---------------------------------------------------+            |
|                          |                                        |
|                          v                                        |
|  +---------------------------------------------------+            |
|  |   Daily Batch Processor (hosted service)          |            |
|  |   Interest accrual, status transitions,           |            |
|  |   IFRS 9 staging, provisioning calculations       |            |
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

### Bounded contexts

- **Origination** — application intake, underwriting workflow, decision recording.
- **Servicing** — active loan lifecycle, payment processing, schedule management.
- **Risk** — IFRS 9 staging, provisioning, early warning.
- **AI Operations** — agents and MCP server.

Each context has clear inputs, outputs, and ownership of its aggregates.

---

## 6. Domain Model Overview

### Core aggregates

- **`LoanApplication`** — captures intake through decision. Owns its own state machine. Transitions: `Pending` → `UnderReview` → `Approved` / `Rejected`.
- **`Loan`** — the active contract after disbursement. State machine: `Disbursed` → `Active` → `InGrace` / `PastDue` → `Defaulted` → `WrittenOff` / `Restructured` / `Recovered` / `ClosedNormally`.
- **`PaymentSchedule`** — planned cash flows over the loan's life.
- **`InterestAccrual`** — daily accrual records, immutable.
- **`Payment`** — actual incoming payment, allocated against schedule items.

### Key value objects

- **`Money`** — amount with currency, no `decimal` exposure in domain APIs.
- **`Percentage`** — basis points, fractions, percent — explicit conversions.
- **`InterestRate`** (abstract) with concrete types:
  - `FixedRate`
  - `FloatingRate(referenceRate, margin, resetSchedule)`
  - `TieredRate(tiers)`
  - `EffectiveRate` — APRC calculator output.
- **`DayCountConvention`** — interface with implementations for Actual/360, Actual/365, Actual/Actual, 30/360, 30E/360.
- **`LoanTerm`** — duration using NodaTime types.

### Critical services

- **`IInterestCalculator`** — computes interest for a period given principal, rate, day-count convention, and dates.
- **`IScheduleGenerator`** — strategy-based: `AnnuityScheduleGenerator`, `DifferentiatedScheduleGenerator`, `BulletScheduleGenerator`, `CustomScheduleGenerator`.
- **`IAprcCalculator`** — iterative solver for EU Consumer Credit Directive effective rate.
- **`IIfrs9StagingService`** — determines stage based on payment behavior and credit deterioration signals.

### Schedule types supported

- **Annuity** — equal periodic payments.
- **Differentiated** — equal principal portions, declining total payments.
- **Bullet** — interest-only with balloon principal at maturity.
- **Custom** — pre-defined irregular schedule.

### Interest rate types supported

- **Fixed** — single rate for the life of the loan.
- **Floating** — reference rate plus margin, with defined reset cadence.
- **Tiered** — different rates by balance band or period.
- **Grace** — special rate (often zero) for the grace period.
- **Penalty** — elevated rate triggered by delinquency.

### Day-count conventions

The codebase implements all five major conventions with reference test cases sourced from ISDA documentation and ECB illustrative examples. Each convention is documented in `/docs/domain/day-count-conventions.md` with mathematical formulas.

---

## 8. Quality Standards

### Code quality

- All public APIs are documented with XML comments.
- Public types in `Promissio.Domain` are sealed by default; inheritance is opt-in and justified.
- No `null` in public APIs — use `Option<T>` patterns or explicit nullable annotations.
- No `decimal` in domain interfaces — always wrapped in value objects.
- No `System.DateTime` in domain code — use NodaTime types.
- Async methods follow established conventions: take `CancellationToken`, use `ConfigureAwait(false)` in library code.

### Testing

- Domain core: at least 90% line coverage, 80% mutation score.
- Application layer: at least 80% line coverage.
- Integration tests cover all happy paths and major failure modes.
- AI evaluations run on every PR touching AI code.

### Performance

- BenchmarkDotNet results checked in and tracked across commits.
- Performance regressions fail CI if they exceed 10% on critical paths.

### Security

- No secrets in source control.
- All external inputs validated at the API boundary.
- Authorization enforced for every MCP tool invocation.
- PII never logged in plain text.

---

## 9. Working with AI Coding Agents

### Philosophy

AI agents are pair programmers, not autonomous developers. The human owns architecture, domain modeling, security decisions, and final review. AI assists with implementation, scaffolding, refactoring, documentation drafts, and test generation.

### What to delegate to AI agents

- HTTP endpoint scaffolding once contracts are defined.
- DTO classes, validators, mappers.
- Test scaffolding (the human verifies the test cases are meaningful).
- Integration test setup with Testcontainers.
- Docker Compose and CI configurations.
- Refactoring sessions with clear before/after specifications.
- Documentation drafts.
- Boilerplate for new domain events, commands, handlers once patterns are established.

### What NOT to delegate

- Architecture decisions and file structure.
- Domain modeling (value objects, aggregates, state machine design).
- Day-count convention implementations — manual verification against ISDA reference data is mandatory.
- APRC calculator — financial math errors are subtle and easy to miss.
- Event sourcing design — events shape the entire audit story.
- MCP tool security boundaries.
- Evaluation golden datasets.
- Architectural Decision Records.

### Operating rules for AI agents

The repository contains `AGENTS.md` and `CLAUDE.md` with detailed operating instructions. Key rules:

1. Always read the relevant ADR before modifying a subsystem.
2. Never modify generated files (EF migrations, OpenAPI spec, Marten projections) directly — regenerate.
3. Always run `dotnet format` before committing.
4. Always run tests before committing.
5. Commit messages follow Conventional Commits.
6. Each PR has a clear, single responsibility.
7. When in doubt about banking domain semantics, escalate to the human — do not guess.

### Verification practices

- Financial math: manually verify three to five reference cases per implementation.
- State machine logic: verify the transition table matches the documented diagram.
- Security-sensitive code: human review mandatory, no exceptions.
- Generated documentation: human edit pass mandatory.

---

## Appendix B — Glossary

- **APRC** — Annual Percentage Rate of Charge. The standardized effective rate disclosed to consumers in the EU.
- **Day-count convention** — Method for converting calendar time into the fraction of a year used in interest calculations.
- **IFRS 9 staging** — Classification of financial assets into Stage 1 (performing), Stage 2 (significant credit deterioration), and Stage 3 (credit-impaired) for impairment accounting.
- **MCP** — Model Context Protocol. An open standard for AI assistants to interact with external systems through tools.
- **RAG** — Retrieval-Augmented Generation. Pattern where LLM responses are grounded in retrieved documents.
- **LLM-as-judge** — Using a large language model to evaluate outputs of another model.
