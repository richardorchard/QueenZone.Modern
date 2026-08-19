using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.Storage;

namespace QueenZone.Web.Tests;

public sealed class MobileAuthServiceTests
{
    [Fact]
    public void StartAuthorization_RejectsUnknownRedirectWithoutRedirecting()
    {
        var result = CreateService().StartAuthorization(
            "code",
            MobileAuthOptions.DefaultClientId,
            "https://evil.example/callback",
            "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM",
            MobileAuthPkce.MethodS256,
            "state-1",
            MemberAuthenticationSchemes.Google);

        Assert.False(result.Success);
        Assert.False(result.RedirectSafe);
        Assert.Equal("invalid_request", result.Error);
    }

    [Theory]
    [InlineData("token")]
    [InlineData(null)]
    public void StartAuthorization_RequiresCodeResponseType(string? responseType)
    {
        var result = CreateService().StartAuthorization(
            responseType,
            MobileAuthOptions.DefaultClientId,
            MobileAuthPkceTestData.RedirectUri,
            "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM",
            MobileAuthPkce.MethodS256,
            "state-1",
            MemberAuthenticationSchemes.Google);

        Assert.False(result.Success);
        Assert.Equal("invalid_request", result.Error);
    }

    [Theory]
    [InlineData("Facebook")]
    [InlineData("")]
    [InlineData(null)]
    public void StartAuthorization_RejectsUnknownProvider(string? provider)
    {
        var result = CreateService().StartAuthorization(
            "code",
            MobileAuthOptions.DefaultClientId,
            MobileAuthPkceTestData.RedirectUri,
            "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM",
            MobileAuthPkce.MethodS256,
            "state-1",
            provider);

        Assert.False(result.Success);
        Assert.Equal("invalid_request", result.Error);
        Assert.True(result.RedirectSafe);
    }

    [Fact]
    public void StartAuthorization_RejectsPlainCodeChallengeMethod()
    {
        var result = CreateService().StartAuthorization(
            "code",
            MobileAuthOptions.DefaultClientId,
            MobileAuthPkceTestData.RedirectUri,
            "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM",
            "plain",
            "state-1",
            MemberAuthenticationSchemes.Google);

        Assert.False(result.Success);
        Assert.Equal("invalid_request", result.Error);
    }

    [Fact]
    public void StartAuthorization_StoresPkceSession()
    {
        var pair = MobileAuthPkceTestData.CreatePair();
        var result = CreateService().StartAuthorization(
            "code",
            MobileAuthOptions.DefaultClientId,
            MobileAuthPkceTestData.RedirectUri,
            pair.Challenge,
            MobileAuthPkce.MethodS256,
            "csrf-state",
            "google");

        Assert.True(result.Success);
        Assert.NotNull(result.Session);
        Assert.Equal(MemberAuthenticationSchemes.Google, result.Session.Provider);
        Assert.Equal(pair.Challenge, result.Session.CodeChallenge);
        Assert.Equal("csrf-state", result.Session.State);
    }

    [Fact]
    public async Task ExchangeAuthorizationCode_IssuesTokens_AndRejectsReuse()
    {
        var pair = MobileAuthPkceTestData.CreatePair();
        var service = CreateService();
        var started = service.StartAuthorization(
            "code",
            MobileAuthOptions.DefaultClientId,
            MobileAuthPkceTestData.RedirectUri,
            pair.Challenge,
            MobileAuthPkce.MethodS256,
            "csrf-state",
            MemberAuthenticationSchemes.GitHub);
        var completed = await service.CompleteExternalLoginAsync(
            started.Session!.RequestId,
            MemberAuthenticationSchemes.GitHub,
            "gh-subject-1",
            "fan@example.com",
            "Fan",
            CancellationToken.None);

        Assert.True(completed.Success);
        Assert.False(string.IsNullOrWhiteSpace(completed.Code));

        var tokens = await service.ExchangeAuthorizationCodeAsync(
            "authorization_code",
            MobileAuthOptions.DefaultClientId,
            MobileAuthPkceTestData.RedirectUri,
            completed.Code,
            pair.Verifier,
            CancellationToken.None);

        Assert.True(tokens.Success);
        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
        Assert.Equal(15 * 60, tokens.ExpiresIn);

        var reused = await service.ExchangeAuthorizationCodeAsync(
            "authorization_code",
            MobileAuthOptions.DefaultClientId,
            MobileAuthPkceTestData.RedirectUri,
            completed.Code,
            pair.Verifier,
            CancellationToken.None);

        Assert.False(reused.Success);
        Assert.Equal("invalid_grant", reused.Error);
        Assert.Null(reused.RefreshToken);
    }

