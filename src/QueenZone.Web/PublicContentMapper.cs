using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Centralized mapping from repository DTOs to stable public view models.
/// Call once at the Web edge; page models and Razor views should consume view models only.
/// </summary>
public static class PublicContentMapper
{
    public static NewsArchiveItem ToNewsArchiveItem(NewsItem item) =>
        new(
            item.Id,
            item.Title,
            item.Excerpt,
            item.PublishedAt,
            NewsRoutes.GetNewsDetailPath(item.Id, item.Title, item.Slug));

    public static IReadOnlyList<NewsArchiveItem> ToNewsArchiveItems(IEnumerable<NewsItem> items) =>
        items.Select(ToNewsArchiveItem).ToList();

    public static NewsDetailItem ToNewsDetailItem(NewsItem item) =>
        new(
            item.Id,
            item.Title,
            item.Excerpt,
            item.Body,
            item.PublishedAt,
            item.SourceUrl,
            NewsRoutes.GetNewsDetailPath(item.Id, item.Title, item.Slug));

    public static NewsDetailItem ToNewsDetailItem(AdminNewsArticle article) =>
        new(
            article.Id,
            article.Title,
            article.Excerpt,
            article.Body,
            article.PublishedAt,
            article.SourceUrl,
            NewsRoutes.GetNewsDetailPath(
                article.Id,
                article.Title,
                string.IsNullOrWhiteSpace(article.Slug) ? null : article.Slug));

    public static ArticleArchiveItem ToArticleArchiveItem(ArticleItem item) =>
        new(
            item.Id,
            item.Title,
            item.Excerpt,
            item.PublishedAt,
            item.CategoryName,
            ArticlesRoutes.GetArticleDetailPath(item.Id, item.Title));

    public static IReadOnlyList<ArticleArchiveItem> ToArticleArchiveItems(IEnumerable<ArticleItem> items) =>
        items.Select(ToArticleArchiveItem).ToList();

    public static ArticleDetailItem ToArticleDetailItem(ArticleItem item) =>
        new(
            item.Id,
            item.Title,
            item.Excerpt,
            item.Body,
            item.PublishedAt,
            item.Source,
            item.CategoryName,
            ArticlesRoutes.GetArticleDetailPath(item.Id, item.Title));

    public static ForumCategorySummary ToForumCategorySummary(ForumCategoryItem category) =>
        new(
            category.Id,
            category.Name,
            category.Description,
            category.PostCount,
            category.LastActivityAt,
            category.LatestThreadTitle,
            ForumRoutes.GetCategoryCanonicalPath(category.Id, category.Name));

    public static IReadOnlyList<ForumCategorySummary> ToForumCategorySummaries(
        IEnumerable<ForumCategoryItem> categories) =>
        categories.Select(ToForumCategorySummary).ToList();

    public static ForumThreadSummary ToForumThreadSummary(ForumTopicItem topic) =>
        new(
            topic.Id,
            topic.Title,
            topic.LastActivityAt,
            topic.AuthorUsername,
            topic.ReplyCount,
            topic.LastPostUsername,
            topic.IsSticky,
            ForumRoutes.GetTopicCanonicalPath(topic.Id, topic.Title));

    public static IReadOnlyList<ForumThreadSummary> ToForumThreadSummaries(
        IEnumerable<ForumTopicItem> topics) =>
        topics.Select(ToForumThreadSummary).ToList();

    public static ForumThreadHeader ToForumThreadHeader(ForumTopicHeader header) =>
        new(
            header.TopicId,
            header.Title.Trim(),
            header.ForumId,
            header.ForumName.Trim(),
            ForumRoutes.GetCategoryCanonicalPath(header.ForumId, header.ForumName),
            ForumRoutes.GetTopicCanonicalPath(header.TopicId, header.Title));

    public static ForumPostViewModel ToForumPostViewModel(ForumPostItem post) =>
        new(
            post.Id,
            post.Body,
            post.PostedAt,
            post.AuthorUsername,
            post.Signature,
            post.AuthorMemberSince,
            ToForumAttachments(post.Attachments));

    public static IReadOnlyList<ForumPostViewModel> ToForumPostViewModels(
        IEnumerable<ForumPostItem> posts) =>
        posts.Select(ToForumPostViewModel).ToList();

    public static ForumIndexStats ToForumIndexStats(
        IReadOnlyList<ForumCategoryItem> categories,
        int threadCount) =>
        new(
            categories.Count,
            threadCount,
            categories.Sum(category => (long)category.PostCount));

    public static BiographyChapterSummary ToBiographyChapterSummary(
        BiographyChapterItem chapter,
        int readingOrderIndex) =>
        new(
            chapter.Id,
            chapter.Title,
            BiographyContent.GetListSummary(chapter),
            BiographyRoutes.GetChapterNumeral(readingOrderIndex),
            BiographyRoutes.GetChapterMarker(chapter.Title, chapter.DisplaySequence),
            BiographyRoutes.GetChapterDetailPath(chapter.Id, chapter.Title));

