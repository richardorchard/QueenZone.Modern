namespace QueenZone.Data;

public sealed class InMemoryForumWriteRepository : IForumWriteRepository
{
    private readonly List<ForumWriteThread> threads = [];
    private readonly List<InMemoryForumWritePost> posts = [];
    private readonly List<ForumCategoryItem> createdCategories = [];
    private readonly object sync = new();
    private int nextTopicId = 200_000;
    private int nextPostId = 2_000_000;
    private InMemoryForumPollRepository? pollRepository;

    public void AttachPollRepository(InMemoryForumPollRepository repository) =>
        pollRepository = repository;

    public async Task<ForumThreadCreateResult> CreateThreadAsync(NewForumThread thread, CancellationToken cancellationToken = default)
    {
        int topicId;
        int postId;
        lock (sync)
        {
            topicId = nextTopicId++;
            postId = nextPostId++;
            threads.Add(new ForumWriteThread(
                topicId,
                thread.CategoryId,
                thread.Subject.Trim(),
                thread.CreatedAt,
                thread.CreatedAt,
                1,
                IsLocked: false,
                HasPoll: thread.Poll is not null,
                StartedByDisplayName: thread.AuthorDisplayName.Trim()));
            posts.Add(new InMemoryForumWritePost(
                postId,
                topicId,
                thread.AuthorMemberId,
                thread.AuthorDisplayName,
                thread.Body,
                thread.CreatedAt));
        }

        if (thread.Poll is not null && pollRepository is not null)
        {
            pollRepository.RegisterTopic(topicId);
            await pollRepository.CreatePollAsync(
                topicId,
                thread.Poll with { CreatedByMemberId = thread.AuthorMemberId },
                cancellationToken);
        }

        return new ForumThreadCreateResult(topicId, postId);
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

    public Task<ForumEditablePost?> GetPostAsync(int postId, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var post = posts.SingleOrDefault(item => item.PostId == postId);
            if (post is null)
            {
                return Task.FromResult<ForumEditablePost?>(null);
            }

            var subject = threads.SingleOrDefault(thread => thread.TopicId == post.TopicId)?.Subject
                ?? SampleForumData.TryGetSeedTopicHeader(post.TopicId)?.Title
                ?? string.Empty;

            var seedCount = SampleForumData.CreateSeedPosts(post.TopicId).Count;
            var createdBefore = posts.Count(item => item.TopicId == post.TopicId && item.PostId <= postId);
            // Created posts append after seed posts for topics that started from sample data.
            var position = seedCount > 0 && threads.Any(thread => thread.TopicId == post.TopicId)
                ? seedCount + createdBefore
                : createdBefore;

            return Task.FromResult<ForumEditablePost?>(new ForumEditablePost(
                post.PostId,
                post.TopicId,
                subject,
                post.Body,
                post.MemberId,
                post.CreatedAt,
                post.EditedAt,
                post.EditCount,
                Math.Max(1, position),
                post.DisplayName));
        }
    }

