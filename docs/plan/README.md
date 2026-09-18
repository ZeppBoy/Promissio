# Development roadmap

This directory is the authoritative roadmap. The root developers_plan.md is an overview, and src/docs/plan contains compatibility links. Phase plans were consolidated from the original monolithic plan; historical week numbers are effort estimates, not delivery commitments.

Read [product context](00-core.md) for goals and the target design. Check [current status](../status.md) for implemented and verified scope.

| Phase | Plan |
|---|---|
| 0 | [Foundation](01-phase-00-foundation.md) |
| 1 | [Interest engine](02-phase-01-interest-engine.md) |
| 2 | [Schedules and APRC](03-phase-02-schedules-aprc.md) |
| 3 | [Loan lifecycle and event sourcing](04-phase-03-loan-aggregate.md) |
| 4 | [Application services and APIs](05-phase-04-apis.md) |
| 5 | [Daily batch processing](06-phase-05-batch-processor.md) |
| 6 | [MCP tools](07-phase-06-mcp-server.md) |
| 7 | [AI agents](08-phase-07-ai-agents.md) |
| 8 | [Evaluations and observability](09-phase-08-evals-observability.md) |
| 9 | [Production polish](10-phase-09-production-polish.md) |
| 10 | [Public launch](11-phase-10-public-launch.md) |

[Frontend strategy](frontend.md) and [long-term strategy](12-launch-strategy-and-roadmap.md) are future plans. The backend-first scope remains unchanged.

## Next decisions

For implementation with the local Qwen model, use the [Qwen3.8-27B-IQ3_M developer handoff](qwen-local/README.md). It contains llama.cpp qualification, bounded task packets and review gates; it does not change the application AI provider decisions.

The [application-to-loan handoff](../adr/0006-origination-servicing-handoff.md) is accepted, and [persistence ownership](../adr/0007-persistence-and-concurrency.md) is accepted for the Phase 3 T2 creation/replay slice. Run its PostgreSQL integration suite on a Docker-capable host before extending persistence. Projection lifecycle and event-schema evolution remain follow-up decisions. Before exposing operations, review [single-tenant authorization](../adr/0008-single-tenant-authorization.md).
