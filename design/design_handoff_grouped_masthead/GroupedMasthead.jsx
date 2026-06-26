// Grouped Masthead — reference implementation (desktop).
// Cleaned from the prototype in ui_kits/website/grouped-masthead.html:
// the demo-only hero/light bands and dark/light toggle are removed.
//
// Wire `dark` from the route (true on pages with a black hero, false elsewhere)
// and `scrolled` from your scroll listener. See README.md §3–§4 for full spec,
// and §4 for the accessibility work this reference does NOT yet implement.
//
// Components come from the Queenzone design-system bundle.
// Data comes from ./nav-data.js (GROUPS).

import { GROUPS } from './nav-data.js';

const { useState, useRef } = React;
const { IconButton, Button } = window.QueenzoneDesignSystem_6c12e8;

function Chevron({ open, color }) {
  return (
    <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2"
      strokeLinecap="round" strokeLinejoin="round"
      style={{ transition: 'transform var(--dur-fast) var(--ease-out)', transform: open ? 'rotate(180deg)' : 'none' }}>
      <path d="m6 9 6 6 6-6" />
    </svg>
  );
}

function Panel({ group, dark }) {
  const surface = dark ? '#171717' : 'var(--qz-white)';
  const border = dark ? 'rgba(255,255,255,0.12)' : 'var(--qz-grey-200)';
  const titleC = dark ? 'var(--qz-white)' : 'var(--qz-charcoal)';
  const descC = dark ? 'rgba(255,255,255,0.55)' : 'var(--qz-grey-500)';
  const hover = dark ? 'rgba(255,255,255,0.05)' : 'var(--qz-warm-white)';
  const [hi, setHi] = useState(-1);
  return (
    <div style={{
      position: 'absolute', top: 'calc(100% + 14px)', left: -16, minWidth: 320,
      background: surface, border: `1px solid ${border}`, borderRadius: 4,
      boxShadow: dark ? '0 24px 60px rgba(0,0,0,0.55)' : '0 18px 50px rgba(17,17,17,0.14)',
      padding: '18px 14px 14px', animation: 'qzPanel var(--dur-fast) var(--ease-out)',
    }}>
      <div style={{
        font: '600 10px/1 var(--font-titling)', letterSpacing: '0.22em', textTransform: 'uppercase',
        color: group.accent, padding: '0 12px 12px', marginBottom: 4, borderBottom: `1px solid ${border}`,
      }}>{group.eyebrow}</div>
      {group.items.map((it, i) => (
        <a key={it.title} href={it.href}
          onMouseEnter={() => setHi(i)} onMouseLeave={() => setHi(-1)}
          style={{
            display: 'block', textDecoration: 'none', padding: '11px 12px', borderRadius: 3,
            background: hi === i ? hover : 'transparent', position: 'relative',
            transition: 'background var(--dur-fast) var(--ease-out)',
          }}>
          <span style={{
            position: 'absolute', left: 0, top: 14, bottom: 14, width: 2, borderRadius: 2,
            background: group.accent, opacity: hi === i ? 1 : 0,
            transition: 'opacity var(--dur-fast) var(--ease-out)',
          }} />
          <span style={{ display: 'flex', alignItems: 'center', gap: 9 }}>
            <span style={{ font: '500 15px/1.2 var(--font-body)', color: titleC }}>{it.title}</span>
            {it.tag && (
              <span style={{
                font: '600 9px/1 var(--font-titling)', letterSpacing: '0.14em', textTransform: 'uppercase',
                color: 'var(--qz-gold)', border: '1px solid rgba(184,154,74,0.5)',
                borderRadius: 2, padding: '3px 5px 2px',
              }}>{it.tag}</span>
            )}
          </span>
          <span style={{ display: 'block', font: '400 13px/1.4 var(--font-body)', color: descC, marginTop: 3 }}>{it.desc}</span>
        </a>
      ))}
    </div>
  );
}

// dark: invert for a black-hero page.  scrolled: pass true past ~12px scroll.
export function GroupedMasthead({ dark = false, scrolled = false }) {
  const [open, setOpen] = useState(-1);
  const timer = useRef(null);
  const enter = (i) => { clearTimeout(timer.current); setOpen(i); };
  const leave = () => { timer.current = setTimeout(() => setOpen(-1), 130); };

  const bg = dark
    ? (scrolled ? 'rgba(17,17,17,0.92)' : 'var(--qz-black)')
    : (scrolled ? 'rgba(255,255,255,0.92)' : 'var(--qz-white)');
  const wordmark = dark ? 'var(--qz-white)' : 'var(--qz-charcoal)';
  const navIdle = dark ? 'rgba(255,255,255,0.82)' : 'var(--qz-charcoal)';
  const navActive = dark ? 'var(--qz-gold)' : 'var(--qz-blue)';
  const crest = dark ? '/assets/crest-white.png' : '/assets/crest-black.png';

  return (
    <header style={{
      position: 'sticky', top: 0, zIndex: 50, background: bg,
      backdropFilter: scrolled ? 'saturate(180%) blur(12px)' : 'none',
      borderBottom: '1px solid rgba(184,154,74,0.55)',
      boxShadow: scrolled ? (dark ? '0 8px 28px rgba(0,0,0,0.45)' : '0 6px 22px rgba(17,17,17,0.10)') : 'none',
      transition: 'background var(--dur-base) var(--ease-out), box-shadow var(--dur-base) var(--ease-out)',
    }}>
      <div style={{ maxWidth: 'var(--container-max)', margin: '0 auto', padding: '0 var(--gutter-lg)', height: 76, display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 24 }}>
        <a href="/" style={{ display: 'flex', alignItems: 'center', gap: 14, textDecoration: 'none' }}>
          <img src={crest} alt="Queen crest" style={{ height: 42, width: 'auto' }} />
          <span style={{ fontFamily: 'var(--font-titling)', fontWeight: 600, fontSize: 21, letterSpacing: '0.18em', textTransform: 'uppercase', color: wordmark }}>Queenzone</span>
        </a>

        <nav style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          {GROUPS.map((g, i) => (
            <div key={g.label} style={{ position: 'relative' }}
              onMouseEnter={() => enter(i)} onMouseLeave={leave}>
              <button onClick={() => setOpen(open === i ? -1 : i)}
                aria-haspopup="true" aria-expanded={open === i}
                style={{
                  display: 'flex', alignItems: 'center', gap: 6, cursor: 'pointer',
                  background: 'none', border: 'none', padding: '8px 14px',
                  font: '500 14px/1 var(--font-body)', letterSpacing: '0.03em',
                  color: open === i ? navActive : navIdle,
                  transition: 'color var(--dur-fast) var(--ease-out)',
                }}>
                {g.label}
                <Chevron open={open === i} color={open === i ? navActive : navIdle} />
              </button>
              {open === i && <Panel group={g} dark={dark} />}
            </div>
          ))}
        </nav>

        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <IconButton label="Search" variant="ghost" onDark={dark}>
            <i data-lucide="search" style={{ width: 19, height: 19 }} />
          </IconButton>
          <Button variant="secondary" size="sm" style={dark ? { color: 'var(--qz-white)', borderColor: 'var(--border-on-dark)' } : {}}>Sign in</Button>
        </div>
      </div>
    </header>
  );
}