    public Task<ForumPostUpdateResult> UpdatePostAsync(
        int postId,
        Guid editorMemberId,
        string sanitisedBody,
        bool isAdmin,
        int editWindowMinutes,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var index = posts.FindIndex(item => item.PostId == postId);
            if (index < 0)
            {
                return Task.FromResult(new ForumPostUpdateResult(ForumPostUpdateStatus.NotFound));
            }

            var post = posts[index];
            var subject = threads.SingleOrDefault(thread => thread.TopicId == post.TopicId)?.Subject
                ?? SampleForumData.TryGetSeedTopicHeader(post.TopicId)?.Title
                ?? string.Empty;
            var utcNow = DateTimeOffset.UtcNow;
            var canEdit = ForumPostEditRules.CanEdit(
                post.MemberId,
                editorMemberId,
                isAdmin,
                post.CreatedAt,
                editWindowMinutes,
                utcNow);

            if (!canEdit)
            {
                if (!isAdmin && post.MemberId == editorMemberId && editWindowMinutes == 0)
                {
                    return Task.FromResult(new ForumPostUpdateResult(ForumPostUpdateStatus.EditingDisabled, post.TopicId, subject));
                }

                if (!isAdmin
                    && post.MemberId == editorMemberId
                    && editWindowMinutes > 0
                    && utcNow > post.CreatedAt.AddMinutes(editWindowMinutes))
                {
                    return Task.FromResult(new ForumPostUpdateResult(ForumPostUpdateStatus.EditWindowExpired, post.TopicId, subject));
                }

                return Task.FromResult(new ForumPostUpdateResult(ForumPostUpdateStatus.Forbidden, post.TopicId, subject));
            }

            posts[index] = post with
            {
                Body = sanitisedBody,
                EditedAt = utcNow,
                EditCount = post.EditCount + 1,
            };

            return Task.FromResult(new ForumPostUpdateResult(ForumPostUpdateStatus.Success, post.TopicId, subject));
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
            return Task.FromResult(posts.Count(post =>
                post.MemberId == memberId && post.CreatedAt >= since && !post.IsHidden));
        }
    }

    public Task<int> CountApprovedPostsByMemberAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            return Task.FromResult(posts.Count(post => post.MemberId == memberId && !post.IsHidden));
        }
    }

    public Task HideAuthorForumContentAsync(
        Guid? memberId,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            SetAuthorHidden(memberId, displayName, hidden: true);
            return Task.CompletedTask;
        }
    }

    public Task UnhideAuthorForumContentAsync(
        Guid? memberId,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            SetAuthorHidden(memberId, displayName, hidden: false);
            return Task.CompletedTask;
        }
    }

    public Task<AuthorForumContentCounts> CountAuthorForumContentAsync(
        Guid? memberId,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            return Task.FromResult(CountAuthorLocked(memberId, displayName));
        }
    }

    public Task<AuthorForumContentCounts?> FindForumAuthorByDisplayNameAsync(
        string displayName,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var counts = CountAuthorLocked(null, displayName);
            if (!counts.HasAnyContent)
            {
                return Task.FromResult<AuthorForumContentCounts?>(null);
            }

            var storedName = posts
                .FirstOrDefault(post => post.MemberId is null
                    && ForumAuthorContentMatching.NamesEqual(post.DisplayName, displayName))
                ?.DisplayName
                ?? threads.FirstOrDefault(thread =>
                    ForumAuthorContentMatching.NamesEqual(thread.StartedByDisplayName, displayName))
                    ?.StartedByDisplayName;

            return Task.FromResult<AuthorForumContentCounts?>(
                counts with { DisplayName = string.IsNullOrWhiteSpace(storedName) ? counts.DisplayName : storedName.Trim() });
        }
    }

    public ForumThreadCreateResult SeedUnlinkedThread(
        int categoryId,
        string displayName,
        string subject,
        string body,
        DateTimeOffset createdAt)
    {
        lock (sync)
        {
            var topicId = nextTopicId++;
            var postId = nextPostId++;
            var name = displayName.Trim();
            threads.Add(new ForumWriteThread(
                topicId,
                categoryId,
                subject.Trim(),
                createdAt,
                createdAt,
                1,
                IsLocked: false,
                StartedByDisplayName: name));
            posts.Add(new InMemoryForumWritePost(
                postId,
                topicId,
                MemberId: null,
                name,
                body,
                createdAt));
            return new ForumThreadCreateResult(topicId, postId);
        }
    }

    public int SeedUnlinkedReply(int topicId, string displayName, string body, DateTimeOffset createdAt)
    {
        lock (sync)
        {
            var index = threads.FindIndex(thread => thread.TopicId == topicId);
            if (index < 0)
            {
                throw new InvalidOperationException("Forum thread not found.");
            }

            var postId = nextPostId++;
            posts.Add(new InMemoryForumWritePost(
                postId,
                topicId,
                MemberId: null,
                displayName.Trim(),
                body,
                createdAt));
            var thread = threads[index];
            threads[index] = thread with
            {
                LastPostAt = createdAt,
                PostCount = thread.PostCount + 1,
            };
            return postId;
        }
    }

    private void SetAuthorHidden(Guid? memberId, string displayName, bool hidden)
    {
        var startedTopicIds = StartedTopicIds(memberId, displayName);
        for (var i = 0; i < threads.Count; i++)
        {
            if (startedTopicIds.Contains(threads[i].TopicId))
            {
                threads[i] = threads[i] with { IsHidden = hidden };
            }
        }

        for (var i = 0; i < posts.Count; i++)
        {
            if (ForumAuthorContentMatching.MatchesPost(
                    memberId, displayName, posts[i].MemberId, posts[i].DisplayName))
            {
                posts[i] = posts[i] with { IsHidden = hidden };
            }
        }
    }

    private AuthorForumContentCounts CountAuthorLocked(Guid? memberId, string displayName)
    {
        var label = displayName.Trim();
        if (memberId is null && ForumAuthorContentMatching.NormalizeDisplayName(displayName).Length == 0)
        {
            return new AuthorForumContentCounts(label, 0, 0, 0, 0);
        }

        var matchingPosts = posts
            .Where(post => ForumAuthorContentMatching.MatchesPost(
                memberId, displayName, post.MemberId, post.DisplayName))
            .ToList();
        var startedTopicIds = StartedTopicIds(memberId, displayName);
        var matchingThreads = threads.Where(thread => startedTopicIds.Contains(thread.TopicId)).ToList();
        return new AuthorForumContentCounts(
            label,
            matchingPosts.Count,
            matchingPosts.Count(post => post.IsHidden),
            matchingThreads.Count,
            matchingThreads.Count(thread => thread.IsHidden));
    }

    private HashSet<int> StartedTopicIds(Guid? memberId, string displayName)
    {
        var fromFirstPost = posts
            .GroupBy(post => post.TopicId)
            .Where(topicPosts =>
            {
                var starter = topicPosts.OrderBy(post => post.PostId).First();
                return ForumAuthorContentMatching.MatchesPost(
                    memberId, displayName, starter.MemberId, starter.DisplayName);
            })
            .Select(topicPosts => topicPosts.Key);

        var fromStartedByName = threads
            .Where(thread =>
            {
                var starter = posts
                    .Where(post => post.TopicId == thread.TopicId)
                    .OrderBy(post => post.PostId)
                    .FirstOrDefault();
                return ForumAuthorContentMatching.MatchesStartedThread(
                    memberId,
                    displayName,
                    starter?.MemberId,
                    starter?.DisplayName,
                    thread.StartedByDisplayName);
            })
            .Select(thread => thread.TopicId);

        return fromFirstPost.Concat(fromStartedByName).ToHashSet();
    }

    public Task<int> EnsureCategoryAsync(
        string slug,
        string name,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var existing = NewsForumDiscussion.FindExistingCategory(
                createdCategories.Concat(SampleForumData.CreateSeedCategories()),
                category => category.Name,
                slug,
                name);
            if (existing is not null)
            {
                return Task.FromResult(existing.Id);
            }

            var nextId = Math.Max(
                createdCategories.Select(category => category.Id).DefaultIfEmpty(0).Max(),
                SampleForumData.CreateSeedCategories().Select(category => category.Id).DefaultIfEmpty(0).Max()) + 1;
            if (NewsForumDiscussion.IsTheMusic(name) || nextId == 1)
            {
                nextId = Math.Max(nextId, 2);
            }

            var created = new ForumCategoryItem(
                nextId,
                name,
                "Discussion of published QueenZone news articles.",
                0,
                null,
                null,
                nextId * 10);
            createdCategories.Add(created);
            return Task.FromResult(nextId);
        }
    }

    public IReadOnlyList<ForumCategoryItem> GetCreatedCategories()
    {
        lock (sync)
        {
            return createdCategories.ToList();
        }
    }

    public IReadOnlyList<ForumWriteThread> GetCreatedThreads()
    {
        lock (sync)
        {
            return threads.Where(thread => !thread.IsHidden).ToList();
        }
    }

    public IReadOnlyList<InMemoryForumWritePost> GetPostsForTopic(int topicId)
    {
        lock (sync)
        {
            return posts.Where(post => post.TopicId == topicId && !post.IsHidden).ToList();
        }
    }
}

public sealed record InMemoryForumWritePost(
    int PostId,
    int TopicId,
    Guid? MemberId,
    string DisplayName,
    string Body,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EditedAt = null,
    int EditCount = 0,
    bool IsHidden = false);
