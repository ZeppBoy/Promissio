# ADR-0005: Documentation ownership and host boundaries

**Status:** Accepted scope following the owner's request to proceed with the structural proposals on 2026-09-10. Documentation review pending.

## Context

Roadmap copies and historical audits gave contradictory status. BatchProcessor was described as runnable but compiled as a library. Benchmark projects were outside solution verification, allowing stale API references to remain hidden.

## Decision

Keep the existing layered projects. Maintain canonical roadmap documents under docs/plan, evidence under docs/verification, and historical reports under docs/audits. Root documents and old src/docs paths remain forwarding links. AGENTS.md and CLAUDE.md remain unchanged.

Treat both APIs, BatchProcessor, AI and MCP as executable composition roots. BatchProcessor receives only a generic-host bootstrap. AI remains an explicitly documented runtime host; no speculative library or extra deployment is added. Business orchestration remains an Application responsibility.

Include both existing benchmark suites in the solution and preserve their cases. A single verification script drives local and CI build, test, formatting and documentation checks.

## Consequences

Starting a host is not evidence of an implemented workflow. Future persistence and authorization require separate decisions. CI builds benchmark harnesses but does not claim meaningful performance measurements from a build or discovery check.

The developer workflow depends on PowerShell 7 and an SDK supporting the solution format. Coverage and mutation scores must be measured independently; a configured target is not a passed target.
