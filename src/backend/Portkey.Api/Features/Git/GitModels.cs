namespace Portkey.Api.Features.Git;

public record GitRepoInfo(
    string Path,
    string Name,
    string Branch,
    bool IsDirty,
    int ChangedFiles,
    int UntrackedFiles,
    string LastCommitMessage,
    string LastCommitAuthor,
    DateTime? LastCommitDate,
    int? AheadBy,
    int? BehindBy,
    string? RemoteUrl
);