    [Fact]
    public async Task ExchangeAuthorizationCode_RejectsWrongVerifier()
    {
        var pair = MobileAuthPkceTestData.CreatePair();
        var other = MobileAuthPkceTestData.CreatePair();
        var service = CreateService();
        var started = service.StartAuthorization(
            "code",
            MobileAuthOptions.DefaultClientId,
            MobileAuthPkceTestData.RedirectUri,
            pair.Challenge,
            MobileAuthPkce.MethodS256,
            "csrf-state",
            MemberAuthenticationSchemes.Microsoft);
        var completed = await service.CompleteExternalLoginAsync(
            started.Session!.RequestId,
            MemberAuthenticationSchemes.Microsoft,
            "ms-subject-1",
            "msfan@example.com",
            "MS Fan",
            CancellationToken.None);

        var tokens = await service.ExchangeAuthorizationCodeAsync(
            "authorization_code",
            MobileAuthOptions.DefaultClientId,
            MobileAuthPkceTestData.RedirectUri,
            completed.Code,
            other.Verifier,
            CancellationToken.None);

        Assert.False(tokens.Success);
        Assert.Equal("invalid_grant", tokens.Error);
    }

    [Fact]
    public async Task CompleteExternalLogin_RejectsSuspendedAccount()
    {
        var pair = MobileAuthPkceTestData.CreatePair();
        var members = new InMemoryMemberAccountRepository();
        var service = CreateService(members);
        var started = service.StartAuthorization(
            "code",
            MobileAuthOptions.DefaultClientId,
            MobileAuthPkceTestData.RedirectUri,
            pair.Challenge,
            MobileAuthPkce.MethodS256,
            "csrf-state",
            MemberAuthenticationSchemes.Google);
        var first = await service.CompleteExternalLoginAsync(
            started.Session!.RequestId,
            MemberAuthenticationSchemes.Google,
            "suspended-subject",
            "suspended@example.com",
            "Suspended",
            CancellationToken.None);
        Assert.True(first.Success);

        var account = await members.FindByExternalLoginAsync(MemberAuthenticationSchemes.Google, "suspended-subject");
        Assert.NotNull(account);
        account!.IsSuspended = true;

        var secondStart = service.StartAuthorization(
            "code",
            MobileAuthOptions.DefaultClientId,
            MobileAuthPkceTestData.RedirectUri,
            pair.Challenge,
            MobileAuthPkce.MethodS256,
            "csrf-state-2",
            MemberAuthenticationSchemes.Google);
        var completed = await service.CompleteExternalLoginAsync(
            secondStart.Session!.RequestId,
            MemberAuthenticationSchemes.Google,
            "suspended-subject",
            "suspended@example.com",
            "Suspended",
            CancellationToken.None);

        Assert.False(completed.Success);
        Assert.Equal("access_denied", completed.Error);
        Assert.Equal("account_suspended", completed.ErrorDescription);
    }

    [Fact]
    public async Task CompleteExternalLogin_RejectsMissingRequest()
    {
        var completed = await CreateService().CompleteExternalLoginAsync(
            null,
            MemberAuthenticationSchemes.Google,
            "subject",
            "fan@example.com",
            "Fan",
            CancellationToken.None);

        Assert.False(completed.Success);
        Assert.Equal("invalid_request", completed.Error);
        Assert.Null(completed.RedirectUri);
    }

