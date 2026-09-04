using QueenZone.Data;
using QueenZone.Data.Entities;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class FanPerformanceCreditResolverTests
{
    [Fact]
    public async Task EnrichAsync_BatchesApprovedCredits_AndLeavesLegacyRowsUncredited()
    {
        var submitter = Guid.NewGuid();
        var repository = new InMemoryFanPerformanceSubmissionRepository(id =>
            id == submitter
                ? new MemberAccount { Id = submitter, DisplayName = "Stage Fan", Email = "stage@example.com" }
                : null);
        var pending = await repository.CreateAsync(NewSubmission(submitter, "Pending cover"));
        var approved = await repository.CreateAsync(NewSubmission(submitter, "Live cover"));
        await repository.PromoteAsync(approved.Id, 187, "admin@test.local", null);
        repository.ForcePromotedStageId(pending.Id, 186);

        var resolver = new FanPerformanceCreditResolver(repository);
        var performances = SampleFanPerformanceData.CreateSeedPerformances();

        var enriched = await resolver.EnrichAsync(performances);

        var credited = Assert.Single(enriched, item => item.Id == 187);
        Assert.Equal(submitter, credited.ContributorMemberId);
        Assert.Equal("Stage Fan", credited.ContributorDisplayName);
        Assert.Equal("Mike Ryde", credited.PerformedBy);
        Assert.All(enriched.Where(item => item.Id != 187), item =>
        {
            Assert.Null(item.ContributorMemberId);
            Assert.Null(item.ContributorDisplayName);
        });
    }

    [Fact]
    public void Apply_DoesNotRenamePerformedBy()
    {
        var performance = new FanPerformance(
            1,
            "Title",
            "The Band",
            "",
            "a.mp3",
            1,
            DateTime.UtcNow);
        var credits = new Dictionary<int, FanPerformanceContributorCredit>
        {
            [1] = new(Guid.NewGuid(), "Submitter"),
        };

        var credited = FanPerformanceCredits.Apply(performance, credits);

        Assert.Equal("The Band", credited.PerformedBy);
        Assert.Equal("Submitter", credited.ContributorDisplayName);
    }

    private static NewFanPerformanceSubmission NewSubmission(Guid memberId, string title) =>
        new(
            memberId,
            title,
            "Reaching Out",
            "The Band",
            null,
            "pending/cover.mp3",
            "cover.mp3",
            1024,
            "audio/mpeg",
            120,
            DateTimeOffset.UtcNow,
            FanPerformanceSubmissionRights.DeclarationVersion);
}
