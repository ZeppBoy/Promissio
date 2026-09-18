# Target boundaries

This is architectural intent, not a description of implemented loan servicing.

## Layering and composition

Domain owns pure invariants, calculations and events. Application owns commands, queries and workflow orchestration. Infrastructure implements persistence and external-service ports defined by Application. Domain and Application never reference API, worker, AI-host or Infrastructure projects.

HTTP, MCP and batch hosts are composition roots: they register Application and Infrastructure implementations and translate transport input/output. Direct Domain references needed for composition must not become an alternate business workflow bypassing Application.

Promissio.AI remains the explicitly named agent runtime host at this stage. Its orchestration belongs in that project; extracting a reusable AI library should occur only when a second host needs it. Avoid adding empty shared projects in anticipation of reuse.

## Ownership proposals before Phase 3

- [Origination handoff](../adr/0006-origination-servicing-handoff.md): application approval versus servicing loan creation.
- [Persistence and concurrency](../adr/0007-persistence-and-concurrency.md): one authority for each fact, event versions, projection consistency and transaction boundaries.
- [Authorization](../adr/0008-single-tenant-authorization.md): authenticated actors and operation permissions within a single-tenant installation.

These proposals identify review questions instead of inventing domain events, states, role names or access policies. Accepted stack choices remain unchanged.

## Delivery sequence

Agree aggregate ownership and event schemas; implement pure lifecycle logic and transition tests; add Marten persistence and rebuild tests; then expose the same application workflows through HTTP and MCP. Batch and AI orchestration must use those workflows.

Detailed state-machine diagrams will accompany the corresponding domain implementation. Their absence today is a tracked Phase 3 requirement, not evidence that a lifecycle has already been approved.
