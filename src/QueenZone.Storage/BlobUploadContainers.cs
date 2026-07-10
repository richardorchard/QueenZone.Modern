namespace QueenZone.Storage;

/// <summary>
/// Canonical UGC container names. Keep these separate from legacy photo-archive containers
/// served via cdn.queenzone.org.
/// </summary>
public static class BlobUploadContainers
{
    public const string Avatars = "ugc-avatars";

    public const string Forum = "ugc-forum";

    public const string Photos = "ugc-photos";

    public const string Articles = "ugc-articles";

    public static readonly IReadOnlyList<string> All =
    [
        Avatars,
        Forum,
        Photos,
        Articles,
    ];
}
