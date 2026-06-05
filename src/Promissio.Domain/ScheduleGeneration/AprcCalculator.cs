using System;
using System.Collections.Generic;
using Promissio.Domain.ValueObjects;
using NodaTime;

namespace Promissio.Domain.ScheduleGeneration;

/// <summary>
/// Calculates the Annual Percentage Rate of Charge (APRC) using an iterative solver.
/// This implementation follows the EU Consumer Credit Directive 2008/48/EC.
/// </summary>
public class AprcCalculator : IAprcCalculator
{
    /// <summary>
    /// Calculates the APRC for a loan.
    /// </summary>
    /// <param name="principal">The initial principal amount.</param>
    /// <param name="totalCost">The total cost of the credit (sum of all payments minus principal).</param>
    /// <param name="termMonths">The total term of the loan in months.</param>
    /// <param name="startDate">The date of the first payment.</param>
    /// <param name="maxIterations">Maximum number of iterations for the solver.</param>
    /// <returns>The APRC as a percentage.</returns>
    [Obsolete("Use Calculate(Money principal, IEnumerable<PaymentScheduleItem> schedule, int maxIterations = 100) instead. The old overload assumes equal payments and is less precise.")]
    public Percentage Calculate(
        Money principal,
        Money totalCost,
        int termMonths,
        LocalDate startDate,
        int maxIterations = 100)
    {
        // The APRC is the annual rate such that the present value of all payments 
        // equals the principal amount.
        // PV = Sum [ Payment_i / (1 + r/12)^i ]
        // where r is the annual APRC.
        
        // Since we are given the total cost, we can assume a simplified model where 
        // payments are equal (annuity-like) for the purpose of the APRC calculation 
        // unless a specific schedule is provided.
        
        decimal totalPayment = principal.Amount + totalCost.Amount;
        decimal periodicPayment = totalPayment / termMonths;
        
        // We use the Bisection Method to find the monthly rate 'm'
        // such that Sum_{i=1}^{n} [ P / (1 + m)^i ] = Principal
        
        double low = -0.99; // Monthly rate can't be less than -100%
        double high = 5.0;  // 500% annual rate is a safe upper bound for consumer credit
        double mid = 0;
        
        for (int i = 0; i < maxIterations; i++)
        {
            mid = (low + high) / 2.0;
            double pv = 0;
            
            // Calculate Present Value of an annuity
            if (Math.Abs(mid) < 1e-9)
            {
                pv = (double)periodicPayment * termMonths;
            }
            else
            {
                // PV = P * [1 - (1 + m)^-n] / m
                pv = (double)periodicPayment * (1 - Math.Pow(1 + mid, -termMonths)) / mid;
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
        
        decimal annualRate = (decimal)(Math.Pow(1.0 + mid, 12.0) - 1.0);
        return new Percentage(annualRate);
    }

    /// <summary>
    /// Calculates the APRC for a loan based on its payment schedule.
    /// </summary>
    /// <param name="principal">The initial principal amount.</param>
    /// <param name="schedule">The actual payment schedule.</param>
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
        // PV = Sum [ Payment_i / (1 + r)^t_i ]
        // where t_i is the time in years from the disbursement date.
        
        // We use the Bisection Method to find the monthly rate 'm'
        // such that Sum_{i=1}^{n} [ Payment_i / (1 + m)^{t_i} ] = Principal
        // Note: Since we are solving for a monthly rate 'm', the exponent t_i 
        // must also be expressed in months.
        
        double low = -0.99; // Monthly rate can't be less than -100%
        double high = 5.0;  // 500% annual rate is a safe upper bound for consumer credit
        double mid = 0;
        
        for (int i = 0; i < maxIterations; i++)
        {
            mid = (low + high) / 2.0;
            double pv = 0;
            
            if (Math.Abs(mid) < 1e-9)
            {
                foreach (var item in materializedSchedule)
                {
                    pv += (double)item.TotalPayment.Amount;
                }
            }
            else
            {
                foreach (var item in materializedSchedule)
                {
                    // Calculate the number of months between disbursement and payment
                    // Using a simple month difference for now, but this will be 
                    // replaced by DayCountConvention logic later.
                    int months = (int)(item.PaymentDate.Year - disbursementDate.Year) * 12 + (item.PaymentDate.Month - disbursementDate.Month);
                    pv += (double)item.TotalPayment.Amount / Math.Pow(1 + mid, months);
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
        
        decimal annualRate = (decimal)(Math.Pow(1.0 + mid, 12.0) - 1.0);
        return new Percentage(annualRate);
    }
}
