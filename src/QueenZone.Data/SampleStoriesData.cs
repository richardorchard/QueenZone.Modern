namespace QueenZone.Data;

public static class SampleStoriesData
{
    public static IReadOnlyList<StoryItem> CreateSeedStories()
    {
        var stories = new List<StoryItem>
        {
            new(
                101,
                "Inside the Making of Bohemian Rhapsody",
                "Six weeks, three studios and a chorus recorded more than 180 times.",
                "<p>Six weeks, three studios and a chorus recorded more than 180 times. This restored archive feature traces the sessions that produced Queen's most ambitious single.</p>",
                new DateTime(2024, 3, 12, 0, 0, 0, DateTimeKind.Utc),
                "Queenzone archive",
                "Recording",
                true),
            new(
                102,
                "Freddie: The Voice That Defined an Era",
                "A thoughtful restored archive feature for the modern site.",
                "<p>A thoughtful restored archive feature for the modern site, preserving the long-form editorial tone of the original Queenzone articles section.</p>",
                new DateTime(2023, 11, 4, 0, 0, 0, DateTimeKind.Utc),
                "Queenzone archive",
                "In Memoriam",
                true),
            new(
                103,
                "The Magic Tour, Night by Night",
                "A structured long-form treatment for tour history.",
                "<p>A structured long-form treatment for tour history, drawn from preserved Queenzone editorial packages.</p>",
                new DateTime(2023, 8, 19, 0, 0, 0, DateTimeKind.Utc),
                "Queenzone archive",
                "Live History",
                true),
            new(
                9001,
                "Hidden moderation draft",
                "This record should never appear in public archive output.",
                "<p>Moderated content remains in the legacy database but is excluded from the modern read-only archive.</p>",
                new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                null,
                null,
                false)
        };

        for (var id = 104; id <= 122; id++)
        {
            var dayOffset = 122 - id;
            stories.Add(new StoryItem(
                id,
                $"Archive sample story {id}",
                $"Excerpt for archive sample story {id}.",
                $"<p>Body for archive sample story {id}.</p>",
                new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(dayOffset),
                "Queenzone archive",
                "Features",
                true));
        }

        return stories;
    }
}