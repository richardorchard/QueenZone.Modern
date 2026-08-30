namespace QueenZone.Data;

public sealed record HomePollOptionResult(
    Guid OptionId,
    string OptionText,
    int DisplayOrder,
    int VoteCount,
    double Percentage);

/// <summary>
/// Current Home poll for website Index and <c>GET /api/v1/content/home-poll</c>.
/// Percentages are computed at read time. Votes are final — one ballot per member.
/// </summary>
public sealed record HomePollResults(
    Guid PollId,
    string Question,
    DateTimeOffset? ClosedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    int TotalVotes,
    bool ViewerHasVoted,
    Guid? SelectedOptionId,
    bool IsClosed,
    IReadOnlyList<HomePollOptionResult> Options);

public sealed record HomePollAdminItem(
    Guid Id,
    string Question,
    bool IsCurrent,
    DateTimeOffset? ClosedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    int VoteCount,
    int OptionCount);

public sealed record HomePollAdminDetail(
    Guid Id,
    string Question,
    bool IsCurrent,
    DateTimeOffset? ClosedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    int VoteCount,
    IReadOnlyList<HomePollOptionResult> Options);

public sealed record AdminHomePollDraft(string Question, IReadOnlyList<string> Options);

public sealed class HomePollException : Exception
{
    public HomePollException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }

    public const string NotFound = "not_found";
    public const string HasVotes = "has_votes";
}
