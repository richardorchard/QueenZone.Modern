// Gallery (Photography) — content data.
// Edit THIS file to add/move/rename collections and images.
//
// In the prototype three restored sleeves stand in for the whole archive,
// keyed a/b/c. In production replace IMAGES with real per-image sources
// (and ideally a separate small thumbSrc) — one entry per photograph.
//
// Each shot row is [imgKey, title, meta, caption]:
//   imgKey  — key into IMAGES (prototype) → real src in production
//   title   — shown under the thumbnail and as the lightbox heading
//   meta    — gold uppercase line (year · format)
//   caption — lightbox-only description
//
// Page count, thumbnail numbering and the "Showing 1–12 of N" range label
// are all derived from shots.length + the perPage prop — never hand-set.

export const IMAGES = {
  a: '/assets/gallery/innuendo.jpg',
  b: '/assets/gallery/greatest-hits-iii.jpg',
  c: '/assets/gallery/hot-space.jpg',
};

export const CATEGORIES = [
  {
    name: 'Album Artwork',
    blurb: 'Original sleeves, inner gatefolds and single covers, scanned front and back.',
    cover: 'a',
    shots: [
      ['a', 'Innuendo', '1991 · LP sleeve', 'The defiant penultimate album cover, photographed for the 1991 Parlophone release.'],
      ['b', 'Greatest Hits III', '1999 · Compilation', 'The third hits collection gathering the solo and late-period material.'],
      ['c', 'Hot Space', '1982 · Gatefold', 'The four-panel grid sleeve for the band’s divisive funk-and-dance turn.'],
      ['a', 'A Night at the Opera', '1975 · Crest sleeve', 'The crest-emblazoned cover of the band’s operatic masterpiece.'],
      ['b', 'News of the World', '1977 · Front', 'Frank Kelly Freas’ robot illustration, restored from an original pressing.'],
      ['c', 'Jazz', '1978 · Inner', 'The eclectic 1978 record’s inner sleeve artwork.'],
      ['a', 'The Game', '1980 · Front', 'The monochrome band portrait that fronted their US breakthrough.'],
      ['b', 'The Works', '1984 · Sleeve', 'The comeback album sleeve, scanned at high resolution.'],
      ['c', 'A Kind of Magic', '1986 · Front', 'The illustrated cover tied to the Highlander era.'],
      ['a', 'Made in Heaven', '1995 · Front', 'The posthumous final album’s cover, shot at Montreux.'],
      ['b', 'The Miracle', '1989 · Morphed faces', 'The composited band portrait of the unified 1989 record.'],
      ['c', 'Sheer Heart Attack', '1974 · Front', 'The oiled-and-prone band shot from the breakthrough album.'],
      ['a', 'Queen II', '1974 · Black side', 'The iconic Mick Rock diamond-pose photograph.'],
      ['b', 'Innuendo', '1991 · Back sleeve', 'The reverse of the Innuendo sleeve with tracklisting.'],
      ['c', 'A Day at the Races', '1976 · Front', 'The companion piece to Opera, in its plain crest sleeve.'],
      ['a', 'Flash Gordon', '1980 · Soundtrack', 'The film-tie-in cover for the cult sci-fi score.'],
      ['b', 'Greatest Hits', '1981 · Compilation', 'The best-selling UK album of all time, in its original sleeve.'],
      ['c', 'Hot Space', '1982 · Back', 'The reverse grid of the Hot Space sleeve.'],
    ],
  },
  {
    name: 'Live & Stadium',
    blurb: 'On stage from the club years to Wembley, Knebworth and Live Aid.',
    cover: 'b',
    shots: [
      ['b', 'Live Aid, Wembley', '1985 · 35mm', 'Freddie at the piano during the celebrated twenty-one minute set.'],
      ['c', 'Magic Tour, Knebworth', '1986 · Stage', 'The final live show before an enormous open-air crowd.'],
      ['a', 'Hyde Park', '1976 · Free concert', 'The free London concert that drew a vast summer crowd.'],
      ['b', 'Hammersmith Odeon', '1979 · Crowd', 'Captured on the Crazy tour of British theatres.'],
      ['c', 'Earls Court', '1977 · Crown rig', 'The famous crown-shaped lighting rig in full effect.'],
      ['a', 'Montreal', '1981 · We Will Rock You', 'From the concert filmed for the live release.'],
      ['b', 'Milton Keynes Bowl', '1982 · Daylight', 'A rare daylight stadium show from the Hot Space tour.'],
      ['c', 'Sun City', '1984 · The Works tour', 'From the controversial Bophuthatswana residency.'],
      ['a', 'Wembley Stadium', '1986 · Two nights', 'Immortalised on film and record on the Magic Tour.'],
      ['b', 'Rock in Rio', '1985 · Brazil', 'Before one of the largest crowds the band ever played.'],
      ['c', 'Budapest', '1986 · Népstadion', 'The landmark show behind the Iron Curtain.'],
      ['a', 'Frankfurt', '1984 · Festhalle', 'From the European leg of The Works tour.'],
      ['b', 'Tokyo', '1975 · Budokan', 'Early scenes of Japanese Queen-mania.'],
      ['c', 'Live Killers era', '1979 · Composite', 'A montage frame from the live-album sessions.'],
    ],
  },
  {
    name: 'Studio Sessions',
    blurb: 'At the desk and behind the glass, from Trident to Mountain.',
    cover: 'c',
    shots: [
      ['c', 'Trident Studios', '1973 · Night sessions', 'Where the debut was cut in stolen night-time hours.'],
      ['a', 'Rockfield', '1975 · Bohemian Rhapsody', 'During the marathon overdub sessions in Wales.'],
      ['b', 'Mountain Studios', '1979 · Montreux', 'The Montreux room the band eventually bought.'],
      ['c', 'Musicland', '1980 · Munich', 'Where much of the early-eighties material took shape.'],
      ['a', 'The Townhouse', '1982 · Mixing', 'Late-night mixing of the Hot Space material.'],
      ['b', 'Sarm West', '1984 · The Works', 'Brian and Freddie at the console.'],
      ['c', 'Olympic Studios', '1989 · The Miracle', 'The unified sessions of the late period.'],
      ['a', 'Metropolis', '1991 · Innuendo', 'The final full-band recordings.'],
    ],
  },
  {
    name: 'Backstage & Candid',
    blurb: 'Dressing rooms, tour buses and quiet moments off stage.',
    cover: 'a',
    shots: [
      ['a', 'Dressing room', '1977 · Polaroid', 'A candid moment before a News of the World show.'],
      ['b', 'On the tour bus', '1978 · Jazz tour', 'Between dates on the North American run.'],
      ['c', 'Soundcheck', '1980 · The Game', 'An empty-arena afternoon soundcheck.'],
      ['a', 'Backstage, Wembley', '1986 · Magic Tour', 'Minutes before walking on stage.'],
      ['b', 'Rehearsal room', '1984 · The Works', 'Working up the live arrangements.'],
      ['c', 'Airport', '1976 · Japan', 'Arriving to crowds of fans in Tokyo.'],
      ['a', 'Hotel suite', '1982 · Hot Space', 'A quiet day off on the European tour.'],
      ['b', 'Catering', '1985 · Rock in Rio', 'A lighter moment behind the scenes.'],
      ['c', 'With the crew', '1986 · Knebworth', 'The band and road crew on the final night.'],
      ['a', 'Press junket', '1989 · The Miracle', 'Promotion duties for the comeback record.'],
    ],
  },
  {
    name: 'Press & Magazines',
    blurb: 'Covers, cuttings and interviews from the music press.',
    cover: 'b',
    shots: [
      ['b', 'Melody Maker', '1974 · Cover', 'An early feature as the band broke through.'],
      ['c', 'NME', '1975 · Bohemian Rhapsody', 'Coverage of the era-defining single.'],
      ['a', 'Rolling Stone', '1977 · Feature', 'The American press takes notice.'],
      ['b', 'Smash Hits', '1980 · Poster', 'A pull-out from the height of their fame.'],
      ['c', 'Record Mirror', '1982 · Interview', 'Discussing the Hot Space direction.'],
      ['a', 'Q Magazine', '1991 · Retrospective', 'A career retrospective from the final year.'],
    ],
  },
  {
    name: 'Memorabilia',
    blurb: 'Tickets, tour programmes, badges and pressed vinyl.',
    cover: 'c',
    shots: [
      ['c', 'Tour programme', '1977 · A World tour', 'The glossy book sold on the News of the World run.'],
      ['a', 'Concert ticket', '1986 · Wembley', 'A stub from the final UK stadium shows.'],
      ['b', 'Promo badge', '1980 · The Game', 'An EMI promotional pin badge.'],
      ['c', 'Picture disc', '1978 · Bicycle Race', 'A collectable shaped picture-disc pressing.'],
      ['a', 'Backstage pass', '1984 · The Works', 'A laminated all-areas tour pass.'],
      ['b', 'Fan-club flyer', '1975 · Official', 'An original membership mailing.'],
    ],
  },
];
