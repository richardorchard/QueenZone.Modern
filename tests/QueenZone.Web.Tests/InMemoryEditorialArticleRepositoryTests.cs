using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class InMemoryEditorialArticleRepositoryTests
{
    [Fact]
    public async Task Published_version_stays_live_while_replacement_draft_is_saved()
    {
        var repo = new InMemoryEditorialArticleRepository();
        var saved = await repo.SaveDraftAsync(Draft("First title"), "admin");
        await repo.SetStatusAsync(saved.Id, EditorialArticleStatus.Published, "admin");

        await repo.SaveDraftAsync(Draft("Replacement title") with { Id = saved.Id }, "admin");

        var live = await repo.GetPublishedBySlugAsync("first-title");
        Assert.NotNull(live);
        Assert.Equal("First title", live.Title);
        Assert.Null(await repo.GetPublishedBySlugAsync("replacement-title"));
    }

    [Fact]
    public async Task Publishing_replacement_atomically_changes_live_version()
    {
        var repo = new InMemoryEditorialArticleRepository();
        var saved = await repo.SaveDraftAsync(Draft("First title"), "admin");
        await repo.SetStatusAsync(saved.Id, EditorialArticleStatus.Published, "admin");
        await repo.SaveDraftAsync(Draft("Replacement title") with { Id = saved.Id }, "admin");

        await repo.SetStatusAsync(saved.Id, EditorialArticleStatus.Published, "admin");

        Assert.Null(await repo.GetPublishedBySlugAsync("first-title"));
        Assert.Equal("Replacement title", (await repo.GetPublishedBySlugAsync("replacement-title"))!.Title);
    }

    [Fact]
    public async Task Legacy_overlay_is_visible_only_after_publish()
    {
        var repo = new InMemoryEditorialArticleRepository();
        var draft = await repo.SaveDraftAsync(Draft("Edited archive") with { LegacyArticleId = 101 }, "admin");
        Assert.Empty(await repo.GetPublishedLegacyOverlaysAsync([101]));
        await repo.SetStatusAsync(draft.Id, EditorialArticleStatus.Published, "admin");
        Assert.Equal("Edited archive", (await repo.GetPublishedLegacyOverlaysAsync([101]))[101].Title);
    }

    private static EditorialArticleDraft Draft(string title) => new(
        null, null, null, title, null, "Excerpt", "<p>Body</p>", "Manual Author", "Interviews",
        "queen,freddie", null, "editors/admin/card.webp", DateTimeOffset.Parse("2026-09-01T00:00:00Z"));
}
