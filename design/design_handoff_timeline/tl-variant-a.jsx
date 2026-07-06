// "The Decades" — vertical, decade-grouped, built to scroll through hundreds of events.
// Compact rows by default (year · node · tag · title · one-line lede); click any row to
// expand the full card (standfirst, archival photo, read-the-story). A sticky rail jumps
// by decade AND by year within the active decade, and tracks position as you scroll.
const { useState: useStateA, useEffect: useEffectA, useRef: useRefA, useMemo: useMemoA } = React;

function DecadeRail({ groups, activeDecade, activeYear, counts, onJumpDecade, onJumpYear }) {
  return (
    <nav style={{ position: 'sticky', top: 96, alignSelf: 'start', maxHeight: 'calc(100vh - 120px)', overflowY: 'auto', paddingRight: 8, display: 'flex', flexDirection: 'column' }}>
      <div style={{ font: "var(--fw-semibold) 10px/1 var(--font-titling)", letterSpacing: '0.2em', textTransform: 'uppercase', color: 'var(--qz-grey-500)', marginBottom: 14, paddingLeft: 16 }}>Jump to</div>
      {groups.map((g) => {
        const on = activeDecade === g.d;
        return (
          <div key={g.d} style={{ marginBottom: 2 }}>
            <button onClick={() => onJumpDecade(g.d)} style={{
              display: 'flex', alignItems: 'baseline', gap: 9, textAlign: 'left', width: '100%', background: 'none', border: 'none',
              cursor: 'pointer', padding: '7px 0 7px 16px', position: 'relative',
              font: "var(--fw-medium) 17px/1 var(--font-titling)", letterSpacing: '0.06em',
              color: on ? 'var(--qz-gold-deep)' : 'var(--text-secondary)', transition: 'color 200ms ease',
            }}>
              <span style={{ position: 'absolute', left: 0, top: '50%', transform: 'translateY(-50%)', width: 2, height: on ? 20 : 0, background: 'var(--qz-gold)', transition: 'height 220ms ease' }}></span>
              {g.d}
              <span style={{ font: "var(--fw-medium) 11px/1 var(--font-body)", color: 'var(--qz-grey-400)' }}>{counts[g.d]}</span>
            </button>
            {/* year sub-markers — revealed only for the active decade */}
            <div style={{ overflow: 'hidden', maxHeight: on ? g.years.length * 30 + 8 : 0, transition: 'max-height 320ms cubic-bezier(.22,.61,.36,1)' }}>
              <div style={{ paddingLeft: 16, margin: '2px 0 8px', borderLeft: '1px solid var(--hairline)' }}>
                {g.years.map((y) => {
                  const yon = activeYear === y;
                  return (
                    <button key={y} onClick={() => onJumpYear(y)} style={{
                      display: 'block', textAlign: 'left', width: '100%', background: 'none', border: 'none', cursor: 'pointer',
                      padding: '5px 0 5px 14px', position: 'relative',
                      font: (yon ? 'var(--fw-semibold)' : 'var(--fw-regular)') + " 12px/1 var(--font-body)", letterSpacing: '0.04em',
                      color: yon ? 'var(--qz-gold-deep)' : 'var(--qz-grey-500)', transition: 'color 160ms ease',
                    }}>
                      <span style={{ position: 'absolute', left: -1, top: '50%', transform: 'translateY(-50%)', width: yon ? 8 : 0, height: 2, background: 'var(--qz-gold)', transition: 'width 180ms ease' }}></span>
                      {y}
                    </button>
                  );
                })}
              </div>
            </div>
          </div>
        );
      })}
    </nav>
  );
}

