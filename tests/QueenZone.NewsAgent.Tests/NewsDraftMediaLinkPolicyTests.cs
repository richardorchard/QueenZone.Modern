using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.NewsAgent.Tests;

public sealed class NewsDraftMediaLinkPolicyTests
{
    [Fact]
    public void Enforce_appends_evidence_backed_links_missing_from_body()
    {
        var result = NewsDraftMediaLinkPolicy.Enforce(
            CreateDraft("Roger Taylor has released a new single and video."),
            CreateEvidence(
                """
                Official release.

                Direct media links supplied by the source:
                - Listen to the song: https://rogertaylor.lnk.to/iseeyounow
                - Watch the video: https://www.youtube.com/watch?v=KZivRNcsoJw
                """));

        Assert.Contains(
            "<a href=\"https://rogertaylor.lnk.to/iseeyounow\">Listen to the song</a>",
            result.Body,
            StringComparison.Ordinal);
        Assert.Contains(
            "<a href=\"https://www.youtube.com/watch?v=KZivRNcsoJw\">Watch the video</a>",
            result.Body,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Enforce_does_not_duplicate_link_already_in_body()
    {
        const string body =
            """<p><a href="https://rogertaylor.lnk.to/iseeyounow">Listen to the song</a></p>""";

        var result = NewsDraftMediaLinkPolicy.Enforce(
            CreateDraft(body),
            CreateEvidence(
                "Direct media links supplied by the source:\n" +
                "- Listen to the song: https://rogertaylor.lnk.to/iseeyounow"));

        Assert.Equal(body, result.Body);
    }

    [Fact]
    public void Enforce_ignores_unsafe_or_unlabelled_urls()
    {
        const string body = "Roger Taylor has released a new single.";
        var result = NewsDraftMediaLinkPolicy.Enforce(
            CreateDraft(body),
            CreateEvidence(
                """
                - Listen to the song: http://127.0.0.1/private
                - Tickets: https://tickets.example.com/roger
                """));

        Assert.Equal(body, result.Body);
    }

    private static NewsDraftStructuredResult CreateDraft(string body) =>
        new(
            "Roger Taylor releases I See You Now",
            "roger-taylor-releases-i-see-you-now",
            "Roger Taylor has released a new single.",
            body,
            ["Roger Taylor"],
            ["https://www.queenonline.com/news/example"],
            ["Queen Online"],
            "Source: Queen Online",
            "Primary source.",
            "Official announcement.",
            null,
            false,
            []);

    private static IReadOnlyList<NewsCandidateEvidence> CreateEvidence(string excerpt) =>
        [
            new(
                1,
                313,
                "https://www.queenonline.com/news/example",
                "https://www.queenonline.com/news/example",
                "Queen Online",
                NewsDiscoveryTrustTier.Primary,
                "Roger Taylor releases I See You Now",
                null,
                excerpt,
                null,
                DateTime.UtcNow,
                null,
                DateTime.UtcNow)
        ];
}
