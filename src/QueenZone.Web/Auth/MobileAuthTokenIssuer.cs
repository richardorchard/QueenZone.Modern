using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace QueenZone.Web;

public sealed class MobileAuthTokenIssuer(
    IOptions<MobileAuthOptions> options,
    IOptions<SiteOptions> site,
    IHostEnvironment environment,
    TimeProvider timeProvider)
{
    public string Issuer => site.Value.PublicBaseUrl.TrimEnd('/');

    public string Audience => options.Value.ClientId;

    public int AccessTokenLifetimeSeconds =>
        Math.Max(1, options.Value.AccessTokenLifetimeMinutes) * 60;

    public string IssueAccessToken(Guid memberId, string email, string displayName)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var signingKey = options.Value.ResolveSigningKey(QueenZoneEnvironments.IsProductionLike(environment));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, memberId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, memberId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, displayName),
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(options.Value.AccessTokenLifetimeMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static void ConfigureJwtBearer(
        JwtBearerOptions jwt,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var mobile = configuration.GetSection(MobileAuthOptions.SectionName).Get<MobileAuthOptions>()
            ?? new MobileAuthOptions();
        var site = configuration.GetSection(SiteOptions.SectionName).Get<SiteOptions>()
            ?? new SiteOptions();
        var signingKey = mobile.ResolveSigningKey(QueenZoneEnvironments.IsProductionLike(environment));
        if (string.IsNullOrEmpty(signingKey))
        {
            signingKey = MobileAuthOptions.DevelopmentSigningKey;
        }

        jwt.MapInboundClaims = true;
        jwt.TokenValidationParameters = CreateValidationParameters(
            issuer: site.PublicBaseUrl.TrimEnd('/'),
            audience: string.IsNullOrWhiteSpace(mobile.ClientId)
                ? MobileAuthOptions.DefaultClientId
                : mobile.ClientId,
            signingKey: signingKey);
        jwt.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                // JSON APIs should not redirect to /account/login.
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.WWWAuthenticate = "Bearer";
                return Task.CompletedTask;
            },
        };
    }

    public static TokenValidationParameters CreateValidationParameters(
        string issuer,
        string audience,
        string signingKey) =>
        new()
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
        };
}
