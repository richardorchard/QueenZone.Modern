namespace QueenZone.Storage;

/// <summary>
/// Canonical UGC container names. Keep these separate from legacy photo-archive containers
/// served via cdn.queenzone.org.
/// </summary>
public static class BlobUploadContainers
{
    public const string Avatars = "ugc-avatars";

    public const string Forum = "ugc-forum";

    public const string News = "ugc-news";

    public const string Photos = "ugc-photos";

    public const string Articles = "ugc-articles";

    /// <summary>
    /// Pending member fan-performance audio. Never write unreviewed audio to
    /// <c>songfiles</c> — that private container is reachable by the published
    /// member-gated proxy.
    /// </summary>
    public const string FanPerformances = "ugc-fan-performances";

    /// <summary>
    /// Published fan-performance audio. Admin promote copies here from
    /// <see cref="FanPerformances"/>. Not a UGC upload target and not in <see cref="All"/>.
    /// </summary>
    public const string SongFiles = "songfiles";

    public static readonly IReadOnlyList<string> All =
    [
        Avatars,
        Forum,
        News,
        Photos,
        Articles,
        FanPerformances,
    ];
}
