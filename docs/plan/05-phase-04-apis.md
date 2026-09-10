# Phase 4 — Application Services and APIs (Weeks 9–10)

> Canonical phase plan. Scope is planned unless [current status](../status.md) records verification. Week numbers are historical effort estimates, not deadlines.

**Goal:** Expose origination and servicing functionality through clean HTTP APIs.

## Week 9 — Origination Service

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

## Week 10 — Servicing Service

**Tasks:**

1. Implement REST endpoints: `GET /loans`, `GET /loans/{id}`, `POST /loans/{id}/payments`, `GET /loans/{id}/schedule`, `GET /loans/{id}/history`.
2. Implement idempotency keys for payment processing.
3. Apply optimistic concurrency according to [the persistence proposal](../adr/0007-persistence-and-concurrency.md): expected stream versions for event-sourced writes; concurrency tokens for independently owned relational data. Review this proposal before implementation.
4. Add integration tests covering concurrent payment scenarios.

**Acceptance criteria:**

- Duplicate payment submissions with the same idempotency key produce identical results.
- Concurrent modifications are detected and rejected with conflict responses.

**AI delegation notes:** This phase is the strongest candidate for AI delegation. Endpoint scaffolding, DTO definitions, validator implementations, and integration test boilerplate are all well-suited to Claude Code. The author reviews architectural choices and validates security-sensitive logic personally.

---
