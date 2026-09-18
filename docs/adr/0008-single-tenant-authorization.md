# ADR-0008: Single-tenant authorization scope

**Status:** Proposed. Explicit security review required before exposing operations.

## Context

The project excludes multi-tenant SaaS, but its MCP acceptance criteria previously referred to cross-tenant isolation. Authentication by an AI client cannot substitute for authorization in Promissio.

## Proposed decision

Keep a single-tenant deployment. Require authenticated actors and server-side permission checks for each exposed business operation. Treat MCP and HTTP as alternate transports into the same authorized workflows. Tool descriptions and prompts do not grant permissions.

Distinguish read, simulation and state-changing capabilities. Preserve auditable actor context and redact sensitive arguments. Reject unauthenticated calls and calls outside the actor's allowed scope.

## Questions to resolve

- Which identity provider, credential types and trust boundaries apply?
- Which roles or permissions are required for each operation?
- Is access limited by portfolio, assignment or another business boundary?
- Which state changes require human approval, and how is that approval recorded?
- How are service identities, delegated calls, revocation and denial auditing handled?

## Consequences

Remove misleading cross-tenant requirements from the active single-tenant roadmap, while retaining strict operation authorization. No existing security check is disabled and no new access policy is implemented by this record.
