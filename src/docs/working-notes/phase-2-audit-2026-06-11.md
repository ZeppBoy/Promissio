# Phase 2 Audit — Payment Schedule Generator & APRC (2026-06-11)

**Scope:** `03-phase-02-schedules-aprc.md` + `00-core.md`
**Supersedes:** Verifies/extends `phase-2-audit-summary.md` (2026-06-11), which reviewed code but did **not** run the test suite.
**Audited by:** Claude Code
**Method:** Read all five generators, `AprcCalculator`, `IAprcCalculator`, `HolidayCalendar`, and all five test files; ran `dotnet test tests/Promissio.Domain.Tests --filter "FullyQualifiedName~ScheduleGeneration"`.

---

## 0. Headline Result

**The build is currently red: 7 of 48 ScheduleGeneration tests fail.**

```
Failed!  - Failed: 7, Passed: 41, Skipped: 0, Total: 48
```

This contradicts `phase-2-audit-summary.md`, which describes the implementation as having only "precision" and "verification" gaps. The "Fixes after audit" commit (`6a48448`) introduced **two regressions that broke previously-correct generators** and **one new crash bug** in the APRC calculator. None of this was caught because the prior audit reviewed code without executing `dotnet test`.

Per `CLAUDE.md` §11 (Honesty Checklist): *"All tests pass (`dotnet test`)"* is unchecked. This phase cannot be considered complete.

---

## 1. Critical Regression — `BulletScheduleGenerator` no longer implements bullet semantics

**File:** `BulletScheduleGenerator.cs`

The plan (`03-phase-02-schedules-aprc.md` Week 6, Task 1) requires: *"Implement `BulletScheduleGenerator` with interest-only periods and balloon payment."* The 2026-06-05 post-fix audit confirmed this was correct: *"Correctly holds principal at full balance; balloon fires at `i == termMonths`."*

The `6a48448` commit **replaced this with a verbatim copy of the differentiated-amortization loop** (identical to `DifferentiatedScheduleGenerator.cs` and `CustomScheduleGenerator.cs` — all three files are now byte-for-byte identical except for comments and constructor signature). It computes a standard annuity `totalPayment` and amortizes principal across all periods, just like an annuity/differentiated loan.

**Result:** A "bullet" loan now pays down principal every period instead of holding it to maturity. This is not a rounding nuance — it changes the product type entirely.

**Failing tests confirm this:**
- `ScheduleGenerationTests.BulletGenerator_ProducesBalancedSchedule` — expects `PrincipalPortion == 0` for periods 1..11, gets `813.72`.
- `ScheduleEdgeCaseTests.BulletGenerator_ZeroInterest_PaysOnlyPrincipalAtEnd` — expects `0`, gets `833.33`.

**Fix:** Restore interest-only behavior for periods `1..amortizationPeriods-1` (principal portion = 0, interest on full balance) with the full remaining balance due at the last amortization period. The grace-period and validation logic added in `6a48448` (negative/zero checks, `HolidayCalendar` parameter) can be kept — only the per-period principal/interest split needs to revert to bullet semantics.

---

## 2. Critical Regression — `CustomScheduleGenerator` ignores its own `customFlows` constructor argument

**File:** `CustomScheduleGenerator.cs`

The plan (Week 6, Task 2) requires: *"Implement `CustomScheduleGenerator` accepting predefined cash flows."* The post-fix audit confirmed: *"`CustomScheduleGenerator` | Correctly delegates to caller-supplied cash flows."*

The `6a48448` commit kept the constructor parameter `List<CustomCashFlow> customFlows` (assigned to `_customFlows`), but the `Generate` method **never reads `_customFlows`**. Instead it computes its own annuity-style schedule, identical to Bullet/Differentiated above.

```
grep -n "_customFlows" CustomScheduleGenerator.cs
15:    private readonly List<CustomCashFlow> _customFlows;
20:        _customFlows = customFlows ?? throw new ArgumentNullException(nameof(customFlows));
```

That's it — `_customFlows` is assigned and never read. Callers can pass any cash flows and they are silently discarded; the generator returns an annuity schedule instead. This is worse than a crash because it fails silently — `CustomGenerator_ProducesBalancedSchedule` and `CustomScheduleBalance_Randomized` still pass because both happen to construct flows whose total equals `principal`, and the test only checks the *sum*, not the actual per-period values returned.

