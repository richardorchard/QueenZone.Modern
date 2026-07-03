namespace QueenZone.Data;

public static class SampleQueenHistoryData
{
    public static IReadOnlyList<QueenHistoryEvent> CreateSeedEvents() =>
    [
        Create(1, "Freddie Mercury is born", "Farrokh Bulsara, later known as Freddie Mercury, is born in Zanzibar.", 1946, 9, 5, QueenHistoryEventCategory.Birthday, 95),
        Create(2, "Brian May is born", "Brian May is born in Hampton, Middlesex.", 1947, 7, 19, QueenHistoryEventCategory.Birthday, 90),
        Create(3, "Roger Taylor is born", "Roger Taylor is born in King's Lynn, Norfolk.", 1949, 7, 26, QueenHistoryEventCategory.Birthday, 90),
        Create(4, "John Deacon is born", "John Deacon is born in Leicester.", 1951, 8, 19, QueenHistoryEventCategory.Birthday, 90),
        Create(5, "Queen perform at Live Aid", "Queen play their celebrated Wembley Stadium set at Live Aid.", 1985, 7, 13, QueenHistoryEventCategory.Concert, 100),
        Create(6, "Bohemian Rhapsody is released", "Bohemian Rhapsody is released as a single in the UK.", 1975, 10, 31, QueenHistoryEventCategory.Release, 100),
        Create(7, "The Freddie Mercury Tribute Concert", "The Freddie Mercury Tribute Concert is held at Wembley Stadium.", 1992, 4, 20, QueenHistoryEventCategory.Concert, 95),
        Create(8, "Queen release their debut album", "Queen release their self-titled debut album in the UK.", 1973, 7, 13, QueenHistoryEventCategory.Release, 85),
        Create(9, "John Deacon joins Queen", "John Deacon joins Brian May, Freddie Mercury and Roger Taylor, completing Queen's classic line-up.", 1971, 3, 1, QueenHistoryEventCategory.Other, 80),
        Create(10, "QueenZone modernisation milestone", "The modern QueenZone rebuild tracks archive-first development and public restoration work.", 2026, 7, 3, QueenHistoryEventCategory.SiteHistory, 60),
    ];

    private static QueenHistoryEvent Create(
        int id,
        string title,
        string summary,
        int year,
        int month,
        int day,
        QueenHistoryEventCategory category,
        int importance) =>
        new(
            id,
            title,
            summary,
            new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc),
            QueenHistoryDatePrecision.ExactDate,
            category,
            importance,
            QueenHistoryEventSourceType.Curated,
            $"curated:{id}",
            null,
            true);
}
