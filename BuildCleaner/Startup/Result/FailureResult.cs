namespace BuildCleaner.Startup.Result;

internal class FailureResult<T>(string error) : IResult<T>
{
    public bool IsSuccess => false;
    public T Value => default!;
    public string Error { get; } = error;
}

