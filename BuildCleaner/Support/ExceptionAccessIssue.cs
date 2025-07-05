namespace BuildCleaner.Support;

public class ExceptionAccessIssue(Exception exception, string folder) : IAccessIssue
{
    public Exception Exception { get; } = exception;
    public string Folder { get; } = folder;
    public string Message => Exception.Message;
}