using Promissio.Infrastructure;
using Xunit;

namespace Promissio.Infrastructure.Tests;

public class InfrastructureTests
{
    [Fact]
    public void TestInfrastructureService()
    {
        var service = new InfrastructureService();
        Assert.NotNull(service);
    }
}
