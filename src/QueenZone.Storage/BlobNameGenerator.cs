using System.Text;

namespace QueenZone.Storage;

internal static class BlobNameGenerator
{
    public static string Create(string originalFileName, BlobUploadContext? context)
    {
        var extension = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(extension) || extension.Length > 16)
        {
            extension = string.Empty;
        }
        else
        {
            extension = extension.ToLowerInvariant();
        }

        var id = Guid.NewGuid().ToString("N");
        if (context?.MemberId is int memberId and > 0)
        {
            return $"members/{memberId}/{id}{extension}";
        }

        if (!string.IsNullOrWhiteSpace(context?.ActorEmail))
        {
            var slug = SanitizeSegment(context.ActorEmail);
            return $"editors/{slug}/{id}{extension}";
        }

        return $"anonymous/{id}{extension}";
    }

    private static string SanitizeSegment(string value)
    {
        var trimmed = value.Trim().ToLowerInvariant();
        var builder = new StringBuilder(trimmed.Length);
        foreach (var ch in trimmed)
        {
            if (char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_' or '.')
            {
                builder.Append(ch);
            }
            else if (ch is '@' or '+')
            {
                builder.Append('-');
            }
        }

        var result = builder.ToString().Trim('-', '.');
        return string.IsNullOrEmpty(result) ? "unknown" : result;
    }
}
