namespace Promissio.Domain;

/// <summary>
/// Represents the outcome of a domain operation that can fail.
/// </summary>
/// <typeparam name="T">The type of value returned on success.</typeparam>
public readonly struct Result<T>
{
    private readonly T? _value;

    internal Result(T value)
    {
        _value = value;
        IsSuccess = true;
        Error = null;
    }

    private Result(string error)
    {
        IsSuccess = false;
        Error = error;
        _value = default;
    }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets the error message if the operation failed, otherwise <c>null</c>.</summary>
    public string? Error { get; }

    /// <summary>Gets the value if successful; throws if unsuccessful.</summary>
    /// <exception cref="InvalidOperationException">Thrown if the result is not successful.</exception>
    public T Value
    {
        get
        {
            if (!IsSuccess)
                throw new InvalidOperationException("Cannot access Value of a failed result. Check IsSuccess first.");
            return _value!;
        }
    }

    /// <summary>Creates a successful result with the given value.</summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>Creates a failed result with the given error message.</summary>
    public static Result<T> Failure(string error)
    {
        ArgumentException.ThrowIfNullOrEmpty(error, nameof(error));
        return new Result<T>(error);
    }

    public override string ToString() =>
        IsSuccess ? $"Success({_value})" : $"Failure({Error})";
}
