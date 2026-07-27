namespace QueenZone.Data;

/// <summary>
/// Seed content for the in-memory photo repository, used when no legacy connection
/// string is configured (local dev without SQL Server, and the test environment).
/// </summary>
public static class SamplePhotoData
{
    public static IReadOnlyList<PhotoCategorySeed> CreateSeedCategories() =>
    [
        new PhotoCategorySeed(9, "Brian May",
        [
            new PhotoItemSeed(101, "Brian in action with his guitar", "/Brian_May/img-101.jpg", "/Brian_May/img-101-t.jpg", new DateTime(1986, 7, 12)),
            new PhotoItemSeed(102, "Soundcheck, Wembley", "/Brian_May/img-102.jpg", "/Brian_May/img-102-t.jpg", new DateTime(1986, 7, 11)),
            new PhotoItemSeed(103, "Red Special close-up", "/Brian_May/img-103.jpg", "/Brian_May/img-103-t.jpg", new DateTime(1980, 3, 2)),
        ]),
        new PhotoCategorySeed(12, "Queen",
        [
            new PhotoItemSeed(201, "Live Aid, Wembley", "/Queen/img-201.jpg", "/Queen/img-201-t.jpg", new DateTime(1985, 7, 13)),
            new PhotoItemSeed(202, "Magic Tour, Knebworth", "/Queen/img-202.jpg", "/Queen/img-202-t.jpg", new DateTime(1986, 8, 9)),
            new PhotoItemSeed(203, "Hyde Park", "/Queen/img-203.jpg", "/Queen/img-203-t.jpg", new DateTime(1976, 9, 18)),
            new PhotoItemSeed(204, "Earls Court crown rig", "/Queen/img-204.jpg", "/Queen/img-204-t.jpg", new DateTime(1977, 6, 6)),
        ]),
        new PhotoCategorySeed(18, "Freddie Mercury",
        [
            new PhotoItemSeed(301, "Freddie at the piano", "/Freddie_Mercury/img-301.jpg", "/Freddie_Mercury/img-301-t.jpg", new DateTime(1986, 7, 12)),
            new PhotoItemSeed(302, "Freddie on stage", "/Freddie_Mercury/img-302.jpg", "/Freddie_Mercury/img-302-t.jpg", new DateTime(1985, 7, 13)),
            new PhotoItemSeed(303, "Freddie portrait", "/Freddie_Mercury/img-303.jpg", "/Freddie_Mercury/img-303-t.jpg", new DateTime(1977, 10, 6)),
            new PhotoItemSeed(304, "Freddie in rehearsal", "/Freddie_Mercury/img-304.jpg", "/Freddie_Mercury/img-304-t.jpg", new DateTime(1980, 3, 2)),
        ]),
    ];
}

public sealed record PhotoCategorySeed(int CatId, string Name, IReadOnlyList<PhotoItemSeed> Items);

public sealed record PhotoItemSeed(int PicId, string Title, string Url, string ThumbUrl, DateTime DateTime);
