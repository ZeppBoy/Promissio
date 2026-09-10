# Payment schedules and APRC

The contracts in [ADR-0004](../adr/0004-phase-2-schedule-and-aprc-contracts.md) were approved for Phase 2 stabilization.

## Schedule calculations

For contractual dates d(i-1), d(i), opening balance B(i-1), and the supplied interest rate:

    I(i) = IInterestCalculator.Calculate(B(i-1), rate, d(i-1), d(i))
    B(i) = B(i-1) - principal(i)
    payment(i) = principal(i) + I(i)

All balances and payments use Money arithmetic and its existing two-decimal ToEven rounding. Grace instalments pay interest only.

Annuity sizing searches the cent grid using this same recurrence. A candidate must cover each interest charge and repay the balance by the last instalment. The bounds are zero and principal plus the maximum full-balance period interest. Bisection narrows to adjacent cents, retaining the feasible upper bound. Payments are capped by the remaining balance plus interest, so there is no overpayment; small balances or the interest-coverage constraint can produce early settlement and trailing zero instalments.

Differentiated principal is principal divided by the number of amortizing instalments, rounded by Money; the residual is paid at maturity. Bullet principal is zero before the final instalment. Custom amounts are passed through after validation; they are never recomputed from a rate.

These are implementation contracts derived from the approved product behavior. They are not claimed to be regulatory examples. The existing [interest engine](interest-calculation-engine.md) and [day-count conventions](day-count-conventions.md) provide the referenced accrual formulas.

## Independently worked schedule checks

Synthetic cases use EUR, 12% annual interest, 30E/360, and equal monthly periods beginning 2024-01-01. Thus each period has a 1% accrual factor.

| Strategy | Principal / periods | Principal portions | Interest portions |
|---|---|---|---|
| Annuity | 1,000 / 1 | 1,000 | 10 |
| Annuity | 1,000 / 2 | 497.51; 502.49 | 10; 5.02 |
| Annuity | 1,200 / 3 | 396.03; 399.99; 403.98 | 12; 8.04; 4.04 |
| Differentiated | 1,200 / 3 | 400; 400; 400 | 12; 8; 4 |
| Bullet | 1,200 / 3 | 0; 0; 1,200 | 12; 12; 12 |

For example, the three-period annuity pays 408.03, 408.03 and 408.02. After the first payment the balance is 803.97; its next interest is 8.04. The final balance is 403.98 and its interest is 4.04. These independently worked expectations are asserted in Phase2ContractTests.

The four canonical snapshots use the pre-existing USD 10,000 fixtures. An independent Python Decimal calculation checked every row, accrual period, principal sum and total before the baselines were accepted. The dated annuity's first eleven payments are 879.06 and its final payment is 879.04.

## Dates and calendars

termMonths is the instalment count, including grace. firstPaymentDate overrides the first contractual due date, permitting a short or long first period. Later dates are anchored to that date, rather than repeatedly advancing a February-clamped date. With no override, dates are anchored to disbursement.

ContractualDate is the unadjusted accrual end; PaymentDate is the payable date. A supplied calendar advances generated payable dates to the following business day without extending accrual. No calendar means no adjustment. Explicit custom dates take precedence and are not silently moved; a supplied calendar that disagrees with them produces a validation error.

## APRC equation and scope

For one drawdown P and known payments C(i), solve:

    P = sum(C(i) / (1 + X)^t(i))

X is the annual rate. Time intervals use the selected monthly, annual or weekly basis. Whole periods are counted backwards from each payment, with residual days divided by the length of the preceding year at the residual boundary. This differs from splitting accrual across calendar years. See [SWD(2012)128 section 4.1.1](https://www.mfcr.cz/assets/attachments/EU-MFCR_Metodika_2012_128-Guidelines-consumer-credit-directive-swd-en.pdf).

TryCalculate reports invalid inputs, unsupported negative rates, absence of a finite root, unrepresentable rates and iteration exhaustion. Calculate throws on those failures for compatibility. Money remains decimal; normalized discount factors and the annual-rate search use double to avoid decimal power overflow. Success requires both a normalized PV residual at most 1e-12 and a rate bracket at most 1e-10 times max(1, rate). Results are not rounded for legal disclosure.

Payments must include the applicable known charges. An upfront fee may be represented as an immediate payment against the gross drawdown, or subtracted from the drawdown, but never both. The calculator does not infer regulatory assumptions for open-ended products or future unknown charges.

## Published numerical references

[COM(2005)483 final/2, Annex II, examples 2–5, printed pages 59–60](https://eur-lex.europa.eu/LexUriServ/LexUriServ.do?uri=COM:2005:0483:FIN:EN:PDF) provides the following monthly cases. This is a Commission proposal containing worked examples, not the enacted 2008 directive. Each has 48 monthly payments.

| Example | Net EUR drawdown | Monthly total | Published APR (%) | Independent 50-digit Decimal Newton calculation (%) |
|---|---:|---:|---:|---:|
| 2 | 6,000 | 149.31 | 9.380593 | 9.38059288301973 |
| 3 | 5,940 | 149.31 | 9.954966 | 9.95496530393217 |
| 4 | 6,000 | 150.56 | 9.856689 | 9.85668859823762 |
| 5 | 5,940 | 152.31 | 11.1070115 | 11.10701466231423 |

Tests use a fractional-rate tolerance of 0.0000005 (half a unit at four decimal percentage places), accommodating the source's displayed precision. The source later repeats example 5 with a different figure in example 14; tests cite example 5 itself. Tests also preserve the original unsourced loan configurations as equation-residual checks.

The dated tests cover ten published monthly time intervals and three annual intervals from SWD(2012)128. Synthetic single-payment inversions isolate interval handling from schedule generation.

## Remaining assurance

Passing these checks is not a certification of all consumer-credit products or regulatory disclosure requirements. Domain coverage and mutation-score targets need separate measurement. Application, integration and AI evaluation projects remain placeholders. Human review of these financial contracts, code and documentation remains part of merge review.
