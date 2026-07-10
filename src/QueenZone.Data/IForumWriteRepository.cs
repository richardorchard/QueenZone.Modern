namespace QueenZone.Data;

public interface IForumWriteRepository
{
    Task<int> CreateThreadAsync(NewForumThread thread, CancellationToken cancellationToken = default);

    Task<int> CreatePostAsync(NewForumPost post, CancellationToken cancellationToken = default);

    Task<ForumWriteThread?> GetThreadAsync(int topicId, CancellationToken cancellationToken = default);

    Task<int> CountPostsByMemberSinceAsync(Guid memberId, DateTimeOffset since, CancellationToken cancellationToken = default);

    Task<int> CountApprovedPostsByMemberAsync(Guid memberId, CancellationToken cancellationToken = default);
}
