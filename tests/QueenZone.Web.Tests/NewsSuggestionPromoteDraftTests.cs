using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class NewsSuggestionPromoteDraftTests
{
    [Fact]
    public void Build_UsesMemberTitleAndNotes()
    {
        var suggestion = new NewsSuggestion(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "https://example.com/brian-interview",
            "hash",
            "Brian speaks out",
            "Important interview about the tour.",
            NewsSuggestionStatus.Pending,
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

        var draft = NewsSuggestionPromoteDraft.Build(suggestion);

        Assert.Equal("Brian speaks out", draft.Title);
        Assert.Equal("Important interview about the tour.", draft.Excerpt);
        Assert.Equal("Important interview about the tour.", draft.Body);
        Assert.Equal(suggestion.Url, draft.SourceUrl);
    }

    [Fact]
    public void Build_DerivesTitleFromUrl_WhenMissing()
    {
        var suggestion = new NewsSuggestion(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "https://news.example.com/articles/queen-reunion-tour",
            "hash",
            null,
            null,
            NewsSuggestionStatus.Pending,
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

        var draft = NewsSuggestionPromoteDraft.Build(suggestion);

        Assert.Equal("queen reunion tour", draft.Title);
        Assert.Contains("Community-suggested", draft.Excerpt);
    }
}
