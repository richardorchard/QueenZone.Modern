namespace QueenZone.Web;

public sealed class NewsSuggestionOptions
{
    public const string SectionName = "NewsSuggestions";

    public int MaxSubmissionsPerMemberPerDay { get; set; } = 5;
}
