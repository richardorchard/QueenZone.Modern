namespace QueenZone.Data;

internal static class HomePollResultsBuilder
{
    public static HomePollResults Build(
        Guid pollId,
        string question,
        DateTimeOffset? closedAt,
        DateTimeOffset createdAt,
        DateTimeOffset? publishedAt,
        IReadOnlyList<(Guid OptionId, string OptionText, int DisplayOrder)> options,
        IReadOnlyDictionary<Guid, int> optionCounts,
        Guid? selectedOptionId)
    {
        var totalVotes = optionCounts.Values.Sum();
        var isClosed = closedAt is not null;
        var resultOptions = options
            .OrderBy(option => option.DisplayOrder)
            .ThenBy(option => option.OptionText)
            .Select(option =>
            {
                var count = optionCounts.GetValueOrDefault(option.OptionId);
                var percentage = totalVotes == 0 ? 0d : Math.Round(100d * count / totalVotes, 1);
                return new HomePollOptionResult(
                    option.OptionId,
                    option.OptionText,
                    option.DisplayOrder,
                    count,
                    percentage);
            })
            .ToList();

        return new HomePollResults(
            pollId,
            question,
            closedAt,
            createdAt,
            publishedAt,
            totalVotes,
            selectedOptionId is not null,
            selectedOptionId,
            isClosed,
            resultOptions);
    }
}
