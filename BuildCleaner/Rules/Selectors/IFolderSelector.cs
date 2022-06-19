namespace BuildCleaner.Rules.Selectors;

public interface IFolderSelector
{
    Task<bool> SelectFolderAsync(string fullFolderPath);
}