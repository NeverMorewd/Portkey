using LibGit2Sharp;
using SystemIO = System.IO;

namespace Portkey.Api.Features.Git;

public class GitScanService(ILogger<GitScanService> logger)
{
    private const int MaxScanDepth = 3;

    public List<GitRepoInfo> ScanDirectory(string rootPath)
    {
        var results = new List<GitRepoInfo>();
        ScanRecursive(rootPath, 0, results);
        return results;
    }

    private void ScanRecursive(string path, int depth, List<GitRepoInfo> results)
    {
        if (depth > MaxScanDepth || !Directory.Exists(path)) return;
        try
        {
            if (Repository.IsValid(path))
            {
                results.Add(GetRepoInfo(path));
                return; // don't recurse into nested repos
            }
            foreach (var dir in Directory.GetDirectories(path)
                         .Where(d => !Path.GetFileName(d).StartsWith('.')))
                ScanRecursive(dir, depth + 1, results);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Skipped {Path} during scan", path);
        }
    }

    public GitRepoInfo GetRepoInfo(string path)
    {
        using var repo = new Repository(path);

        var branch = repo.Head.FriendlyName;
        var tip = repo.Head.Tip;

        int changedFiles = 0, untrackedFiles = 0;
        bool isDirty = false;
        try
        {
            var status = repo.RetrieveStatus(new StatusOptions
            {
                IncludeUntracked = true,
                RecurseUntrackedDirs = false
            });
            isDirty = status.IsDirty;
            changedFiles = status.Added.Count() + status.Modified.Count()
                         + status.Removed.Count() + status.Missing.Count()
                         + status.Staged.Count();
            untrackedFiles = status.Untracked.Count();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not retrieve status for {Path}", path);
        }

        int? aheadBy = null, behindBy = null;
        try
        {
            var tracking = repo.Head.TrackedBranch;
            if (tracking?.Tip != null && tip != null)
            {
                var div = repo.ObjectDatabase.CalculateHistoryDivergence(tip, tracking.Tip);
                aheadBy = div.AheadBy;
                behindBy = div.BehindBy;
            }
        }
        catch { /* no remote tracking info */ }

        string? remoteUrl = null;
        try { remoteUrl = repo.Network.Remotes["origin"]?.Url; } catch { }

        return new GitRepoInfo(
            Path: path,
            Name: SystemIO.Path.GetFileName(path.TrimEnd(SystemIO.Path.DirectorySeparatorChar)),
            Branch: branch,
            IsDirty: isDirty,
            ChangedFiles: changedFiles,
            UntrackedFiles: untrackedFiles,
            LastCommitMessage: tip?.MessageShort?.Trim() ?? "",
            LastCommitAuthor: tip?.Author.Name ?? "",
            LastCommitDate: tip?.Author.When.UtcDateTime,
            AheadBy: aheadBy,
            BehindBy: behindBy,
            RemoteUrl: remoteUrl
        );
    }
}
