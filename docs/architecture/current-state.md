# Current solution structure

Reviewed against the working tree on 2026-10-19. Consult [delivery status](../status.md) for the latest tested scope.

The solution contains eight source projects, six test projects and two benchmark projects. Projects are organized by architectural layer; origination, servicing, risk and AI are conceptual boundaries, not independently implemented services.

| Project | Kind | Current responsibility |
|---|---|---|
| Promissio.Domain | Library | Value objects, day counts, interest, schedules, dated APRC, loan lifecycle and event replay |
| Promissio.Application | Library | MediatR creation, activation, aging and current-state query workflows; persistence and query ports |
| Promissio.Infrastructure | Library | Marten loan repository, atomic handoff idempotency receipt, inline current-state projection and NodaTime persistence configuration; no EF Core model |
| Promissio.Api.Origination | Web host | Empty HTTP application; no business routes or Scalar UI |
| Promissio.Api.Servicing | Web host | Empty HTTP application; no business routes or Scalar UI |
| Promissio.BatchProcessor | Generic host executable | Starts and stops a host; no scheduled jobs or financial batch work |
| Promissio.AI | Web host | Reserved agent runtime; development OpenAPI only, no agents |
| Promissio.AI.McpServer | Web host | Reserved MCP boundary; no MCP transport, tools or authorization wired |

## Project references today

Arrows below mean compile-time references, not network calls.

```mermaid
flowchart TD
  Origination[Origination API] --> Application
  Servicing[Servicing API] --> Application
  Origination --> Domain
  Servicing --> Domain
  Application --> Domain
  Infrastructure --> Application
  Infrastructure --> Domain
  Batch[Batch host] --> Application
  Batch --> Domain
  Batch --> Infrastructure
  AI[AI host] --> Application
  AI --> Domain
  AI --> Infrastructure
  MCP[MCP host] --> Application
  MCP --> Domain
  MCP --> Infrastructure
```

Infrastructure references Application to implement its loan persistence and query ports and reaches Domain transitively through those contracts. APIs do not yet compose Infrastructure. Domain references NodaTime only, and Application has no dependency on persistence or transports. The current-state projection is derived from loan events and updated in the event append transaction; the event stream remains authoritative.

## Runtime and tests

Docker Compose defines PostgreSQL, Qdrant and Jaeger dependencies; it does not launch the application hosts or Langfuse. Domain, Application and Infrastructure unit verification require no database. The Integration suite requires a working Docker endpoint for its PostgreSQL Testcontainer. AI evaluations remain a placeholder, not coverage.

The infrastructure tests verify repository registration. Application tests cover creation, activation, aging and current-state query orchestration. Real PostgreSQL Testcontainers scenarios cover event persistence, handoff idempotency, state transitions, immediate projection reads and rebuilds. Endpoint behavior, batch idempotency and AI evaluations remain future work.

Both benchmark projects are in the solution so their API drift becomes a build failure. Their scopes and commands are documented in [benchmarks](../../benchmarks/README.md).
