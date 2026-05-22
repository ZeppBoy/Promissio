using FluentValidation;

namespace Promissio.Application;

public class LoanApplicationValidator : AbstractValidator<LoanApplication>
{
    public LoanApplicationValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Loan amount must be positive");
            
        RuleFor(x => x.TermInMonths)
            .GreaterThan(0)
            .WithMessage("Loan term must be greater than zero");
    }
}

public record LoanApplication(decimal Amount, int TermInMonths);