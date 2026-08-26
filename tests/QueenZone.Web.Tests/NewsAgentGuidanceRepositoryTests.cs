using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class NewsAgentGuidanceRepositoryTests
{
    [Fact]
    public async Task SaveDraft_mutates_single_draft_and_never_published_row()
    {
        var repository = CreateRepository();

        var first = await repository.SaveDraftAsync(
            NewsAgentGuidanceType.Triage,
            "prefer member-news",
            "first@test.local",
            null);
        var second = await repository.SaveDraftAsync(
            NewsAgentGuidanceType.Triage,
            "prefer archival stories",
            "second@test.local",
            first.RowVersion);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, second.RevisionNumber);
        Assert.Equal(NewsAgentGuidanceStatus.Draft, second.Status);
        Assert.Equal("prefer archival stories", second.Content);
        Assert.Null(await repository.GetPublishedAsync(NewsAgentGuidanceType.Triage));
    }

    [Fact]
    public async Task Publish_supersedes_previous_and_enforces_single_published()
    {
        var repository = CreateRepository();
        var draft = await repository.SaveDraftAsync(NewsAgentGuidanceType.Draft, "keep it short", "a@test.local", null);
        var firstPublished = await repository.PublishDraftAsync(NewsAgentGuidanceType.Draft, "a@test.local", draft.RowVersion);

        var nextDraft = await repository.SaveDraftAsync(NewsAgentGuidanceType.Draft, "keep it shorter", "b@test.local", null);
        var secondPublished = await repository.PublishDraftAsync(NewsAgentGuidanceType.Draft, "b@test.local", nextDraft.RowVersion);

        Assert.Equal(NewsAgentGuidanceStatus.Published, secondPublished.Status);
        Assert.Equal(2, secondPublished.RevisionNumber);
        Assert.NotEqual(firstPublished.Id, secondPublished.Id);

        var history = await repository.ListHistoryAsync(NewsAgentGuidanceType.Draft);
        Assert.Equal(2, history.Count);
        Assert.Equal(NewsAgentGuidanceStatus.Superseded, history.Single(item => item.Id == firstPublished.Id).Status);
        Assert.Single(history, item => item.Status == NewsAgentGuidanceStatus.Published);
        Assert.Null(await repository.GetDraftAsync(NewsAgentGuidanceType.Draft));
    }

    [Fact]
    public async Task Rollback_copies_old_content_into_new_published_revision()
    {
        var repository = CreateRepository();
        var draft = await repository.SaveDraftAsync(NewsAgentGuidanceType.Triage, "original overlay", "a@test.local", null);
        var original = await repository.PublishDraftAsync(NewsAgentGuidanceType.Triage, "a@test.local", draft.RowVersion);
        var replacementDraft = await repository.SaveDraftAsync(NewsAgentGuidanceType.Triage, "replacement overlay", "b@test.local", null);
        await repository.PublishDraftAsync(NewsAgentGuidanceType.Triage, "b@test.local", replacementDraft.RowVersion);

        var rolledBack = await repository.RollbackAsync(NewsAgentGuidanceType.Triage, original.Id, "c@test.local");

        Assert.NotEqual(original.Id, rolledBack.Id);
        Assert.Equal(3, rolledBack.RevisionNumber);
        Assert.Equal("original overlay", rolledBack.Content);
        Assert.Equal(NewsAgentGuidanceStatus.Published, rolledBack.Status);
        var history = await repository.ListHistoryAsync(NewsAgentGuidanceType.Triage);
        Assert.Equal(3, history.Count);
        Assert.Equal(NewsAgentGuidanceStatus.Superseded, history.Single(item => item.Id == original.Id).Status);
    }

    [Fact]
    public async Task RestoreCompiledDefault_publishes_empty_overlay()
    {
        var repository = CreateRepository();
        var draft = await repository.SaveDraftAsync(NewsAgentGuidanceType.Draft, "temporary overlay", "a@test.local", null);
        await repository.PublishDraftAsync(NewsAgentGuidanceType.Draft, "a@test.local", draft.RowVersion);

        var restored = await repository.RestoreCompiledDefaultAsync(NewsAgentGuidanceType.Draft, "b@test.local");

        Assert.Equal(string.Empty, restored.Content);
        Assert.Equal(NewsAgentGuidanceStatus.Published, restored.Status);
        Assert.Equal(NewsAgentGuidanceText.ComputeContentHash(string.Empty), restored.ContentHash);
        Assert.True(string.IsNullOrWhiteSpace((await repository.GetPublishedAsync(NewsAgentGuidanceType.Draft))!.Content));
    }

    [Fact]
    public async Task SaveDraft_rejects_stale_row_version()
    {
        var repository = CreateRepository();
        var draft = await repository.SaveDraftAsync(NewsAgentGuidanceType.Triage, "first", "a@test.local", null);
        await repository.SaveDraftAsync(NewsAgentGuidanceType.Triage, "second", "a@test.local", draft.RowVersion);

        await Assert.ThrowsAsync<NewsAgentGuidanceConcurrencyException>(() =>
            repository.SaveDraftAsync(NewsAgentGuidanceType.Triage, "third", "a@test.local", draft.RowVersion));
    }

    [Fact]
    public async Task SaveDraft_strips_control_characters_and_hashes_normalized_content()
    {
        var repository = CreateRepository();
        var draft = await repository.SaveDraftAsync(
            NewsAgentGuidanceType.Triage,
            "prefer\u0001 short\nsummaries",
            "a@test.local",
            null);

        Assert.Equal("prefer short\nsummaries", draft.Content);
        Assert.Equal(NewsAgentGuidanceText.ComputeContentHash("prefer short\nsummaries"), draft.ContentHash);
    }

    private static InMemoryNewsAgentGuidanceRepository CreateRepository() =>
        new(new SharedNewsAgentGuidanceStore());
}
