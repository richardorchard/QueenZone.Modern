import { archiveDestinations } from '../../content/archiveHub';
import { media } from '../../content/media';
import type { FeatureItem } from '../../ui/FeatureRail';
import type { ThreadRowItem } from '../../ui/ThreadRow';

export type SamplePhoto = {
  id: string;
  image: number;
  caption: string;
  meta: string[];
  category: 'LIVE' | 'STUDIO' | 'PORTRAITS' | 'BACKSTAGE';
};

export const homeLead = {
  kicker: 'Hero feature',
  title: 'The day Queen stole Live Aid',
  standfirst: 'Twenty-one minutes on a July afternoon in 1985 that rewrote the rules of the stadium show.',
  meta: ['13 July 1985', '8 min read'],
  image: media.hero,
} as const;

export const featuredStories: FeatureItem[] = [
  {
    id: 'bohemian-rhapsody',
    title: 'Inside the making of Bohemian Rhapsody',
    kicker: 'From the vaults',
    kickerRole: 'restored',
    meta: ['1975', '12 min read'],
    image: media.studio,
  },
  {
    id: 'budapest',
    title: 'The night Freddie played Budapest',
    kicker: 'Feature',
    kickerRole: 'featured',
    meta: ['1986', '9 min read'],
    image: media.crowd,
  },
  {
    id: 'kind-of-magic',
    title: 'A Kind of Magic, forty years on',
    kicker: 'Restored',
    kickerRole: 'restored',
    meta: ['1986', '6 min read'],
    image: media.portrait,
  },
];

export const onThisDay = {
  eyebrow: 'This day in Queen history',
  numeral: 'MCMLXXV',
  body: '20 August 1975 — the band begin sessions at Rockfield Studios for the album that would become A Night at the Opera.',
  actionLabel: 'Read the entry',
} as const;

export const archiveShort = archiveDestinations.slice(0, 4);

export const sampleThreads: ThreadRowItem[] = [
  {
    id: 'magic-tour',
    title: 'Which 1986 Magic Tour night is the definitive one?',
    authorInitial: 'B',
    author: 'Brian_77',
    board: 'Live performances',
    replies: '148',
  },
  {
    id: 'mountain-studios',
    title: 'Cataloguing the unreleased Mountain Studios reels',
    authorInitial: 'M',
    author: 'MontreuxMike',
    board: 'The vaults',
    replies: '92',
  },
  {
    id: 'deacon-interviews',
    title: 'John Deacon interviews — a complete index',
    authorInitial: 'D',
    author: 'DeaconField',
    board: 'Members',
    replies: '211',
  },
  {
    id: 'contact-sheets',
    title: 'Restoring the 1977 contact sheets: a method',
    authorInitial: 'S',
    author: 'SilverSalt',
    board: 'Photography',
    replies: '37',
  },
  {
    id: 'original-members',
    title: 'Original Queenzone.com members — sign in here',
    authorInitial: 'A',
    author: 'AdminQZ',
    board: 'Community',
    replies: '604',
  },
  {
    id: 'opera-mix',
    title: 'On the mix of A Night at the Opera',
    authorInitial: 'R',
    author: 'RogerTaylorFan',
    board: 'Recordings',
    replies: '58',
  },
];

export const samplePhotos: SamplePhoto[] = [
  { id: '1', image: media.stage, caption: 'Wembley Stadium, Live Aid', meta: ['13 July 1985', 'Restored 2024'], category: 'LIVE' },
  { id: '2', image: media.crowd, caption: 'Seventy-two thousand, waiting', meta: ['13 July 1985'], category: 'LIVE' },
  { id: '3', image: media.portrait, caption: 'Backstage portrait, unpublished', meta: ['c. 1984'], category: 'PORTRAITS' },
  { id: '4', image: media.studio, caption: 'Rockfield Studios, Monmouthshire', meta: ['August 1975'], category: 'STUDIO' },
  { id: '5', image: media.hero, caption: 'The piano, before the set', meta: ['13 July 1985'], category: 'BACKSTAGE' },
  { id: '6', image: media.crowd, caption: 'Magic Tour, Budapest', meta: ['27 July 1986'], category: 'LIVE' },
  { id: '7', image: media.portrait, caption: 'Contact sheet 04, frame 11', meta: ['c. 1977'], category: 'PORTRAITS' },
  { id: '8', image: media.studio, caption: 'Mixing desk, Mountain Studios', meta: ['c. 1979'], category: 'STUDIO' },
  { id: '9', image: media.stage, caption: 'News of the World tour', meta: ['November 1977'], category: 'LIVE' },
  { id: '10', image: media.hero, caption: 'Soundcheck, Wembley', meta: ['11 July 1986'], category: 'BACKSTAGE' },
  { id: '11', image: media.crowd, caption: 'Knebworth, the final night', meta: ['9 August 1986'], category: 'LIVE' },
  { id: '12', image: media.portrait, caption: 'Studio portrait, unpublished', meta: ['c. 1975'], category: 'PORTRAITS' },
];

export const photoCategories = ['ALL', 'LIVE', 'STUDIO', 'PORTRAITS', 'BACKSTAGE'] as const;

export const searchSuggestions = [
  { title: 'The day Queen stole Live Aid', tag: 'Story · 8 min read', editorial: true, target: 'story' as const },
  { title: 'Live Aid, Wembley Stadium — 14 photographs', tag: 'Gallery', editorial: false, target: 'photos' as const },
  { title: 'Live Aid: the full twenty-one minutes, remastered', tag: 'News · 13 July 1985', editorial: false, target: 'news' as const },
  { title: 'Was Live Aid really the best live set of all time?', tag: 'Forum · 312 replies', editorial: false, target: 'thread' as const },
  { title: 'Rehearsals at the Shaw Theatre, July 1985', tag: 'Story · 5 min read', editorial: true, target: 'story' as const },
];

export const forumStats = [
  { value: '104,882', label: 'Posts' },
  { value: '6,410', label: 'Members' },
  { value: '18', label: 'Boards' },
] as const;
