namespace Promissio.Domain.Calculations.DayCounts;

/// <summary>
/// Provides static instances of common day-count conventions.
/// </summary>
public static class DayCountConventions
{
    /// <summary>
    /// Actual/Actual - Counts actual days and divides by actual days in the year.
    /// Used in US Treasury bonds and some interbank markets.
    /// </summary>
    public static readonly DayCountConvention ActualActual = new ActualActual();

    /// <summary>
    /// Actual/365 - Counts actual days and divides by 365.
    /// Used in UK and some European markets.
    /// </summary>
    public static readonly DayCountConvention Actual365 = new Actual365();

    /// <summary>
    /// Actual/360 - Counts actual days and divides by 360.
    /// Used in US money markets and LIBOR-based products.
    /// </summary>
    public static readonly DayCountConvention Actual360 = new Actual360();

    /// <summary>
    /// 30/360 - Assumes 30-day months and 360-day years.
    /// Used in US corporate bonds.
    /// </summary>
    public static readonly DayCountConvention Thirty360 = new Thirty360();

    /// <summary>
    /// 30/360 European (ISDA) - European variant of 30/360.
    /// Used in European bonds.
    /// </summary>
    public static readonly DayCountConvention Thirty360European = new Thirty360European();
}
