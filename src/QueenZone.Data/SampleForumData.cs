namespace QueenZone.Data;

public static class SampleForumData
{
    public static IReadOnlyList<ForumCategoryItem> CreateSeedCategories() =>
    [
        new(
            1,
            "The Music",
            "Albums, songs, lyrics and the catalogue, track by track.",
            41_200,
            new DateTime(2024, 6, 12, 14, 0, 0, DateTimeKind.Utc),
            "Ranking every studio album",
            10),
        new(
            2,
            "Live & Tours",
            "Setlists, bootlegs and memories from every era of touring.",
            28_900,
            new DateTime(2024, 6, 12, 12, 0, 0, DateTimeKind.Utc),
            "Magic Tour — night by night",
            20),
        new(
            3,
            "Recordings & Rarities",
            "Sessions, outtakes, demos and the hunt for lost tapes.",
            19_300,
            new DateTime(2024, 6, 11, 18, 0, 0, DateTimeKind.Utc),
            "Unheard Montreux session tapes",
            30),
        new(
            4,
            "The Archive Project",
            "Restoring and cataloguing the original Queenzone.com archive.",
            6_100,
            new DateTime(2024, 6, 10, 9, 0, 0, DateTimeKind.Utc),
            "Earls Court 1977 negatives",
            40),
        new(
            5,
            "Gear & Technique",
            "The Red Special, amps, harmonies and how the sound was made.",
            11_400,
            new DateTime(2024, 6, 9, 16, 0, 0, DateTimeKind.Utc),
            "Brian May's Red Special mods",
            50),
        new(
            6,
            "The Lounge",
            "Introductions, off-topic and everything in between.",
            14_800,
            new DateTime(2024, 6, 12, 8, 0, 0, DateTimeKind.Utc),
            "How did you find Queenzone?",
            60)
    ];

    public static ForumArchiveStats CreateSeedStats()
    {
        var categories = CreateSeedCategories();
        return new ForumArchiveStats(
            categories.Count,
            12_600,
            categories.Sum(category => (long)category.PostCount));
    }
}