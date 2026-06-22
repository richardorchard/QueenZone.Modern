# Queenzone — UI Kit Source Reference

Full source for the website recreation (desktop + mobile). These compose the primitives in COMPONENT_SOURCE.md. They are visual/interaction references, not production code — recreate the screens in your codebase using its routing, data and component patterns.


---

## `ui_kits/website/README.md`

```md
# Queenzone Website — UI Kit

A high-fidelity recreation of **Queenzone.org** — the preserved archive of the original Queenzone.com community. Editorial, cinematic and mobile-first; ~90% monochrome with sparing accent colour, built entirely from the design system's components and tokens. An alternating dark/light section rhythm (rich-black Featured Articles, This Day and footer) gives it a distinct "collector's box-set" identity rather than a bright official-site look.

## Run it
- **Desktop:** open `index.html` — full multi-page click-through.
- **Mobile:** open `mobile.html` — four mobile screens in iPhone frames.

Both load the compiled bundle (`window.QueenzoneDesignSystem_6c12e8`), then the local `.jsx` surfaces.

## Pages (desktop — `index.html`)
The header nav routes between full pages: **News · Articles · Photography · Forum · Timeline**, plus the homepage and article view. The masthead is the **inverted (dark) variant** — rich-black background, white wordmark, gold active state — set via `<Header dark />`; every page opens on a dark hero or page-hero so it flows seamlessly.

| File | Surface |
|---|---|
| `App.jsx` | Root router (home · news · articles · gallery · timeline · article) + search overlay |
| `Header.jsx` | Sticky header — crest mark, wordmark, page nav (active state), search, sign-in |
| `Hero.jsx` | Full-bleed cinematic hero feature |
| `Sections1.jsx` | Explore the Archive · **Featured Articles** (dark, tag filter) · Featured Photography |
| `Sections2.jsx` | **This Day in Queen History** (dark) · Popular Discussions · Recently Restored · Timeline Highlights |
| `Pages1.jsx` | `PageHero` · **News Index** (year filter, list) · **Articles Index** (lead feature + grid) |
| `Pages2.jsx` | **Photo Gallery** (filterable masonry + lightbox) · **Timeline** (vertical, alternating) |
| `Forum.jsx` | **Forum** — community masthead with stats, board index, recent-threads table |
| `Footer.jsx` | Rich-black footer with crest watermark + newsletter |
| `ArticleView.jsx` | Long-form reading view with drop-cap and archival hero |
| `data.js` | All page content (`window.QZ_DATA`) |

## Mobile (`mobile.html`)
`MobileScreens.jsx` — **Home · News · Photography · Article**, each rendered inside an `IOSDevice` frame (`ios-frame.jsx` starter).

## Interactions
- **Navigate** — header nav and the Explore-the-Archive cards route between pages.
- **Read an article** — hero CTA, Featured Article cards and News rows open the article view; "Back" returns home.
- **Filter** — tag/year/category chips on Articles, News and Gallery.
- **Lightbox** — click any photo in the Gallery to open it full-screen.
- **Search** — header search opens a full-screen overlay; header blurs on scroll.

## Components used
`Button` · `IconButton` · `Input` · `Badge` · `Tag` · `SectionHeader` · `ArticleCard` · `CrestSeal`, plus Lucide icons (CDN) and the crest assets in `/assets`.

## Notes
Imagery uses abstract monochrome placeholders (`/assets/img-*.jpg`) standing in for licensed archival Queen photography — replace with real restored photographs in production.
```

---

## `ui_kits/website/data.js`

All page content (`window.QZ_DATA`). Replace with your CMS / data layer.

```jsx
// Queenzone homepage content — fictional but plausible archive material.
// Tone: knowledgeable, passionate, respectful. No clickbait.
window.QZ_DATA = {
  hero: {
    category: 'Featured Article',
    title: 'The Day Queen Stole Live Aid',
    standfirst: 'Twenty-one minutes on a July afternoon in 1985 that rewrote the rules of the stadium show — and turned a struggling band\u2019s fortunes on their head.',
    meta: '13 July 1985 · 8 min read',
    image: '../../assets/img-hero.jpg',
  },

  explore: [
    { label: 'News Archive', count: '4,000+ articles', icon: 'newspaper', tone: 'cta' },
    { label: 'Articles', count: '100+ features', icon: 'book-open', tone: 'editorial' },
    { label: 'Photography', count: 'Tens of thousands', icon: 'camera', tone: 'archive' },
    { label: 'Forum History', count: '100,000+ posts', icon: 'messages-square', tone: 'cta' },
  ],

  featured: [
    { image: '../../assets/img-studio.jpg', category: 'Recording', title: 'Inside the Making of Bohemian Rhapsody', excerpt: 'Six weeks, three studios and a chorus recorded more than 180 times.', meta: 'Restored archive · 12 min read', badge: { tone: 'archive', label: 'Archive' } },
    { image: '../../assets/img-portrait.jpg', category: 'In Memoriam', title: 'Freddie: The Voice That Defined an Era', excerpt: 'A four-octave range, and a presence no stadium could contain.', meta: '5 September · 9 min read', badge: { tone: 'editorial', label: 'Featured' } },
    { image: '../../assets/img-crowd.jpg', category: 'Live History', title: 'The Magic Tour, Night by Night', excerpt: 'The 1986 run that would become the final tour with all four members.', meta: 'Restored archive · 15 min read', badge: null },
  ],

  photography: [
    { image: '../../assets/img-stage.jpg', caption: 'Wembley Stadium, July 1986' },
    { image: '../../assets/img-portrait.jpg', caption: 'Studio portrait, 1974' },
    { image: '../../assets/img-crowd.jpg', caption: 'Hyde Park, September 1976' },
    { image: '../../assets/img-studio.jpg', caption: 'Mountain Studios, Montreux' },
  ],

  thisDay: [
    { date: '13 Jul 1985', text: 'Queen perform at Live Aid, Wembley Stadium, in a set later voted the greatest live performance in rock history.' },
    { date: '31 Oct 1975', text: '\u2018Bohemian Rhapsody\u2019 is released as a single, breaking every rule of contemporary radio.' },
    { date: '20 Apr 1992', text: 'The Freddie Mercury Tribute Concert is held at Wembley before 72,000 people.' },
  ],

  discussions: [
    { title: 'The definitive ranking of every studio album', replies: 1284, era: 'Albums', last: '2h ago' },
    { title: 'Unheard Montreux session tapes — what do we know?', replies: 642, era: 'Recordings', last: '5h ago' },
    { title: 'Restoring the 1977 News of the World tour photos', replies: 318, era: 'Photography', last: '1d ago' },
    { title: 'Brian May\u2019s Red Special: every documented modification', replies: 906, era: 'Gear', last: '2d ago' },
  ],

  restored: [
    { image: '../../assets/img-crowd.jpg', category: 'Restored', title: 'Earls Court 1977: The Lost Negatives', meta: 'Restored June 2026' },
    { image: '../../assets/img-studio.jpg', category: 'Restored', title: 'The Trident Studios Demo Reels', meta: 'Restored May 2026' },
  ],

  timeline: [
    { year: '1971', text: 'The classic line-up is complete as John Deacon joins.' },
    { year: '1975', text: 'A Night at the Opera becomes the most expensive album ever made.' },
    { year: '1985', text: 'Live Aid cements Queen as the greatest live band of their generation.' },
    { year: '1991', text: 'The world loses Freddie Mercury; the legacy endures.' },
  ],

  tags: ['All', 'A Night at the Opera', 'Live Aid', 'Freddie Mercury', 'Brian May', '1970s', '1980s', 'Recordings'],

  // ---- News index (chronological archive list) ----
  newsYears: ['All', '1985', '1986', '1991', '1992', '2011'],
  news: [
    { date: '13 Jul 1985', cat: 'Live', title: 'Queen confirmed for Live Aid at Wembley Stadium', excerpt: 'The band will take a 20-minute slot on the afternoon bill alongside the era\u2019s biggest names.' },
    { date: '02 Jun 1986', cat: 'Tour', title: 'The Magic Tour opens in Stockholm', excerpt: 'A new stage design and the largest production the band has yet mounted across Europe.' },
    { date: '12 Aug 1986', cat: 'Live', title: 'Knebworth Park draws a record crowd', excerpt: 'What would become the final concert with all four original members.' },
    { date: '24 Nov 1991', cat: 'News', title: 'A statement from the band', excerpt: 'The community gathers as news reaches fans across the world.' },
    { date: '20 Apr 1992', cat: 'Tribute', title: 'The Tribute Concert fills Wembley once more', excerpt: '72,000 attend as artists from across music pay their respects.' },
    { date: '07 Mar 2011', cat: 'Reissue', title: 'The remastered studio catalogue is announced', excerpt: 'Forty years of recordings restored and reissued for a new generation.' },
  ],

  // ---- Photo gallery (archive grid) ----
  galleryFilters: ['All', 'Live', 'Studio', 'Portrait', 'Backstage'],
  gallery: [
    { image: '../../assets/img-stage.jpg', caption: 'Wembley Stadium', year: '1986', cat: 'Live', span: 'tall' },
    { image: '../../assets/img-portrait.jpg', caption: 'Studio portrait', year: '1974', cat: 'Portrait', span: 'tall' },
    { image: '../../assets/img-crowd.jpg', caption: 'Hyde Park', year: '1976', cat: 'Live', span: 'wide' },
    { image: '../../assets/img-studio.jpg', caption: 'Mountain Studios', year: '1978', cat: 'Studio', span: 'normal' },
    { image: '../../assets/img-hero.jpg', caption: 'Live Aid', year: '1985', cat: 'Live', span: 'wide' },
    { image: '../../assets/img-stage.jpg', caption: 'Earls Court', year: '1977', cat: 'Live', span: 'normal' },
    { image: '../../assets/img-studio.jpg', caption: 'Trident Studios', year: '1973', cat: 'Studio', span: 'normal' },
    { image: '../../assets/img-portrait.jpg', caption: 'Backstage, Montreal', year: '1981', cat: 'Backstage', span: 'tall' },
    { image: '../../assets/img-crowd.jpg', caption: 'Rock in Rio', year: '1985', cat: 'Live', span: 'wide' },
  ],

  // ---- Full timeline ----
  timelineFull: [
    { year: '1970', title: 'A band is named', text: 'Brian May and Roger Taylor are joined by a new singer, who renames the band Queen.' },
    { year: '1971', title: 'The line-up completes', text: 'John Deacon joins on bass, completing the classic four-piece.' },
    { year: '1973', title: 'The debut album', text: 'Queen release their self-titled debut, recorded largely in down-time at Trident Studios.' },
    { year: '1975', title: 'A Night at the Opera', text: 'The most expensive album ever made to that point, and the arrival of \u2018Bohemian Rhapsody\u2019.' },
    { year: '1981', title: 'Greatest Hits', text: 'The compilation becomes one of the best-selling albums in history.' },
    { year: '1985', title: 'Live Aid', text: 'Twenty-one minutes that cement Queen as the greatest live band of their generation.' },
    { year: '1986', title: 'The Magic Tour', text: 'The final tour with all four original members draws record crowds across Europe.' },
    { year: '1991', title: 'A legacy endures', text: 'The world loses Freddie Mercury; the music and community carry on.' },
  ],

  // ---- Forum / community ----
  forumStats: { members: '18,400', threads: '12,600', posts: '100,000+' },
  forumBoards: [
    { name: 'The Music', desc: 'Albums, songs, lyrics and the catalogue, track by track.', icon: 'disc-3', threads: 3120, posts: '41,200', last: { thread: 'Ranking every studio album', when: '2h ago' } },
    { name: 'Live & Tours', desc: 'Setlists, bootlegs and memories from every era of touring.', icon: 'mic-2', threads: 2480, posts: '28,900', last: { thread: 'Magic Tour — night by night', when: '4h ago' } },
    { name: 'Recordings & Rarities', desc: 'Sessions, outtakes, demos and the hunt for lost tapes.', icon: 'radio', threads: 1760, posts: '19,300', last: { thread: 'Unheard Montreux session tapes', when: '5h ago' } },
    { name: 'The Archive Project', desc: 'Restoring and cataloguing the original Queenzone.com archive.', icon: 'archive', threads: 540, posts: '6,100', last: { thread: 'Earls Court 1977 negatives', when: '1d ago' } },
    { name: 'Gear & Technique', desc: 'The Red Special, amps, harmonies and how the sound was made.', icon: 'guitar', threads: 980, posts: '11,400', last: { thread: 'Brian May\u2019s Red Special mods', when: '2d ago' } },
    { name: 'The Lounge', desc: 'Introductions, off-topic and everything in between.', icon: 'armchair', threads: 1720, posts: '14,800', last: { thread: 'How did you find Queenzone?', when: '6h ago' } },
  ],
  forumThreads: [
    { title: 'The definitive ranking of every studio album', board: 'The Music', author: 'brightonrock', replies: 1284, views: '24.1k', when: '2h ago', pinned: true },
    { title: 'Unheard Montreux session tapes — what do we know?', board: 'Recordings & Rarities', author: 'mountain_studio', replies: 642, views: '11.8k', when: '5h ago' },
    { title: 'Restoring the 1977 News of the World tour photos', board: 'The Archive Project', author: 'negative_space', replies: 318, views: '6.3k', when: '1d ago' },
    { title: 'Brian May\u2019s Red Special: every documented modification', board: 'Gear & Technique', author: 'sixpence', replies: 906, views: '18.0k', when: '2d ago' },
    { title: 'Your first Queen concert — share the memory', board: 'Live & Tours', author: 'somebodytolove', replies: 2104, views: '39.5k', when: '3h ago' },
  ],
};
```