**Fix:** Either restore the original "echo `_customFlows` verbatim" implementation, or — if grace/validation handling for custom schedules is now a requirement — apply grace-period/validation around the caller-supplied flows rather than discarding them. This needs a product decision (see Open Question 1 below) before re-implementing.

---

## 3. New Bug — `AprcCalculator` crashes (`OverflowException`) on long-term loans

**File:** `AprcCalculator.cs:69, 93` (`DecimalPower`)

`ScheduleEdgeCaseTests.AprcCalculator_VeryLongSchedule_Computes` (360-month / 30-year schedule) fails with:

```
System.OverflowException: Value was either too large or too small for a Decimal.
  at AprcCalculator.DecimalPower(Decimal baseValue, Int32 exponent) line 93
  at AprcCalculator.Calculate(...) line 69
```

`DecimalPower` is a naive `for` loop multiplying `decimal` values with no overflow guard. The bisection bounds are `low = -0.99m`, `high = 5.0m`. On the first iteration, `mid ≈ 2.005`, so `(1 + mid)^360 = 3.005^360`, which is ~10^171 — far beyond `decimal.MaxValue` (~7.9×10^28). Any schedule with `Period` > ~60 will overflow during early bisection iterations before the search narrows toward the true (small) rate.

**Fix:** Either (a) use `double` for the bisection's power computation (acceptable here since it's a search bound, not a final result — final `Percentage` can still be constructed from a `decimal` derived from the converged `mid`), or (b) tighten `high` adaptively / use a smarter initial bracket, or (c) catch overflow in `DecimalPower` and treat it as "PV → 0" (push `high` down). Given `00-core.md` bans `double` precision loss in domain *results*, but this is an internal search variable — using `double` for the power and casting back to `decimal` only for the final `Percentage` is the pragmatic fix, with a code comment explaining why.

---

## 4. `IAprcCalculator` self-consistency check now fails — `AprcCalculator_Annuity_MatchesReference`

**Files:** `AnnuityScheduleGenerator.cs`, `AprcCalculator.cs`

For a fee-less annuity, the payment `M` is derived so that `Σ M/(1+r)^t = P` exactly, where `r` is the nominal monthly rate. The bisection solver should therefore reconverge on `i = r` exactly, and `AprcCalculator_Annuity_MatchesReference` (10% nominal, 12 months) expects APRC ≈ EAR(10%) = 0.104715723888028.

**It now fails**: actual = `0.1044776325...`, off by `0.000238` — outside even the 1bp (`0.0001`) tolerance, let alone "four decimal places."

**Root cause:** `6a48448` changed the per-period interest calculation to use `_interestCalculator.Calculate(...)` (day-count-aware, `Actual/Actual` in the test), while `totalPayment` is still sized using the naive `rate = interestRate.Rate.Fraction / 12` (a flat 30/360-style monthly rate). These two day-count assumptions disagree slightly — `Actual/Actual` periods are not exactly 1/12 of a year — so the generated cash flows are no longer a perfect annuity at rate `r`, and the bisection finds a different `i`. The mismatch is small per period but compounds across the schedule.

This is exactly the class of bug `CLAUDE.md` Pitfall 3 warns about: *"plausible-but-wrong financial math... wrong rounding direction, off-by-one day count."* Two day-count models are now mixed within a single schedule generator.

**Fix:** Either (a) size `totalPayment` using the same day-count convention as the interest calculator (i.e., derive the periodic rate from actual period lengths rather than a flat `/12`), or (b) make the interest-portion calculation use the flat nominal monthly rate (`balance * rate/12`) to match the payment formula, and reserve `IInterestCalculator`/day-count for contexts where actual dated accrual is required (e.g., daily accrual in the batch processor, Phase 3+). Needs a product decision — see Open Question 2.

---

## 5. `AprcReferenceTests` — values are not verified EU reference cases

**File:** `AprcReferenceTests.cs`

The class doc-comment claims: *"Verification of APRC calculations against official EU reference examples. Reference: EU Consumer Credit Directive 2008/48/EC and ISDA 2006 Definitions."* `00-core.md` and `CLAUDE.md` both require citing and verifying against an authoritative source for exactly this kind of test.

Analysis of the three cases:

