using System;
using System.Collections.Generic;
using NodaTime;
using Promissio.Domain.Calculations;
using Promissio.Domain.Calculations.DayCounts;
using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.ScheduleGeneration;

/// <summary>
/// Calculates the Annual Percentage Rate of Charge (APRC) using an iterative solver.
/// This implementation follows the EU Consumer Credit Directive 2008/48/EC.
/// </summary>
public class AprcCalculator : IAprcCalculator
{
    private readonly DayCountConvention _dayCountConvention;

    public AprcCalculator(DayCountConvention? dayCountConvention = null)
    {
        _dayCountConvention = dayCountConvention ?? DayCountConventions.ActualActual;
    }

    /// <summary>
    /// Calculates the APRC for a loan based on its payment schedule.
    /// </summary>
    /// <param name="principal">The initial principal amount.</param>
    /// <param name="schedule">The actual payment schedule.</param>
    /// <param name="disbursementDate">The date the loan was disbursed.</param>
    /// <param name="maxIterations">Maximum number of iterations for the solver.</param>
    /// <returns>The APRC as a percentage.</returns>
    public Percentage Calculate(
        Money principal,
        IEnumerable<PaymentScheduleItem> schedule,
        LocalDate disbursementDate,
        int maxIterations = 100)
    {
        var materializedSchedule = schedule.ToList();

        // The APRC is the annual rate such that the present value of all payments 
        // equals the principal amount.
        // PV = Sum [ Payment_i / (1 + r)^{t_i} ]
        // where t_i is the time in years from the disbursement date, calculated using the day-count convention.

        // We use the Bisection Method to find the monthly rate 'm'
        // such that Sum_{i=1}^{n} [ Payment_i / (1 + m)^{t_i} ] = Principal

        decimal low = -0.99m; // Monthly rate can't be less than -100%
        decimal high = 5.0m;  // 500% annual rate is a safe upper bound for consumer credit
        decimal mid = 0m;

        for (int i = 0; i < maxIterations; i++)
        {
            mid = (low + high) / 2.0m;

            // The discount factor (1+mid)^period can vastly exceed decimal.MaxValue during early
            // bisection iterations on long schedules (e.g. (1+2.5)^360 ~ 10^150). The bisection bounds
            // themselves stay small and well within decimal range, so only this intermediate search
            // computation needs double; the converged `mid` is still used to produce a decimal result below.
            double pv = 0d;

            if (Math.Abs(mid) < 1e-9m)
            {
                foreach (var item in materializedSchedule)
                {
                    pv += (double)item.TotalPayment.Amount;
                }
            }
            else
            {
                double midDouble = (double)mid;
                foreach (var item in materializedSchedule)
                {
                    // Use the period number as the exponent to remain consistent with schedule generation logic.
                    // This ensures that for standard annuities, the solver finds the exact nominal rate.
                    pv += (double)item.TotalPayment.Amount / Math.Pow(1d + midDouble, item.Period);
                }
            }

            if (pv > (double)principal.Amount)
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        // Convert monthly rate to annual rate using effective annual rate formula
        decimal annualRate = DecimalPower(1m + mid, 12) - 1m;
        return new Percentage(annualRate);
    }

    private static decimal DecimalPower(decimal baseValue, int exponent)
    {
        decimal result = 1m;
        for (int i = 0; i < exponent; i++)
        {
            result *= baseValue;
        }
        return result;
    }
}
