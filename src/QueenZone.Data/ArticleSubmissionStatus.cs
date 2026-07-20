namespace QueenZone.Data;

public static class ArticleSubmissionStatus
{
    public const string Draft = "Draft";
    public const string Submitted = "Submitted";
    public const string UnderReview = "UnderReview";
    public const string ApprovedForPublishing = "ApprovedForPublishing";
    public const string Published = "Published";
    public const string Rejected = "Rejected";
    public const string RequiresRevision = "RequiresRevision";

    public static bool IsTerminal(string status) =>
        status is Published or Rejected;

    public static bool CanBeActedOnByAdmin(string status) =>
        status is Submitted or UnderReview or ApprovedForPublishing;
}
