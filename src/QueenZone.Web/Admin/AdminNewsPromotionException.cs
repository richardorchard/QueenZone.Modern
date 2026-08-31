namespace QueenZone.Web;

/// <summary>
/// User-facing promotion failure (workflow or unexpected write error).
/// </summary>
public sealed class AdminNewsPromotionException(string message) : Exception(message);
