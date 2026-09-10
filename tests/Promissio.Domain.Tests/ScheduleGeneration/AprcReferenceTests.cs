using FluentAssertions;
using NodaTime;
using Promissio.Domain.Calculations;
using Promissio.Domain.Calculations.DayCounts;
using Promissio.Domain.ScheduleGeneration;
using Promissio.Domain.ValueObjects;
using Xunit;

namespace Promissio.Domain.Tests.ScheduleGeneration;

/// <summary>
/// Published numerical examples 2–5, Annex II, COM(2005)483 final/2, pages 59–61.
/// https://eur-lex.europa.eu/LexUriServ/LexUriServ.do?uri=COM:2005:0483:FIN:EN:PDF
/// These are Commission proposal examples, not worked examples in the enacted 2008 directive.
/// Synthetic dated regressions are labelled separately.
/// </summary>
public sealed class AprcReferenceTests
{
    [Theory]
    [InlineData("6000", "149.31", "0.09380593")]
    [InlineData("5940", "149.31", "0.09954966")]
    [InlineData("6000", "150.56", "0.09856689")]
    [InlineData("5940", "152.31", "0.111070115")]
    public void PublishedCommissionExamples_MatchToFourDecimalPercentagePlaces(
        string netDrawdown, string payment, string expectedFraction)
    {
        // The source specifies relative months; this synthetic date anchors those intervals.
        LocalDate start = new(2024, 1, 1);
        Money total = new(Parse(payment), "EUR");
        PaymentScheduleItem[] schedule = Enumerable.Range(1, 48)
            .Select(i => new PaymentScheduleItem(i, start.PlusMonths(i), Money.Zero("EUR"), total, total)).ToArray();
        Percentage actual = new AprcCalculator().Calculate(new Money(Parse(netDrawdown), "EUR"), schedule, start);
        actual.Fraction.Should().BeApproximately(Parse(expectedFraction), 0.0000005m);
    }

    [Theory]
    [InlineData(10000, "0.05", 36)]
    [InlineData(5000, "0.08", 24)]
    [InlineData(20000, "0.10", 60)]
    public void OriginalLoanConfigurations_SatisfyPresentValueEquation(int amount, string fraction, int months)
    {
        // Retain the old configurations as synthetic integration regressions; their former
        // claimed "EU" expected rates had no identifiable source.
        LocalDate start = new(2024, 1, 1);
        Money principal = new(amount, "EUR");
        PaymentScheduleItem[] schedule = new AnnuityScheduleGenerator(new InterestCalculator())
            .Generate(principal, new FixedRate(new Percentage(Parse(fraction)), DayCountConventions.ActualActual),
                months, start).ToArray();
        Percentage aprc = new AprcCalculator().Calculate(principal, schedule, start);
        double pv = schedule.Sum(p => (double)p.TotalPayment.Amount /
            Math.Pow(1 + (double)aprc.Fraction, p.Period / 12d));
        pv.Should().BeApproximately(amount, 0.000001);
    }

    private static decimal Parse(string value) => decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
}
