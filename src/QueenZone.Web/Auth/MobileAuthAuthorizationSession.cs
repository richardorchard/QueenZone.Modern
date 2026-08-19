namespace QueenZone.Web;

public sealed record MobileAuthAuthorizationSession(
    string RequestId,
    string ClientId,
    string RedirectUri,
    string CodeChallenge,
    string State,
    string Provider,
    DateTimeOffset ExpiresAt);
