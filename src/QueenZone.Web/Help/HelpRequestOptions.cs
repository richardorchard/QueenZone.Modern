namespace QueenZone.Web;

public sealed class HelpRequestOptions
{
    public const string SectionName = "HelpRequests";

    public int MaxAnonymousPerIpPerHour { get; set; } = 3;

    public int MaxPerEmailPerDay { get; set; } = 2;

    public int MaxPerMemberPerDay { get; set; } = 5;

    public int MinimumDwellSeconds { get; set; } = 3;
}