---

## `ui_kits/website/App.jsx`

Root router + search overlay.

```jsx
// Search overlay — full-screen, editorial, with quick suggestions.
function SearchOverlay({ open, onClose }) {
  const { Input, Tag } = window.QueenzoneDesignSystem_6c12e8;
  React.useEffect(() => { if (open) setTimeout(() => window.lucide && window.lucide.createIcons(), 30); }, [open]);
  if (!open) return null;
  const suggestions = ['Bohemian Rhapsody', 'Live Aid 1985', 'A Night at the Opera', 'Freddie Mercury', 'Wembley 1986', 'Brian May'];
  return (
    <div onClick={onClose} style={{ position: 'fixed', inset: 0, zIndex: 100, background: 'rgba(17,17,17,0.72)', backdropFilter: 'blur(8px)', display: 'flex', alignItems: 'flex-start', justifyContent: 'center', paddingTop: '12vh', animation: 'qzFade 240ms ease' }}>
      <div onClick={(e) => e.stopPropagation()} style={{ width: 'min(720px, 90vw)', background: 'var(--qz-white)', borderRadius: 'var(--radius-md)', boxShadow: 'var(--shadow-lift)', padding: 36, position: 'relative' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 8 }}>
          <img src="../../assets/crest-black.png" alt="" style={{ height: 30 }} />
          <span style={{ font: 'var(--fw-semibold) 13px/1 var(--font-titling)', letterSpacing: '0.18em', textTransform: 'uppercase', color: 'var(--text-muted)' }}>Search the Archive</span>
        </div>
        <Input size="lg" placeholder="Search 4,000+ articles, photos and discussions…" iconLeft={<i data-lucide="search" style={{ width: 20, height: 20 }}></i>} autoFocus style={{ fontSize: 18 }} />
        <div style={{ marginTop: 26 }}>
          <div style={{ font: 'var(--fw-medium) 12px/1 var(--font-body)', textTransform: 'uppercase', letterSpacing: '0.08em', color: 'var(--text-muted)', marginBottom: 14 }}>Popular searches</div>
          <div style={{ display: 'flex', gap: 9, flexWrap: 'wrap' }}>
            {suggestions.map((s) => <Tag key={s} href="#" onClick={(e) => e.preventDefault()}>{s}</Tag>)}
          </div>
        </div>
      </div>
    </div>
  );
}

// Root app — routes between homepage, index pages and article reading view.
function App() {
  const [view, setView] = React.useState('home');
  const [article, setArticle] = React.useState(null);
  const [search, setSearch] = React.useState(false);

  React.useEffect(() => {
    window.lucide && window.lucide.createIcons();
    const el = document.querySelector('.qz-scroll'); if (el) el.scrollTop = 0;
  }, [view, search]);

  const openArticle = (s) => { setArticle(s); setView('article'); };
  const go = (page) => setView(page);

  return (
    <div className="qz-scroll" style={{ height: '100vh', overflowY: 'auto', background: 'var(--qz-white)' }}>
      <Header onSearch={() => setSearch(true)} onHome={() => setView('home')} onNav={go} active={view} dark />
      {view === 'home' && (
        <React.Fragment>
          <Hero onOpen={() => openArticle(window.QZ_DATA.featured[0])} />
          <ExploreArchive onNav={go} />
          <FeaturedArticles onOpen={openArticle} />
          <Photography />
          <ThisDay />
          <Discussions />
          <Restored />
          <Timeline />
        </React.Fragment>
      )}
      {view === 'news' && <NewsIndex onOpen={openArticle} />}
      {view === 'articles' && <ArticlesIndex onOpen={openArticle} />}
      {view === 'gallery' && <PhotoGallery />}
      {view === 'timeline' && <TimelinePage />}
      {view === 'forum' && <ForumPage onOpenThread={() => openArticle(window.QZ_DATA.featured[0])} />}
      {view === 'article' && <ArticleView article={article} onBack={() => setView('home')} />}
      <Footer />
      <SearchOverlay open={search} onClose={() => setSearch(false)} />
    </div>
  );
}
window.App = App;
```

---

## `ui_kits/website/Header.jsx`

Sticky masthead — inverted dark variant, gilt gold hairline.

```jsx
// Queenzone site header — crest mark, editorial wordmark, quiet nav.
// `dark` renders the inverted masthead: rich-black background, white type.
function Header({ onSearch, onHome, onNav, active, dark = false }) {
  const { IconButton, Button } = window.QueenzoneDesignSystem_6c12e8;
  const [scrolled, setScrolled] = React.useState(false);
  React.useEffect(() => {
    const el = document.querySelector('.qz-scroll');
    const onScroll = () => setScrolled((el ? el.scrollTop : window.scrollY) > 12);
    const target = el || window;
    target.addEventListener('scroll', onScroll);
    return () => target.removeEventListener('scroll', onScroll);
  }, []);

  const nav = [
    { label: 'News', page: 'news' },
    { label: 'Articles', page: 'articles' },
    { label: 'Photography', page: 'gallery' },
    { label: 'Forum', page: 'forum' },
    { label: 'Timeline', page: 'timeline' },
  ];

  // Tone tokens
  const bg = dark
    ? (scrolled ? 'rgba(17,17,17,0.92)' : 'var(--qz-black)')
    : (scrolled ? 'rgba(255,255,255,0.92)' : 'var(--qz-white)');
  const wordmark = dark ? 'var(--qz-white)' : 'var(--qz-charcoal)';
  const navIdle = dark ? 'rgba(255,255,255,0.82)' : 'var(--qz-charcoal)';
  const accent = dark ? 'var(--qz-gold)' : 'var(--qz-blue)';
  const crest = dark ? '../../assets/crest-white.png' : '../../assets/crest-black.png';

  return (
    <header style={{
      position: 'sticky', top: 0, zIndex: 50,
      background: bg,
      backdropFilter: scrolled ? 'saturate(180%) blur(12px)' : 'none',
      // Gilt hairline — the brand's antique-gold "key highlight", as a single editorial rule
      // separating the masthead from the content beneath it. Soft shadow on scroll for depth.
      borderBottom: '1px solid rgba(184,154,74,0.55)',
      boxShadow: scrolled
        ? (dark ? '0 8px 28px rgba(0,0,0,0.45)' : '0 6px 22px rgba(17,17,17,0.10)')
        : 'none',
      transition: 'background var(--dur-base) var(--ease-out), box-shadow var(--dur-base) var(--ease-out)',
    }}>
      <div style={{ maxWidth: 'var(--container-max)', margin: '0 auto', padding: '0 var(--gutter-lg)', height: 76, display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 24 }}>
        <a href="#" onClick={(e) => { e.preventDefault(); onHome && onHome(); }} style={{ display: 'flex', alignItems: 'center', gap: 14, textDecoration: 'none' }}>
          <img src={crest} alt="Queen crest" style={{ height: 42, width: 'auto' }} />
          <span style={{ fontFamily: 'var(--font-titling)', fontWeight: 600, fontSize: 21, letterSpacing: '0.18em', textTransform: 'uppercase', color: wordmark }}>Queenzone</span>
        </a>

        <nav style={{ display: 'flex', alignItems: 'center', gap: 30 }}>
          {nav.map((n) => {
            const isActive = active === n.page && n.page !== 'home';
            return (
              <a key={n.label} href="#" onClick={(e) => { e.preventDefault(); onNav && onNav(n.page); }} style={{
                font: 'var(--fw-medium) 14px/1 var(--font-body)', letterSpacing: '0.03em',
                color: isActive ? accent : navIdle, textDecoration: 'none',
                paddingBottom: 2, borderBottom: isActive ? `2px solid ${accent}` : '2px solid transparent',
                transition: 'color var(--dur-fast) var(--ease-out)',
              }}>{n.label}</a>
            );
          })}
        </nav>

        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <IconButton label="Search" variant="ghost" onDark={dark} onClick={onSearch}><i data-lucide="search" style={{ width: 19, height: 19 }}></i></IconButton>
          <Button variant="secondary" size="sm" style={dark ? { color: 'var(--qz-white)', borderColor: 'var(--border-on-dark)' } : {}}>Sign in</Button>
        </div>
      </div>
    </header>
  );
}
window.Header = Header;
```