    [Fact]
    public async Task ExchangeAuthorizationCode_RejectsUnsupportedGrantType()
    {
        var result = await CreateService().ExchangeAuthorizationCodeAsync(
            "password",
            MobileAuthOptions.DefaultClientId,
            MobileAuthPkceTestData.RedirectUri,
            "code",
            "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk",
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("unsupported_grant_type", result.Error);
    }

    [Fact]
    public async Task ExchangeRefreshToken_RotatesAndRejectsReuse()
    {
        var issued = await IssueTokensAsync();

        var refreshed = await issued.Service.ExchangeRefreshTokenAsync(
            MobileAuthOptions.DefaultClientId,
            issued.RefreshToken,
            CancellationToken.None);

        Assert.True(refreshed.Success);
        Assert.False(string.IsNullOrWhiteSpace(refreshed.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshed.RefreshToken));
        Assert.NotEqual(issued.RefreshToken, refreshed.RefreshToken);
        Assert.DoesNotContain(issued.RefreshToken!, refreshed.ErrorDescription ?? string.Empty);

        var reused = await issued.Service.ExchangeRefreshTokenAsync(
            MobileAuthOptions.DefaultClientId,
            issued.RefreshToken,
            CancellationToken.None);

        Assert.False(reused.Success);
        Assert.Equal("invalid_grant", reused.Error);
        Assert.Null(reused.RefreshToken);
        Assert.DoesNotContain(issued.RefreshToken!, reused.ErrorDescription ?? string.Empty);
        Assert.DoesNotContain(issued.RefreshToken!, reused.Error ?? string.Empty);
    }

    [Fact]
    public async Task ExchangeRefreshToken_RejectsRevokedToken()
    {
        var issued = await IssueTokensAsync();
        await issued.Service.RevokeRefreshTokenAsync(issued.RefreshToken, CancellationToken.None);

        var refreshed = await issued.Service.ExchangeRefreshTokenAsync(
            MobileAuthOptions.DefaultClientId,
            issued.RefreshToken,
            CancellationToken.None);

        Assert.False(refreshed.Success);
        Assert.Equal("invalid_grant", refreshed.Error);
        Assert.DoesNotContain(issued.RefreshToken!, refreshed.ErrorDescription ?? string.Empty);
    }

    [Fact]
    public async Task ExchangeRefreshToken_RejectsExpiredToken()
    {
        var time = new ManualTimeProvider(new DateTimeOffset(2026, 8, 19, 12, 0, 0, TimeSpan.Zero));
        var service = CreateService(timeProvider: time);
        var pair = MobileAuthPkceTestData.CreatePair();
        var started = service.StartAuthorization(
            "code",
            MobileAuthOptions.DefaultClientId,
            MobileAuthPkceTestData.RedirectUri,
            pair.Challenge,
            MobileAuthPkce.MethodS256,
            "csrf-state",
            MemberAuthenticationSchemes.Google);
        var completed = await service.CompleteExternalLoginAsync(
            started.Session!.RequestId,
            MemberAuthenticationSchemes.Google,
            "expired-refresh-subject",
            "expired-refresh@example.com",
            "Expired Refresh",
            CancellationToken.None);
        var issued = await service.ExchangeAuthorizationCodeAsync(
            "authorization_code",
            MobileAuthOptions.DefaultClientId,
            MobileAuthPkceTestData.RedirectUri,
            completed.Code,
            pair.Verifier,
            CancellationToken.None);
        Assert.True(issued.Success);

        time.Advance(TimeSpan.FromDays(31));
        var refreshed = await service.ExchangeRefreshTokenAsync(
            MobileAuthOptions.DefaultClientId,
            issued.RefreshToken,
            CancellationToken.None);

        Assert.False(refreshed.Success);
        Assert.Equal("invalid_grant", refreshed.Error);
        Assert.DoesNotContain(issued.RefreshToken!, refreshed.ErrorDescription ?? string.Empty);
    }

    [Fact]
    public async Task RevokeAllRefreshTokensForMember_InvalidatesActiveRefresh()
    {
        var issued = await IssueTokensAsync();
        var session = await issued.Service.ExchangeRefreshTokenAsync(
            MobileAuthOptions.DefaultClientId,
            issued.RefreshToken,
            CancellationToken.None);
        Assert.True(session.Success);

        var memberId = Guid.Parse(
            new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
                .ReadJwtToken(session.AccessToken).Subject);
        await issued.Service.RevokeAllRefreshTokensForMemberAsync(memberId, CancellationToken.None);

        var afterRevoke = await issued.Service.ExchangeRefreshTokenAsync(
            MobileAuthOptions.DefaultClientId,
            session.RefreshToken,
            CancellationToken.None);

        Assert.False(afterRevoke.Success);
        Assert.Equal("invalid_grant", afterRevoke.Error);
    }

    [Fact]
    public async Task CompleteExternalLogin_RejectsProviderMismatch()
    {
        var pair = MobileAuthPkceTestData.CreatePair();
        var service = CreateService();
        var started = service.StartAuthorization(
            "code",
            MobileAuthOptions.DefaultClientId,
            MobileAuthPkceTestData.RedirectUri,
            pair.Challenge,
            MobileAuthPkce.MethodS256,
            "csrf-state",
            MemberAuthenticationSchemes.Discord);

        var completed = await service.CompleteExternalLoginAsync(
            started.Session!.RequestId,
            MemberAuthenticationSchemes.Google,
            "discord-subject-1",
            "mix@example.com",
            "Mix",
            CancellationToken.None);

        Assert.False(completed.Success);
        Assert.Equal("access_denied", completed.Error);
        Assert.Null(completed.Code);
    }

    [Fact]
    public void StartAuthorization_FailsClosed_WhenProductionSigningKeyMissing()
    {
        var pair = MobileAuthPkceTestData.CreatePair();
        var result = CreateService(environmentName: "Production").StartAuthorization(
            "code",
            MobileAuthOptions.DefaultClientId,
            MobileAuthPkceTestData.RedirectUri,
            pair.Challenge,
            MobileAuthPkce.MethodS256,
            "state-1",
            MemberAuthenticationSchemes.Google);

        Assert.False(result.Success);
        Assert.Equal("temporarily_unavailable", result.Error);
        Assert.Null(result.Session);
    }

    [Fact]
    public async Task ExchangeAuthorizationCode_FailsClosed_WhenProductionSigningKeyMissing()
    {
        var pair = MobileAuthPkceTestData.CreatePair();
        var result = await CreateService(environmentName: "Production").ExchangeAuthorizationCodeAsync(
            "authorization_code",
            MobileAuthOptions.DefaultClientId,
            MobileAuthPkceTestData.RedirectUri,
            "unused-code",
            pair.Verifier,
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("temporarily_unavailable", result.Error);
        Assert.Null(result.AccessToken);
    }

    [Fact]
    public async Task ExchangeRefreshToken_FailsClosed_WhenProductionSigningKeyMissing()
    {
        var result = await CreateService(environmentName: "Production").ExchangeRefreshTokenAsync(
            MobileAuthOptions.DefaultClientId,
            "unused-refresh-token",
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("temporarily_unavailable", result.Error);
        Assert.Null(result.RefreshToken);
    }

    private static async Task<(MobileAuthService Service, string RefreshToken)> IssueTokensAsync()
    {
        var pair = MobileAuthPkceTestData.CreatePair();
        var service = CreateService();
        var started = service.StartAuthorization(
            "code",
            MobileAuthOptions.DefaultClientId,
            MobileAuthPkceTestData.RedirectUri,
            pair.Challenge,
            MobileAuthPkce.MethodS256,
            "csrf-state",
            MemberAuthenticationSchemes.Google);
        var completed = await service.CompleteExternalLoginAsync(
            started.Session!.RequestId,
            MemberAuthenticationSchemes.Google,
            "refresh-subject-1",
            "refresh@example.com",
            "Refresh Fan",
            CancellationToken.None);
        var tokens = await service.ExchangeAuthorizationCodeAsync(
            "authorization_code",
            MobileAuthOptions.DefaultClientId,
            MobileAuthPkceTestData.RedirectUri,
            completed.Code,
            pair.Verifier,
            CancellationToken.None);
        Assert.True(tokens.Success);
        return (service, tokens.RefreshToken!);
    }

    private static MobileAuthService CreateService(
        InMemoryMemberAccountRepository? accounts = null,
        string environmentName = "Testing",
        TimeProvider? timeProvider = null)
    {
        var clock = timeProvider ?? TimeProvider.System;
        var options = Options.Create(new MobileAuthOptions());
        var site = Options.Create(new SiteOptions());
        var environment = new FakeHostEnvironment(environmentName);
        var members = new MemberAccountService(
            accounts ?? new InMemoryMemberAccountRepository(),
            new InMemoryLegacyMemberLookupRepository(new Dictionary<string, LegacyMemberMatch>()),
            new AzureBlobUploadService(new InMemoryBlobStorageBackend(), Options.Create(new BlobUploadOptions())),
            new MemberUploadQuotaService(
                new Microsoft.Extensions.Caching.Memory.MemoryCache(
                    new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()),
                clock,
                Options.Create(new UploadQuotaOptions { Enabled = false })));

        return new MobileAuthService(
            new MobileAuthAuthorizationSessionStore(clock),
            new InMemoryMobileAuthGrantRepository(new SharedMobileAuthGrantStore()),
            new MobileAuthTokenIssuer(options, site, environment, clock),
            members,
            options,
            clock);
    }

    private sealed class ManualTimeProvider(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset now = start;

        public override DateTimeOffset GetUtcNow() => now;

        public void Advance(TimeSpan delta) => now += delta;
    }
}
