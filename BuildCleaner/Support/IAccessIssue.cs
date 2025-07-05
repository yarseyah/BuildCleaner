namespace BuildCleaner.Support;

public interface IAccessIssue
{
    string Folder { get; }
    string Message { get; }
}