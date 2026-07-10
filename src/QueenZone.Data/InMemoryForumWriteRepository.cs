namespace QueenZone.Data;

public sealed class InMemoryForumWriteRepository : IForumWriteRepository
{
    private readonly List<ForumWriteThread> threads = [];
    private readonly List<InMemoryForumWritePost> posts = [];
    private readonly object sync = new();
    private int nextTopicId = 200_000;
    private int nextPostId = 2_000_000;

    public Task<int> CreateThreadAsync(NewForumThread thread, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var topicId = nextTopicId++;
            var postId = nextPostId++;
            threads.Add(new ForumWriteThread(
                topicId,
                thread.CategoryId,
                thread.Subject.Trim(),
                thread.CreatedAt,
                thread.CreatedAt,
                1,
                IsLocked: false));
            posts.Add(new InMemoryForumWritePost(
                postId,
                topicId,
                thread.AuthorMemberId,
                thread.AuthorDisplayName,
                thread.Body,
                thread.CreatedAt));
            return Task.FromResult(topicId);
        }
    }

    public Task<int> CreatePostAsync(NewForumPost post, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var index = threads.FindIndex(thread => thread.TopicId == post.TopicId);
            if (index < 0)
            {
                var header = SampleForumData.TryGetSeedTopicHeader(post.TopicId)
                    ?? throw new InvalidOperationException("Forum thread not found.");
                threads.Add(new ForumWriteThread(
                    post.TopicId,
                    header.ForumId,
                    header.Title,
                    post.CreatedAt,
                    post.CreatedAt,
                    SampleForumData.CreateSeedPosts(post.TopicId).Count,
                    IsLocked: false));
                index = threads.Count - 1;
            }

            var thread = threads[index];
            if (thread.IsLocked)
            {
                throw new InvalidOperationException("Forum thread is locked.");
            }

            var postId = nextPostId++;
            posts.Add(new InMemoryForumWritePost(
                postId,
                post.TopicId,
                post.AuthorMemberId,
                post.AuthorDisplayName,
                post.Body,
                post.CreatedAt));
            threads[index] = thread with
            {
                LastPostAt = post.CreatedAt,
                PostCount = thread.PostCount + 1,
            };
            return Task.FromResult(postId);
        }
    }

    public Task<ForumWriteThread?> GetThreadAsync(int topicId, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var thread = threads.SingleOrDefault(thread => thread.TopicId == topicId);
            if (thread is not null)
            {
                return Task.FromResult<ForumWriteThread?>(thread);
            }

            var header = SampleForumData.TryGetSeedTopicHeader(topicId);
            return Task.FromResult<ForumWriteThread?>(header is null
                ? null
                : new ForumWriteThread(
                    topicId,
                    header.ForumId,
                    header.Title,
                    DateTimeOffset.MinValue,
                    DateTimeOffset.MinValue,
                    SampleForumData.CreateSeedPosts(topicId).Count,
                    IsLocked: false));
        }
    }

    public Task<int> CountPostsByMemberSinceAsync(Guid memberId, DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            return Task.FromResult(posts.Count(post => post.MemberId == memberId && post.CreatedAt >= since));
        }
    }

    public Task<int> CountApprovedPostsByMemberAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            return Task.FromResult(posts.Count(post => post.MemberId == memberId));
        }
    }

    public IReadOnlyList<ForumWriteThread> GetCreatedThreads()
    {
        lock (sync)
        {
            return threads.ToList();
        }
    }

    public IReadOnlyList<InMemoryForumWritePost> GetPostsForTopic(int topicId)
    {
        lock (sync)
        {
            return posts.Where(post => post.TopicId == topicId).ToList();
        }
    }
}

public sealed record InMemoryForumWritePost(
    int PostId,
    int TopicId,
    Guid MemberId,
    string DisplayName,
    string Body,
    DateTimeOffset CreatedAt);
