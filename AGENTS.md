# Promissio — AI Agent Operating Manual

> **Audience:** Any AI coding agent working on the Promissio codebase (Claude Code, Cursor, Aider, Kilo, Continue, GitHub Copilot Workspace, and others).
> **Status:** Living document. Treat as authoritative. When this file conflicts with your training defaults, this file wins.
> **Companion files:** `CLAUDE.md` (Claude Code specifics), `developers_plan.md` (roadmap and architectural intent), `FRONTEND_PLAN.md` (frontend strategy).

---

## How to use this document

Read this file fully before making any changes. Re-read the sections relevant to your current task before each significant edit. Do not assume — verify against this manual.

If a rule here seems wrong or outdated for your specific task, **stop and ask the human owner** before proceeding. Do not improvise around domain rules, especially financial ones.

---

## 1. Project Context

Promissio is an open-source loan servicing platform for .NET, with built-in AI augmentation for credit operations. It demonstrates production-grade engineering for the full loan lifecycle: origination, servicing, daily batch processing, delinquency handling, and AI-assisted operations.

**You are working on a portfolio-grade project, not a prototype.** Code quality, domain authenticity, test coverage, observability, and documentation all matter equally. There are no "throwaway" parts.

**Target audiences for the code:**

1. Engineering managers and senior engineers at fintech companies evaluating the project owner's skills.
2. Banking domain practitioners who recognize correct day-count conventions, sensible IFRS 9 staging, authentic state transitions.
3. AI engineers learning patterns for agentic systems in regulated industries.
4. The broader .NET community.

If a change you propose would not survive the scrutiny of any of these audiences, do not make it.

---

## 2. Core Principles

1. **Correctness over speed in financial logic.** A wrong interest calculation is unrecoverable; a slow one is fixable. Always favor verifiable correctness.
2. **Domain authenticity.** Every concept that exists in real loan servicing must exist in the code with the right semantics, not a simplified approximation.
3. **Make decisions visible.** Important architectural choices live in ADRs (`/docs/adr/`), not in commit messages or memory.
4. **Tests are not optional.** Every piece of domain logic has unit tests. Every workflow has integration tests. AI components have evaluation suites.
5. **Idiomatic modern .NET.** Use .NET 9 features when they improve clarity. Avoid retro patterns (manual mapping where Mapster would do, `Task.Result` where `await` works).
6. **No silent assumptions.** When information is missing, ask. Never fabricate values, identifiers, dates, or rates.
7. **Transparency over cleverness.** Code is read more often than written. Optimize for the reader's understanding, not the writer's pride.

---

## 3. Project Structure

```
src/
  Promissio.Domain/              ← Pure domain. No external deps except NodaTime.
  Promissio.Application/         ← MediatR handlers, application services.
  Promissio.Infrastructure/      ← EF Core, Marten, external integrations.
  Promissio.Api.Origination/     ← Origination HTTP API (minimal APIs).
  Promissio.Api.Servicing/       ← Servicing HTTP API (minimal APIs).
  Promissio.BatchProcessor/      ← Daily batch worker service.
  Promissio.AI/                  ← AI orchestration, agents.
  Promissio.AI.McpServer/        ← Standalone MCP server.
tests/
  Promissio.Domain.Tests/
  Promissio.Application.Tests/
  Promissio.Integration.Tests/
  Promissio.AI.Evals/
docs/
  adr/                           ← Architecture Decision Records.
  domain/                        ← Mathematical formulas, banking concepts.
  architecture/                  ← C4 diagrams, system overview.
  mcp/                           ← MCP server documentation.
benchmarks/
  results/                       ← Checked-in BenchmarkDotNet output.
```

**Rules:**

- Never add a dependency on `Promissio.Infrastructure` or any API project from `Promissio.Domain` or `Promissio.Application`. The domain knows nothing about persistence or transport.
- New cross-cutting concerns get their own project, not stuffed into Infrastructure.
- When adding a new project, update `Directory.Packages.props` for Central Package Management.

---

## 4. Technology Stack — Locked Decisions

These choices are decided. Do not propose alternatives unless explicitly asked.

### Runtime and framework
- .NET 9 currently; will migrate to .NET 10 after GA.
- C# 13 features allowed where they improve clarity.
- ASP.NET Core minimal APIs (not controllers).

### Persistence
- PostgreSQL 16 as the primary database.
- EF Core 9 for relational reads/writes outside the event store.
- Marten for event sourcing on PostgreSQL.
- No in-memory database providers for tests. Use Testcontainers.

