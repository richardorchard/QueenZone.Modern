namespace QueenZone.Data;

/// <summary>Input for creating a poll with a new thread (or attaching to an existing topic).</summary>
public sealed record NewForumPoll(
    string Question,
    bool IsMultiChoice,
    int? MaxChoices,
    DateTimeOffset? ClosesAt,
    IReadOnlyList<string> Options,
    Guid CreatedByMemberId);

public sealed record ForumPollOptionResult(
    Guid OptionId,
    string OptionText,
    int DisplayOrder,
    int VoteCount,
    double Percentage,
    bool SelectedByViewer);

/// <summary>
/// Poll presentation for a topic page. Percentages are computed at read time.
/// Votes are final in v1 — members cannot change a ballot after casting.
/// </summary>
public sealed record ForumPollResults(
    Guid PollId,
    int LegacyTopicId,
    string Question,
    bool IsMultiChoice,
    int? MaxChoices,
    DateTimeOffset? ClosesAt,
    DateTimeOffset? ClosedAt,
    DateTimeOffset CreatedAt,
    Guid CreatedByMemberId,
    int TotalVotes,
    int DistinctVoters,
    bool ViewerHasVoted,
    bool IsClosed,
    bool CanViewerVote,
    bool CanViewerClose,
    IReadOnlyList<ForumPollOptionResult> Options);

public sealed class ForumPollVoteException : Exception
{
    public ForumPollVoteException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }

    public const string Closed = "closed";
    public const string AlreadyVoted = "already_voted";
    public const string MaxChoices = "max_choices";
    public const string InvalidOptions = "invalid_options";
    public const string NotFound = "not_found";
    public const string Forbidden = "forbidden";
}
