// Queenzone Timeline — researched event data (1970 → today).
// PLEASE VERIFY the facts/dates; the final 2024 entry especially (catalogue deal).
// Category → brand accent:
//   music     Royal Blue     releases & recordings
//   live      Royal Purple   tours & landmark performances
//   milestone Antique Gold   formation, honours, cultural landmarks (rarest accent)
//   loss      Burgundy       Freddie's passing & farewell
// NOTE: the questionnaire listed a "Community" category — I folded the two
// community/fan moments (Tribute Concert, Queenzone founding) into "milestone"
// to keep the palette to four accents. Say the word and I'll split it back out.

window.QZ_CATS = {
  music:     { label: 'Music',     color: 'var(--qz-blue)',     tint: 'var(--qz-blue-tint)',     deep: 'var(--qz-blue-deep)' },
  live:      { label: 'Live',      color: 'var(--qz-purple)',   tint: 'var(--qz-purple-tint)',   deep: 'var(--qz-purple-deep)' },
  milestone: { label: 'Milestone', color: 'var(--qz-gold-deep)',tint: 'var(--qz-gold-tint)',     deep: 'var(--qz-gold-deep)' },
  loss:      { label: 'Loss',      color: 'var(--qz-burgundy)', tint: 'var(--qz-burgundy-tint)', deep: 'var(--qz-burgundy-deep)' },
};

