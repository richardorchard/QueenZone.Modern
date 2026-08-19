using System.Security.Claims;
using System.Text.Json;

namespace QueenZone.Web;

internal static class AppleAuthenticationSupport
{
    internal static string NormalizePrivateKey(string privateKey) =>
        privateKey.Replace("\\n", "\n", StringComparison.Ordinal).Trim();

    internal static void AddNameClaim(ClaimsIdentity identity, string? userJson)
    {
        if (identity.HasClaim(claim => claim.Type == ClaimTypes.Name)
            || string.IsNullOrWhiteSpace(userJson))
        {
            return;
        }

        try
        {
            using var user = JsonDocument.Parse(userJson);
            if (!user.RootElement.TryGetProperty("name", out var name))
            {
                return;
            }

            var firstName = GetString(name, "firstName");
            var lastName = GetString(name, "lastName");
            var displayName = string.Join(' ', new[] { firstName, lastName }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
            if (displayName.Length > MemberAccountService.MaxDisplayNameLength)
            {
                displayName = displayName[..MemberAccountService.MaxDisplayNameLength].TrimEnd();
            }

            if (!string.IsNullOrWhiteSpace(displayName))
            {
                identity.AddClaim(new Claim(ClaimTypes.Name, displayName));
            }
        }
        catch (JsonException)
        {
            // The one-time profile payload is optional and untrusted. The verified ID token
            // still supplies the stable subject and email used for account creation/linking.
        }
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()?.Trim()
                : null;
}
