using QueenZone.Data;

namespace QueenZone.NewsAgent;

public static class NewsTriageDuplicateRules
{
    public static bool IsActiveDuplicateSource(NewsCandidateStatus status) =>
        NewsCandidateDuplicateRules.IsActiveDuplicateSource(status);
}
