namespace QueenZone.Data;

public static class SampleFanPerformanceData
{
    public static IReadOnlyList<FanPerformance> CreateSeedPerformances() =>
    [
        new(187, "Reaching Out", "Mike Ryde",
            "Cover of the Rock Therapy charity single which featured also at the start of 'Return Of The Champion' (Queen & Paul Rodgers).",
            SongFileUrl.Build("2014417798057369.mp3"), 5_120_835, new DateTime(2014, 4, 17, 15, 17, 0, DateTimeKind.Utc)),
        new(186, "Liar", "Mike Ryde",
            "All instruments and vocals by Mike Ryde.",
            SongFileUrl.Build("201446609054910.mp3"), 12_446_824, new DateTime(2014, 4, 6, 16, 26, 0, DateTimeKind.Utc)),
        new(176, "Dear Mr Murdoch", "Manu and Zippo",
            "A fan tribute cover recorded for the QueenZone fan stage.",
            SongFileUrl.Build("2013716161367028.mp3"), 4_946_760, new DateTime(2013, 7, 16, 2, 3, 0, DateTimeKind.Utc)),
        new(173, "Hammer to Fall", "Sonic Snafu",
            "A fan tribute cover recorded for the QueenZone fan stage.",
            SongFileUrl.Build("2013511083375819.mp3"), 4_773_311, new DateTime(2013, 5, 1, 16, 27, 0, DateTimeKind.Utc)),
    ];
}
