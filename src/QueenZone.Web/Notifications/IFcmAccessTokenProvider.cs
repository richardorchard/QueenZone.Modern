namespace QueenZone.Web;

/// <summary>
/// OAuth2 access token for FCM HTTP v1. Returns null when credentials are
/// missing or the token cannot be minted — callers skip FCM sends.
/// </summary>
internal interface IFcmAccessTokenProvider
{
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
