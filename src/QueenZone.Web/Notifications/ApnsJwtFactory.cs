using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace QueenZone.Web;

internal sealed class ApnsJwtFactory
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(50);

    private readonly Lock gate = new();
    private string? cachedJwt;
    private DateTimeOffset expiresAt;

    public string? TryCreateToken(ApnsPushOptions options)
    {
        if (!OptionsValidation.LooksConfigured(options.TeamId)
            || !OptionsValidation.LooksConfigured(options.KeyId)
            || !OptionsValidation.LooksConfigured(options.PrivateKeyPem))
        {
            return null;
        }

        lock (gate)
        {
            var now = DateTimeOffset.UtcNow;
            if (cachedJwt is not null && now < expiresAt)
            {
                return cachedJwt;
            }

            cachedJwt = Sign(options);
            expiresAt = now.Add(TokenLifetime);
            return cachedJwt;
        }
    }

    private static string Sign(ApnsPushOptions options)
    {
        var pem = AppleAuthenticationSupport.NormalizePrivateKey(options.PrivateKeyPem!);
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(pem);

        var key = new ECDsaSecurityKey(ecdsa) { KeyId = options.KeyId!.Trim() };
        var credentials = new SigningCredentials(key, SecurityAlgorithms.EcdsaSha256);
        var issuedAt = DateTimeOffset.UtcNow;
        var token = new JwtSecurityToken(
            issuer: options.TeamId!.Trim(),
            claims:
            [
                new Claim(
                    JwtRegisteredClaimNames.Iat,
                    issuedAt.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64),
            ],
            expires: issuedAt.UtcDateTime.Add(TokenLifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
