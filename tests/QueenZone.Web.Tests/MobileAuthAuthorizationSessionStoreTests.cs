namespace QueenZone.Web.Tests;

public sealed class MobileAuthAuthorizationSessionStoreTests
{
    [Fact]
    public void Take_RejectsExpiredSession()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 19, 12, 0, 0, TimeSpan.Zero));
        var store = new MobileAuthAuthorizationSessionStore(time);
        var session = store.Create(
            MobileAuthOptions.DefaultClientId,
            MobileAuthPkceTestData.RedirectUri,
            "challenge",
            "state",
            MemberAuthenticationSchemes.Google,
            TimeSpan.FromMinutes(5));

        time.Advance(TimeSpan.FromMinutes(6));

        Assert.Null(store.Take(session.RequestId));
    }

    [Fact]
    public void Take_ReturnsLiveSessionOnce()
    {
        var store = new MobileAuthAuthorizationSessionStore(TimeProvider.System);
        var session = store.Create(
            MobileAuthOptions.DefaultClientId,
            MobileAuthPkceTestData.RedirectUri,
            "challenge",
            "state",
            MemberAuthenticationSchemes.Discord,
            TimeSpan.FromMinutes(5));

        var first = store.Take(session.RequestId);
        var second = store.Take(session.RequestId);

        Assert.NotNull(first);
        Assert.Equal(session.RequestId, first.RequestId);
        Assert.Null(second);
    }

    private sealed class FakeTimeProvider(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset now = start;

        public override DateTimeOffset GetUtcNow() => now;

        public void Advance(TimeSpan delta) => now += delta;
    }
}