---

## `ui_kits/website/Hero.jsx`

```jsx
// Full-bleed cinematic hero — one strong image, editorial overlay.
function Hero({ onOpen }) {
  const { Button, Badge } = window.QueenzoneDesignSystem_6c12e8;
  const h = window.QZ_DATA.hero;
  return (
    <section style={{ position: 'relative', minHeight: 'min(78vh, 720px)', display: 'flex', alignItems: 'flex-end', overflow: 'hidden', background: 'var(--qz-black)' }}>
      <img src={h.image} alt="" style={{ position: 'absolute', inset: 0, width: '100%', height: '100%', objectFit: 'cover', opacity: 0.82 }} />
      <div style={{ position: 'absolute', inset: 0, background: 'var(--scrim-bottom)' }}></div>
      <img src="../../assets/crest-white.png" alt="" style={{ position: 'absolute', top: 40, right: 48, width: 120, opacity: 0.12, pointerEvents: 'none' }} />

      <div style={{ position: 'relative', maxWidth: 'var(--container-max)', margin: '0 auto', padding: '0 var(--gutter-lg) 72px', width: '100%' }}>
        <div style={{ maxWidth: 760 }}>
          <div style={{ marginBottom: 22 }}><Badge tone="editorial" variant="solid">{h.category}</Badge></div>
          <h1 style={{ font: 'var(--fw-medium) clamp(44px, 6vw, 80px)/1.02 var(--font-display)', letterSpacing: '-0.015em', color: 'var(--qz-white)', margin: '0 0 22px' }}>{h.title}</h1>
          <p style={{ font: 'var(--fw-regular) 20px/1.55 var(--font-body)', color: 'rgba(255,255,255,0.84)', margin: '0 0 30px', maxWidth: 620 }}>{h.standfirst}</p>
          <div style={{ display: 'flex', alignItems: 'center', gap: 22, flexWrap: 'wrap' }}>
            <Button variant="cta" size="lg" onClick={onOpen}>Read the article</Button>
            <span style={{ font: 'var(--fw-medium) 13px/1 var(--font-body)', textTransform: 'uppercase', letterSpacing: '0.08em', color: 'rgba(255,255,255,0.6)' }}>{h.meta}</span>
          </div>
        </div>
      </div>
    </section>
  );
}
window.Hero = Hero;
```

---

## `ui_kits/website/Sections1.jsx`

```jsx
// Homepage sections — part 1: Explore the Archive, Featured Articles, Photography.
const QZWrap = ({ children, bg, style }) => (
  <section style={{ background: bg || 'transparent', ...style }}>
    <div style={{ maxWidth: 'var(--container-max)', margin: '0 auto', padding: '88px var(--gutter-lg)' }}>{children}</div>
  </section>
);

function ExploreArchive({ onNav }) {
  const { Badge } = window.QueenzoneDesignSystem_6c12e8;
  const items = window.QZ_DATA.explore;
  const pageFor = { 'News Archive': 'news', 'Articles': 'articles', 'Photography': 'gallery', 'Forum History': 'home' };
  return (
    <QZWrap bg="var(--qz-warm-white)">
      <div style={{ textAlign: 'center', marginBottom: 48 }}>
        <div className="qz-eyebrow" style={{ color: 'var(--accent-archive)', marginBottom: 14 }}>The Queenzone.com Archive</div>
        <h2 style={{ font: 'var(--type-h2)', margin: 0 }}>Explore the Archive</h2>
        <p style={{ font: 'var(--type-lead)', color: 'var(--text-secondary)', maxWidth: 580, margin: '16px auto 0' }}>Decades of news, articles, photography and conversation from the original community — preserved, organised and published.</p>
      </div>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 20 }}>
        {items.map((it) => (
          <a key={it.label} href="#" onClick={(e) => { e.preventDefault(); onNav && onNav(pageFor[it.label] || 'home'); }} style={{
            display: 'flex', flexDirection: 'column', gap: 16, padding: '30px 26px',
            background: 'var(--qz-white)', border: '1px solid var(--border-default)', borderRadius: 'var(--radius-sm)',
            textDecoration: 'none', transition: 'box-shadow var(--dur-base) var(--ease-out), transform var(--dur-base) var(--ease-out)',
          }}
            onMouseEnter={(e) => { e.currentTarget.style.boxShadow = 'var(--shadow-lift)'; e.currentTarget.style.transform = 'translateY(-3px)'; }}
            onMouseLeave={(e) => { e.currentTarget.style.boxShadow = 'none'; e.currentTarget.style.transform = 'translateY(0)'; }}>
            <i data-lucide={it.icon} style={{ width: 28, height: 28, color: 'var(--qz-charcoal)', strokeWidth: 1.4 }}></i>
            <div>
              <div style={{ font: 'var(--fw-semibold) 19px/1.3 var(--font-display)', color: 'var(--text-primary)', marginBottom: 6 }}>{it.label}</div>
              <div style={{ font: 'var(--fw-medium) 13px/1 var(--font-body)', textTransform: 'uppercase', letterSpacing: '0.06em', color: 'var(--text-muted)' }}>{it.count}</div>
            </div>
          </a>
        ))}
      </div>
    </QZWrap>
  );
}

function FeaturedArticles({ onOpen }) {
  const { SectionHeader, ArticleCard, Badge, Button, Tag } = window.QueenzoneDesignSystem_6c12e8;
  const articles = window.QZ_DATA.featured;
  const tags = window.QZ_DATA.tags;
  const [active, setActive] = React.useState('All');
  return (
    <QZWrap bg="var(--qz-black)">
      <SectionHeader onDark eyebrow="Editorial" title="Featured Articles"
        action={<Button variant="ghost" size="sm" style={{ color: 'var(--qz-white)' }} iconRight={<i data-lucide="arrow-right" style={{ width: 15, height: 15 }}></i>}>View all</Button>} />
      <div style={{ display: 'flex', gap: 9, flexWrap: 'wrap', margin: '24px 0 40px' }}>
        {tags.map((t) => <Tag key={t} href="#" onDark active={t === active} onClick={(e) => { e.preventDefault(); setActive(t); }}>{t}</Tag>)}
      </div>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 36 }}>
        {articles.map((s) => (
          <ArticleCard key={s.title} onDark image={s.image} category={s.category} title={s.title}
            excerpt={s.excerpt} meta={s.meta} onClick={(e) => { e.preventDefault(); onOpen && onOpen(s); }}
            badge={s.badge ? <Badge tone={s.badge.tone} variant="solid">{s.badge.label}</Badge> : null} />
        ))}
      </div>
    </QZWrap>
  );
}

function Photography() {
  const { SectionHeader, Button } = window.QueenzoneDesignSystem_6c12e8;
  const photos = window.QZ_DATA.photography;
  return (
    <section style={{ background: 'var(--qz-warm-white)' }}>
      <div style={{ maxWidth: 'var(--container-max)', margin: '0 auto', padding: '88px var(--gutter-lg)' }}>
        <SectionHeader eyebrow="The Photographic Archive" title="Featured Photography"
          action={<Button variant="secondary" size="sm">Browse gallery</Button>} />
        <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr 1fr', gridTemplateRows: '230px 230px', gap: 14, marginTop: 40 }}>
          {photos.map((p, i) => (
            <figure key={i} style={{ position: 'relative', margin: 0, overflow: 'hidden', borderRadius: 'var(--radius-md)', background: 'var(--qz-grey-200)', gridColumn: i === 0 ? 'span 1' : undefined, gridRow: i === 0 ? 'span 2' : undefined }}>
              <img src={p.image} alt={p.caption} style={{ width: '100%', height: '100%', objectFit: 'cover', filter: 'grayscale(1)', transition: 'transform var(--dur-slow) var(--ease-out)' }}
                onMouseEnter={(e) => e.currentTarget.style.transform = 'scale(1.04)'}
                onMouseLeave={(e) => e.currentTarget.style.transform = 'scale(1)'} />
              <figcaption style={{ position: 'absolute', left: 0, right: 0, bottom: 0, padding: '28px 16px 14px', background: 'var(--scrim-soft)', font: 'var(--fw-medium) 12px/1.3 var(--font-body)', textTransform: 'uppercase', letterSpacing: '0.06em', color: 'rgba(255,255,255,0.92)' }}>{p.caption}</figcaption>
            </figure>
          ))}
        </div>
      </div>
    </section>
  );
}
window.ExploreArchive = ExploreArchive;
window.FeaturedArticles = FeaturedArticles;
window.Photography = Photography;
window.QZWrap = QZWrap;
```

---

## `ui_kits/website/Sections2.jsx`

