namespace BuildCleaner.Startup.Result;

public static class Results
{
    public static IResult<T> Ok<T>(T value) => new SuccessResult<T>(value);
    public static IResult<T> Fail<T>(string error) => new FailureResult<T>(error);
}

