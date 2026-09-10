namespace Promissio.Domain.ScheduleGeneration;

/// <summary>The contractual repayment frequency used to measure APRC time intervals.</summary>
public enum AprcPeriodUnit
{
    /// <summary>Equal months plus any residual days.</summary>
    Months,
    /// <summary>Equal years plus any residual days.</summary>
    Years,
    /// <summary>Equal weeks plus any residual days.</summary>
    Weeks
}
