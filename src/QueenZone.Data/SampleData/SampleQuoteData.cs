namespace QueenZone.Data;

public static class SampleQuoteData
{
    public static IReadOnlyList<QuoteItem> CreateSeedQuotes() =>
    [
        new QuoteItem(
            1,
            "I won't be a rock star. I will be a legend.",
            "Freddie Mercury",
            DateTime.UtcNow,
            true),
        new QuoteItem(
            2,
            "We want to be the Cecil B. DeMille of rock and roll.",
            "Freddie Mercury",
            DateTime.UtcNow,
            true),
        new QuoteItem(
            3,
            "Queen was always a very theatrical band.",
            "Brian May",
            DateTime.UtcNow,
            true),
    ];
}
