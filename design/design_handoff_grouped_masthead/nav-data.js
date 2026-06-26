// Grouped Masthead — information architecture.
// Three top-level groups; new sections slot into an existing group so the
// nav bar never grows. Edit THIS file to add/move/rename a section.
//
// accent: brand token used for the group's eyebrow label + hover bar.
// items[].tag: optional pill (e.g. 'New'); rendered in antique gold.
// items[].href: real route — replace the '#' placeholders on integration.

export const GROUPS = [
  {
    label: 'The Band',
    eyebrow: 'About Queen',
    accent: 'var(--qz-purple)',
    items: [
      { title: 'Biography',   href: '#', desc: 'The story of the band, member by member', tag: 'New' },
      { title: 'Discography', href: '#', desc: 'Every studio album, single and release',   tag: 'New' },
      { title: 'Timeline',    href: '#', desc: 'Five decades, year by year' },
    ],
  },
  {
    label: 'Archive',
    eyebrow: 'The Publication',
    accent: 'var(--qz-blue)',
    items: [
      { title: 'News',        href: '#', desc: '4,000+ articles from the original archive' },
      { title: 'Stories',     href: '#', desc: 'Long-form features and editorial' },
      { title: 'Photography', href: '#', desc: 'Tens of thousands of restored images' },
    ],
  },
  {
    label: 'Community',
    eyebrow: 'The Fans',
    accent: 'var(--qz-burgundy)',
    items: [
      { title: 'Forum',            href: '#', desc: '100,000+ posts from the membership' },
      { title: 'Fan Performances', href: '#', desc: 'Covers, tributes and live sets', tag: 'New' },
    ],
  },
];
