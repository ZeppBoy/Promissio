using System.Text.Json;
using FluentAssertions;
using NodaTime;
using Promissio.Domain.Calculations.DayCounts;
using Promissio.Domain.ValueObjects;
using Promissio.Domain.ValueObjects.Converters;
using Xunit;

namespace Promissio.Domain.Tests;

public sealed class DomainQualityGateTests
{
    [Fact]
    public void Result_Success_ExposesValueAndFormatsOutcome()
    {
        Result<string> result = Result<string>.Success("approved");

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Value.Should().Be("approved");
        result.ToString().Should().Be("Success(approved)");
    }

    [Fact]
    public void Result_Failure_ExposesErrorAndRejectsValueAccess()
    {
        Result<string> result = Result<string>.Failure("declined");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("declined");
        result.ToString().Should().Be("Failure(declined)");
        Action readValue = () => _ = result.Value;
        readValue.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot access Value of a failed result. Check IsSuccess first.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Result_Failure_RejectsMissingError(string? error)
    {
        Action create = () => Result<string>.Failure(error!);

        create.Should().Throw<ArgumentException>().WithParameterName("error");
    }

    [Fact]
    public void MoneyConverter_RoundTripsMoneyUsingDocumentedShape()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new MoneyConverter());
        var value = new Money(123.45m, "EUR");

        string json = JsonSerializer.Serialize(value, options);
        Money? restored = JsonSerializer.Deserialize<Money>(json, options);

