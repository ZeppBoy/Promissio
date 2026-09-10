# ADR-0006: Origination-to-servicing handoff

**Status:** Proposed. Domain owner review required before Phase 3 implementation.

## Context

The target model contains LoanApplication and Loan, while the phase plan previously placed approval on Loan without explaining how application approval relates to it.

## Proposed decision

LoanApplication owns the underwriting decision. Application-layer orchestration creates or activates the servicing Loan from approved, immutable terms. Repeated delivery of the handoff must not create another loan. Domain aggregates remain independent of persistence and each other.

## Questions to resolve

- At what business milestone does a servicing Loan begin to exist: approval, contract acceptance or disbursement?
- Is approval itself an event on LoanApplication, and what terms must the handoff carry?
- What identifier and uniqueness rule make the handoff idempotent?
- What happens when approved terms change before disbursement?

Do not choose state names, transitions, identifiers or event schemas from this proposal. The accepted decision must be accompanied by domain diagrams and test scenarios.

## Consequences

Separating underwriting ownership from servicing avoids two aggregates independently approving the same transaction. Cross-aggregate coordination, retries and failure recovery belong in Application and its persistence implementation.
