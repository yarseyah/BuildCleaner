namespace BuildCleaner.Startup.Result;

public interface IResult<out T>
{
    bool IsSuccess { get; }
    T Value { get; }
    string Error { get; }
}

