using Microsoft.Extensions.DependencyInjection;
using Promissio.Domain;

namespace Promissio.Domain.Tests;

public class DomainTests
{
    [Fact]
    public void TestDomainService()
    {
        var service = new DomainService();
        var currentDate = service.GetCurrentDate();
        
        Assert.NotNull(currentDate);
    }
}