```jsx
// Homepage sections — part 2: This Day, Popular Discussions, Recently Restored, Timeline.
function ThisDay() {
  const { SectionHeader } = window.QueenzoneDesignSystem_6c12e8;
  const items = window.QZ_DATA.thisDay;
  return (
    <section style={{ background: 'var(--qz-black)', position: 'relative', overflow: 'hidden' }}>
      <img src="../../assets/crest-white.png" alt="" style={{ position: 'absolute', right: -70, top: -40, width: 300, opacity: 0.05, pointerEvents: 'none' }} />
      <div style={{ maxWidth: 'var(--container-max)', margin: '0 auto', padding: '88px var(--gutter-lg)', position: 'relative' }}>
        <SectionHeader onDark eyebrow="On This Day" title="This Day in Queen History" />
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 0, marginTop: 40, borderTop: '1px solid var(--border-on-dark)' }}>
          {items.map((it, i) => (
            <div key={i} style={{ padding: '32px 28px 32px 0', borderRight: i < 2 ? '1px solid var(--border-on-dark)' : 'none', paddingLeft: i > 0 ? 28 : 0 }}>
              <div style={{ font: 'var(--fw-semibold) 14px/1 var(--font-titling)', letterSpacing: '0.14em', textTransform: 'uppercase', color: 'var(--qz-gold)', marginBottom: 16 }}>{it.date}</div>
              <p style={{ font: 'var(--fw-regular) 18px/1.5 var(--font-display)', color: 'rgba(255,255,255,0.86)', margin: 0 }}>{it.text}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

function Discussions() {
  const { SectionHeader, Button, Tag } = window.QueenzoneDesignSystem_6c12e8;
  const items = window.QZ_DATA.discussions;
  const QZWrap = window.QZWrap;
  return (
    <QZWrap>
      <SectionHeader eyebrow="The Community" title="Popular Discussions"
        action={<Button variant="ghost" size="sm">Visit the forum</Button>} />
      <div style={{ marginTop: 36, border: '1px solid var(--border-default)', borderRadius: 'var(--radius-sm)', overflow: 'hidden' }}>
        {items.map((d, i) => (
          <a key={i} href="#" onClick={(e) => e.preventDefault()} style={{
            display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 24,
            padding: '22px 28px', textDecoration: 'none',
            borderTop: i > 0 ? '1px solid var(--border-default)' : 'none', background: 'var(--qz-white)',
            transition: 'background var(--dur-fast) var(--ease-out)',
          }}
            onMouseEnter={(e) => e.currentTarget.style.background = 'var(--qz-grey-50)'}
            onMouseLeave={(e) => e.currentTarget.style.background = 'var(--qz-white)'}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 20, minWidth: 0 }}>
              <i data-lucide="message-circle" style={{ width: 20, height: 20, color: 'var(--text-muted)', flexShrink: 0, strokeWidth: 1.5 }}></i>
              <div style={{ minWidth: 0 }}>
                <div style={{ font: 'var(--fw-semibold) 17px/1.3 var(--font-body)', color: 'var(--text-primary)', marginBottom: 5, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{d.title}</div>
                <div style={{ font: 'var(--fw-medium) 12px/1 var(--font-body)', textTransform: 'uppercase', letterSpacing: '0.05em', color: 'var(--text-muted)' }}>{d.era} · {d.replies.toLocaleString()} replies · {d.last}</div>
              </div>
            </div>
            <i data-lucide="chevron-right" style={{ width: 18, height: 18, color: 'var(--text-muted)', flexShrink: 0 }}></i>
          </a>
        ))}
      </div>
    </QZWrap>
  );
}

function Restored() {
  const { SectionHeader, ArticleCard, Badge, Button } = window.QueenzoneDesignSystem_6c12e8;
  const items = window.QZ_DATA.restored;
  const QZWrap = window.QZWrap;
  return (
    <QZWrap bg="var(--qz-warm-white)">
      <SectionHeader eyebrow="From the Vaults" title="Recently Restored"
        action={<Button variant="ghost" size="sm">All restorations</Button>} />
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 40, marginTop: 40 }}>
        {items.map((r) => (
          <ArticleCard key={r.title} layout="horizontal" image={r.image} category={r.category}
            title={r.title} meta={r.meta} badge={<Badge tone="special" variant="solid">Restored</Badge>} />
        ))}
      </div>
    </QZWrap>
  );
}

function Timeline() {
  const items = window.QZ_DATA.timeline;
  return (
    <section style={{ background: 'var(--qz-black)', position: 'relative', overflow: 'hidden' }}>
      <img src="../../assets/crest-white.png" alt="" style={{ position: 'absolute', left: -80, bottom: -60, width: 380, opacity: 0.05, pointerEvents: 'none' }} />
      <div style={{ maxWidth: 'var(--container-max)', margin: '0 auto', padding: '92px var(--gutter-lg)', position: 'relative' }}>
        <div style={{ textAlign: 'center', marginBottom: 56 }}>
          <div className="qz-eyebrow" style={{ color: 'var(--qz-gold)', marginBottom: 14 }}>Five Decades</div>
          <h2 style={{ font: 'var(--type-h2)', color: 'var(--qz-white)', margin: 0 }}>Timeline Highlights</h2>
        </div>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 28 }}>
          {items.map((t, i) => (
            <div key={i} style={{ paddingTop: 26, borderTop: '1px solid var(--border-on-dark)' }}>
              <div style={{ font: 'var(--fw-semibold) 30px/1 var(--font-titling)', letterSpacing: '0.04em', color: 'var(--qz-gold)', marginBottom: 16 }}>{t.year}</div>
              <p style={{ font: 'var(--fw-regular) 16px/1.55 var(--font-body)', color: 'rgba(255,255,255,0.78)', margin: 0 }}>{t.text}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
window.ThisDay = ThisDay;
window.Discussions = Discussions;
window.Restored = Restored;
window.Timeline = Timeline;
```

---

## `ui_kits/website/Pages1.jsx`

```jsx
// Shared page hero band + News Index + Articles Index.
function PageHero({ eyebrow, title, lead, count }) {
  return (
    <section style={{ background: 'var(--qz-black)', position: 'relative', overflow: 'hidden' }}>
      <img src="../../assets/crest-white.png" alt="" style={{ position: 'absolute', right: -60, top: -50, width: 300, opacity: 0.06, pointerEvents: 'none' }} />
      <div style={{ maxWidth: 'var(--container-max)', margin: '0 auto', padding: '72px var(--gutter-lg) 64px', position: 'relative' }}>
        <div className="qz-eyebrow" style={{ color: 'var(--qz-gold)', marginBottom: 18 }}>{eyebrow}</div>
        <h1 style={{ font: 'var(--fw-medium) clamp(40px, 5vw, 64px)/1.02 var(--font-display)', letterSpacing: '-0.015em', color: 'var(--qz-white)', margin: 0 }}>{title}</h1>
        {lead && <p style={{ font: 'var(--fw-regular) 19px/1.55 var(--font-body)', color: 'rgba(255,255,255,0.7)', margin: '20px 0 0', maxWidth: 620 }}>{lead}</p>}
        {count && <div style={{ font: 'var(--fw-medium) 13px/1 var(--font-body)', textTransform: 'uppercase', letterSpacing: '0.1em', color: 'rgba(255,255,255,0.45)', marginTop: 26 }}>{count}</div>}
      </div>
    </section>
  );
}

function NewsIndex({ onOpen }) {
  const { Input, Tag, IconButton } = window.QueenzoneDesignSystem_6c12e8;
  const news = window.QZ_DATA.news;
  const years = window.QZ_DATA.newsYears;
  const [year, setYear] = React.useState('All');
  return (
    <React.Fragment>
      <PageHero eyebrow="The Archive" title="News" lead="Four decades of Queen news, restored from the original Queenzone.com archive and presented in full." count="4,000+ articles" />
      <section style={{ maxWidth: 'var(--container-max)', margin: '0 auto', padding: '48px var(--gutter-lg) 96px' }}>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 20, flexWrap: 'wrap', paddingBottom: 28, marginBottom: 8, borderBottom: '1px solid var(--border-default)' }}>
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
            {years.map((y) => <Tag key={y} href="#" active={y === year} onClick={(e) => { e.preventDefault(); setYear(y); }}>{y}</Tag>)}
          </div>
          <div style={{ width: 260 }}><Input size="sm" placeholder="Search the news archive…" iconLeft={<i data-lucide="search" style={{ width: 17, height: 17 }}></i>} /></div>
        </div>
        <div>
          {news.map((n, i) => (
            <a key={i} href="#" onClick={(e) => { e.preventDefault(); onOpen && onOpen({ image: window.QZ_DATA.hero.image, category: n.cat, title: n.title, excerpt: n.excerpt, meta: n.date }); }}
              style={{ display: 'grid', gridTemplateColumns: '140px 1fr 24px', alignItems: 'center', gap: 28, padding: '26px 8px', textDecoration: 'none', borderBottom: '1px solid var(--hairline)' }}
              onMouseEnter={(e) => { e.currentTarget.style.background = 'var(--qz-grey-50)'; const t = e.currentTarget.querySelector('.nt'); if (t) t.style.color = 'var(--qz-blue)'; }}
              onMouseLeave={(e) => { e.currentTarget.style.background = 'transparent'; const t = e.currentTarget.querySelector('.nt'); if (t) t.style.color = 'var(--text-primary)'; }}>
              <div>
                <div style={{ font: 'var(--fw-semibold) 13px/1 var(--font-titling)', letterSpacing: '0.1em', textTransform: 'uppercase', color: 'var(--accent-archive)' }}>{n.date}</div>
                <div style={{ font: 'var(--fw-medium) 11px/1 var(--font-body)', letterSpacing: '0.08em', textTransform: 'uppercase', color: 'var(--text-muted)', marginTop: 8 }}>{n.cat}</div>
              </div>
              <div>
                <h3 className="nt" style={{ font: 'var(--fw-semibold) 23px/1.25 var(--font-display)', color: 'var(--text-primary)', margin: '0 0 6px', transition: 'color var(--dur-fast) var(--ease-out)' }}>{n.title}</h3>
                <p style={{ font: 'var(--fw-regular) 15px/1.5 var(--font-body)', color: 'var(--text-secondary)', margin: 0 }}>{n.excerpt}</p>
              </div>
              <i data-lucide="arrow-up-right" style={{ width: 20, height: 20, color: 'var(--text-muted)' }}></i>
            </a>
          ))}
        </div>
        <div style={{ display: 'flex', justifyContent: 'center', marginTop: 48 }}>
          <button style={{ font: 'var(--fw-medium) 13px/1 var(--font-body)', letterSpacing: '0.06em', textTransform: 'uppercase', color: 'var(--qz-charcoal)', background: 'transparent', border: '1px solid var(--border-strong)', borderRadius: 'var(--radius-sm)', padding: '14px 32px', cursor: 'pointer' }}>Load earlier news</button>
        </div>
      </section>
    </React.Fragment>
  );
}

function ArticlesIndex({ onOpen }) {
  const { SectionHeader, ArticleCard, Badge, Tag } = window.QueenzoneDesignSystem_6c12e8;
  const articles = window.QZ_DATA.featured;
  const all = articles.concat(window.QZ_DATA.restored.map((r) => ({ ...r, excerpt: 'A restored feature from the Queenzone.com archive.' })));
  const tags = window.QZ_DATA.tags;
  const [active, setActive] = React.useState('All');
  const lead = all[0];
  return (
    <React.Fragment>
      <PageHero eyebrow="Long-form" title="Articles" lead="In-depth features, essays and oral histories — the long reads from the Queenzone.com archive." count="100+ features" />
      <section style={{ maxWidth: 'var(--container-max)', margin: '0 auto', padding: '56px var(--gutter-lg) 40px' }}>
        <a href="#" onClick={(e) => { e.preventDefault(); onOpen && onOpen(lead); }} style={{ display: 'grid', gridTemplateColumns: '1.15fr 1fr', gap: 48, alignItems: 'center', textDecoration: 'none', paddingBottom: 56, marginBottom: 8, borderBottom: '1px solid var(--hairline)' }}>
          <div style={{ position: 'relative', overflow: 'hidden', borderRadius: 'var(--radius-md)', aspectRatio: '3 / 2', background: 'var(--qz-grey-200)' }}>
            <img src={lead.image} alt="" style={{ width: '100%', height: '100%', objectFit: 'cover', filter: 'grayscale(1)' }} />
          </div>
          <div>
            <div style={{ marginBottom: 18 }}><Badge tone="editorial" variant="solid">Featured</Badge></div>
            <h2 style={{ font: 'var(--fw-medium) 42px/1.05 var(--font-display)', letterSpacing: '-0.015em', color: 'var(--text-primary)', margin: '0 0 16px' }}>{lead.title}</h2>
            <p style={{ font: 'var(--fw-regular) 18px/1.6 var(--font-body)', color: 'var(--text-secondary)', margin: '0 0 18px' }}>{lead.excerpt}</p>
            <div style={{ font: 'var(--fw-medium) 12px/1 var(--font-body)', textTransform: 'uppercase', letterSpacing: '0.06em', color: 'var(--text-muted)' }}>{lead.meta}</div>
          </div>
        </a>
      </section>
      <section style={{ maxWidth: 'var(--container-max)', margin: '0 auto', padding: '0 var(--gutter-lg) 96px' }}>
        <div style={{ display: 'flex', gap: 9, flexWrap: 'wrap', marginBottom: 44 }}>
          {tags.map((t) => <Tag key={t} href="#" active={t === active} onClick={(e) => { e.preventDefault(); setActive(t); }}>{t}</Tag>)}
        </div>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 36 }}>
          {all.slice(1).map((s) => (
            <ArticleCard key={s.title} image={s.image} category={s.category} title={s.title} excerpt={s.excerpt} meta={s.meta}
              onClick={(e) => { e.preventDefault(); onOpen && onOpen(s); }}
              badge={s.badge ? <Badge tone={s.badge.tone} variant="solid">{s.badge.label}</Badge> : null} />
          ))}
        </div>
      </section>
    </React.Fragment>
  );
}
window.PageHero = PageHero;
window.NewsIndex = NewsIndex;
window.ArticlesIndex = ArticlesIndex;
```