### Date and time
- **NodaTime everywhere in domain and application code.** No `System.DateTime`, `DateTimeOffset`, or `TimeSpan` in domain models.
- Use `LocalDate` for business dates, `Instant` for points in time, `Duration` for elapsed time.
- Time zones are explicit. UTC is not a substitute for proper zone handling.

### Money and percentages
- **Never expose raw `decimal` in domain APIs.** Wrap in `Money` (amount + currency) or `Percentage` (with explicit unit).
- All money arithmetic goes through `Money` operators. Same-currency only; cross-currency requires explicit FX conversion.

### Mediation and validation
- MediatR for command/query handling.
- FluentValidation for input validation at the API boundary.

### Mapping
- Mapster for object mapping. Avoid AutoMapper (performance and source-gen reasons).

### AI layer
- `Microsoft.Extensions.AI` as the unified abstraction.
- Anthropic Claude as primary provider (Sonnet 4.7 for quality, Haiku 4.5 for cost-sensitive ops).
- OpenAI GPT-5 as fallback through the same abstraction.
- Semantic Kernel for agent orchestration.
- ModelContextProtocol C# SDK for the MCP server.
- Langfuse for LLM observability.

### Testing
- xUnit + FluentAssertions.
- Testcontainers.NET for real PostgreSQL in integration tests.
- CsCheck or FsCheck for property-based tests of value objects.
- Stryker.NET for mutation testing of critical financial logic.
- Bogus for test data generation.
- Verify.Xunit for snapshot tests of generated schedules.
- NBomber for load tests.

### Observability
- OpenTelemetry for traces, metrics, logs.
- Langfuse for LLM-specific observability.

---

## 5. Coding Standards

### General

- Public types are `sealed` by default. Inheritance is opt-in and must be justified.
- Public APIs are documented with XML comments.
- Internal implementation may skip XML docs unless complex.
- Prefer composition over inheritance. Prefer interfaces over abstract classes when there is no shared implementation.

### Nullability

- `<Nullable>enable</Nullable>` is on project-wide.
- No `null` in public domain APIs. Use `Maybe<T>`, `Result<T>`, or explicit `?` annotations.
- Never use `null!` to bypass the compiler. If you need it, the design is wrong.

### Async patterns

- All async methods take a `CancellationToken` parameter, named `cancellationToken`, defaulting to `default` only when truly optional.
- Use `ConfigureAwait(false)` in library code (`Promissio.Domain`, `Promissio.Application`, `Promissio.Infrastructure`).
- Never call `.Result`, `.Wait()`, or `GetAwaiter().GetResult()`. If you need synchronous code, write it synchronous.
- Use `IAsyncEnumerable<T>` for streaming results where the caller benefits from iteration.

### Error handling

- Domain operations that can fail return `Result<T>` or `Result<T, TError>`. They do not throw for expected failures.
- Throw exceptions only for genuinely exceptional conditions (configuration errors, programming bugs).
- Custom exceptions live in the relevant project's `Exceptions/` folder and inherit from a project-specific base.
- Never catch `Exception` without rethrowing or logging with full context.

### Naming

- Aggregates: `Loan`, `LoanApplication`, `PaymentSchedule`.
- Value objects: `Money`, `InterestRate`, `Percentage`, `LoanTerm`.
- Commands: imperative verbs — `ApproveLoanCommand`, `RecordPaymentCommand`.
- Queries: descriptive nouns — `GetLoanByIdQuery`, `ListOverdueLoansQuery`.
- Events: past tense — `LoanApproved`, `PaymentReceived`, `InterestAccrued`.
- Services: `IInterestCalculator`, `IScheduleGenerator`.
- Test classes: `<ClassUnderTest>Tests` for unit, `<Feature>Tests` for integration.

### Formatting

- Run `dotnet format` before committing. CI rejects unformatted code.
- 4-space indentation, no tabs.
- File-scoped namespaces.
- One type per file. Exception: tightly related small types (e.g., a sealed record and its validator).

---

## 6. Domain Modeling Rules

### Aggregates

- Each aggregate has a single entry point — the aggregate root.
- Invariants are enforced in the constructor and in every command method. There are no "valid" aggregates with broken invariants.
- Commands return `Result<T>` indicating success or business failure. They emit domain events on success.
- Aggregates do not call external services. They are pure logic. External orchestration lives in application services.

### State machines

- State transitions are declared explicitly in code (not inferred from boolean flags).
- Invalid transitions throw `InvalidStateTransitionException` with full context (current state, attempted transition, reason).
- Every state transition emits a domain event.
- State machines are documented in `/docs/domain/state-machines.md` with diagrams that must stay in sync with code.

### Value objects