        json.Should().Be("{\"amount\":123.45,\"currency\":\"EUR\"}");
        restored.Should().Be(value);
    }

    [Fact]
    public void MoneyConverter_RejectsMissingCurrency()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new MoneyConverter());

        Action deserialize = () => JsonSerializer.Deserialize<Money>("{\"amount\":10,\"currency\":null}", options);

        deserialize.Should().Throw<JsonException>().WithMessage("Currency is required.");
    }

    [Fact]
    public void DomainService_UsesConfiguredClockAndBusinessTimeZone()
    {
        Instant instant = Instant.FromUtc(2024, 1, 1, 23, 30);
        var service = new DomainService(new TestClock(instant), DateTimeZoneProviders.Tzdb["Europe/Warsaw"]);

        service.GetCurrentDate().Should().Be(new LocalDate(2024, 1, 2));
    }

    [Fact]
    public void DomainService_RejectsNullDependencies()
    {
        Action nullClock = () => new DomainService(null!, DateTimeZone.Utc);
        Action nullZone = () => new DomainService(SystemClock.Instance, null!);

        nullClock.Should().Throw<ArgumentNullException>().WithParameterName("clock");
        nullZone.Should().Throw<ArgumentNullException>().WithParameterName("businessTimeZone");
    }

    [Fact]
    public void Money_RejectsEmptyCurrency()
    {
        Action create = () => new Money(10m, string.Empty);

        create.Should().Throw<ArgumentException>()
            .WithParameterName("currency")
            .WithMessage("Currency must not be empty.*");
    }

    [Fact]
    public void Money_AllComparisonOperatorsRejectDifferentCurrencies()
    {
        var usd = new Money(10m, "USD");
        var eur = new Money(10m, "EUR");

        Action greaterThan = () => _ = usd > eur;
        Action lessThanOrEqual = () => _ = usd <= eur;
        Action greaterThanOrEqual = () => _ = usd >= eur;

        greaterThan.Should().Throw<InvalidOperationException>().WithMessage("Cannot compare USD with EUR");
        lessThanOrEqual.Should().Throw<InvalidOperationException>().WithMessage("Cannot compare USD with EUR");
        greaterThanOrEqual.Should().Throw<InvalidOperationException>().WithMessage("Cannot compare USD with EUR");
    }

    [Fact]
    public void Percentage_DecimalBasisPoints_ConvertsExactly()
    {
        Percentage.FromBasisPoints(12.5m).Fraction.Should().Be(0.00125m);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void Percentage_DecimalBasisPoints_EnforcesBoundary(decimal basisPoints)
    {
        if (basisPoints < 0)
        {
            Action create = () => Percentage.FromBasisPoints(basisPoints);
            create.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("basisPoints")
                .WithMessage("Basis points must be non-negative.*");
            return;
        }

        Percentage.FromBasisPoints(basisPoints).AsBasisPoints.Should().Be((long)basisPoints);
    }

    [Fact]
    public void Percentage_Constructor_RejectsNegativeFraction()
    {
        Action create = () => new Percentage(-0.01m);

        create.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("fraction")
            .WithMessage("Percent must be non-negative.*");
    }

    [Fact]
    public void Percentage_Subtraction_AllowsZeroAndRejectsNegativeResult()
    {
        Percentage fivePercent = Percentage.FromPercent(5m);

        (fivePercent - fivePercent).Fraction.Should().Be(0m);
        Action negative = () => _ = fivePercent - Percentage.FromPercent(6m);
        negative.Should().Throw<InvalidOperationException>()
            .WithMessage("Result of subtraction would be negative.");
    }

    [Fact]
    public void Percentage_Multiplication_HandlesBothOrdersAndRejectsNegativeFactor()
    {
        Percentage fivePercent = Percentage.FromPercent(5m);

        (2m * fivePercent).AsPercent.Should().Be(10m);
        Action negative = () => _ = fivePercent * -1m;
        negative.Should().Throw<InvalidOperationException>()
            .WithMessage("Result of multiplication would be negative.");
    }

    [Fact]
    public void FixedRate_FormatsItsRateAndConvention()
    {
        var rate = new FixedRate(Percentage.FromPercent(5m), new Actual360());

        rate.ToString().Should().Be("FixedRate(5%, Actual/360)");
    }

    [Fact]
    public void FloatingRate_EqualityRequiresAllComponents()
    {
        var original = new FloatingRate(Percentage.FromPercent(3m), Percentage.FromPercent(1m), new Actual360());
        var same = new FloatingRate(Percentage.FromPercent(3m), Percentage.FromPercent(1m), new Actual360());
        var differentBase = new FloatingRate(Percentage.FromPercent(4m), Percentage.FromPercent(1m), new Actual360());
        var differentMargin = new FloatingRate(Percentage.FromPercent(3m), Percentage.FromPercent(2m), new Actual360());
        var differentConvention = new FloatingRate(Percentage.FromPercent(3m), Percentage.FromPercent(1m), new Actual365());

        original.Equals(same).Should().BeTrue();
        original.Equals(differentBase).Should().BeFalse();
        original.Equals(differentMargin).Should().BeFalse();
        original.Equals(differentConvention).Should().BeFalse();
        original.Equals(null).Should().BeFalse();
        original!.GetHashCode().Should().Be(same!.GetHashCode());
        original.ToString().Should().Be("FloatingRate(base=3%, margin=1%, total=4%)");
    }

    [Fact]
    public void TieredRate_RejectsNullAndEmptyTiers()
    {
        Action nullTiers = () => new TieredRate(null!, new Actual360());
        var empty = new TieredRate([], new Actual360());
        Action findRate = () => empty.EffectiveRateForBalance(new Money(1m, "USD"));

        nullTiers.Should().Throw<ArgumentNullException>().WithParameterName("tiers");
        findRate.Should().Throw<InvalidOperationException>()
            .WithMessage("TieredRate must have at least one tier.");
    }

    [Fact]
    public void TieredRate_EqualityHashAndFormattingReflectTiersAndConvention()
    {
        TieredRate.Tier tier = new(Percentage.FromPercent(3m), new Money(10_000m, "USD"));
        var original = new TieredRate([tier], new Actual360());
        var same = new TieredRate([tier], new Actual360());
        var differentTier = new TieredRate(
            [new TieredRate.Tier(Percentage.FromPercent(4m), new Money(10_000m, "USD"))],
            new Actual360());
        var differentConvention = new TieredRate([tier], new Actual365());

        original.Equals(same).Should().BeTrue();
        original.Equals(differentTier).Should().BeFalse();
        original.Equals(differentConvention).Should().BeFalse();
        original.Equals(null).Should().BeFalse();
        original!.GetHashCode().Should().Be(same!.GetHashCode());
        tier.ToString().Should().Be("Tier(rate=3%, limit=10000.00 USD)");
        original.ToString().Should().Be("TieredRate(tiers: Tier(rate=3%, limit=10000.00 USD))");
    }

    [Fact]
    public void EffectiveRate_EqualityRequiresRateAndConvention()
    {
        var original = new EffectiveRate(Percentage.FromPercent(5m), new Actual360());
        var same = new EffectiveRate(Percentage.FromPercent(5m), new Actual360());
        var differentRate = new EffectiveRate(Percentage.FromPercent(6m), new Actual360());
        var differentConvention = new EffectiveRate(Percentage.FromPercent(5m), new Actual365());

        original.Equals(same).Should().BeTrue();
        original.Equals(differentRate).Should().BeFalse();
        original.Equals(differentConvention).Should().BeFalse();
        original.Equals(null).Should().BeFalse();
        original!.GetHashCode().Should().Be(same!.GetHashCode());
        original.ToString().Should().Be("EffectiveRate(5%)");
    }

    private sealed class TestClock(Instant instant) : IClock
    {
        public Instant GetCurrentInstant() => instant;
    }
}
