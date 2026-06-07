# Phase 2 Audit — Extended Summary

## 📊 Executive Overview
The implementation successfully establishes the structural skeleton required by Phase 2. All four major schedule types (**Annuity, Differentiated, Bullet, and Custom**) are present, and the **APRC (Annual Percentage Rate of Charge)** calculator correctly utilizes the bisection method as specified. However, the audit reveals that while the "shape" of the code is correct, the **financial precision and verification** do not yet meet the production-grade standards defined in `AGENTS.md`.

---

## 🔍 Key Findings & Discrepancies

### 1. Mathematical & Verification Failures (Critical)
*   **Incorrect Test Assertions:** The most significant issue is that the APRC tests are failing because they are mathematically impossible. They expect a 10% nominal rate to result in a 10% APRC. In reality, the compounding effect of monthly payments makes the APRC ~10.47%. The calculator is correct; the tests are wrong.
*   **Rounding Drift:** The `AnnuityScheduleGenerator` suffers from cumulative rounding errors. Over long loan terms, the sum of individual period portions drifts away from the original principal, causing randomized property tests to fail.
*   **Lack of Authoritative Data:** The plan mandates validation against **official EU reference examples**, but the codebase currently relies on self-generated test cases which may not reflect regulatory reality.

### 2. Architectural & Domain Rule Violations
*   **Constructor Invariants:** The `Percentage` value object violates core project rules by allowing negative values through its primary constructor (validation is only present in factory methods).
*   **Precision Risks:** The use of `(decimal)Math.Pow((double)...)` in the annuity formula introduces a `double` cast, which is a high-risk pattern for financial software where `decimal` precision is required.
*   **Weak API Typing:** The use of raw `int` for `termMonths` and `gracePeriodMonths` in the `IScheduleGenerator` interface fails to enforce domain constraints at the boundary.

### 3. Implementation Gaps (Planned vs. Actual)
*   **Holiday Calendars:** Completely missing from the current implementation despite being a planned requirement.
*   **First Period Logic:** The system currently assumes all periods are exactly one month apart (`PlusMonths(i)`), failing to account for "short" or "long" first periods common in real-world loans.
*   **Incomplete Snapshot Coverage:** Only the Annuity schedule has snapshot tests; Differentiated, Bullet, and Custom schedules lack these "gold standard" verifications.

---

## 🛠 Proposed Remediation Roadmap

| Priority | Action Item | Impact |
| :--- | :--- | :--- |
| **🔴 Critical** | **Correct APRC Test Expectations** | Fixes false-negative test results by updating expected values to ~10.47%. |
| **🔴 Critical** | **Annuity Rounding Fix** | Ensures the final payment absorbs the rounding residual to maintain principal balance. |
| **🟡 Important** | **EU Reference Data Integration** | Imports official EU examples to validate the APRC calculator against regulatory standards. |
| **🟡 Important** | **Precision & Invariant Fixes** | Removes `double` casts and enforces non-negative constraints in `Percentage` constructors. |
| **🟢 Nice to Have** | **Holiday & First Period Support** | Completes the final requirements of the Phase 2 plan. |