- Value objects are records with value-based equality.
- They are immutable. No mutating methods. Operations return new instances.
- They validate their invariants in the constructor.
- They override `ToString()` for readable logs.
- `GetHashCode` must be consistent with equality — usually automatic with records.

### Domain events

- Events are named in past tense and describe what happened, not what should happen next.
- Events carry enough data that consumers don't need to query the aggregate.
- Events are immutable records.
- Event schema changes are versioned. Never break existing event consumers without a migration plan.

---

## 7. Testing Requirements

### Coverage targets

- `Promissio.Domain`: at least 90% line coverage, at least 80% mutation score (Stryker.NET).
- `Promissio.Application`: at least 80% line coverage.
- `Promissio.Infrastructure`: integration tests for every public method; no specific coverage target.
- AI layer: evaluation suites pass thresholds defined per agent (see `Promissio.AI.Evals`).

### What to test

- Every public method of every aggregate.
- Every state transition (valid and invalid).
- Every value object's algebraic properties (associativity, identity, commutativity where applicable).
- Every API endpoint's happy path and at least three failure modes.
- Every batch operation's idempotency.
- Every interest calculation against known reference values from ISDA / EU documentation.

### Property-based tests

Use CsCheck (preferred) or FsCheck for properties like:

- `Money` addition is associative for same currency.
- `InterestRate.Apply(principal, period).Sum() == TotalInterest(principal, rate, term)` within rounding tolerance.
- Schedule generation: sum of principal portions equals original principal.
- APRC calculation: round-trip stability.

### Integration tests

- Use Testcontainers for real PostgreSQL. Do not mock the database for integration tests.
- Each test owns its data. Tests do not depend on execution order.
- Clean state between tests via transactions or schema reset, not by deleting individual rows.

### Snapshot tests

- Use Verify.Xunit for generated schedules. Lock in expected output for canonical examples.
- When schedule format changes legitimately, update snapshots in the same commit as the code change, and explain in commit message why.

---

## 8. Banking Domain Semantics — Critical Rules

**Read this section before touching anything in `Promissio.Domain` or `Promissio.Application` related to financial calculations.**

### Never guess on financial math

If you do not know the correct day-count convention, the right rounding rule, the proper handling of leap years, or the right method for APRC iteration — **stop and ask the human owner**. Do not invent. Do not extrapolate from similar-looking code elsewhere.

LLMs are known to produce plausible-looking but subtly incorrect financial code. Off-by-one errors in day counts, wrong rounding direction in interest calculation, missing edge cases in grace periods — these are common AI failure modes. Verification against reference data is mandatory.

### Day-count conventions

- All implementations live in `Promissio.Domain/Calculations/DayCounts/`.
- Each convention has at least 20 test vectors with values sourced from ISDA documentation or ECB illustrative examples.
- Reference sources are cited in the implementation file's XML comment.
- Mathematical formulas are documented in `/docs/domain/day-count-conventions.md`.

### Interest calculation

- The only path to compute interest is through `IInterestCalculator`.
- Never write inline interest math in handlers, services, or endpoints.
- Rounding: use banker's rounding (`MidpointRounding.ToEven`) unless a specific business rule says otherwise. Document the choice in code.

### APRC calculation

- The `AprcCalculator` implements iterative solver per EU Consumer Credit Directive 2008/48/EC (and 2023/2225 where applicable).
- Reference test cases match official EU examples to four decimal places.
- Never modify this calculator without re-running the full reference test suite.

### State transitions

- The Loan state machine is documented in `/docs/domain/loan-state-machine.md`.
- When adding a new state or transition, update both code and documentation in the same PR.
- Common transitions:
  - `Disbursed` → `Active` on first business day after disbursement.
  - `Active` → `InGrace` while within grace period.
  - `Active` / `InGrace` → `PastDue` based on days past due threshold (configurable, default 1).
  - `PastDue` → `Defaulted` based on days past due threshold (configurable, default 90).
  - `Defaulted` → `WrittenOff` / `Restructured` / `Recovered`.

### IFRS 9 staging

- Stage 1: performing, no significant credit deterioration.
- Stage 2: significant credit deterioration since origination, but not credit-impaired.
- Stage 3: credit-impaired.
- Stage assignment logic lives in `IIfrs9StagingService`.
- Triggers for Stage 2 are documented in `/docs/domain/ifrs9-staging.md`.
- Do not modify staging logic without reviewing the documentation and discussing with the human owner.

---

## 9. AI Layer Specific Rules

### MCP server tool design

