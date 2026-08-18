using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace QueenZone.Web;

public sealed class HelpRequestFormStamp(IDataProtectionProvider dataProtectionProvider, TimeProvider timeProvider)
{
    public const string ProtectorPurpose = "QueenZone.HelpRequest.FormStamp";

    private readonly IDataProtector protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);

    public string Issue() =>
        protector.Protect(timeProvider.GetUtcNow().ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture));

    public bool IsAcceptable(string? stamp, int minimumDwellSeconds)
    {
        if (minimumDwellSeconds <= 0)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(stamp))
        {
            return false;
        }

        try
        {
            var payload = protector.Unprotect(stamp);
            if (!long.TryParse(
                    payload,
                    System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var issuedUnixSeconds))
            {
                return false;
            }

            var issuedAt = DateTimeOffset.FromUnixTimeSeconds(issuedUnixSeconds);
            var age = timeProvider.GetUtcNow() - issuedAt;
            return age >= TimeSpan.FromSeconds(minimumDwellSeconds) && age < TimeSpan.FromHours(6);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException or ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}
