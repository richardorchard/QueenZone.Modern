using System.Diagnostics;

namespace QueenZone.Web;

public static class QueenZoneTelemetry
{
    public const string ActivitySourceName = "QueenZone.Web";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
