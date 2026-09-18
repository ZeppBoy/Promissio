namespace Promissio.Application.LoanCreation;

/// <summary>
/// Identifies the outcome of appending uncommitted events to an existing loan stream.
/// </summary>
public enum LoanSaveStatus
{
    /// <summary>The events were appended at the expected stream version.</summary>
    Saved = 0,

    /// <summary>The stream changed after it was loaded, so no events were appended.</summary>
    ConcurrencyConflict = 1
}