| Case | Principal | Rate | Term | Expected APRC | `(1+r/12)^12 - 1` (EAR) |
|------|-----------|------|------|---------------|--------------------------|
| 1 | €10,000 | 5% | 36mo | 0.052381 | **0.051162** |
| 2 | €5,000 | 8% | 24mo | 0.083056 | 0.083000 |
| 3 | €20,000 | 10% | 60mo | 0.104716 | **0.104716** |

For a fee-less annuity (no origination fees, equal monthly periods), APRC = EAR(nominal rate) **independent of principal and term** — that's the mathematical identity discussed in §4. Case 3's expected value matches EAR(10%) to 6dp, and is *identical* to `AprcCalculator_Annuity_MatchesReference`'s expected value for a completely different principal/term — strongly suggesting it was copy-derived from that same formula, not sourced from an EU document. Case 1's expected value (0.052381) does **not** match EAR(5%) (0.051162) — a ~0.12pp discrepancy that can't be explained by day-count differences alone. There is no citation, URL, or worked-example reference in the file.

**Currently failing:**
- Case 1 (5%, 36mo): expected `0.052381`, actual `0.051096`.
- Case 2 (8%, 24mo): expected `0.083056`, actual `0.082860`.
- Case 3 (10%, 60mo) currently passes, but only because it happens to be close to the (also-now-wrong, see §4) EAR identity within the 1bp tolerance.

**Fix:** Per `CLAUDE.md` §3 ("Adding an interest calculation feature"), source actual worked examples from EUR-Lex Annex I of Directive 2008/48/EC (which includes worked APRC examples) or another authoritative text, cite the source inline, and replace these three numbers. Do not hand-derive "expected" values from the same formula under test.

---

## 6. Holiday calendar — parameter wired through, but never used (dead code)

**Files:** all four generators, `IScheduleGenerator.cs`, `HolidayCalendar.cs`

`6a48448` added `HolidayCalendar? holidayCalendar = null` to `IScheduleGenerator.Generate` and to all four implementations, plus a new `HolidayCalendar` value object (`IsHoliday`, `NextBusinessDay`, `PreviousBusinessDay`, `NearestBusinessDay`).

**None of the four generators reference the `holidayCalendar` parameter inside their bodies.** `paymentDate = startDate.PlusMonths(i)` is computed unconditionally with no business-day adjustment. Week 5 Task 4 ("holiday calendar adjustments") is therefore **still not implemented** — the new parameter and value object give the *appearance* of support without the behavior, which is arguably a regression in honesty (`CLAUDE.md` Pitfall 6: don't document/imply support that doesn't exist).

Minor: `HolidayCalendar.PreviousBusinessDay` (line 40) has a stray bare `;` (empty statement) on its own line — leftover from editing, harmless but should be cleaned up. `NearestBusinessDay` computes `prev` but never uses it (always returns `next`), so `PreviousBusinessDay` is effectively dead code too.

**Fix:** Either implement the adjustment (apply `holidayCalendar?.NextBusinessDay(paymentDate)` when computing each `paymentDate`, decide whether interest accrual periods use adjusted or unadjusted dates) or remove the parameter/value object until it's actually wired up, per Pitfall 6. Given Task 4 explicitly calls for this, recommend implementing it now — it's small.

---

## 7. Short / long first period — still not implemented

**Files:** all four generators

Confirmed unchanged from the prior two audits: `paymentDate = startDate.PlusMonths(i)` for `i = 1..termMonths`. No support for a first period shorter or longer than one month (a common product feature — e.g., a loan disbursed on the 20th with payments due on the 1st of each month). Week 5 Task 4 item; carried forward as open from both prior audits.

---

## 8. What Is Correct

