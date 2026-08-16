using Sapphire.Shared.Kernel.Common;

namespace Sapphire.Shared.Kernel.Common;

/// <summary>
/// Result pattern for operation outcomes without exceptions.
/// Used throughout the application layer to return success/failure states.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new ArgumentException("Success result cannot have an error", nameof(error));
        
        if (!isSuccess && error == Error.None)
            throw new ArgumentException("Failure result must have an error", nameof(error));

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

/// <summary>
/// Generic result pattern for operations that return a value.
/// </summary>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    public TValue Value => IsSuccess 
        ? _value! 
        : throw new InvalidOperationException("Cannot access value of a failed result");

    protected internal Result(TValue? value, bool isSuccess, Error error) 
        : base(isSuccess, error)
    {
        _value = value;
    }

    public static implicit operator Result<TValue>(TValue value) => Success(value);
    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);
}
