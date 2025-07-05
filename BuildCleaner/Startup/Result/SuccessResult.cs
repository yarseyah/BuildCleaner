namespace BuildCleaner.Startup.Result;

internal class SuccessResult<T> : IResult<T>
{
    public SuccessResult(T value)
    {
        Value = value;
    }

    public bool IsSuccess => true;
    public T Value { get; }
    public string Error => string.Empty;
}

