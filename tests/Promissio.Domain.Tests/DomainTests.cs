using Promissio.Domain;

namespace Promissio.Domain.Tests;

public class DomainTests
{
    [Fact]
    public void TestDomainService()
    {
        var service = new DomainService();
        Assert.NotNull(service.GetCurrentDate());
    }
}