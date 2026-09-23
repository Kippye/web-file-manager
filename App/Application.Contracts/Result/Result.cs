namespace Application.Contracts;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public IReadOnlyList<ApplicationError> Errors { get; }

    private Result(bool isSuccess, T? value, IReadOnlyList<ApplicationError> errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
    }

    public static Result<T> Success(T value) =>
        new(true, value, Array.Empty<ApplicationError>());
    public static Result<T> Failure(ApplicationError error) =>
        new(false, default, new[] { error });
    public static Result<T> Failure(IEnumerable<ApplicationError> errors) =>
        new(false, default, errors.ToList());
    public static Result<T> Failure(EApplicationErrorCode code, string message, string? field = default) =>
        new(false, default, new[] { new ApplicationError { Code = code, Message = message, Field = field } });
}

// Non-generic version for operations without return values
public class Result
{
    public bool IsSuccess { get; }
    public IReadOnlyList<ApplicationError> Errors { get; }

    private Result(bool isSuccess, IReadOnlyList<ApplicationError> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public static Result Success() =>
        new(true, Array.Empty<ApplicationError>());
    /// <summary>
    /// Shorthand for creating a generic Result success
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="result"></param>
    /// <returns></returns>
    public static Result<T> Success<T>(T result) =>
        Result<T>.Success(result);
    public static Result Failure(ApplicationError error) =>
        new(false, new[] { error });
    public static Result Failure(IEnumerable<ApplicationError> errors) =>
        new(false, errors.ToList());
    public static Result Failure(EApplicationErrorCode code, string message, string? field = default) =>
        new(false, new[] { new ApplicationError { Code = code, Message = message, Field = field } });
}
