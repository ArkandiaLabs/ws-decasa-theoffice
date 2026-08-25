namespace TheOffice.Domain.Common;

public class Result<T>
{
  public bool IsSuccess { get; }
  public T? Value { get; }
  public string? Error { get; }

  private Result(bool isSuccess, T? value, string? error)
  {
    if (isSuccess && error != null)
      throw new InvalidOperationException();
    if (!isSuccess && error == null)
      throw new InvalidOperationException();

    IsSuccess = isSuccess;
    Value = value;
    Error = error;
  }

  public static Result<T> Success(T value) => new Result<T>(true, value, null);
  public static Result<T> Failure(string error) => new Result<T>(false, default, error);

  public static implicit operator Result<T>(T value) => Success(value);
}

public class Result
{
  public bool IsSuccess { get; }
  public string? Error { get; }

  private Result(bool isSuccess, string? error)
  {
    if (isSuccess && error != null)
      throw new InvalidOperationException();
    if (!isSuccess && error == null)
      throw new InvalidOperationException();

    IsSuccess = isSuccess;
    Error = error;
  }

  public static Result Success() => new Result(true, null);
  public static Result Failure(string error) => new Result(false, error);

  public static Result<T> Success<T>(T value) => Result<T>.Success(value);
  public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);
}
