# Phase 8 — Evaluations and Observability (Weeks 19–20)

> **Companion:** всегда подгружать вместе с `00-core.md`.
> **Weeks:** 19, 20
> **Goal:** Make the AI layer measurable, observable, and regression-resistant.

---

## Week 19 — Evaluation Framework

### Tasks

1. Build a custom evaluation runner or integrate Promptfoo.
2. Define golden datasets for each agent.
3. Implement LLM-as-judge evaluators for open-ended outputs.
4. Integrate evaluations into CI — every PR touching AI code runs the relevant evaluations.
5. Implement regression detection: a 5% accuracy drop fails the build.

### Acceptance criteria

- Evaluation suite runs reliably in CI.
- Regression detection demonstrably catches a deliberately introduced bug.

---

## Week 20 — Observability

### Tasks

1. Configure OpenTelemetry for the entire .NET stack with traces, metrics, and logs.
2. Configure Langfuse for LLM-specific observability: cost, latency, quality scores, prompt versions.
3. Build a Jaeger dashboard for end-to-end request traces.
4. Build a cost tracking dashboard showing daily AI spend by agent.

### Acceptance criteria

- A single request crossing all layers (HTTP → application → domain → AI agent → MCP tools) produces a single coherent trace.
- LLM costs are attributable to specific agents and tenants.

---

## AI delegation notes

Observability configuration is largely boilerplate and well-suited to AI assistance. The design choices (what to trace, what to measure, what alerts to set) require engineering judgment from the author.
