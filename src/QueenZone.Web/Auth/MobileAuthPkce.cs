using System.Security.Cryptography;
using System.Text;

namespace QueenZone.Web;

/// <summary>
/// RFC 7636 S256 helpers for the mobile public client. The PKCE pair is generated on the
/// device; QueenZone only stores the challenge and later verifies the verifier.
/// </summary>
public static class MobileAuthPkce
{
    public const string MethodS256 = "S256";

    public const int MinVerifierLength = 43;

    public const int MaxVerifierLength = 128;

    public static bool IsValidCodeVerifier(string? verifier)
    {
        if (string.IsNullOrEmpty(verifier)
            || verifier.Length < MinVerifierLength
            || verifier.Length > MaxVerifierLength)
        {
            return false;
        }

        foreach (var character in verifier)
        {
            if (!IsUnreserved(character))
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsValidCodeChallenge(string? challenge) =>
        IsValidCodeVerifier(challenge);

    public static string CreateS256Challenge(string verifier)
    {
        ArgumentException.ThrowIfNullOrEmpty(verifier);
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return ToBase64Url(hash);
    }

    public static bool VerifyS256(string verifier, string challenge)
    {
        if (!IsValidCodeVerifier(verifier) || !IsValidCodeChallenge(challenge))
        {
            return false;
        }

        var expected = CreateS256Challenge(verifier);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(challenge));
    }

    public static string ToBase64Url(ReadOnlySpan<byte> bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string CreateOpaqueToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return ToBase64Url(bytes);
    }

    public static string Sha256Hex(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash);
    }

    private static bool IsUnreserved(char character) =>
        char.IsAsciiLetterOrDigit(character)
        || character is '-' or '.' or '_' or '~';
}
