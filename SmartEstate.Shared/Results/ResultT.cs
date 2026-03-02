using SmartEstate.Shared.Errors;

namespace SmartEstate.Shared.Results;

public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, AppError? error) : base(isSuccess, error)
    {
        Value = value;
    }

    public static Result<T> Ok(T value) => new(true, value, null);

    public new static Result<T> Fail(string code, string message, IReadOnlyDictionary<string, object?>? meta = null)
        => new(false, default, new AppError(code, message, meta));

    public static new Result<T> Fail(AppError error) => new(false, default, error);
}
