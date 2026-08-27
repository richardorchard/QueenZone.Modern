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
    /// Hides topics started by the given member and every post they wrote. Rows are retained so
    /// <see cref="UnhidePostsByMemberAsync"/> can restore them on reinstatement.
    /// </summary>
    Task HidePostsByMemberAsync(Guid memberId, CancellationToken cancellationToken = default);

    Task UnhidePostsByMemberAsync(Guid memberId, CancellationToken cancellationToken = default);
}
