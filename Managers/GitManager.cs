using LibGit2Sharp;

namespace DotLurker.Managers;

public class GitManager : IDisposable
{
    private readonly IRepository _repository;

    public GitManager(string gitFolderPath)
    {
        _repository = new Repository(gitFolderPath);
    }

    public IEnumerable<string> GetChanges()
    {
        return _repository.RetrieveStatus()
            .Where(x => x.State is FileStatus.ModifiedInIndex or 
                FileStatus.ModifiedInWorkdir or 
                FileStatus.NewInIndex or 
                FileStatus.NewInWorkdir)
            .Select(x => x.FilePath);
    }

    public void Dispose()
    {
        _repository?.Dispose();
    }
}