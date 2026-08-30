namespace QueenZone.Data;

public static class SampleTriviaData
{
    public static IReadOnlyList<TriviaFactItem> CreateSeedFacts() =>
    [
        new TriviaFactItem(
            1,
            "Freddie Mercury was born Farrokh Bulsara in Zanzibar in 1946.",
            DateTime.UtcNow,
            true,
            "Band",
            TriviaDifficulty.Easy,
            "Queen official biography"),
        new TriviaFactItem(
            2,
            "A Night at the Opera takes its title from the Marx Brothers film of the same name.",
            DateTime.UtcNow,
            true,
            "Albums",
            TriviaDifficulty.Medium,
            null),
        new TriviaFactItem(
            3,
            "Brian May's Red Special was built with his father using wood from a fireplace mantel.",
            DateTime.UtcNow,
            false,
            "Band",
            TriviaDifficulty.Hard,
            "Unpublished draft — not in public rotation"),
    ];
}
