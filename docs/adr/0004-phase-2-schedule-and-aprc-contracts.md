# ADR-0004: Phase 2 schedule and APRC contracts

**Status:** Accepted — financial contracts approved by the owner on 2026-09-08; implementation, worked examples, rounding and date policies, reference interpretation, and scope exclusions reviewed and signed off by the owner on 2026-09-18. This approval is product acceptance, not legal or regulatory certification.

## Context

Schedule strategies had regressed into copies of annuity logic. Custom inputs and calendars were ignored, and APRC discounted period labels rather than dates. The apparent snapshot passes did not await Verify. Existing APRC expectations lacked identifiable worked examples.

## Decision

- Annuity payment sizing and posted interest both call the same contractual interest calculator. The search operates on cent-denominated payments and finds the smallest payment that covers interest and retires principal by maturity. No unpaid interest is silently capitalized.
- Differentiated schedules repay equal principal portions; bullet schedules retain principal until maturity. Final repayments settle the remaining balance exactly.
- Custom flows retain their supplied amounts and optional dates. Grace is validated against the supplied principal portions, never inserted. The number of flows must match the instalment count, currencies must agree and principal must balance exactly.
- A supplied holiday calendar moves generated payment dates forward across weekends and supplied holidays. Accrual retains unadjusted contractual dates. Explicit custom dates are preserved; conflicting calendar inputs are rejected.
- An optional first contractual payment date supports short or long first periods. Subsequent monthly dates are anchored to it; without it, dates remain anchored to disbursement.
- APRC solves the single-drawdown, known-payment present-value equation using payment dates. Its time measurement follows Commission SWD(2012)128, section 4.1.1, independently of the contractual interest convention.
- APRC expected failures have a result-returning entry point. The existing Calculate method remains a throwing convenience wrapper for trusted callers.

## Consequences

The rounding residual can reduce the final payment. Small balances or the requirement to cover interest in long, high-rate schedules can settle principal early; subsequent instalments are zero. The schedule never collects principal beyond the original balance. This is a bounded non-negative-amortization contract, not support for capitalizing unpaid interest.

No national calendar is fabricated. A null calendar means no adjustment. A calendar contains immutable supplied holidays and treats Saturday and Sunday as closed.

The old optional APRC DayCountConvention constructor is replaced by an AprcPeriodUnit constructor (monthly by default, with annual and weekly options). This is a source-level change for explicit convention callers; none exist in this repository. Returning a rate with an arbitrary accrual convention would misleadingly imply regulatory APRC semantics. PaymentScheduleItem adds ContractualDate to distinguish accrual from payment timing.

APRC supports known non-negative payments, including immediate charges, and a single positive drawdown. Negative rates remain outside Percentage's existing contract. Missing charges, multiple drawdowns, unknown future product assumptions and legal disclosure rounding are not inferred. Internal floating-point discount factors do not post Money; independent decimal calculations verify the published examples.

No tests are removed. Unsupported reference expectations are replaced by cited Commission examples, and the original loan configurations remain as present-value regression tests. The nominal/EAR identity tests now explicitly use regular 30E/360 periods, where that identity applies.

## References

- [ADR-0003](0003-interest-rate-owns-day-count-convention.md)
- [Calculation rules and verification](../domain/payment-schedules.md)
- [Commission time-interval guidance](https://www.mfcr.cz/assets/attachments/EU-MFCR_Metodika_2012_128-Guidelines-consumer-credit-directive-swd-en.pdf)

The owner completed the required human review and edit on 2026-09-18. This does not constitute legal or regulatory certification.
