namespace BuildCleaner.Support;

public class GeneralAccessIssue(string message, string folder) : IAccessIssue
{
    public string Message { get; } = message;
    public string Folder { get; } = folder;
}