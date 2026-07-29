namespace QueenZone.Data.Entities;

public sealed class QueenLinkCheckEntity
{
    public int QueenFeaturedSiteId { get; set; }

    public string Url { get; set; } = string.Empty;

    public DateTime LastCheckedAtUtc { get; set; }

    public bool IsAvailable { get; set; }

    public bool IsConfirmedDead { get; set; }

    public int ConsecutiveFailureCount { get; set; }

    public int? LastStatusCode { get; set; }

    public string? LastError { get; set; }
}