---

## `ui_kits/website/Pages2.jsx`

```jsx
// Photo Gallery (with lightbox) + Timeline page.
function PhotoGallery() {
  const { Tag, IconButton } = window.QueenzoneDesignSystem_6c12e8;
  const photos = window.QZ_DATA.gallery;
  const filters = window.QZ_DATA.galleryFilters;
  const [filter, setFilter] = React.useState('All');
  const [active, setActive] = React.useState(null);
  const shown = filter === 'All' ? photos : photos.filter((p) => p.cat === filter);
  React.useEffect(() => { window.lucide && window.lucide.createIcons(); }, [active, filter]);

  const span = (s) => s === 'tall' ? { gridRow: 'span 2' } : s === 'wide' ? { gridColumn: 'span 2' } : {};

  return (
    <React.Fragment>
      <PageHero eyebrow="The Photographic Archive" title="Photography" lead="Tens of thousands of photographs — live, studio and backstage — scanned and restored from the original community archive." count="Tens of thousands of images" />
      <section style={{ maxWidth: 'var(--container-max)', margin: '0 auto', padding: '40px var(--gutter-lg) 96px' }}>
        <div style={{ display: 'flex', gap: 9, flexWrap: 'wrap', paddingBottom: 32, marginBottom: 36, borderBottom: '1px solid var(--border-default)' }}>
          {filters.map((f) => <Tag key={f} href="#" active={f === filter} onClick={(e) => { e.preventDefault(); setFilter(f); }}>{f}</Tag>)}
        </div>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gridAutoRows: '200px', gap: 14 }}>
          {shown.map((p, i) => (
            <figure key={i} onClick={() => setActive(p)} style={{ position: 'relative', margin: 0, overflow: 'hidden', borderRadius: 'var(--radius-md)', background: 'var(--qz-grey-200)', cursor: 'pointer', ...span(p.span) }}>
              <img src={p.image} alt={p.caption} style={{ width: '100%', height: '100%', objectFit: 'cover', filter: 'grayscale(1)', transition: 'transform var(--dur-slow) var(--ease-out), filter var(--dur-slow) var(--ease-out)' }}
                onMouseEnter={(e) => { e.currentTarget.style.transform = 'scale(1.05)'; e.currentTarget.style.filter = 'grayscale(0)'; }}
                onMouseLeave={(e) => { e.currentTarget.style.transform = 'scale(1)'; e.currentTarget.style.filter = 'grayscale(1)'; }} />
              <figcaption style={{ position: 'absolute', left: 0, right: 0, bottom: 0, padding: '26px 14px 12px', background: 'var(--scrim-soft)', display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', gap: 8, font: 'var(--fw-medium) 11.5px/1.3 var(--font-body)', textTransform: 'uppercase', letterSpacing: '0.05em', color: 'rgba(255,255,255,0.92)', pointerEvents: 'none' }}>
                <span>{p.caption}</span><span style={{ color: 'var(--qz-gold)' }}>{p.year}</span>
              </figcaption>
            </figure>
          ))}
        </div>
      </section>

      {active && (
        <div onClick={() => setActive(null)} style={{ position: 'fixed', inset: 0, zIndex: 120, background: 'rgba(10,10,10,0.92)', backdropFilter: 'blur(6px)', display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 40, animation: 'qzFade 240ms ease' }}>
          <div style={{ position: 'absolute', top: 24, right: 28 }}>
            <IconButton label="Close" variant="outline" onDark onClick={() => setActive(null)}><i data-lucide="x" style={{ width: 20, height: 20 }}></i></IconButton>
          </div>
          <figure onClick={(e) => e.stopPropagation()} style={{ margin: 0, maxWidth: 'min(1000px, 90vw)', maxHeight: '86vh', display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
            <img src={active.image} alt={active.caption} style={{ maxWidth: '100%', maxHeight: '76vh', objectFit: 'contain', borderRadius: 'var(--radius-md)', filter: 'grayscale(1)' }} />
            <figcaption style={{ marginTop: 20, display: 'flex', gap: 16, alignItems: 'center', font: 'var(--fw-medium) 13px/1 var(--font-body)', textTransform: 'uppercase', letterSpacing: '0.08em', color: 'rgba(255,255,255,0.8)' }}>
              <span>{active.caption}</span><span style={{ color: 'var(--qz-gold)' }}>{active.year}</span><span style={{ color: 'rgba(255,255,255,0.4)' }}>{active.cat}</span>
            </figcaption>
          </figure>
        </div>
      )}
    </React.Fragment>
  );
}

function TimelinePage() {
  const items = window.QZ_DATA.timelineFull;
  return (
    <React.Fragment>
      <PageHero eyebrow="Five Decades" title="Timeline" lead="The story of Queen, year by year — a guided path through the moments that defined the band and gathered the community." />
      <section style={{ background: 'var(--qz-warm-white)' }}>
        <div style={{ maxWidth: 880, margin: '0 auto', padding: '80px var(--gutter-lg) 100px', position: 'relative' }}>
          <div style={{ position: 'absolute', left: 'calc(50% - 0.5px)', top: 80, bottom: 100, width: 1, background: 'var(--border-strong)' }}></div>
          {items.map((t, i) => {
            const left = i % 2 === 0;
            return (
              <div key={i} style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', columnGap: 56, marginBottom: 44, position: 'relative' }}>
                <span style={{ position: 'absolute', left: 'calc(50% - 5px)', top: 8, width: 10, height: 10, borderRadius: '50%', background: 'var(--qz-gold)', boxShadow: '0 0 0 5px var(--qz-warm-white)' }}></span>
                <div style={{ gridColumn: left ? 1 : 2, textAlign: left ? 'right' : 'left', paddingRight: left ? 8 : 0, paddingLeft: left ? 0 : 8 }}>
                  <div style={{ font: 'var(--fw-semibold) 34px/1 var(--font-titling)', letterSpacing: '0.04em', color: 'var(--qz-gold-deep)', marginBottom: 12 }}>{t.year}</div>
                  <h3 style={{ font: 'var(--fw-semibold) 24px/1.2 var(--font-display)', color: 'var(--text-primary)', margin: '0 0 8px' }}>{t.title}</h3>
                  <p style={{ font: 'var(--fw-regular) 16px/1.6 var(--font-body)', color: 'var(--text-secondary)', margin: 0 }}>{t.text}</p>
                </div>
              </div>
            );
          })}
        </div>
      </section>
    </React.Fragment>
  );
}
window.PhotoGallery = PhotoGallery;
window.TimelinePage = TimelinePage;
```

---

## `ui_kits/website/Forum.jsx`

