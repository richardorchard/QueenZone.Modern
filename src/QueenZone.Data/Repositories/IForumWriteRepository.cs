namespace QueenZone.Data;

public interface IForumWriteRepository
{
    Task<ForumThreadCreateResult> CreateThreadAsync(NewForumThread thread, CancellationToken cancellationToken = default);

    Task<int> CreatePostAsync(NewForumPost post, CancellationToken cancellationToken = default);

    Task<ForumEditablePost?> GetPostAsync(int postId, CancellationToken cancellationToken = default);

    Task<ForumPostUpdateResult> UpdatePostAsync(
        int postId,
        Guid editorMemberId,
        string sanitisedBody,
        bool isAdmin,
        int editWindowMinutes,
        CancellationToken cancellationToken = default);

    Task<ForumWriteThread?> GetThreadAsync(int topicId, CancellationToken cancellationToken = default);

    Task<int> CountPostsByMemberSinceAsync(Guid memberId, DateTimeOffset since, CancellationToken cancellationToken = default);

    Task<int> CountApprovedPostsByMemberAsync(Guid memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hides topics started by this author and every matching post. Rows are retained so
    /// <see cref="UnhideAuthorForumContentAsync"/> can restore them.
    /// When <paramref name="memberId"/> is set, matches that id or unlinked posts/threads whose
    /// display name equals <paramref name="displayName"/> (case-insensitive exact). Name-only
    /// matching never hides another member's <c>AuthorMemberId</c>.
    /// </summary>
    Task HideAuthorForumContentAsync(
        Guid? memberId,
        string displayName,
        CancellationToken cancellationToken = default);

    Task UnhideAuthorForumContentAsync(
        Guid? memberId,
        string displayName,
        CancellationToken cancellationToken = default);

    Task<AuthorForumContentCounts> CountAuthorForumContentAsync(
        Guid? memberId,
        string displayName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exact display-name match for forum authors with no member row. Does not create accounts.
    /// </summary>
    Task<AuthorForumContentCounts?> FindForumAuthorByDisplayNameAsync(
        string displayName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a category by URL slug first, then by case-insensitive name, or
    /// creates one named <paramref name="name"/>. Never returns The Music.
    /// </summary>
    Task<int> EnsureCategoryAsync(string slug, string name, CancellationToken cancellationToken = default);
}
