using Portkey.Api.Features.Git;
using Microsoft.Extensions.Logging.Abstractions;

namespace Portkey.Tests;

public class GitScanServiceTests
{
    private readonly GitScanService _svc = new(NullLogger<GitScanService>.Instance);

    [Fact]
    public void ScanDirectory_ReturnsEmpty_WhenPathDoesNotExist()
    {
        var result = _svc.ScanDirectory(@"C:\this\does\not\exist\at\all");
        Assert.Empty(result);
    }

    [Fact]
    public void ScanDirectory_FindsCurrentRepo()
    {
        // Assume the test is run inside the Portkey repo
        var repoRoot = FindRepoRoot();
        if (repoRoot is null) return; // skip if not in a git repo

        var results = _svc.ScanDirectory(repoRoot);
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.False(string.IsNullOrEmpty(r.Branch)));
    }

    [Fact]
    public void GetRepoInfo_ReturnsValidData()
    {
        var repoRoot = FindRepoRoot();
        if (repoRoot is null) return;

        var info = _svc.GetRepoInfo(repoRoot);

        Assert.False(string.IsNullOrEmpty(info.Name));
        Assert.False(string.IsNullOrEmpty(info.Branch));
        Assert.False(string.IsNullOrEmpty(info.Path));
    }

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
}
