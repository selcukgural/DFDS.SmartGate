using System.Diagnostics.CodeAnalysis;

namespace DFDS.SmartGate.Domain.Common;

/// <summary>
/// Outcome of an operation that has no return value. Failures carry a <see cref="DomainError"/>;
/// exceptions are reserved for programming errors.
/// </summary>
public readonly record struct Result
{
    /// <summary>
    /// Initializes a new result with an optional failure error.
    /// </summary>
    /// <param name="error">The domain error associated with a failed result; <see langword="null"/> indicates success.</param>
    private Result(DomainError? error) => Error = error;

    /// <summary>
    /// Gets the domain error associated with a failed operation, if any.
    /// </summary>
    public DomainError? Error { get; }

    /// <summary>
    /// Gets a value indicating whether the operation completed successfully.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => Error is not null;

    /// <summary>
    /// Creates a successful result without a value.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result Success() => new(null);

    /// <summary>
    /// Creates a failed result with the specified domain error.
    /// </summary>
    /// <param name="error">The domain error describing the failure.</param>
    /// <returns>A failed result.</returns>
    public static Result Failure(DomainError error) => new(error);

    /// <summary>
    /// Creates a successful result containing the provided value.
    /// </summary>
    /// <typeparam name="TValue">The type of the successful value.</typeparam>
    /// <param name="value">The value produced by the successful operation.</param>
    /// <returns>A successfully completed typed result.</returns>
    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.Success(value);

    /// <summary>
    /// Creates a failed typed result with the specified domain error.
    /// </summary>
    /// <typeparam name="TValue">The type of the value that would have been produced on success.</typeparam>
    /// <param name="error">The domain error describing the failure.</param>
    /// <returns>A failed typed result.</returns>
    public static Result<TValue> Failure<TValue>(DomainError error) => Result<TValue>.Failure(error);

    /// <summary>
    /// Converts a domain error into a failed result.
    /// </summary>
    /// <param name="error">The domain error to wrap as a failed result.</param>
    public static implicit operator Result(DomainError error) => Failure(error);
}

/// <summary>
/// Outcome of an operation that yields a <typeparamref name="TValue"/> on success.
/// </summary>
public readonly record struct Result<TValue>
{
    /// <summary>
    /// Initializes a new typed result with a value and optional error.
    /// </summary>
    /// <param name="value">The successful value, if any.</param>
    /// <param name="error">The domain error associated with a failed result.</param>
    private Result(TValue? value, DomainError? error)
    {
        Value = value;
        Error = error;
    }

    /// <summary>
    /// Gets the domain error associated with a failed operation, if any.
    /// </summary>
    public DomainError? Error { get; }

    /// <summary>
    /// Gets a value indicating whether the operation completed successfully.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => Error is not null;

    /// <summary>
    /// The successful value. Reading it on a failed result is a programming error.
    /// </summary>
    public TValue Value => IsSuccess
        ? field!
        : throw new InvalidOperationException($"Cannot read Value of a failed result ({Error.Value.Code}).");

    /// <summary>
    /// Creates a successful typed result.
    /// </summary>
    /// <param name="value">The value to store in the result.</param>
    /// <returns>A successful typed result.</returns>
    internal static Result<TValue> Success(TValue value) => new(value, null);

    /// <summary>
    /// Creates a failed typed result.
    /// </summary>
    /// <param name="error">The domain error describing the failure.</param>
    /// <returns>A failed typed result.</returns>
    internal static Result<TValue> Failure(DomainError error) => new(default, error);

    /// <summary>
    /// Converts a value into a successful typed result.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    public static implicit operator Result<TValue>(TValue value) => Success(value);

    /// <summary>
    /// Converts a domain error into a failed typed result.
    /// </summary>
    /// <param name="error">The domain error to wrap.</param>
    public static implicit operator Result<TValue>(DomainError error) => Failure(error);

    /// <summary>
    /// Converts a typed failure into an untyped <see cref="Result"/> (e.g. when propagating from a helper).
    /// </summary>
    /// <param name="result">The typed result to convert.</param>
    public static implicit operator Result(Result<TValue> result) =>
        result.IsSuccess ? Result.Success() : Result.Failure(result.Error.Value);
}
