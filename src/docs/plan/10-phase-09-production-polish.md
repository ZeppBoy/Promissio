# Phase 9 — Production Polish (Weeks 21–22)

> **Companion:** всегда подгружать вместе с `00-core.md`.
> **Weeks:** 21, 22
> **Goal:** Bring the project to public-launch quality.

---

## Week 21 — Performance and Reliability

### Tasks

1. Conduct load testing with NBomber against critical endpoints.
2. Run BenchmarkDotNet on all hot paths; document results.
3. Implement resilience patterns with Polly: retries, circuit breakers, timeouts on all external calls.
4. Add health check endpoints for all services.
5. Tune database indexes based on query patterns.

### Acceptance criteria

- API endpoints sustain at least 500 requests per second on a developer laptop.
- All external integrations have appropriate resilience policies.

---

## Week 22 — Developer Experience and Documentation

### Tasks

1. Polish README with GIFs, badges, clear value proposition, and getting-started flow.
2. Write architecture documentation in `/docs/architecture/` with C4 diagrams (Context, Container, Component levels).
3. Write ADRs for all major decisions in `/docs/adr/`.
4. Record a 10-minute demo video showing the platform's capabilities.
5. Write a CONTRIBUTING guide for potential external contributors.
6. Create sample notebooks and example clients.

### Acceptance criteria

- A stranger can clone the repo and have a working system within five minutes.
- All major architectural decisions are documented and discoverable.