- `Percentage` constructor now correctly enforces non-negative invariant (resolves item 2 from `phase-2-audit-summary.md`).
- `IAprcCalculator` / `IScheduleGenerator` interfaces are clean: `NodaTime`, `Money`, `Percentage`, no banned types.
- Grace-period handling (interest-only periods, `amortizationPeriods = termMonths - gracePeriodMonths`) is correct for Annuity and Differentiated, including the zero-interest and `grace == term-1` edge cases — all related tests pass (`AnnuityGenerator_FullGrace_EqualToBullet`, `DifferentiatedGenerator_FullGrace_EqualToBullet`, zero-interest cases for Annuity/Differentiated).
- Argument validation (`gracePeriodMonths < 0`, `gracePeriodMonths >= termMonths`, zero/negative principal, negative term) is now present and tested across generators.
- `AprcCalculator` no longer has the flawed `(Money totalCost, int termMonths)` overload — the single schedule-based `Calculate` is now the only public method, addressing prior audit priority 2.
- `AprcCalculator` annualization is compound (`(1+mid)^12 - 1`), addressing Bug 3 from the first audit.
- 41/48 ScheduleGeneration tests pass; randomized balance tests (`ScheduleGenerators_RandomizedBalanceInvariant`, `*BalanceRandomized`, 200 configs each) pass for Annuity, Differentiated, Bullet (only because Bullet's broken amortization still happens to sum to principal), and Custom.
- `Verify.Xunit` snapshot tests exist for all four schedule types and currently pass (no `.verified.txt` committed yet — see Open Question 3).

---

## 9. Acceptance Criteria — Status

### `03-phase-02-schedules-aprc.md`

| Criterion | Status |
|-----------|--------|
| Invariant checks pass for ≥100 random configs | ✅ Present (200 configs/generator) — but Bullet's invariant is checked against *broken* semantics |
| Verify.Xunit snapshot tests for canonical examples | ⚠️ Present but no `.verified.txt` committed (see Open Question 3) |
| APRC matches EU reference examples to 4dp | ❌ 2 of 3 reference cases fail; values themselves unverified (§5) |
| All generators produce balanced schedules | ⚠️ Numerically balanced, but Bullet/Custom are balanced *for the wrong product type* |

### `00-core.md`

| Required | Status |
|----------|--------|
| `IAprcCalculator` interface | ✅ |
| `DayCountConvention` wired into generators via `IInterestCalculator` | ⚠️ Wired for interest portions, but payment-sizing formula still uses flat `/12` — inconsistency causes §4 |
| Schedule types: Annuity, Differentiated, Bullet, Custom | ❌ Bullet and Custom no longer implement their documented semantics |

---

## 10. Remaining Work — Priority Order

| Priority | Item | Rationale |
|----------|------|-----------|
| 1 | Restore `BulletScheduleGenerator` interest-only/balloon semantics | Product-type regression; 2 failing tests |
| 2 | Restore `CustomScheduleGenerator` use of `_customFlows` | Silent data-loss regression; constructor API is misleading |
| 3 | Fix `AprcCalculator.DecimalPower` overflow for long schedules | Crashes on any 30-year loan |
| 4 | Reconcile day-count used for payment sizing vs. interest accrual in Annuity/Differentiated | Breaks the APRC self-consistency identity (§4) |
| 5 | Replace `AprcReferenceTests` with cited, verified EU examples | Acceptance criterion; current numbers unverifiable and 2/3 fail |
| 6 | Implement holiday-calendar business-day adjustment or remove the dead parameter/value object | Avoid implying unimplemented behavior (Pitfall 6) |
| 7 | Short/long first period support | Carried from Weeks 5/6 Task 4, deferred twice already |
| 8 | Commit `.verified.txt` snapshot baselines | So snapshot tests actually guard against regressions like §1/§2 in the future |

---

## 11. Open Questions for Human Review

1. **Custom schedule + grace/validation**: should `CustomScheduleGenerator` simply echo `_customFlows` (original behavior), or should it apply grace-period/validation logic on top of caller-supplied flows? The original implementation predates the grace-period and validation work added in `6a48448`.
2. **Day-count consistency**: should annuity/differentiated payment sizing use the same day-count convention as interest accrual (more correct, but changes payment amounts vs. the simple `rate/12` formula used today), or should interest accrual be simplified back to flat monthly for consistency with payment sizing? This decision affects both schedule output and APRC accuracy.
3. **Snapshot baselines**: `ScheduleSnapshotTests` currently pass with no committed `.verified.txt` files. Confirm whether Verify is auto-accepting on first run in this environment, and if so, commit the baselines so future changes are actually checked against them — otherwise these tests provide no regression protection.
4. **Holiday calendar source**: carried from prior audits — confirm calendar source (TARGET2 vs. local) before implementing §6.

---

## 12. Process Note

`phase-2-audit-summary.md` (committed alongside this audit) appears to have been produced by reviewing the diff/code without running `dotnet test`. Per `CLAUDE.md` §1 ("Be precise about uncertainty") and §11 (Honesty Checklist), any audit or "fixes complete" claim for this codebase should include the actual `dotnet test` output. Recommend treating "tests pass" as a claim that must be demonstrated, not asserted, in future Phase 2 work.
