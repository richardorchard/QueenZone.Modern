using System.Net;

namespace QueenZone.Web;

/// <summary>
/// Formats Q_ALBUM_SONG_T.SONG_LYRICS for display. Lyrics are plain legacy text, not
/// editorial HTML, so this always HTML-encodes the content and only converts line
/// breaks - it must never allow raw markup through (unlike NewsArticleContent.FormatBody,
/// which is for sanitized article bodies and would let stray tags in lyrics text break
/// surrounding page structure, e.g. an unmatched "&lt;/li&gt;"-shaped fragment closing the
/// tracklist early).
/// </summary>
public static class LyricsFormatter
{
    public static string Format(string lyrics) =>
        WebUtility.HtmlEncode(lyrics)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\n", "<br>", StringComparison.Ordinal);
}
