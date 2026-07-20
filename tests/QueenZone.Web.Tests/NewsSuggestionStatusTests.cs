using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class NewsSuggestionStatusTests
{
    [Theory]
    [InlineData(NewsSuggestionStatus.Pending, true)]
    [InlineData(NewsSuggestionStatus.UnderReview, true)]
    [InlineData(NewsSuggestionStatus.Promoted, false)]
    [InlineData(NewsSuggestionStatus.Rejected, false)]
    [InlineData(NewsSuggestionStatus.Duplicate, false)]
    public void IsActive_ReflectsReviewableStatuses(string status, bool expected)
    {
        Assert.Equal(expected, NewsSuggestionStatus.IsActive(status));
    }

    [Fact]
    public void Normalize_ThrowsForUnknownStatus()
    {
        var ex = Assert.Throws<ArgumentException>(() => NewsSuggestionStatus.Normalize("Unknown"));
        Assert.Contains("Unknown", ex.Message);
    }
}
