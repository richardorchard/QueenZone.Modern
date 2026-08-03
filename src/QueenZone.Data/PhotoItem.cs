namespace QueenZone.Data;

public sealed record PhotoItem(
    int PicId,
    int CatId,
    string CategoryName,
    string CategorySlug,
    string Title,
    string ImageUrl,
    string ThumbnailUrl,
    int ThumbWidth,
    int ThumbHeight,
    /// <summary>Original full-image width from <c>PIC_WIDTH</c>. Zero means unknown/unset.</summary>
    int PictureWidth,
    /// <summary>Original full-image height from <c>PIC_HEIGHT</c>. Zero means unknown/unset.</summary>
    int PictureHeight,
    int Year,
    DateTime DateTime,
    /// <summary>
    /// Display name of the member who submitted the image, when known.
    /// Prefers a linked modern <c>MemberAccounts.DisplayName</c>, otherwise legacy <c>USERS_T.USERNAME</c>.
    /// </summary>
    string? SubmittedByDisplayName = null)
{
    /// <summary>True when both original dimensions are positive (usable for display and wallpaper filters).</summary>
    public bool HasPictureDimensions => PictureWidth > 0 && PictureHeight > 0;

    /// <summary>Visitor-facing label such as <c>1920 x 1080</c>, or null when dimensions are unknown.</summary>
    public string? PictureDimensionsLabel =>
        HasPictureDimensions ? $"{PictureWidth} x {PictureHeight}" : null;
}
