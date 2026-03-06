namespace HRMS.Shared.Common
{
    /// <summary>
    /// Represents the outcome of an operation, encapsulating success/failure state
    /// and avoiding exception-driven control flow for business rule violations.
    /// </summary>
    public class Result
    {
        protected Result(bool isSuccess, string? error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public string? Error { get; }

        public static Result Success() => new(true, null);
        public static Result Failure(string error) => new(false, error);

        public static Result<T> Success<T>(T value) => Result<T>.Success(value);
        public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);
        public static Result<T> NotFound<T>(string resourceName, object id) =>
            Result<T>.Failure($"{resourceName} with identifier '{id}' was not found.");
    }

    /// <summary>
    /// Represents the outcome of an operation that returns a value.
    /// </summary>
    /// <typeparam name="T">The type of the value returned on success.</typeparam>
    public sealed class Result<T> : Result
    {
        private readonly T? _value;

        private Result(bool isSuccess, T? value, string? error)
            : base(isSuccess, error)
        {
            _value = value;
        }

        public T Value => IsSuccess
            ? _value!
            : throw new InvalidOperationException("Cannot access Value on a failed result.");

        public static Result<T> Success(T value) => new(true, value, null);
        public new static Result<T> Failure(string error) => new(false, default, error);
    }
}
