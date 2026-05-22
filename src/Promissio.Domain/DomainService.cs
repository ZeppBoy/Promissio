using NodaTime;

namespace Promissio.Domain;

public class DomainService
{
    public LocalDate GetCurrentDate()
    {
        return new LocalDate(2026, 5, 22);
    }
}