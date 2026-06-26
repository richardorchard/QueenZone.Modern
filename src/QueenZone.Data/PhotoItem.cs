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
    int Year,
    DateTime DateTime);