```jsx
// Forum / Community page — boards index + recent threads, editorial-archival.
function ForumPage({ onOpenThread }) {
  const { Button, Tag, Badge, IconButton, Input } = window.QueenzoneDesignSystem_6c12e8;
  const stats = window.QZ_DATA.forumStats;
  const boards = window.QZ_DATA.forumBoards;
  const threads = window.QZ_DATA.forumThreads;
  const [tab, setTab] = React.useState('Latest');
  const tabs = ['Latest', 'Top', 'Unanswered'];

  return (
    <React.Fragment>
      {/* Forum masthead */}
      <section style={{ background: 'var(--qz-black)', position: 'relative', overflow: 'hidden' }}>
        <img src="../../assets/crest-white.png" alt="" style={{ position: 'absolute', right: -70, top: -60, width: 320, opacity: 0.06, pointerEvents: 'none' }} />
        <div style={{ maxWidth: 'var(--container-max)', margin: '0 auto', padding: '72px var(--gutter-lg) 56px', position: 'relative' }}>
          <div className="qz-eyebrow" style={{ color: 'var(--qz-gold)', marginBottom: 18 }}>The Community</div>
          <h1 style={{ font: 'var(--fw-medium) clamp(40px, 5vw, 64px)/1.02 var(--font-display)', letterSpacing: '-0.015em', color: 'var(--qz-white)', margin: 0 }}>Forum</h1>
          <p style={{ font: 'var(--fw-regular) 19px/1.55 var(--font-body)', color: 'rgba(255,255,255,0.7)', margin: '20px 0 32px', maxWidth: 640 }}>The conversation that built Queenzone.com — more than 100,000 posts of news, debate and memory, preserved and open once more.</p>
          <div style={{ display: 'flex', gap: 40, flexWrap: 'wrap' }}>
            {[['members', stats.members], ['threads', stats.threads], ['posts', stats.posts]].map(([k, v]) => (
              <div key={k}>
                <div style={{ font: 'var(--fw-semibold) 30px/1 var(--font-titling)', letterSpacing: '0.03em', color: 'var(--qz-gold)' }}>{v}</div>
                <div style={{ font: 'var(--fw-medium) 12px/1 var(--font-body)', textTransform: 'uppercase', letterSpacing: '0.1em', color: 'rgba(255,255,255,0.5)', marginTop: 8 }}>{k}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Boards */}
      <section style={{ maxWidth: 'var(--container-max)', margin: '0 auto', padding: '64px var(--gutter-lg) 32px' }}>
        <div style={{ display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between', gap: 24, borderBottom: '1px solid var(--hairline)', paddingBottom: 'var(--space-4)', marginBottom: 40 }}>
          <div>
            <div className="qz-eyebrow" style={{ marginBottom: 12 }}>Discussion Boards</div>
            <h2 style={{ font: 'var(--type-h2)', margin: 0 }}>Browse the boards</h2>
          </div>
          <Button variant="cta" size="md" iconLeft={<i data-lucide="pen-line" style={{ width: 16, height: 16 }}></i>}>New thread</Button>
        </div>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 0, border: '1px solid var(--border-default)', borderRadius: 'var(--radius-sm)', overflow: 'hidden' }}>
          {boards.map((b, i) => {
            const col = i % 2; const row = Math.floor(i / 2);
            return (
              <a key={b.name} href="#" onClick={(e) => e.preventDefault()} style={{
                display: 'flex', gap: 18, padding: '26px 28px', textDecoration: 'none', background: 'var(--qz-white)',
                borderTop: row > 0 ? '1px solid var(--border-default)' : 'none',
                borderLeft: col === 1 ? '1px solid var(--border-default)' : 'none',
                transition: 'background var(--dur-fast) var(--ease-out)',
              }}
                onMouseEnter={(e) => { e.currentTarget.style.background = 'var(--qz-grey-50)'; }}
                onMouseLeave={(e) => { e.currentTarget.style.background = 'var(--qz-white)'; }}>
                <div style={{ width: 46, height: 46, flexShrink: 0, borderRadius: 'var(--radius-sm)', background: 'var(--qz-warm-white)', border: '1px solid var(--border-default)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                  <i data-lucide={b.icon} style={{ width: 22, height: 22, color: 'var(--accent-archive)', strokeWidth: 1.5 }}></i>
                </div>
                <div style={{ minWidth: 0, flex: 1 }}>
                  <h3 style={{ font: 'var(--fw-semibold) 20px/1.2 var(--font-display)', color: 'var(--text-primary)', margin: '0 0 5px' }}>{b.name}</h3>
                  <p style={{ font: 'var(--fw-regular) 14px/1.5 var(--font-body)', color: 'var(--text-secondary)', margin: '0 0 12px' }}>{b.desc}</p>
                  <div style={{ display: 'flex', gap: 16, font: 'var(--fw-medium) 11.5px/1 var(--font-body)', textTransform: 'uppercase', letterSpacing: '0.06em', color: 'var(--text-muted)' }}>
                    <span>{b.threads.toLocaleString()} threads</span><span>{b.posts} posts</span>
                  </div>
                  <div style={{ marginTop: 12, paddingTop: 12, borderTop: '1px solid var(--hairline)', font: 'var(--fw-regular) 12.5px/1.4 var(--font-body)', color: 'var(--text-muted)' }}>
                    Latest: <span style={{ color: 'var(--qz-charcoal)', fontWeight: 'var(--fw-medium)' }}>{b.last.thread}</span> · {b.last.when}
                  </div>
                </div>
              </a>
            );
          })}
        </div>
      </section>

      {/* Recent threads */}
      <section style={{ maxWidth: 'var(--container-max)', margin: '0 auto', padding: '40px var(--gutter-lg) 96px' }}>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 20, flexWrap: 'wrap', marginBottom: 28 }}>
          <div style={{ display: 'flex', gap: 8 }}>
            {tabs.map((t) => <Tag key={t} href="#" active={t === tab} onClick={(e) => { e.preventDefault(); setTab(t); }}>{t}</Tag>)}
          </div>
          <div style={{ width: 260 }}><Input size="sm" placeholder="Search discussions…" iconLeft={<i data-lucide="search" style={{ width: 17, height: 17 }}></i>} /></div>
        </div>
        <div style={{ border: '1px solid var(--border-default)', borderRadius: 'var(--radius-sm)', overflow: 'hidden' }}>
          {/* header row */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 110px 90px 110px', gap: 20, padding: '14px 24px', background: 'var(--qz-warm-white)', borderBottom: '1px solid var(--border-default)', font: 'var(--fw-semibold) 11px/1 var(--font-body)', textTransform: 'uppercase', letterSpacing: '0.08em', color: 'var(--text-muted)' }}>
            <span>Thread</span><span style={{ textAlign: 'right' }}>Replies</span><span style={{ textAlign: 'right' }}>Views</span><span style={{ textAlign: 'right' }}>Activity</span>
          </div>
          {threads.map((th, i) => (
            <a key={i} href="#" onClick={(e) => { e.preventDefault(); onOpenThread && onOpenThread(th); }} style={{
              display: 'grid', gridTemplateColumns: '1fr 110px 90px 110px', gap: 20, alignItems: 'center', padding: '20px 24px', textDecoration: 'none',
              borderTop: i > 0 ? '1px solid var(--hairline)' : 'none', background: 'var(--qz-white)', transition: 'background var(--dur-fast) var(--ease-out)',
            }}
              onMouseEnter={(e) => { e.currentTarget.style.background = 'var(--qz-grey-50)'; }}
              onMouseLeave={(e) => { e.currentTarget.style.background = 'var(--qz-white)'; }}>
              <div style={{ minWidth: 0 }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 6 }}>
                  {th.pinned && <i data-lucide="pin" style={{ width: 13, height: 13, color: 'var(--qz-gold-deep)' }}></i>}
                  <h3 style={{ font: 'var(--fw-semibold) 17px/1.3 var(--font-display)', color: 'var(--text-primary)', margin: 0, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{th.title}</h3>
                </div>
                <div style={{ font: 'var(--fw-medium) 11.5px/1 var(--font-body)', textTransform: 'uppercase', letterSpacing: '0.05em', color: 'var(--text-muted)' }}>{th.board} · by {th.author}</div>
              </div>
              <span style={{ textAlign: 'right', font: 'var(--fw-semibold) 16px/1 var(--font-body)', color: 'var(--qz-charcoal)' }}>{th.replies.toLocaleString()}</span>
              <span style={{ textAlign: 'right', font: 'var(--fw-regular) 14px/1 var(--font-body)', color: 'var(--text-muted)' }}>{th.views}</span>
              <span style={{ textAlign: 'right', font: 'var(--fw-medium) 12px/1 var(--font-body)', textTransform: 'uppercase', letterSpacing: '0.05em', color: 'var(--text-muted)' }}>{th.when}</span>
            </a>
          ))}
        </div>
      </section>
    </React.Fragment>
  );
}
window.ForumPage = ForumPage;
```

---

## `ui_kits/website/ArticleView.jsx`

