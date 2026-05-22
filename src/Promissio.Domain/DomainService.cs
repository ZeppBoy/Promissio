using NodaTime;

namespace Promissio.Domain;

public class DomainService
{
    public LocalDate GetCurrentDate()
    {
        return SystemClock.Instance.Now.Date;
    }
}