# ADR-0006: Origination-to-servicing handoff

**Status:** Accepted 2026-09-16.

## Context

The target model contains LoanApplication and Loan, while the phase plan previously placed approval on Loan without explaining how application approval relates to it.

## Decision

LoanApplication owns the underwriting decision. Application-layer orchestration creates or activates the servicing Loan from approved, immutable terms. Repeated delivery of the handoff must not create another loan. Domain aggregates remain independent of persistence and each other.

The servicing `Loan` is created only at confirmed disbursement, using the exact approved and borrower-accepted terms version. Approved terms are immutable per version. Changes before disbursement create a new terms version and require fresh approval and borrower acceptance.

`LoanId` is a strongly typed `Guid`. `LoanApplicationId` is the handoff idempotency key and permits exactly one servicing loan per application. An identical retry returns the existing `LoanId`; a retry with a different terms version or disbursement payload returns a conflict. The idempotency outcome and initial loan stream are committed atomically.

The existing Application-layer `LoanApplication` record remains a transient DTO. A future Domain `LoanApplication` aggregate owns underwriting; that aggregate is not introduced by this decision.

## Consequences

Separating underwriting ownership from servicing avoids two aggregates independently approving the same transaction. Cross-aggregate coordination, retries and failure recovery belong in Application and its persistence implementation.
