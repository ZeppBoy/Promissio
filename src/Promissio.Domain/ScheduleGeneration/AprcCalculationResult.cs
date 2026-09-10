using Promissio.Domain.ValueObjects;

namespace Promissio.Domain.ScheduleGeneration;

/// <summary>An APRC value or an explicit reason the calculation could not be completed.</summary>
public sealed record AprcCalculationResult
{
    /// <summary>The annual rate, present only on success.</summary>
    public Percentage? Value { get; }
    /// <summary>The failure explanation, present only on failure.</summary>
    public string? Error { get; }
    /// <summary>Whether a rate was successfully calculated.</summary>
    public bool IsSuccess => Value is not null;

    private AprcCalculationResult(Percentage? value, string? error)
    {
        Value = value;
        Error = error;
    }

    internal static AprcCalculationResult Success(Percentage value) => new(value, null);
    internal static AprcCalculationResult Failure(string error) => new(null, error);
}
