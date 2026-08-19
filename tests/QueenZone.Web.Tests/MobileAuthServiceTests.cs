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
            "refresh_token",
            MobileAuthOptions.DefaultClientId,
            MobileAuthPkceTestData.RedirectUri,
            "code",
            "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk",
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("unsupported_grant_type", result.Error);
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

    private static MobileAuthService CreateService(InMemoryMemberAccountRepository? accounts = null)
    {
        var options = Options.Create(new MobileAuthOptions());
        var site = Options.Create(new SiteOptions());
        var environment = new FakeHostEnvironment("Testing");
        var members = new MemberAccountService(
            accounts ?? new InMemoryMemberAccountRepository(),
            new InMemoryLegacyMemberLookupRepository(new Dictionary<string, LegacyMemberMatch>()),
            new AzureBlobUploadService(new InMemoryBlobStorageBackend(), Options.Create(new BlobUploadOptions())),
            new MemberUploadQuotaService(
                new Microsoft.Extensions.Caching.Memory.MemoryCache(
                    new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()),
                TimeProvider.System,
                Options.Create(new UploadQuotaOptions { Enabled = false })));

        return new MobileAuthService(
            new MobileAuthAuthorizationSessionStore(TimeProvider.System),
            new InMemoryMobileAuthGrantRepository(new SharedMobileAuthGrantStore()),
            new MobileAuthTokenIssuer(options, site, environment, TimeProvider.System),
            members,
            options,
            TimeProvider.System);
    }
}
