using FluentValidation;
using Promissio.Application;

namespace Promissio.Application.Tests;

public class ApplicationTests
{
    [Fact]
    public void TestLoanApplicationValidator()
    {
        var validator = new LoanApplicationValidator();
        Assert.NotNull(validator);
    }
}