- Every tool exposed by `Promissio.AI.McpServer` requires authorization. There is no anonymous tool access.
- Tool parameter schemas are explicit and validated. No `object` or `dynamic` parameters.
- Tool return shapes are structured records with documented fields.
- Tool descriptions are written for an LLM consumer, not a human developer. They explain when the tool should be used, not just what it does.
- Every tool invocation is logged with: tool name, arguments (with PII redacted), result hash, latency, cost.

### Agent prompts

- All system prompts live in `Promissio.AI/Prompts/` as separate `.md` or `.txt` files, not inline strings.
- Prompt versions are tracked. When changing a prompt, increment its version and document the change in `Promissio.AI/Prompts/CHANGELOG.md`.
- Prompts include: role definition, authority limits, compliance constraints, output format requirements, few-shot examples.

### Evaluations

- Every agent has a golden dataset in `Promissio.AI.Evals/Datasets/`.
- Evaluations run on every PR that touches AI code.
- A 5% drop in any metric fails the build.
- LLM-as-judge evaluators are themselves version-controlled and tested.

### Observability

- All LLM calls go through Langfuse instrumentation.
- Cost, latency, and quality metrics are tagged per agent.
- Errors and refusals are surfaced in dashboards, not just logs.

### Safety and compliance for agents

- Collections agents must never threaten, never disclose account information to third parties, never schedule contact outside permitted hours.
- Customer communication drafts must include mandatory regulatory disclosures.
- Decision-supporting agents must produce reasoning traces for audit.
- When an agent declines to act due to a constraint, the refusal reason is logged with full context.

---

## 10. What You Should Do

When working on a task, your default behavior is:

1. **Read the relevant files first.** Before editing, view the file. Before editing a domain area, read the relevant ADR and the section of `developers_plan.md` covering that phase.
2. **Plan before acting.** For non-trivial changes, write a short plan (in your output, or via a task tool if available) before making edits.
3. **Make atomic changes.** Each commit does one thing. If a change requires multiple steps, make multiple commits.
4. **Write tests alongside code.** Not after. Not later. Same commit.
5. **Run tests before claiming a change is done.** "I wrote the code" is not the same as "I verified it works."
6. **Format before committing.** Run `dotnet format` or equivalent.
7. **Update documentation.** If the change affects an ADR, a state machine diagram, a domain concept doc, or a README — update them in the same PR.
8. **Write clear commit messages.** Conventional Commits format. Body explains why, not what (the diff shows what).
9. **Escalate uncertainty.** When unsure about banking semantics, security, or architectural intent — ask the human.

---

## 11. What You Must Never Do

These are non-negotiable.

1. **Never invent financial formulas, rates, dates, or identifiers.** If you don't know a reference value, ask.
2. **Never bypass tests** to make a change "look complete." Failing tests are stopping conditions.
3. **Never modify generated files directly** (EF migrations, OpenAPI specs, Marten projection rebuilds). Regenerate.
4. **Never add secrets to source control.** Not even in test fixtures. Use `dotnet user-secrets` locally and environment variables in CI.
5. **Never log PII in plain text.** Customer names, account numbers, IDs go through redaction.
6. **Never disable a security check** without a security ADR explaining why.
7. **Never commit code that does not build.** `dotnet build` must succeed.
8. **Never silence a compiler warning** with `#pragma warning disable` without justification in a comment.
9. **Never use `dynamic`, `var` as a substitute for typing, or `object`** in public APIs.
10. **Never change a state transition rule** without updating the state machine documentation.
11. **Never delete an ADR.** ADRs are append-only. Superseded ADRs are marked superseded and link to the replacement.
12. **Never delete tests** to "make the build green." Failing tests are signals, not obstacles.

---

## 12. Verification Practices

### Financial math

For every new interest calculation, schedule generation, or APRC implementation:

- Manually verify three to five reference cases against authoritative source (ISDA paper, EU directive example, IFRS 9 illustrative example).
- Cite the source in the test file's class comment.
- If no authoritative source exists for the case you're implementing, escalate to the human owner.

### State machine logic

- Verify the transition table in code matches the diagram in documentation.
- For new transitions, draw the updated diagram before writing code.

### Security-sensitive code

- Authorization checks, token validation, PII redaction, prompt injection defenses — these require human review. Mark the PR for explicit security review.

### Generated documentation

- AI-generated documentation passes a human edit before merging. Mention this in the PR description.

---

## 13. Documentation Requirements

### Every public API

- XML doc comments on every public type and member.
- `<summary>` is one sentence describing intent.
- `<remarks>` explains non-obvious behavior, edge cases, gotchas.
- `<example>` for non-trivial usage.

### Every architectural decision

- Recorded as an ADR in `/docs/adr/`.
- Format: title, status (proposed/accepted/superseded), context, decision, consequences.
- ADRs are numbered sequentially.
- Reference relevant ADRs in code comments where they explain a non-obvious choice.