// img: a real asset path (greyscaled in-page) or null for an archival placeholder.
window.QZ_TIMELINE = [
  { year: '1970', cat: 'milestone', title: 'Queen is born',
    text: 'Freddie Bulsara joins Brian May and Roger Taylor\u2019s band Smile, renames it Queen and designs the regal crest that still anchors everything.',
    more: 'The first billed performance as Queen takes place in 1970; Freddie adopts the surname Mercury soon after.', img: null },
  { year: '1971', cat: 'milestone', title: 'The classic line-up',
    text: 'John Deacon joins on bass, completing the four-piece that would stay intact for twenty years.',
    more: 'Deacon was the last of some sixty bassists the band auditioned \u2014 chosen as much for his quiet steadiness as his playing.', img: null },
  { year: '1973', cat: 'music', title: 'The debut album',
    text: 'Queen release their self-titled debut on 13 July 1973, recorded largely in stolen night-time hours at Trident Studios.',
    more: 'Much of it was cut using downtime the band were given in exchange for being \u201Cguinea pigs\u201D for new studio equipment.', img: null },
  { year: '1974', cat: 'music', title: 'Killer Queen',
    text: '\u2018Sheer Heart Attack\u2019 and the \u2018Killer Queen\u2019 single deliver the band\u2019s first major chart success on both sides of the Atlantic.',
    more: 'Written by Freddie, \u2018Killer Queen\u2019 reached No. 2 in the UK and announced the band\u2019s theatrical ambition.', img: null },
  { year: '1975', cat: 'music', title: 'A Night at the Opera',
    text: 'The lavish album \u2014 and \u2018Bohemian Rhapsody\u2019, UK No. 1 for nine weeks \u2014 rewrite the rules of the rock single.',
    more: 'Reputedly the most expensive album ever made to that point; its promo film is often credited as pop\u2019s first true music video.', img: null },
  { year: '1977', cat: 'music', title: 'We Will Rock You',
    text: '\u2018News of the World\u2019 gives the world two of the most-played stadium anthems ever written, back to back on side one.',
    more: '\u2018We Will Rock You\u2019 and \u2018We Are the Champions\u2019 were designed for crowds to sing \u2014 and have been sung ever since.', img: null },
  { year: '1980', cat: 'music', title: 'The Game',
    text: 'Their first US No. 1 album, powered by \u2018Crazy Little Thing Called Love\u2019 and \u2018Another One Bites the Dust\u2019.',
    more: 'The first Queen album to use a synthesiser \u2014 a deliberate break from the \u201Cno synths\u201D note printed on earlier sleeves.', img: null },
  { year: '1981', cat: 'music', title: 'Greatest Hits',
    text: 'The compilation becomes the best-selling album in British history; the band play vast South American stadiums.',
    more: 'It has since sold many millions of copies and is a fixture of \u201Cbest-selling albums of all time\u201D lists.', img: null },
  { year: '1985', cat: 'live', title: 'Live Aid',
    text: 'On 13 July at Wembley, twenty-one minutes widely regarded as the greatest live performance in rock.',
    more: 'The set was tightly rehearsed and played to the back row; it revived the band\u2019s fortunes overnight.', img: 'assets/img-hero.jpg' },
  { year: '1986', cat: 'live', title: 'The Magic Tour',
    text: 'The final tour with all four members draws record crowds; the last show with Freddie takes place at Knebworth on 9 August.',
    more: 'No one knew at the time that Knebworth would be the last time the classic line-up played live together.', img: 'assets/img-crowd.jpg' },
  { year: '1991', cat: 'loss', title: 'Innuendo & farewell',
    text: 'The \u2018Innuendo\u2019 album tops the UK chart. On 24 November, Freddie Mercury dies, a day after confirming his illness.',
    more: '\u2018The Show Must Go On\u2019 \u2014 recorded when he could barely stand \u2014 became a defiant epitaph.', img: 'assets/img-portrait.jpg' },
  { year: '1992', cat: 'milestone', title: 'The Tribute Concert',
    text: 'On 20 April, 72,000 fill Wembley for The Freddie Mercury Tribute Concert, raising awareness and funds for AIDS.',
    more: 'Broadcast worldwide to an estimated billion viewers, it launched the Mercury Phoenix Trust.', img: null },
  { year: '1995', cat: 'music', title: 'Made in Heaven',
    text: 'The posthumous final album, built around Freddie\u2019s last vocals, is released in November.',
    more: 'The surviving members completed the recordings from sessions Freddie insisted on making while he still could.', img: null },
  { year: '1999', cat: 'milestone', title: 'Queenzone.com',
    text: 'The online fan community that would become this archive is founded \u2014 the beginning of two decades of gathering.',
    more: 'From hand-coded HTML to a 100,000-post forum, Queenzone became one of the definitive fan homes on the web.', img: null },
  { year: '2001', cat: 'milestone', title: 'Rock and Roll Hall of Fame',
    text: 'Queen are inducted, formally recognising their place in the history of popular music.',
    more: 'A run of honours followed through the 2000s, including songwriting and industry lifetime awards.', img: null },
  { year: '2002', cat: 'milestone', title: 'We Will Rock You',
    text: 'The Ben Elton musical opens in London\u2019s West End and runs for twelve years, taking the catalogue to the stage.',
    more: 'It went on to be staged in dozens of countries, introducing the songs to a new theatre-going audience.', img: null },
  { year: '2012', cat: 'live', title: 'A new voice',
    text: 'Queen + Adam Lambert play their first shows together; May and Taylor perform at the London Olympics closing ceremony.',
    more: 'The Lambert partnership would grow into sold-out world tours across the following decade.', img: null },
  { year: '2018', cat: 'milestone', title: 'Bohemian Rhapsody',
    text: 'The biopic becomes the highest-grossing music film ever made and goes on to win four Academy Awards.',
    more: 'A new generation discovered the band; catalogue streaming and sales surged around the release.', img: null },
  { year: '2024', cat: 'milestone', title: 'A lasting legacy',
    text: 'Reissues, streaming records and a landmark catalogue deal carry the music \u2014 and this community \u2014 to new generations.',
    more: 'PLEASE VERIFY: the reported catalogue acquisition figure and date before publishing this entry.', img: null },
];

// Decade buckets for grouping + jump navigation.
window.QZ_DECADES = ['1970s', '1980s', '1990s', '2000s', '2010s', '2020s'];
window.qzDecadeOf = function (year) { return year.slice(0, 3) + '0s'; };