    /// <summary>
    /// Maps chapters while preserving repository presentation order (typically newest first).
    /// Chapter numerals still follow ascending display-sequence reading order.
    /// </summary>
    public static IReadOnlyList<BiographyChapterSummary> ToBiographyChapterSummaries(
        IEnumerable<BiographyChapterItem> chapters)
    {
        var source = chapters as IReadOnlyList<BiographyChapterItem> ?? chapters.ToList();
        var readingOrderIndexById = BiographyChapterOrdering
            .ByDisplaySequenceAscending(source)
            .Select((chapter, index) => (chapter.Id, index))
            .ToDictionary(pair => pair.Id, pair => pair.index);

        return source
            .Select(chapter =>
            {
                var readingOrderIndex = readingOrderIndexById.TryGetValue(chapter.Id, out var index)
                    ? index
                    : 0;
                return ToBiographyChapterSummary(chapter, readingOrderIndex);
            })
            .ToList();
    }

    public static BiographyChapterDetail ToBiographyChapterDetail(
        BiographyChapterItem chapter,
        int readingOrderIndex) =>
        new(
            chapter.Id,
            chapter.Title,
            BiographyContent.GetListSummary(chapter),
            chapter.Body,
            BiographyRoutes.GetChapterNumeral(Math.Max(readingOrderIndex, 0)),
            BiographyRoutes.GetChapterMarker(chapter.Title, chapter.DisplaySequence),
            BiographyRoutes.GetReadTimeLabel(chapter.Body),
            BiographyRoutes.GetChapterDetailPath(chapter.Id, chapter.Title));

    public static BiographyChapterNavViewModel ToBiographyChapterNav(BiographyChapterNav navigation) =>
        new(
            ToBiographyChapterLink(navigation.Previous),
            ToBiographyChapterLink(navigation.Next));

    public static PhotoCategorySummary ToPhotoCategorySummary(
        PhotoCategory category,
        string? coverImageUrl = null) =>
        new(
            category.CatId,
            category.Name,
            category.Slug,
            category.ImageCount,
            PhotoRoutes.GetCategoryPath(category.Slug),
            coverImageUrl);

    public static IReadOnlyList<PhotoCategorySummary> ToPhotoCategorySummaries(
        IEnumerable<PhotoCategory> categories,
        IReadOnlyDictionary<int, string>? coverImageUrls = null) =>
        categories
            .Select(category =>
            {
                string? cover = null;
                coverImageUrls?.TryGetValue(category.CatId, out cover);
                return ToPhotoCategorySummary(category, cover);
            })
            .ToList();

    public static PhotoThumbnailItem ToPhotoThumbnailItem(PhotoItem photo) =>
        new(
            photo.PicId,
            photo.Title,
            photo.ThumbnailUrl,
            photo.ThumbWidth,
            photo.ThumbHeight,
            photo.Year,
            PhotoRoutes.GetDetailPath(photo.CategorySlug, photo.PicId));

    public static IReadOnlyList<PhotoThumbnailItem> ToPhotoThumbnailItems(IEnumerable<PhotoItem> photos) =>
        photos.Select(ToPhotoThumbnailItem).ToList();

    public static PhotoDetailItem ToPhotoDetailItem(PhotoItem photo) =>
        new(
            photo.PicId,
            photo.Title,
            photo.ImageUrl,
            photo.Year,
            PhotoRoutes.GetDetailPath(photo.CategorySlug, photo.PicId));

    public static AlbumCardItem ToAlbumCardItem(AlbumSummary album) =>
        new(
            album.AlbumId,
            album.Name,
            album.Slug,
            album.ReleaseYear,
            album.ThumbnailUrl,
            DiscographyRoutes.GetAlbumPath(album.AlbumId, album.Slug));

    public static IReadOnlyList<AlbumCardItem> ToAlbumCardItems(IEnumerable<AlbumSummary> albums) =>
        albums.Select(ToAlbumCardItem).ToList();

    public static AlbumDetailViewModel ToAlbumDetailViewModel(AlbumDetail album) =>
        new(
            album.AlbumId,
            album.Name,
            DiscographyRoutes.GetAlbumPath(album.AlbumId, album.Slug),
            album.ReleaseYear,
            album.ArtistName,
            album.GeneralNotes,
            album.CoverUrl,
            album.Songs.Select(ToAlbumTrackViewModel).ToList());

    public static AlbumTrackViewModel ToAlbumTrackViewModel(AlbumSong song) =>
        new(song.SongId, song.Title, song.IsSingle, song.Lyrics, song.Notes);

    private static BiographyChapterLink? ToBiographyChapterLink(BiographyChapterItem? chapter) =>
        chapter is null
            ? null
            : new BiographyChapterLink(
                chapter.Title,
                BiographyRoutes.GetChapterDetailPath(chapter.Id, chapter.Title));

    private static IReadOnlyList<ForumAttachmentViewModel> ToForumAttachments(
        IReadOnlyList<ForumPostAttachment>? attachments)
    {
        if (attachments is null || attachments.Count == 0)
        {
            return [];
        }

        return attachments
            .Select(attachment => new ForumAttachmentViewModel(
                attachment.FileName,
                attachment.Url,
                attachment.Extension,
                attachment.FormattedSize))
            .ToList();
    }
}
