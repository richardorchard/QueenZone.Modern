namespace QueenZone.Web.Infrastructure;

/// <summary>
/// Validates member post-login return URLs so redirects stay on-site.
/// Mirrors ASP.NET Core <c>Url.IsLocalUrl</c> for absolute-path local URLs
/// (single leading <c>/</c>), rejecting protocol-relative and absolute external URLs.
/// </summary>
public static class LocalReturnUrl
{
    public const string Default = "/";

    /// <summary>
    /// Returns <paramref name="returnUrl"/> when it is a safe local path; otherwise <see cref="Default"/>.
    /// </summary>
    public static string Resolve(string? returnUrl) =>
        IsLocal(returnUrl) ? returnUrl! : Default;

    /// <summary>
    /// True only for paths that start with a single <c>/</c> (not <c>//</c> or <c>/\</c>)
    /// and contain no control characters.
    /// </summary>
    public static bool IsLocal(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        // Allows "/" or "/foo" but not "//" or "/\".
        if (url[0] != '/')
        {
            return false;
        }

        if (url.Length == 1)
        {
            return true;
        }

        if (url[1] == '/' || url[1] == '\\')
        {
            return false;
        }

        return !HasControlCharacter(url.AsSpan(1));
    }

    private static bool HasControlCharacter(ReadOnlySpan<char> value)
    {
        foreach (var ch in value)
        {
            if (char.IsControl(ch))
            {
                return true;
            }
        }

        return false;
    }
}
