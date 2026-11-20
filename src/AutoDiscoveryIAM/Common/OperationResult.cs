namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;

public static class OperationResult
{
  public static OperationResult<T> Ok<T>(T data)
      => new OperationResult<T>(true, data);

  public static OperationResult<T> Fail<T>(ErrorInfo errorInfo, string error)
      => new OperationResult<T>(false, default, error, errorInfo);
}

public class OperationResult<T>
{
  public bool Success { get; }
  public T? Data { get; }
  public string? Error { get; }
  public ErrorInfo? ErrorInfo { get; }

  internal OperationResult(bool success, T? data, string? error = null, ErrorInfo? errorInfo = null)
  {
    Success = success;
    Data = data;
    Error = error;
    ErrorInfo = errorInfo;
  }
}
