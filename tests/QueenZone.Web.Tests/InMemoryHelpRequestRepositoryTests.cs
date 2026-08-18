using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class InMemoryHelpRequestRepositoryTests
{
    [Fact]
    public async Task ListAsync_FiltersByStatusAndPagesNewestFirst()
    {
        var repository = new InMemoryHelpRequestRepository();
        var older = await repository.CreateAsync(Sample("Older subject", submittedAt: DateTimeOffset.Parse("2026-08-16T10:00:00Z")));
        var newer = await repository.CreateAsync(Sample("Newer subject", submittedAt: DateTimeOffset.Parse("2026-08-17T10:00:00Z")));
        await repository.UpdateStatusAsync(older.Id, HelpRequestStatus.Resolved, "admin@test.local", "done");

        var open = await repository.ListAsync(HelpRequestStatus.Open, page: 1, pageSize: 50);
        Assert.Equal(1, open.TotalCount);
        Assert.Equal(newer.Id, Assert.Single(open.Items).Id);

        var all = await repository.ListAsync("all", page: 1, pageSize: 1);
        Assert.Equal(2, all.TotalCount);
        Assert.Equal(newer.Id, Assert.Single(all.Items).Id);
        Assert.Equal(1, await repository.CountOpenAsync());
    }

    [Fact]
    public async Task CountHelpers_HonourIdentityAndTimeWindow()
    {
        var repository = new InMemoryHelpRequestRepository();
        var memberId = Guid.NewGuid();
        await repository.CreateAsync(Sample(
            "Member one",
            email: "fan@example.com",
            memberId: memberId,
            submittedAt: DateTimeOffset.Parse("2026-08-17T11:00:00Z")));
        await repository.CreateAsync(Sample(
            "Guest one",
            email: "fan@example.com",
            submittedAt: DateTimeOffset.Parse("2026-08-17T11:30:00Z")));
        await repository.CreateAsync(Sample(
            "Old guest",
            email: "fan@example.com",
            submittedAt: DateTimeOffset.Parse("2026-08-15T11:00:00Z")));

        var since = DateTimeOffset.Parse("2026-08-16T12:00:00Z");
        Assert.Equal(2, await repository.CountByEmailSinceAsync("FAN@EXAMPLE.COM", since));
        Assert.Equal(1, await repository.CountByMemberSinceAsync(memberId, since));
    }

    private static HelpRequest Sample(
        string subject,
        string email = "guest@example.com",
        Guid? memberId = null,
        DateTimeOffset? submittedAt = null) =>
        new(
            Guid.NewGuid(),
            HelpRequestTopic.Account,
            subject,
            "This is a sufficiently long help message.",
            "Guest User",
            email,
            email.ToUpperInvariant(),
            memberId,
            HelpRequestStatus.Open,
            submittedAt ?? DateTimeOffset.UtcNow,
            null,
            null,
            null);
}