```jsx
// Long-form article reading view — editorial measure, archival hero.
function ArticleView({ article, onBack }) {
  const { Badge, Tag, Button, IconButton } = window.QueenzoneDesignSystem_6c12e8;
  const s = article || window.QZ_DATA.featured[0];
  const body = [
    'It began, as these things often do, with low expectations. By the summer of 1985 the band had weathered a difficult few years — a patchy reception for recent records, and whispers that their finest moment had already passed.',
    'What unfolded across twenty-one minutes at Wembley Stadium would settle the argument for a generation. From the opening bars, the performance was less a set than a conversation with ninety thousand people, every one of them held in the palm of a single hand.',
    'The genius was in the restraint. Where others reached for spectacle, here was a band that understood the power of space — a held note, a raised fist, a chorus handed back to the crowd and sung straight back, word for word.',
    'In the decades since, the footage has been studied frame by frame: the pacing, the song choices, the sheer economy of it all. It remains, by common consent, the high-water mark of the live stadium performance.',
  ];
  return (
    <article style={{ background: 'var(--qz-white)' }}>
      <div style={{ position: 'relative', height: 'min(56vh, 520px)', overflow: 'hidden', background: 'var(--qz-black)' }}>
        <img src={s.image} alt="" style={{ position: 'absolute', inset: 0, width: '100%', height: '100%', objectFit: 'cover', opacity: 0.78, filter: 'grayscale(1)' }} />
        <div style={{ position: 'absolute', inset: 0, background: 'var(--scrim-bottom)' }}></div>
        <div style={{ position: 'absolute', top: 24, left: 'max(24px, calc((100% - var(--container-text)) / 2))' }}>
          <IconButton label="Back" variant="outline" onDark onClick={onBack}><i data-lucide="arrow-left" style={{ width: 18, height: 18 }}></i></IconButton>
        </div>
        <div style={{ position: 'absolute', bottom: 0, left: 0, right: 0, padding: '0 var(--gutter-lg) 48px' }}>
          <div style={{ maxWidth: 'var(--container-text)', margin: '0 auto' }}>
            <div style={{ marginBottom: 18 }}><Badge tone="editorial" variant="solid">{s.category}</Badge></div>
            <h1 style={{ font: 'var(--fw-medium) clamp(36px, 5vw, 60px)/1.03 var(--font-display)', letterSpacing: '-0.015em', color: 'var(--qz-white)', margin: 0 }}>{s.title}</h1>
          </div>
        </div>
      </div>

      <div style={{ maxWidth: 'var(--container-text)', margin: '0 auto', padding: '48px var(--gutter-lg) 96px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 16, paddingBottom: 28, marginBottom: 40, borderBottom: '1px solid var(--hairline)' }}>
          <div style={{ width: 42, height: 42, borderRadius: '50%', background: 'var(--qz-grey-200)', display: 'flex', alignItems: 'center', justifyContent: 'center', font: 'var(--fw-semibold) 15px/1 var(--font-display)', color: 'var(--qz-charcoal)' }}>QZ</div>
          <div>
            <div style={{ font: 'var(--fw-semibold) 14px/1.3 var(--font-body)', color: 'var(--text-primary)' }}>The Queenzone Archive</div>
            <div style={{ font: 'var(--fw-medium) 12px/1 var(--font-body)', textTransform: 'uppercase', letterSpacing: '0.05em', color: 'var(--text-muted)', marginTop: 3 }}>{s.meta}</div>
          </div>
          <div style={{ marginLeft: 'auto', display: 'flex', gap: 6 }}>
            <IconButton label="Bookmark" variant="ghost"><i data-lucide="bookmark" style={{ width: 18, height: 18 }}></i></IconButton>
            <IconButton label="Share" variant="ghost"><i data-lucide="share-2" style={{ width: 18, height: 18 }}></i></IconButton>
          </div>
        </div>

        <p style={{ font: 'var(--fw-regular) 22px/1.6 var(--font-display)', color: 'var(--qz-charcoal)', marginBottom: 36 }}>{s.excerpt}</p>
        <div className="qz-prose">
          {body.map((para, i) => (
            <p key={i} style={{ font: 'var(--fw-regular) 18px/1.75 var(--font-body)', color: 'var(--qz-grey-700)', margin: '0 0 26px' }}>
              {i === 0 ? <span style={{ float: 'left', font: 'var(--fw-medium) 76px/0.78 var(--font-display)', color: 'var(--qz-charcoal)', margin: '6px 14px 0 0' }}>{para[0]}</span> : null}
              {i === 0 ? para.slice(1) : para}
            </p>
          ))}
        </div>

        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginTop: 40, paddingTop: 32, borderTop: '1px solid var(--hairline)' }}>
          <Tag href="#">Live Aid</Tag><Tag href="#">1985</Tag><Tag href="#">Performance</Tag><Tag href="#">Wembley</Tag>
        </div>
      </div>
    </article>
  );
}
window.ArticleView = ArticleView;
```

---

## `ui_kits/website/Footer.jsx`

```jsx
// Site footer — crest seal, navigation columns, restrained.
function Footer() {
  const { Input, Button } = window.QueenzoneDesignSystem_6c12e8;
  const cols = [
    { h: 'Archive', links: ['News', 'Articles', 'Photography', 'Discography', 'Timeline'] },
    { h: 'Community', links: ['Forum', 'Members', 'Submit an Article', 'Restoration Project'] },
    { h: 'About', links: ['Our History', 'The Mission', 'Contact', 'Privacy'] },
  ];
  return (
    <footer style={{ background: 'var(--qz-black)', borderTop: '1px solid var(--border-on-dark)' }}>
      <div style={{ maxWidth: 'var(--container-max)', margin: '0 auto', padding: '72px var(--gutter-lg) 40px' }}>
        <div style={{ display: 'grid', gridTemplateColumns: '1.4fr 1fr 1fr 1fr', gap: 48, paddingBottom: 56, borderBottom: '1px solid var(--border-on-dark)' }}>
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 14, marginBottom: 22 }}>
              <img src="../../assets/crest-white.png" alt="" style={{ height: 44 }} />
              <span style={{ fontFamily: 'var(--font-titling)', fontWeight: 600, fontSize: 18, letterSpacing: '0.18em', textTransform: 'uppercase', color: 'var(--qz-white)' }}>Queenzone</span>
            </div>
            <p style={{ font: 'var(--fw-regular) 15px/1.6 var(--font-body)', color: 'rgba(255,255,255,0.6)', maxWidth: 320, marginBottom: 22 }}>The preserved archive of Queenzone.com — one of the longest-running independent Queen fan communities. Its news, articles, photography and forums, published at last.</p>
            <div style={{ display: 'flex', gap: 8, maxWidth: 320 }}>
              <Input placeholder="Email for archive updates" size="sm" style={{ background: 'rgba(255,255,255,0.06)', borderColor: 'var(--border-on-dark)', color: 'var(--qz-white)' }} />
              <Button variant="cta" size="sm">Join</Button>
            </div>
          </div>
          {cols.map((c) => (
            <div key={c.h}>
              <div style={{ font: 'var(--fw-semibold) 12px/1 var(--font-titling)', letterSpacing: '0.18em', textTransform: 'uppercase', color: 'var(--qz-gold)', marginBottom: 20 }}>{c.h}</div>
              <ul style={{ listStyle: 'none', margin: 0, padding: 0, display: 'flex', flexDirection: 'column', gap: 13 }}>
                {c.links.map((l) => <li key={l}><a href="#" onClick={(e) => e.preventDefault()} style={{ font: 'var(--fw-regular) 15px/1 var(--font-body)', color: 'rgba(255,255,255,0.72)', textDecoration: 'none' }} onMouseEnter={(e) => e.currentTarget.style.color = 'var(--qz-white)'} onMouseLeave={(e) => e.currentTarget.style.color = 'rgba(255,255,255,0.72)'}>{l}</a></li>)}
              </ul>
            </div>
          ))}
        </div>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', paddingTop: 28, flexWrap: 'wrap', gap: 12 }}>
          <span style={{ font: 'var(--fw-regular) 13px/1 var(--font-body)', color: 'rgba(255,255,255,0.42)' }}>© 2026 Queenzone.org · An independent fan archive. Not affiliated with Queen or its representatives.</span>
          <span style={{ font: 'var(--fw-regular) 13px/1 var(--font-body)', color: 'rgba(255,255,255,0.42)' }}>Restored with care since 2001</span>
        </div>
      </div>
    </footer>
  );
}
window.Footer = Footer;
```

---

## `ui_kits/website/MobileScreens.jsx`

