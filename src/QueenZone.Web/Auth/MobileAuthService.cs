using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web;

public sealed class MobileAuthService(
    MobileAuthAuthorizationSessionStore sessions,
    IMobileAuthGrantRepository grants,
    MobileAuthTokenIssuer tokens,
    MemberAccountService memberAccountService,
    IOptions<MobileAuthOptions> options,
    TimeProvider timeProvider)
{
    public MobileAuthStartResult StartAuthorization(
        string? responseType,
        string? clientId,
        string? redirectUri,
        string? codeChallenge,
        string? codeChallengeMethod,
        string? state,
        string? provider)
    {
        var mobile = options.Value;
        if (!string.Equals(responseType, "code", StringComparison.Ordinal))
        {
            return MobileAuthStartResult.Failed("invalid_request", "response_type must be code.");
        }

        if (!string.Equals(clientId, mobile.ClientId, StringComparison.Ordinal))
        {
            return MobileAuthStartResult.Failed("invalid_client", "Unknown client_id.");
        }

        if (!IsRegisteredRedirectUri(mobile, redirectUri))
        {
            return MobileAuthStartResult.Failed(
                "invalid_request",
                "redirect_uri is not registered.",
                redirectSafe: false);
        }

        if (string.IsNullOrWhiteSpace(state) || state.Length > 512)
        {
            return MobileAuthStartResult.Failed("invalid_request", "state is required.", redirectUri, state);
        }

        if (!string.Equals(codeChallengeMethod, MobileAuthPkce.MethodS256, StringComparison.Ordinal)
            || !MobileAuthPkce.IsValidCodeChallenge(codeChallenge))
        {
            return MobileAuthStartResult.Failed(
                "invalid_request",
                "PKCE S256 code_challenge is required.",
                redirectUri,
                state);
        }

        if (!tokens.CanIssueTokens)
        {
            return MobileAuthStartResult.Failed(
                "temporarily_unavailable",
                "Mobile auth is not configured.",
                redirectUri,
                state);
        }

        var normalizedProvider = MemberAuthenticationSchemes.NormalizeExternalProvider(provider);
        if (normalizedProvider is null)
        {
            return MobileAuthStartResult.Failed("invalid_request", "Unknown provider.", redirectUri, state);
        }

        var session = sessions.Create(
            mobile.ClientId,
            redirectUri!,
            codeChallenge!,
            state,
            normalizedProvider,
            TimeSpan.FromMinutes(mobile.AuthorizationCodeLifetimeMinutes));

        return MobileAuthStartResult.Started(session);
    }

    public async Task<MobileAuthCallbackResult> CompleteExternalLoginAsync(
        string? requestId,
        string provider,
        string providerKey,
        string email,
        string displayName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            return MobileAuthCallbackResult.Failed("invalid_request", "Missing authorization request.");
        }

        var session = sessions.Take(requestId);
        if (session is null)
        {
            return MobileAuthCallbackResult.Failed("invalid_request", "Authorization request expired.");
        }

        if (!string.Equals(session.Provider, provider, StringComparison.OrdinalIgnoreCase))
        {
            return MobileAuthCallbackResult.Failed(
                "access_denied",
                "Provider mismatch.",
                session.RedirectUri,
                session.State);
        }

        var account = await memberAccountService.FindOrCreateFromExternalLoginAsync(
            session.Provider,
            providerKey,
            email,
            displayName,
            cancellationToken);

        if (account.IsSuspended)
        {
            return MobileAuthCallbackResult.Failed(
                "access_denied",
                "account_suspended",
                session.RedirectUri,
                session.State);
        }

        var rawCode = MobileAuthPkce.CreateOpaqueToken();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await grants.StoreAuthorizationCodeAsync(
            new MobileAuthAuthorizationCodeEntity
            {
                Id = Guid.NewGuid(),
                CodeHash = MobileAuthPkce.Sha256Hex(rawCode),
                MemberAccountId = account.Id,
                ClientId = session.ClientId,
                RedirectUri = session.RedirectUri,
                CodeChallenge = session.CodeChallenge,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(options.Value.AuthorizationCodeLifetimeMinutes),
            },
            cancellationToken);

        return MobileAuthCallbackResult.Succeeded(session.RedirectUri, session.State, rawCode);
    }

    public async Task<MobileAuthTokenResult> ExchangeAuthorizationCodeAsync(
        string? grantType,
        string? clientId,
        string? redirectUri,
        string? code,
        string? codeVerifier,
        CancellationToken cancellationToken)
    {
        var mobile = options.Value;
        if (!string.Equals(grantType, "authorization_code", StringComparison.Ordinal))
        {
            return MobileAuthTokenResult.Failed("unsupported_grant_type", "grant_type must be authorization_code.");
        }

        if (!string.Equals(clientId, mobile.ClientId, StringComparison.Ordinal)
            || !IsRegisteredRedirectUri(mobile, redirectUri)
            || string.IsNullOrWhiteSpace(code)
            || !MobileAuthPkce.IsValidCodeVerifier(codeVerifier))
        {
            return MobileAuthTokenResult.Failed("invalid_grant", "The authorization code grant is invalid.");
        }

        if (!tokens.CanIssueTokens)
        {
            return MobileAuthTokenResult.Failed("temporarily_unavailable", "Mobile auth is not configured.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var stored = await grants.RedeemAuthorizationCodeAsync(
            MobileAuthPkce.Sha256Hex(code),
            now,
            cancellationToken);

        if (stored is null
            || !string.Equals(stored.ClientId, clientId, StringComparison.Ordinal)
            || !string.Equals(stored.RedirectUri, redirectUri, StringComparison.Ordinal)
            || !MobileAuthPkce.VerifyS256(codeVerifier!, stored.CodeChallenge))
        {
            return MobileAuthTokenResult.Failed("invalid_grant", "The authorization code grant is invalid.");
        }

        var account = await memberAccountService.FindByIdAsync(stored.MemberAccountId, cancellationToken);
        if (account is null || account.IsSuspended)
        {
            return MobileAuthTokenResult.Failed("invalid_grant", "The authorization code grant is invalid.");
        }

        return await IssueTokenPairAsync(account, now, cancellationToken);
    }

    public async Task<MobileAuthTokenResult> ExchangeRefreshTokenAsync(
        string? clientId,
        string? refreshToken,
        CancellationToken cancellationToken)
    {
        var mobile = options.Value;
        if (!string.Equals(clientId, mobile.ClientId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(refreshToken))
        {
            return MobileAuthTokenResult.Failed("invalid_grant", "The refresh token grant is invalid.");
        }

        if (!tokens.CanIssueTokens)
        {
            return MobileAuthTokenResult.Failed("temporarily_unavailable", "Mobile auth is not configured.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var tokenHash = MobileAuthPkce.Sha256Hex(refreshToken);
        var stored = await grants.FindRefreshTokenByHashAsync(tokenHash, cancellationToken);
        if (stored is null)
        {
            return MobileAuthTokenResult.Failed("invalid_grant", "The refresh token grant is invalid.");
        }

        if (stored.RevokedAt is not null)
        {
            await grants.RevokeAllRefreshTokensForMemberAsync(stored.MemberAccountId, now, cancellationToken);
            return MobileAuthTokenResult.Failed("invalid_grant", "The refresh token grant is invalid.");
        }

        if (stored.ExpiresAt <= now
            || !string.Equals(stored.ClientId, clientId, StringComparison.Ordinal)
            || !await grants.TryRevokeRefreshTokenAsync(tokenHash, now, cancellationToken))
        {
            return MobileAuthTokenResult.Failed("invalid_grant", "The refresh token grant is invalid.");
        }

        var account = await memberAccountService.FindByIdAsync(stored.MemberAccountId, cancellationToken);
        if (account is null || account.IsSuspended)
        {
            await grants.RevokeAllRefreshTokensForMemberAsync(stored.MemberAccountId, now, cancellationToken);
            return MobileAuthTokenResult.Failed("invalid_grant", "The refresh token grant is invalid.");
        }

        return await IssueTokenPairAsync(account, now, cancellationToken);
    }

    public async Task RevokeRefreshTokenAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        await grants.TryRevokeRefreshTokenAsync(
            MobileAuthPkce.Sha256Hex(refreshToken),
            now,
            cancellationToken);
    }

    public Task<int> RevokeAllRefreshTokensForMemberAsync(
        Guid memberAccountId,
        CancellationToken cancellationToken) =>
        grants.RevokeAllRefreshTokensForMemberAsync(
            memberAccountId,
            timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);

    private async Task<MobileAuthTokenResult> IssueTokenPairAsync(
        MemberAccount account,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var mobile = options.Value;
        var accessToken = tokens.IssueAccessToken(account.Id, account.Email, account.DisplayName);
        var refreshToken = MobileAuthPkce.CreateOpaqueToken();
        await grants.StoreRefreshTokenAsync(
            new MobileAuthRefreshTokenEntity
            {
                Id = Guid.NewGuid(),
                TokenHash = MobileAuthPkce.Sha256Hex(refreshToken),
                MemberAccountId = account.Id,
                ClientId = mobile.ClientId,
                CreatedAt = utcNow,
                ExpiresAt = utcNow.AddDays(mobile.RefreshTokenLifetimeDays),
            },
            cancellationToken);

        return MobileAuthTokenResult.Succeeded(
            accessToken,
            refreshToken,
            tokens.AccessTokenLifetimeSeconds);
    }

    public static bool IsRegisteredRedirectUri(MobileAuthOptions mobile, string? redirectUri)
    {
        if (string.IsNullOrWhiteSpace(redirectUri)
            || !Uri.TryCreate(redirectUri, UriKind.Absolute, out var parsed)
            || parsed.Scheme is "javascript" or "data" or "file")
        {
            return false;
        }

        return mobile.RedirectUris.Any(allowed =>
            string.Equals(allowed, redirectUri, StringComparison.Ordinal));
    }
}

public sealed record MobileAuthStartResult(
    bool Success,
    string? Error,
    string? ErrorDescription,
    bool RedirectSafe,
    string? RedirectUri,
    string? State,
    MobileAuthAuthorizationSession? Session)
{
    public static MobileAuthStartResult Started(MobileAuthAuthorizationSession session) =>
        new(true, null, null, true, session.RedirectUri, session.State, session);

    public static MobileAuthStartResult Failed(
        string error,
        string description,
        string? redirectUri = null,
        string? state = null,
        bool redirectSafe = true) =>
        new(false, error, description, redirectSafe && redirectUri is not null, redirectUri, state, null);
}

public sealed record MobileAuthCallbackResult(
    bool Success,
    string? Error,
    string? ErrorDescription,
    string? RedirectUri,
    string? State,
    string? Code)
{
    public static MobileAuthCallbackResult Succeeded(string redirectUri, string state, string code) =>
        new(true, null, null, redirectUri, state, code);

    public static MobileAuthCallbackResult Failed(
        string error,
        string description,
        string? redirectUri = null,
        string? state = null) =>
        new(false, error, description, redirectUri, state, null);
}

public sealed record MobileAuthTokenResult(
    bool Success,
    string? Error,
    string? ErrorDescription,
    string? AccessToken,
    string? RefreshToken,
    int ExpiresIn)
{
    public static MobileAuthTokenResult Succeeded(string accessToken, string refreshToken, int expiresIn) =>
        new(true, null, null, accessToken, refreshToken, expiresIn);

    public static MobileAuthTokenResult Failed(string error, string description) =>
        new(false, error, description, null, null, 0);
}