function EventRowA({ ev, first }) {
  const [open, setOpen] = useStateA(false);
  const c = window.QZ_CATS[ev.cat];
  return (
    <div data-year={ev.year} style={{ position: 'relative', paddingLeft: 34 }}>
      {/* spine + node */}
      <span style={{ position: 'absolute', left: 5, top: first ? 22 : 0, bottom: 0, width: 1, background: 'var(--hairline)' }}></span>
      <span style={{ position: 'absolute', left: 0, top: 20, width: 11, height: 11, borderRadius: '50%', background: c.color, boxShadow: '0 0 0 4px var(--qz-warm-white)', zIndex: 1 }}></span>

      {/* clickable compact header */}
      <button onClick={() => setOpen(!open)} style={{
        display: 'grid', gridTemplateColumns: '78px 1fr auto', alignItems: 'baseline', gap: 'clamp(14px,2.2vw,34px)',
        width: '100%', textAlign: 'left', background: 'none', border: 'none', cursor: 'pointer',
        padding: '13px 0', borderTop: first ? 'none' : '1px solid var(--hairline)',
      }}>
        <span style={{ font: "var(--fw-medium) 21px/1 var(--font-titling)", letterSpacing: '0.02em', color: 'var(--qz-charcoal)' }}>{ev.year}</span>
        <span style={{ minWidth: 0 }}>
          <span style={{ display: 'block', lineHeight: 1.25 }}>
            <span style={{ display: 'inline-flex', verticalAlign: '0.12em', marginRight: 14 }}><CatTag cat={ev.cat} /></span>
            <span style={{ font: "var(--fw-semibold) 20px/1.25 var(--font-display)", color: 'var(--text-primary)' }}>{ev.title}</span>
          </span>
          {!open && <span style={{ display: 'block', font: "var(--fw-regular) 15px/1.5 var(--font-body)", color: 'var(--text-muted)', margin: '5px 0 0', maxWidth: 620, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{ev.text}</span>}
        </span>
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="var(--qz-grey-500)" strokeWidth="2" style={{ marginTop: 6, transform: open ? 'rotate(180deg)' : 'none', transition: 'transform 300ms ease', flexShrink: 0 }}><path d="m6 9 6 6 6-6"></path></svg>
      </button>

      {/* expandable detail */}
      <div style={{ display: 'grid', gridTemplateRows: open ? '1fr' : '0fr', transition: 'grid-template-rows 380ms cubic-bezier(.22,.61,.36,1)' }}>
        <div style={{ overflow: 'hidden' }}>
          <div style={{ display: 'grid', gridTemplateColumns: '78px 1fr', gap: 'clamp(14px,2.2vw,34px)', paddingBottom: 26 }}>
            <span></span>
            <div style={{ display: 'grid', gridTemplateColumns: ev.img ? '1fr 190px' : '1fr', gap: 28, alignItems: 'start', maxWidth: ev.img ? 'none' : 640 }}>
              <div>
                <p style={{ font: "var(--fw-regular) 16px/1.62 var(--font-body)", color: 'var(--text-secondary)', margin: '0 0 12px' }}>{ev.text}</p>
                <p style={{ font: "var(--fw-regular) 15px/1.6 var(--font-body)", color: 'var(--text-secondary)', margin: 0, paddingTop: 12, borderTop: '1px solid var(--hairline)' }}>{ev.more}</p>
                <a href="#" onClick={(e) => e.preventDefault()} style={{ display: 'inline-flex', alignItems: 'center', gap: 6, marginTop: 16, font: "var(--fw-semibold) 12px/1 var(--font-body)", letterSpacing: '0.08em', textTransform: 'uppercase', color: 'var(--link)', textDecoration: 'none' }}>
                  Read the story
                  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M5 12h14M13 6l6 6-6 6"></path></svg>
                </a>
              </div>
              {ev.img && <EventPhoto img={ev.img} title={ev.title} ratio="1 / 1" />}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

function TimelineDecades() {
  const [filter, setFilter] = useStateA('all');
  const [activeDecade, setActiveDecade] = useStateA(window.QZ_DECADES[0]);
  const [activeYear, setActiveYear] = useStateA(null);
  const secRefs = useRefA({});
  const rootRef = useRefA(null);
  const all = window.QZ_TIMELINE;
  const events = filter === 'all' ? all : all.filter((e) => e.cat === filter);

  // group by decade, with the sorted unique years present in each
  const groups = useMemoA(() => {
    return window.QZ_DECADES.map((d) => {
      const items = events.filter((e) => window.qzDecadeOf(e.year) === d);
      const years = [...new Set(items.map((e) => e.year))].sort();
      return { d, items, years };
    }).filter((g) => g.items.length);
  }, [filter]);
  const counts = {};
  groups.forEach((g) => { counts[g.d] = g.items.length; });

  // track active decade + year as the reader scrolls
  useEffectA(() => {
    const decObs = new IntersectionObserver((es) => {
      es.forEach((e) => { if (e.isIntersecting) setActiveDecade(e.target.getAttribute('data-decade')); });
    }, { rootMargin: '-25% 0px -70% 0px' });
    Object.values(secRefs.current).forEach((el) => el && decObs.observe(el));

    const rows = rootRef.current ? rootRef.current.querySelectorAll('[data-year]') : [];
    const yearObs = new IntersectionObserver((es) => {
      es.forEach((e) => { if (e.isIntersecting) setActiveYear(e.target.getAttribute('data-year')); });
    }, { rootMargin: '-30% 0px -65% 0px' });
    rows.forEach((el) => yearObs.observe(el));

    return () => { decObs.disconnect(); yearObs.disconnect(); };
  }, [filter, groups.length]);

  const jumpDecade = (d) => {
    const el = secRefs.current[d];
    if (el) window.scrollTo({ top: el.getBoundingClientRect().top + window.pageYOffset - 84, behavior: 'smooth' });
  };
  const jumpYear = (y) => {
    const el = rootRef.current && rootRef.current.querySelector('[data-year="' + y + '"]');
    if (el) window.scrollTo({ top: el.getBoundingClientRect().top + window.pageYOffset - 96, behavior: 'smooth' });
  };

  return (
    <div style={{ background: 'var(--qz-warm-white)', minHeight: '100vh' }}>
      {/* hero */}
      <header style={{ position: 'relative', background: 'var(--qz-black)', overflow: 'hidden', padding: 'clamp(70px,9vw,120px) var(--gutter-lg) clamp(56px,7vw,90px)' }}>
        <img src="assets/crest-white.png" alt="" style={{ position: 'absolute', top: '50%', right: '4%', transform: 'translateY(-50%)', width: 260, opacity: 0.06 }} />
        <div style={{ maxWidth: 1180, margin: '0 auto', position: 'relative' }}>
          <div style={{ font: "var(--fw-semibold) 12px/1 var(--font-titling)", letterSpacing: '0.22em', textTransform: 'uppercase', color: 'var(--qz-gold)', marginBottom: 20 }}>Five decades &middot; 1970 &ndash; today</div>
          <h1 style={{ font: "var(--fw-regular) clamp(48px,7vw,88px)/1 var(--font-display)", letterSpacing: '-0.015em', color: '#fff', margin: '0 0 22px' }}>The Queen Timeline</h1>
          <p style={{ font: "var(--fw-regular) 20px/1.55 var(--font-body)", color: 'rgba(255,255,255,0.78)', margin: 0, maxWidth: 620 }}>The story of the band, year by year — a guided path through the music, the performances and the moments that gathered a community.</p>
        </div>
      </header>

      {/* sticky filter bar */}
      <div style={{ position: 'sticky', top: 0, zIndex: 20, background: 'rgba(247,246,243,0.92)', backdropFilter: 'saturate(180%) blur(10px)', borderBottom: '1px solid var(--hairline)' }}>
        <div style={{ maxWidth: 1180, margin: '0 auto', padding: '14px var(--gutter-lg)', display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 20, flexWrap: 'wrap' }}>
          <FilterChips active={filter} onChange={setFilter} />
          <span style={{ font: "var(--fw-medium) 12px/1 var(--font-body)", letterSpacing: '0.04em', color: 'var(--text-muted)' }}>{events.length} {events.length === 1 ? 'event' : 'events'}</span>
        </div>
      </div>

      {/* body: rail + events */}
      <div style={{ maxWidth: 1180, margin: '0 auto', padding: 'clamp(44px,5vw,72px) var(--gutter-lg) 140px', display: 'grid', gridTemplateColumns: '182px 1fr', gap: 'clamp(24px,4vw,72px)' }}>
        <DecadeRail groups={groups} activeDecade={activeDecade} activeYear={activeYear} counts={counts} onJumpDecade={jumpDecade} onJumpYear={jumpYear} />
        <div ref={rootRef}>
          {groups.map((g) => (
            <section key={g.d} data-decade={g.d} ref={(el) => (secRefs.current[g.d] = el)} style={{ marginBottom: 52 }}>
              <div style={{ display: 'flex', alignItems: 'baseline', gap: 16, marginBottom: 20 }}>
                <h2 style={{ font: "var(--fw-medium) 38px/1 var(--font-display)", color: 'var(--qz-charcoal)', margin: 0 }}>{g.d}</h2>
                <span style={{ flex: 1, height: 1, background: 'var(--border-strong)' }}></span>
                <span style={{ font: "var(--fw-medium) 12px/1 var(--font-body)", letterSpacing: '0.04em', color: 'var(--text-muted)' }}>{g.items.length}</span>
              </div>
              <div>
                {g.items.map((ev, i) => <EventRowA key={ev.year + ev.title} ev={ev} first={i === 0} />)}
              </div>
            </section>
          ))}
          {!groups.length && <p style={{ color: 'var(--text-muted)', font: 'var(--type-body)' }}>No events in this category.</p>}
        </div>
      </div>
    </div>
  );
}

window.TimelineDecades = TimelineDecades;
