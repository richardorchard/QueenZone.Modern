namespace QueenZone.Data;

public static class SampleLinksData
{
    public static IReadOnlyList<QueenLinkCategory> CreateSeedCategories() =>
    [
        new QueenLinkCategory(
            1,
            "Official",
            [
                new QueenLink(1, "Queen Online", "https://www.queenonline.com/", "The official Queen website.", 1, true),
                new QueenLink(2, "Brian May", "https://brianmay.com/", "Official site for Brian May.", 1, true),
                new QueenLink(3, "Roger Taylor", "https://rogertaylorofficial.com/", "Official site for Roger Taylor.", 1, true),
            ]),
        new QueenLinkCategory(
            2,
            "Community",
            [
                new QueenLink(4, "Queen Concerts", "https://www.queenconcerts.com/", "Tour dates, setlists and live history.", 2, true),
                new QueenLink(5, "Queen Vault", "https://www.queenvault.com/", "A fan-maintained archive of Queen releases.", 2, false),
            ]),
    ];
}
