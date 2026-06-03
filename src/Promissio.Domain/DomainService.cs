using NodaTime;

namespace Promissio.Domain;

/// <summary>
/// Domain-level service for cross-cutting concerns.
/// </summary>
/// <remarks>
/// The business time zone must be provided explicitly — never assume UTC.
/// In the EU consumer credit context, the relevant time zone is typically the
/// jurisdiction of the lender (e.g., Europe/Berlin, Europe/Warsaw).
/// </remarks>
public class DomainService
{
    private readonly IClock _clock;
    private readonly DateTimeZone _businessTimeZone;

    public DomainService(IClock clock, DateTimeZone businessTimeZone)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _businessTimeZone = businessTimeZone ?? throw new ArgumentNullException(nameof(businessTimeZone));
    }

    /// <summary>
    /// Returns the current business date in the configured business time zone.
    /// </summary>
    public LocalDate GetCurrentDate() =>
        _clock.GetCurrentInstant().InZone(_businessTimeZone).Date;
}
