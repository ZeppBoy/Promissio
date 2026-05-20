# Promissio — Developers Plan

> **Status:** Draft v1.0 — Living document, updated as the project evolves.
> **Owner:** Project lead
> **Last updated:** 2026-05-17

---

## Table of Contents

1. [Project Mission](#1-project-mission)
2. [Goals and Non-Goals](#2-goals-and-non-goals)
3. [Target Audience](#3-target-audience)
4. [Technology Stack](#4-technology-stack)
5. [High-Level Architecture](#5-high-level-architecture)
6. [Domain Model Overview](#6-domain-model-overview)
7. [Development Roadmap](#7-development-roadmap)
8. [Quality Standards](#8-quality-standards)
9. [Working with AI Coding Agents](#9-working-with-ai-coding-agents)
10. [Public Launch Strategy](#10-public-launch-strategy)
11. [Long-Term Roadmap](#11-long-term-roadmap)

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

This codebase is built for the following readers:

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

## 7. Development Roadmap

The project is organized into ten phases over approximately 22 weeks of part-time development. Each phase has explicit goals, atomic tasks suitable for AI-agent delegation, and acceptance criteria.

### Phase 0 — Foundation (Week 1)

**Goal:** Establish project skeleton, conventions, and infrastructure baseline.

**Tasks:**

1. Create solution structure with the following projects:
   - `src/Promissio.Domain` — class library, no external dependencies except NodaTime.
   - `src/Promissio.Application` — application services, MediatR handlers.
   - `src/Promissio.Infrastructure` — EF Core, Marten, external integrations.
   - `src/Promissio.Api.Origination` — origination HTTP surface.
   - `src/Promissio.Api.Servicing` — servicing HTTP surface.
   - `src/Promissio.BatchProcessor` — daily batch worker service.
   - `src/Promissio.AI` — AI orchestration and agents.
   - `src/Promissio.AI.McpServer` — standalone MCP server.
   - `tests/Promissio.Domain.Tests`
   - `tests/Promissio.Application.Tests`
   - `tests/Promissio.Integration.Tests`
   - `tests/Promissio.AI.Evals`
2. Configure `Directory.Build.props` and `Directory.Packages.props` for Central Package Management.
3. Set up `.editorconfig`, `.gitignore`, `.gitattributes`.
4. Add `docker-compose.yml` with PostgreSQL 16, Qdrant, Langfuse, Jaeger.
5. Write initial `README.md` skeleton including project description, goals, status, and getting-started instructions.
6. Write `AGENTS.md` with coding standards, naming conventions, and AI-agent operating instructions.
7. Write `CLAUDE.md` with Claude Code-specific guidance.
8. Configure GitHub Actions: build and test on every pull request.
9. Add `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `LICENSE` (MIT or Apache 2.0).

**Acceptance criteria:**

- `dotnet build` succeeds from a clean clone.
- `dotnet test` runs and passes (initially zero tests, but framework operational).
- `docker compose up` brings up all infrastructure services.
- README has working "Getting started" section reproducible by a stranger.

**AI delegation notes:** Phase 0 should be done with minimal AI assistance. The author must internalize the foundation before delegating subsequent work. AI tools can help with `.editorconfig`, `.gitignore`, GitHub Actions YAML — but the project structure itself is an architectural decision to own personally.

---

### Phase 1 — Domain Core: Interest Engine (Weeks 2–4)

**Goal:** Build the financial mathematics foundation of the platform.

#### Week 2 — Value Objects and Base Types

**Tasks:**

1. Implement `Money` value object with currency, equality, arithmetic operators, and JSON converter.
2. Implement `Percentage` value object supporting basis points, fractions, and percent representations.
3. Implement `LoanTerm` using NodaTime types.
4. Define `InterestRate` abstract base and four concrete implementations: `FixedRate`, `FloatingRate`, `TieredRate`, `EffectiveRate`.
5. Write property-based tests for all value objects using CsCheck or FsCheck.

**Acceptance criteria:**

- All value objects are immutable, have value-based equality, and override `GetHashCode` correctly.
- Property-based tests cover algebraic invariants (e.g., addition associativity for `Money` with same currency).
- 90%+ line coverage on value object code.

#### Week 3 — Day-Count Conventions

**Tasks:**

1. Define `IDayCountConvention` interface.
2. Implement `Actual360`, `Actual365`, `ActualActual`, `Thirty360`, `Thirty360European` conventions.
3. Source reference test cases from ISDA documentation; encode at least 20 test vectors per convention.
4. Write `/docs/domain/day-count-conventions.md` with mathematical formulas, business context, and links to source materials.

**Acceptance criteria:**

- All conventions match reference values to the cent for at least 20 test cases each.
- Documentation is reviewable by a non-developer banking analyst.

#### Week 4 — Interest Calculation Engine

**Tasks:**

1. Implement `InterestCalculator` accepting principal, rate, convention, start date, end date.
2. Handle leap years, partial periods, grace periods, and month-end conventions.
3. Add 30+ scenario tests with known correct outputs.
4. Add BenchmarkDotNet benchmarks for hot paths.

**Acceptance criteria:**

- All scenarios match reference calculations.
- Benchmark results checked into `/benchmarks/results/` and tracked across commits.
- Mutation testing via Stryker.NET achieves at least 80% mutation score on calculator code.

**AI delegation notes:** This phase is ideal for pair programming with Claude Code or Cursor. The author specifies the formula and edge cases; the AI generates the implementation skeleton and test scaffolding. The author validates against reference data manually for at least three to five cases per implementation. LLMs occasionally introduce subtle off-by-one errors in financial math — never accept AI-generated financial code without independent verification.

---

### Phase 2 — Payment Schedule Generator (Weeks 5–6)

**Goal:** Implement all four schedule types and the APRC calculator.

#### Week 5 — Annuity and Differentiated Schedules

**Tasks:**

1. Define `IScheduleGenerator` interface.
2. Implement `AnnuityScheduleGenerator` using the standard annuity formula.
3. Implement `DifferentiatedScheduleGenerator` with equal principal portions.
4. Handle edge cases: short first period, long first period, grace period adjustments, holiday calendars.
5. Verify invariants: total payments equal principal plus total interest, no rounding drift.

**Acceptance criteria:**

- Generated schedules pass invariant checks for at least 100 random loan configurations.
- Snapshot tests via Verify.Xunit lock in expected schedules for canonical examples.

#### Week 6 — Bullet, Custom, and APRC Calculation

**Tasks:**

1. Implement `BulletScheduleGenerator` with interest-only periods and balloon payment.
2. Implement `CustomScheduleGenerator` accepting predefined cash flows.
3. Implement `AprcCalculator` using the Newton-Raphson or bisection iterative method per EU Consumer Credit Directive 2008/48/EC.
4. Validate against official EU example cases.

**Acceptance criteria:**

- APRC values match EU reference examples to four decimal places.
- All schedule generators produce balanced schedules (sum of principal portions equals original principal).

**AI delegation notes:** Schedule generation logic is well-suited to AI assistance once formulas are specified. The author should always manually validate three to five reference cases per schedule type, since LLMs may produce code that is plausible but incorrect for edge cases like grace periods or non-standard first periods.

---

### Phase 3 — Loan Aggregate and State Machine (Weeks 7–8)

**Goal:** Model the loan lifecycle with rigorous state transitions and event sourcing.

#### Week 7 — Loan Aggregate

**Tasks:**

1. Implement `Loan` aggregate root with invariants enforced at construction and on every command.
2. Define explicit commands: `CreateLoan`, `ApproveLoan`, `DisburseLoan`, `ApplyPayment`, `MoveToPastDue`, `RestructureLoan`, `WriteOff`.
3. Emit domain events for every state transition.
4. Implement state machine validation: rejected transitions throw explicit exceptions with diagnostic context.

**Acceptance criteria:**

- All state transitions are tested.
- Invalid transitions are rejected with informative exceptions.
- Domain events carry sufficient information for downstream consumers (no need to query the aggregate).

#### Week 8 — Marten Event Sourcing Integration

**Tasks:**

1. Configure Marten with PostgreSQL.
2. Persist all domain events.
3. Build read-model projections for common queries (active loans, overdue loans, portfolio summary).
4. Implement time-travel queries: retrieve loan state as of any past date.

**Acceptance criteria:**

- Integration tests verify event persistence and projection rebuilds.
- Time-travel query returns correct historical state for at least 10 scenarios.

**AI delegation notes:** Marten configuration and projection scaffolding are well-suited to AI assistance. Domain event design (which events exist, what data they carry) should be the author's decision — these shape the entire system's auditability story.

---

### Phase 4 — Application Services and APIs (Weeks 9–10)

**Goal:** Expose origination and servicing functionality through clean HTTP APIs.

#### Week 9 — Origination Service

**Tasks:**

1. Implement REST endpoints: `POST /applications`, `GET /applications/{id}`, `POST /applications/{id}/approve`, `POST /applications/{id}/reject`.
2. Use MediatR handlers for all command/query processing.
3. Apply FluentValidation to all inputs.
4. Generate OpenAPI specification served via Scalar.
5. Add integration tests using Testcontainers.

**Acceptance criteria:**

- Full happy-path flow tested: create application → review → approve → disburse → active loan.
- Invalid inputs produce structured error responses.
- OpenAPI spec is complete and matches actual API behavior.

#### Week 10 — Servicing Service

**Tasks:**

1. Implement REST endpoints: `GET /loans`, `GET /loans/{id}`, `POST /loans/{id}/payments`, `GET /loans/{id}/schedule`, `GET /loans/{id}/history`.
2. Implement idempotency keys for payment processing.
3. Apply optimistic concurrency control via row version.
4. Add integration tests covering concurrent payment scenarios.

**Acceptance criteria:**

- Duplicate payment submissions with the same idempotency key produce identical results.
- Concurrent modifications are detected and rejected with conflict responses.

**AI delegation notes:** This phase is the strongest candidate for AI delegation. Endpoint scaffolding, DTO definitions, validator implementations, and integration test boilerplate are all well-suited to Claude Code. The author reviews architectural choices and validates security-sensitive logic personally.

---

### Phase 5 — Daily Batch Processor (Weeks 11–12)

**Goal:** Implement end-of-day operations with the operational rigor expected in real loan servicing.

#### Week 11 — Scheduler and Daily Run Logic

**Tasks:**

1. Implement a hosted service that tracks the last successful run date.
2. Design idempotent operations — running the batch twice for the same date must produce identical results.
3. Implement graceful shutdown handling.
4. Add structured logging with correlation IDs spanning the entire batch run.

**Acceptance criteria:**

- Re-running a completed batch produces no duplicate accruals or transitions.
- Batch can resume after interruption from the last completed step.

#### Week 12 — Daily Operations

**Tasks:**

1. Implement daily interest accrual for all active loans.
2. Implement past-due detection and state transitions (Active → PastDue based on days past due).
3. Implement penalty rate activation logic.
4. Implement IFRS 9 stage transitions (Stage 1 → Stage 2 → Stage 3 based on days past due and credit deterioration signals).
5. Calculate provisioning per simplified IFRS 9 expected credit loss approach.

**Acceptance criteria:**

- A simulated portfolio of 1,000 loans processes correctly through a 90-day simulation.
- IFRS 9 stage transitions match expected behavior for canonical scenarios.

**AI delegation notes:** Batch orchestration logic is well-suited to AI assistance. IFRS 9 staging rules require domain expertise — the author should specify rules precisely rather than asking the AI to infer them.

---

### Phase 6 — AI Operations Layer: MCP Server (Weeks 13–15)

**Goal:** Expose loan operations as MCP tools consumable by any compatible AI client.

#### Week 13 — MCP Server Foundation

**Tasks:**

1. Set up MCP server as a standalone process using ModelContextProtocol C# SDK.
2. Implement authentication and authorization for tool invocations.
3. Implement initial tool set:
   - `get_loan_by_id`
   - `search_loans` with filters
   - `get_payment_history`
   - `get_schedule`
   - `calculate_payoff_amount` for any future date
   - `simulate_restructuring` for what-if scenarios

**Acceptance criteria:**

- MCP server is connectable from Claude Desktop.
- All tools work with realistic loan data.
- Authorization prevents cross-tenant data access.

#### Week 14 — Advanced Tools

**Tasks:**

1. Implement `analyze_loan_health` — generates risk score, days past due trend, payment behavior summary.
2. Implement `generate_payment_reminder` — drafts customer-facing message respecting compliance constraints.
3. Implement `propose_restructuring_options` — given customer financial situation, suggests restructuring options.

**Acceptance criteria:**

- All advanced tools produce structured, well-typed outputs.
- Generated messages pass a compliance check (no threats, no improper third-party disclosure).

#### Week 15 — Documentation and Client Testing

**Tasks:**

1. Write comprehensive MCP server documentation in `/docs/mcp/`.
2. Manually test all tools through Claude Desktop with realistic scenarios.
3. Record demo video showing a banker workflow using Claude with Promissio MCP server.

**Acceptance criteria:**

- Documentation is sufficient for a third-party developer to connect and use the server.
- Demo video is published and linked from README.

**AI delegation notes:** MCP tool design (which tools to expose, parameter schemas, security boundaries) should be the author's decision. Implementation of individual tools is well-suited to AI assistance.

---

### Phase 7 — AI Agents (Weeks 16–18)

**Goal:** Build production-quality agents demonstrating mature AI engineering patterns in a regulated context.

#### Week 16 — Credit Decisioning Copilot

**Tasks:**

1. Build a Semantic Kernel agent that assists underwriters in credit decisions.
2. Implement RAG over mock internal credit policies stored in Qdrant.
3. Provide tool calls to internal scoring service.
4. Produce structured output: decision recommendation, reasoning, identified risk factors.
5. Build a golden dataset of 30+ scenarios with expected decisions.
6. Implement evaluation metrics: decision accuracy, reasoning quality (LLM-as-judge).

**Acceptance criteria:**

- Evaluation suite runs in under five minutes.
- Decision accuracy on golden dataset exceeds 85%.
- Reasoning quality (judged by Claude or GPT-5) exceeds 4 out of 5 average.

#### Week 17 — Early Warning Agent

**Tasks:**

1. Build an agent that monitors active loans and detects deterioration signals.
2. Combine hard data (payment behavior trends) with soft signals (mock customer communications sentiment).
3. Generate prioritized alerts for credit officers.
4. Build a simulated portfolio dataset and evaluate alert quality.

**Acceptance criteria:**

- Agent correctly identifies at least 80% of deteriorating loans in the simulated portfolio.
- False positive rate is below 15%.

#### Week 18 — Collections Conversation Agent

**Tasks:**

1. Build a multi-turn conversational agent for outbound collections (chat-based, not voice).
2. Implement compliance constraints: no threats, no improper third-party disclosure, no after-hours contact, mandatory disclosures.
3. Maintain conversation memory across turns.
4. Build evaluation suite covering conversational quality, empathy, and compliance violation detection.

**Acceptance criteria:**

- Compliance violation rate in evaluation scenarios is zero.
- Conversational quality (judged by independent LLM) exceeds 4 out of 5 average.

**AI delegation notes:** Prompt engineering is iterative and best done interactively with the actual model. The author defines compliance constraints and evaluation criteria. AI assistance helps with structuring agent code, integration with the MCP server, and writing evaluation scaffolding.

---

### Phase 8 — Evaluations and Observability (Weeks 19–20)

**Goal:** Make the AI layer measurable, observable, and regression-resistant.

#### Week 19 — Evaluation Framework

**Tasks:**

1. Build a custom evaluation runner or integrate Promptfoo.
2. Define golden datasets for each agent.
3. Implement LLM-as-judge evaluators for open-ended outputs.
4. Integrate evaluations into CI — every PR touching AI code runs the relevant evaluations.
5. Implement regression detection: a 5% accuracy drop fails the build.

**Acceptance criteria:**

- Evaluation suite runs reliably in CI.
- Regression detection demonstrably catches a deliberately introduced bug.

#### Week 20 — Observability

**Tasks:**

1. Configure OpenTelemetry for the entire .NET stack with traces, metrics, and logs.
2. Configure Langfuse for LLM-specific observability: cost, latency, quality scores, prompt versions.
3. Build a Jaeger dashboard for end-to-end request traces.
4. Build a cost tracking dashboard showing daily AI spend by agent.

**Acceptance criteria:**

- A single request crossing all layers (HTTP → application → domain → AI agent → MCP tools) produces a single coherent trace.
- LLM costs are attributable to specific agents and tenants.

**AI delegation notes:** Observability configuration is largely boilerplate and well-suited to AI assistance. The design choices (what to trace, what to measure, what alerts to set) require engineering judgment from the author.

---

### Phase 9 — Production Polish (Weeks 21–22)

**Goal:** Bring the project to public-launch quality.

#### Week 21 — Performance and Reliability

**Tasks:**

1. Conduct load testing with NBomber against critical endpoints.
2. Run BenchmarkDotNet on all hot paths; document results.
3. Implement resilience patterns with Polly: retries, circuit breakers, timeouts on all external calls.
4. Add health check endpoints for all services.
5. Tune database indexes based on query patterns.

**Acceptance criteria:**

- API endpoints sustain at least 500 requests per second on a developer laptop.
- All external integrations have appropriate resilience policies.

#### Week 22 — Developer Experience and Documentation

**Tasks:**

1. Polish README with GIFs, badges, clear value proposition, and getting-started flow.
2. Write architecture documentation in `/docs/architecture/` with C4 diagrams (Context, Container, Component levels).
3. Write ADRs for all major decisions in `/docs/adr/`.
4. Record a 10-minute demo video showing the platform's capabilities.
5. Write a CONTRIBUTING guide for potential external contributors.
6. Create sample notebooks and example clients.

**Acceptance criteria:**

- A stranger can clone the repo and have a working system within five minutes.
- All major architectural decisions are documented and discoverable.

---

### Phase 10 — Public Launch (Week 23)

**Goal:** Maximize visibility within the .NET and AI engineering communities.

**Tasks:**

1. Publish a Reddit post on r/dotnet with demo content.
2. Submit to Hacker News, timed for a weekday morning Pacific time.
3. Publish a LinkedIn post with the demo video.
4. Publish a Twitter or Bluesky thread describing the architecture.
5. Submit a talk proposal to DotNet Kyiv for the next event.
6. Submit talk proposals to NDC London, NDC Oslo, Build Stuff, JOnTheBeach for upcoming editions.
7. Send respectful, brief messages to David Fowler, Stephen Toub, and other .NET community figures asking for feedback.
8. Publish a long-form blog post on a personal site or Medium summarizing the project and its lessons.

**Acceptance criteria:**

- Project has at least one public talk accepted within three months.
- Project has at least 100 GitHub stars within six months.

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

## 10. Public Launch Strategy

### Build in public

Throughout development, share progress publicly:

- Weekly LinkedIn posts summarizing the week's progress.
- Monthly blog posts diving into specific topics (day-count conventions, MCP server design, evaluation patterns).
- Twitter/Bluesky thread for every meaningful milestone.

This creates compounding visibility, generates inbound interest, and serves as a commitment device against abandonment.

### Target communities

- **r/dotnet** — primary .NET community.
- **r/csharp** — for code-quality and idiom discussions.
- **Hacker News** — for architecture and AI engineering posts.
- **Latent Space Discord** — for AI engineering visibility.
- **DotNet Kyiv** — local talks.
- **NDC, Build Stuff, JOnTheBeach** — European conference talks.
- **AI Engineer Summit / World's Fair** — AI engineering specifically.

### Talk topics derived from the project

- "Building production AI agents in regulated industries: a .NET banking case study."
- "MCP servers for enterprise systems: lessons from building Promissio."
- "Beyond annuity: implementing real loan servicing in .NET."
- "Evaluating AI agents under compliance constraints."
- "Event sourcing for audit trails: practical patterns."

---

## 11. Long-Term Roadmap

The 22-week plan delivers the core platform. Beyond that, potential expansions include:

### Year 2 — Depth

- Multi-currency support with proper FX handling.
- Securitization-friendly cash flow modeling.
- More sophisticated IFRS 9 implementation including macroeconomic overlays.
- Real-time scoring service with model versioning.
- Document analysis agent for income proofs and identity documents.
- Open Banking integration (mock initially) for cash flow-based underwriting.

### Year 2 — Breadth

- Frontend developer console using Blazor or React for demos.
- Multi-language MCP clients (Python, TypeScript examples).
- Deployment recipes for Azure and AWS.
- Helm charts for Kubernetes deployment.

### Year 3 — Ecosystem

- Cookbook of recipes for common lending scenarios.
- Tutorials for specific compliance regimes.
- Partner integrations with notable .NET libraries.
- Conference workshop materials.

---

## Appendix A — Reference Materials

### Banking domain

- ISDA documentation on day-count conventions.
- EU Consumer Credit Directive 2008/48/EC and 2023/2225 (revised).
- IFRS 9 Financial Instruments standard.
- Basel III framework documents.
- European Banking Authority guidelines on credit risk management.

### Software engineering

- "Designing Data-Intensive Applications" by Martin Kleppmann.
- "Domain-Driven Design" by Eric Evans.
- "Implementing Domain-Driven Design" by Vaughn Vernon.
- Marten documentation.
- NodaTime user guide.

### AI engineering

- "AI Engineering" by Chip Huyen.
- Anthropic engineering blog.
- Latent Space podcast and newsletter.
- Hamel Husain's writing on evaluations.
- Eugene Yan's writing on applied LLM systems.
- Model Context Protocol specification.

---

## Appendix B — Glossary

- **APRC** — Annual Percentage Rate of Charge. The standardized effective rate disclosed to consumers in the EU.
- **Day-count convention** — Method for converting calendar time into the fraction of a year used in interest calculations.
- **IFRS 9 staging** — Classification of financial assets into Stage 1 (performing), Stage 2 (significant credit deterioration), and Stage 3 (credit-impaired) for impairment accounting.
- **MCP** — Model Context Protocol. An open standard for AI assistants to interact with external systems through tools.
- **RAG** — Retrieval-Augmented Generation. Pattern where LLM responses are grounded in retrieved documents.
- **LLM-as-judge** — Using a large language model to evaluate outputs of another model.

---

*End of plan. Updates to this document should be discussed in pull requests; the document evolves with the project.*
