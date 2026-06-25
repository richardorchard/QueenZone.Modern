namespace QueenZone.Data;

public static class SampleBiographyData
{
    public static IReadOnlyList<BiographyChapterItem> CreateSeedChapters() =>
    [
        new(
            1,
            "The Formation of a Band",
            "From the ashes of Smile, a guitarist, a drummer and an art-student singer found their final shape — and a name nobody else would have dared to choose.",
            """
            <p>When the band Smile lost its singer in 1970, guitarist Brian May and drummer Roger Taylor were left with songs and no voice for them. The voice arrived in the form of a flamboyant art student, born Farrokh Bulsara, who had been quietly studying them from the wings — and who promptly renamed himself Freddie Mercury and the group Queen.</p>
            <p>The line-up was completed in early 1971 when bassist John Deacon, the quietest and most technically minded of the four, answered an advertisement. With that, the chemistry was set: two virtuoso instrumentalists, an inventive rhythm section, and a frontman of limitless theatrical ambition.</p>
            <p>They spent two years sharpening their craft in and around London, recording in stolen night-time hours at Trident Studios in exchange for cleaning and odd jobs. The self-titled debut, released in July 1973, announced a band already certain of its identity — heavy, ornate, and unafraid of grandeur.</p>
            """,
            1,
            new DateTime(1973, 7, 13, 0, 0, 0, DateTimeKind.Utc)),
        new(
            2,
            "Breakthrough",
            "Two albums in a single year carried Queen from promising newcomers to genuine chart contenders, with 'Killer Queen' the moment radio finally caught on.",
            """
            <p>Nineteen seventy-four was the year Queen refused to wait their turn. 'Queen II' arrived in the spring, a dense and theatrical song-cycle split between a luminous 'White' side and a darker 'Black' one, and it pushed the band into the upper reaches of the UK chart for the first time.</p>
            <p>Before the year was out they returned with 'Sheer Heart Attack'. Leaner and more focused, it traded some of the sprawl for sharp, radio-ready songwriting — and at its centre sat 'Killer Queen', a glittering miniature that finally delivered the hit single that had so far eluded them.</p>
            """,
            2,
            new DateTime(1974, 11, 8, 0, 0, 0, DateTimeKind.Utc)),
        new(
            3,
            "A Night at the Opera",
            "The most expensive album ever made at the time produced a six-minute single with no chorus — and Queen became a global phenomenon overnight.",
            """
            <p>Determined to make a definitive statement, Queen poured months and an unprecedented budget into 'A Night at the Opera', released in late 1975. The result was a maximalist tour through vaudeville, ballad, hard rock and mock-operatic excess — and, improbably, it held together.</p>
            <p>At its heart was 'Bohemian Rhapsody', a near-six-minute suite with no conventional chorus that the band insisted on releasing as a single despite considerable scepticism. Propelled by one of the earliest promotional videos, it spent nine weeks at the top of the UK chart and became a cultural landmark.</p>
            """,
            3,
            new DateTime(1975, 11, 21, 0, 0, 0, DateTimeKind.Utc)),
        new(
            4,
            "The Stadium Years",
            "With 'We Will Rock You' and 'We Are the Champions', Queen wrote the songs that would echo around sports grounds for generations.",
            """
            <p>By 1977 Queen had grasped something few of their peers understood: that the stadium was not just a bigger room, but a different instrument entirely. 'News of the World' was built for it, opening with the stomp-stomp-clap of 'We Will Rock You' and the raised-fist swell of 'We Are the Champions'.</p>
            <p>Those two songs, designed to be sung back by tens of thousands, became permanent fixtures of public life far beyond rock music. The album stripped away some of the operatic detail in favour of directness and force.</p>
            """,
            4,
            new DateTime(1977, 10, 28, 0, 0, 0, DateTimeKind.Utc)),
        new(
            5,
            "Legacy",
            "From a tribute concert watched by a billion to a posthumous final album, Queen's music has only grown — carried forward by each new generation.",
            """
            <p>In April 1992, the surviving members staged The Freddie Mercury Tribute Concert at Wembley Stadium, a star-studded benefit watched by an estimated billion people worldwide and raising awareness and funds in the fight against AIDS.</p>
            <p>Three years later came 'Made in Heaven', completed from Mercury's final recordings into a luminous, elegiac farewell. It was the last word from the band as a creative force, and a fittingly graceful one.</p>
            <p>In the decades since, Queen's catalogue has only expanded its reach — through compilations, stage musicals, a record-breaking biographical film, and continued touring under the Queen name with guest vocalists.</p>
            """,
            5,
            new DateTime(1995, 11, 6, 0, 0, 0, DateTimeKind.Utc))
    ];
}