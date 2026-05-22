using Microsoft.Extensions.DependencyInjection;
using Promissio.Application;

namespace Promissio.Application.Tests;

public class ApplicationTests
{
    [Fact]
    public void TestApplicationService()
    {
        var validator = new LoanApplicationValidator();
        Assert.NotNull(validator);
    }
}