### Every domain concept

- Documented in `/docs/domain/` with mathematical formulas, business context, and source references.
- Examples: day-count conventions, IFRS 9 staging, APRC formula, accrual methodology.

### Every MCP tool

- Documented in `/docs/mcp/tools.md` with name, parameters, return shape, authorization requirements, example invocation.

---

## 14. Git and Commit Conventions

### Branches

- `main` is always deployable.
- Feature branches: `feat/<short-description>`.
- Fix branches: `fix/<short-description>`.
- Documentation branches: `docs/<short-description>`.

### Commit messages

Conventional Commits format:

```
<type>(<scope>): <short summary>

<longer body explaining why this change is needed>

<footer with references, breaking changes, etc.>
```

Types: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `chore`, `build`, `ci`.

Examples:

```
feat(domain): add Actual/Actual day-count convention

Implements the Actual/Actual convention per ISDA documentation,
with the leap-year treatment as defined in section 4.16.
Reference cases sourced from ISDA 2006 Definitions, Annex A.
```

```
fix(servicing): handle idempotency for duplicate payment submissions

Previously, submitting the same payment twice produced two
accrual entries. Now the second submission returns the
original payment ID without side effects.

Closes #42
```

### Pull requests

- One PR per logical change. No "while I was at it" mixed bags.
- PR description includes: what changed, why, how to test, ADR references.
- PR is small enough to be reviewed in one sitting (rule of thumb: under 400 lines of diff in production code).
- Self-review the diff before requesting human review.

---

## 15. When to Escalate to the Human

Escalate immediately when:

1. You are asked to modify financial calculations and you cannot find an authoritative reference for the expected behavior.
2. You discover a banking concept in the code that seems wrong but a test or comment asserts it is correct.
3. A security control appears to be in the way of a feature.
4. You would need to delete or significantly change a test to make a feature pass.
5. The task asks you to do something this document forbids.
6. You cannot reconcile two pieces of guidance (e.g., a code comment contradicts an ADR).
7. You are asked to add a dependency that conflicts with the locked stack in Section 4.
8. You are asked to expose customer PII through a tool or API.
9. You are uncertain whether an action belongs in the domain layer, application layer, or infrastructure layer, and the existing structure does not give a clear answer.

How to escalate:

- Stop the current task.
- Summarize the situation in clear terms: what you're trying to do, what is blocking you, what options you see, which one you would choose and why.
- Wait for human input. Do not proceed with a guess.

---

## 16. Working Style

### Be concise

Output for code changes should be minimal narration: brief plan, then the change. Do not restate the user's request. Do not announce what you are about to do at length.

### Be honest

If a change you made may have side effects, say so. If you are unsure something works, say so. If you took a shortcut, say so. Honesty about uncertainty is more valuable than false confidence.

### Be incremental

Prefer small, verifiable steps over large speculative changes. If a refactor would touch 30 files, propose it first and get agreement on scope.

### Be skeptical of your own code

When you complete a change, ask yourself: "Could a banking analyst look at this and find it wrong?" If yes, fix it. If you can't tell, ask.

---

## Appendix A — Reference Materials

### Banking domain

- ISDA 2006 Definitions, especially day-count conventions.
- EU Consumer Credit Directive 2008/48/EC and 2023/2225 (revised).
- IFRS 9 Financial Instruments standard.
- Basel III framework documents.
- European Banking Authority guidelines on credit risk.

### .NET and engineering

- Microsoft Learn .NET 9 documentation.
- NodaTime user guide.
- Marten documentation.
- Stephen Toub's performance series on .NET.

### AI engineering

- Anthropic engineering blog.
- Model Context Protocol specification.
- "AI Engineering" by Chip Huyen.
- Hamel Husain on evaluations.

---

## Appendix B — File Quick Reference

| File | Purpose |
|---|---|
| `AGENTS.md` (this file) | Operating manual for any AI agent. |
| `CLAUDE.md` | Claude Code specific additions. |
| `developers_plan.md` | Roadmap, phases, architectural intent. |
| `FRONTEND_PLAN.md` | Frontend strategy (Claude Desktop + AI Workspace). |
| `/docs/adr/` | Architecture Decision Records. |
| `/docs/domain/` | Banking concepts, mathematical formulas. |
| `/docs/architecture/` | C4 diagrams, system overview. |
| `/docs/mcp/` | MCP server documentation. |
| `Directory.Packages.props` | Central Package Management. |
| `Directory.Build.props` | Shared MSBuild settings. |

---

*End of operating manual. Updates to this file are discussed in PRs and require human approval.*
