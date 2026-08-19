using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class SharedMobileAuthGrantStore
{
    public Lock Gate { get; } = new();

    public List<MobileAuthAuthorizationCodeEntity> AuthorizationCodes { get; } = [];

    public List<MobileAuthRefreshTokenEntity> RefreshTokens { get; } = [];
}