```jsx
// Queenzone mobile screens — rendered inside IOSDevice frames.
// Mobile-first: the primary device per the brief.

function MHeader({ dark }) {
  const bg = dark ? 'transparent' : 'rgba(255,255,255,0.9)';
  const fg = dark ? 'var(--qz-white)' : 'var(--qz-charcoal)';
  const crest = dark ? '../../assets/crest-white.png' : '../../assets/crest-black.png';
  return (
    <div style={{ position: 'sticky', top: 0, zIndex: 30, background: bg, backdropFilter: dark ? 'none' : 'saturate(180%) blur(10px)', borderBottom: dark ? 'none' : '1px solid var(--hairline)', padding: '52px 18px 14px', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 9 }}>
        <img src={crest} alt="" style={{ height: 26 }} />
        <span style={{ fontFamily: 'var(--font-titling)', fontWeight: 600, fontSize: 14, letterSpacing: '0.16em', textTransform: 'uppercase', color: fg }}>Queenzone</span>
      </div>
      <div style={{ display: 'flex', gap: 14, color: fg }}>
        <i data-lucide="search" style={{ width: 20, height: 20 }}></i>
        <i data-lucide="menu" style={{ width: 20, height: 20 }}></i>
      </div>
    </div>
  );
}

function MChips({ items, dark }) {
  const [a, setA] = React.useState(items[0]);
  const { Tag } = window.QueenzoneDesignSystem_6c12e8;
  return (
    <div style={{ display: 'flex', gap: 8, overflowX: 'auto', padding: '0 18px 4px', WebkitOverflowScrolling: 'touch' }}>
      {items.map((t) => <div key={t} style={{ flex: '0 0 auto' }}><Tag href="#" onDark={dark} active={t === a} onClick={(e) => { e.preventDefault(); setA(t); }}>{t}</Tag></div>)}
    </div>
  );
}

// 1 — Home
function MobileHome() {
  const { Badge, Button } = window.QueenzoneDesignSystem_6c12e8;
  const h = window.QZ_DATA.hero;
  const ex = window.QZ_DATA.explore;
  const td = window.QZ_DATA.thisDay[0];
  return (
    <div style={{ background: 'var(--qz-white)', minHeight: '100%' }}>
      <MHeader dark />
      <div style={{ marginTop: -88 }}>
        <section style={{ position: 'relative', minHeight: 500, display: 'flex', alignItems: 'flex-end', overflow: 'hidden', background: 'var(--qz-black)' }}>
          <img src={h.image} alt="" style={{ position: 'absolute', inset: 0, width: '100%', height: '100%', objectFit: 'cover', opacity: 0.82 }} />
          <div style={{ position: 'absolute', inset: 0, background: 'var(--scrim-bottom)' }}></div>
          <div style={{ position: 'relative', padding: '0 20px 30px' }}>
            <div style={{ marginBottom: 14 }}><Badge tone="editorial" variant="solid">{h.category}</Badge></div>
            <h1 style={{ font: 'var(--fw-medium) 34px/1.05 var(--font-display)', letterSpacing: '-0.015em', color: 'var(--qz-white)', margin: '0 0 12px' }}>{h.title}</h1>
            <p style={{ font: 'var(--fw-regular) 15px/1.5 var(--font-body)', color: 'rgba(255,255,255,0.82)', margin: '0 0 18px' }}>{h.standfirst}</p>
            <Button variant="cta" size="md" fullWidth>Read the article</Button>
          </div>
        </section>
      </div>

      <section style={{ padding: '36px 20px', background: 'var(--qz-warm-white)' }}>
        <div className="qz-eyebrow" style={{ color: 'var(--accent-archive)', marginBottom: 8 }}>The Queenzone.com Archive</div>
        <h2 style={{ font: 'var(--fw-medium) 26px/1.1 var(--font-display)', margin: '0 0 20px' }}>Explore the Archive</h2>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
          {ex.map((it) => (
            <div key={it.label} style={{ padding: '18px 16px', background: 'var(--qz-white)', border: '1px solid var(--border-default)', borderRadius: 'var(--radius-sm)' }}>
              <i data-lucide={it.icon} style={{ width: 22, height: 22, color: 'var(--qz-charcoal)', strokeWidth: 1.4 }}></i>
              <div style={{ font: 'var(--fw-semibold) 15px/1.25 var(--font-display)', marginTop: 12, color: 'var(--text-primary)' }}>{it.label}</div>
              <div style={{ font: 'var(--fw-medium) 11px/1 var(--font-body)', textTransform: 'uppercase', letterSpacing: '0.05em', color: 'var(--text-muted)', marginTop: 5 }}>{it.count}</div>
            </div>
          ))}
        </div>
      </section>

      <section style={{ padding: '36px 20px', background: 'var(--qz-black)' }}>
        <div style={{ font: 'var(--fw-semibold) 12px/1 var(--font-titling)', letterSpacing: '0.18em', textTransform: 'uppercase', color: 'var(--qz-gold)', marginBottom: 10 }}>On This Day</div>
        <div style={{ font: 'var(--fw-semibold) 13px/1 var(--font-titling)', letterSpacing: '0.12em', textTransform: 'uppercase', color: 'rgba(255,255,255,0.55)', marginBottom: 14 }}>{td.date}</div>
        <p style={{ font: 'var(--fw-regular) 19px/1.45 var(--font-display)', color: 'var(--qz-white)', margin: 0 }}>{td.text}</p>
      </section>
    </div>
  );
}

// 2 — News list
function MobileNews() {
  const news = window.QZ_DATA.news;
  return (
    <div style={{ background: 'var(--qz-white)', minHeight: '100%' }}>
      <MHeader />
      <section style={{ background: 'var(--qz-black)', padding: '28px 20px 26px', position: 'relative', overflow: 'hidden' }}>
        <img src="../../assets/crest-white.png" alt="" style={{ position: 'absolute', right: -40, top: -20, width: 150, opacity: 0.06 }} />
        <div className="qz-eyebrow" style={{ color: 'var(--qz-gold)', marginBottom: 10 }}>The Archive</div>
        <h1 style={{ font: 'var(--fw-medium) 40px/1 var(--font-display)', color: 'var(--qz-white)', margin: 0 }}>News</h1>
        <div style={{ font: 'var(--fw-medium) 12px/1 var(--font-body)', textTransform: 'uppercase', letterSpacing: '0.1em', color: 'rgba(255,255,255,0.45)', marginTop: 14 }}>4,000+ articles</div>
      </section>
      <div style={{ padding: '18px 0 8px' }}><MChips items={window.QZ_DATA.newsYears} /></div>
      <div style={{ padding: '0 20px 30px' }}>
        {news.map((n, i) => (
          <a key={i} href="#" onClick={(e) => e.preventDefault()} style={{ display: 'block', textDecoration: 'none', padding: '20px 0', borderBottom: '1px solid var(--hairline)' }}>
            <div style={{ display: 'flex', gap: 12, marginBottom: 8 }}>
              <span style={{ font: 'var(--fw-semibold) 12px/1 var(--font-titling)', letterSpacing: '0.1em', textTransform: 'uppercase', color: 'var(--accent-archive)' }}>{n.date}</span>
              <span style={{ font: 'var(--fw-medium) 11px/1 var(--font-body)', letterSpacing: '0.08em', textTransform: 'uppercase', color: 'var(--text-muted)' }}>{n.cat}</span>
            </div>
            <h3 style={{ font: 'var(--fw-semibold) 20px/1.25 var(--font-display)', color: 'var(--text-primary)', margin: '0 0 5px' }}>{n.title}</h3>
            <p style={{ font: 'var(--fw-regular) 14px/1.5 var(--font-body)', color: 'var(--text-secondary)', margin: 0 }}>{n.excerpt}</p>
          </a>
        ))}
      </div>
    </div>
  );
}

// 3 — Photo gallery
function MobileGallery() {
  const photos = window.QZ_DATA.gallery;
  return (
    <div style={{ background: 'var(--qz-white)', minHeight: '100%' }}>
      <MHeader />
      <section style={{ background: 'var(--qz-black)', padding: '28px 20px 26px' }}>
        <div className="qz-eyebrow" style={{ color: 'var(--qz-gold)', marginBottom: 10 }}>The Photographic Archive</div>
        <h1 style={{ font: 'var(--fw-medium) 40px/1 var(--font-display)', color: 'var(--qz-white)', margin: 0 }}>Photography</h1>
      </section>
      <div style={{ padding: '18px 0 14px' }}><MChips items={window.QZ_DATA.galleryFilters} /></div>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8, padding: '0 18px 28px' }}>
        {photos.slice(0, 8).map((p, i) => (
          <figure key={i} style={{ margin: 0, position: 'relative', aspectRatio: '1', overflow: 'hidden', borderRadius: 'var(--radius-sm)', background: 'var(--qz-grey-200)' }}>
            <img src={p.image} alt={p.caption} style={{ width: '100%', height: '100%', objectFit: 'cover', filter: 'grayscale(1)' }} />
            <figcaption style={{ position: 'absolute', left: 0, right: 0, bottom: 0, padding: '20px 10px 8px', background: 'var(--scrim-soft)', font: 'var(--fw-medium) 10px/1.3 var(--font-body)', textTransform: 'uppercase', letterSpacing: '0.05em', color: 'rgba(255,255,255,0.92)', display: 'flex', justifyContent: 'space-between' }}>
              <span>{p.caption}</span><span style={{ color: 'var(--qz-gold)' }}>{p.year}</span>
            </figcaption>
          </figure>
        ))}
      </div>
    </div>
  );
}

// 4 — Article reading view
function MobileArticle() {
  const { Badge, Tag } = window.QueenzoneDesignSystem_6c12e8;
  const s = window.QZ_DATA.featured[0];
  const body = [
    'It began, as these things often do, with low expectations. By the summer of 1985 the band had weathered a difficult few years, and whispers that their finest moment had passed.',
    'What unfolded across twenty-one minutes at Wembley would settle the argument for a generation — less a set than a conversation with ninety thousand people, every one held in the palm of a single hand.',
  ];
  return (
    <div style={{ background: 'var(--qz-white)', minHeight: '100%' }}>
      <div style={{ position: 'relative', height: 340, overflow: 'hidden', background: 'var(--qz-black)' }}>
        <img src={s.image} alt="" style={{ position: 'absolute', inset: 0, width: '100%', height: '100%', objectFit: 'cover', opacity: 0.78, filter: 'grayscale(1)' }} />
        <div style={{ position: 'absolute', inset: 0, background: 'var(--scrim-bottom)' }}></div>
        <div style={{ position: 'absolute', top: 52, left: 18 }}>
          <div style={{ width: 38, height: 38, borderRadius: '50%', background: 'rgba(255,255,255,0.14)', border: '1px solid rgba(255,255,255,0.3)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <i data-lucide="arrow-left" style={{ width: 18, height: 18, color: '#fff' }}></i>
          </div>
        </div>
        <div style={{ position: 'absolute', bottom: 0, padding: '0 20px 24px' }}>
          <div style={{ marginBottom: 12 }}><Badge tone="editorial" variant="solid">{s.category}</Badge></div>
          <h1 style={{ font: 'var(--fw-medium) 30px/1.08 var(--font-display)', letterSpacing: '-0.015em', color: 'var(--qz-white)', margin: 0 }}>{s.title}</h1>
        </div>
      </div>
      <div style={{ padding: '24px 20px 40px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12, paddingBottom: 18, marginBottom: 22, borderBottom: '1px solid var(--hairline)' }}>
          <div style={{ width: 36, height: 36, borderRadius: '50%', background: 'var(--qz-grey-200)', display: 'flex', alignItems: 'center', justifyContent: 'center', font: 'var(--fw-semibold) 13px/1 var(--font-display)' }}>QZ</div>
          <div>
            <div style={{ font: 'var(--fw-semibold) 13px/1.3 var(--font-body)', color: 'var(--text-primary)' }}>The Queenzone Archive</div>
            <div style={{ font: 'var(--fw-medium) 11px/1 var(--font-body)', textTransform: 'uppercase', letterSpacing: '0.05em', color: 'var(--text-muted)', marginTop: 3 }}>{s.meta}</div>
          </div>
        </div>
        <p style={{ font: 'var(--fw-regular) 18px/1.55 var(--font-display)', color: 'var(--qz-charcoal)', margin: '0 0 22px' }}>{s.excerpt}</p>
        {body.map((p, i) => (
          <p key={i} style={{ font: 'var(--fw-regular) 16px/1.7 var(--font-body)', color: 'var(--qz-grey-700)', margin: '0 0 20px' }}>
            {i === 0 ? <span style={{ float: 'left', font: 'var(--fw-medium) 58px/0.8 var(--font-display)', color: 'var(--qz-charcoal)', margin: '4px 10px 0 0' }}>{p[0]}</span> : null}
            {i === 0 ? p.slice(1) : p}
          </p>
        ))}
        <div style={{ display: 'flex', gap: 7, flexWrap: 'wrap', marginTop: 14 }}>
          <Tag href="#">Live Aid</Tag><Tag href="#">1985</Tag><Tag href="#">Wembley</Tag>
        </div>
      </div>
    </div>
  );
}

Object.assign(window, { MobileHome, MobileNews, MobileGallery, MobileArticle });
```
