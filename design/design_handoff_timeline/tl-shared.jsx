// Shared timeline helpers — category tags, filter chips, reveal-on-scroll, photo frame.
const { useState, useEffect, useRef } = React;

// Small uppercase category tag (Cinzel), coloured by meaning.
function CatTag({ cat, onDark }) {
  const c = window.QZ_CATS[cat];
  if (!c) return null;
  return (
    <span style={{
      display: 'inline-flex', alignItems: 'center', gap: 7,
      font: "var(--fw-semibold) 10px/1 var(--font-titling)", letterSpacing: '0.18em', textTransform: 'uppercase',
      color: onDark ? '#fff' : c.deep,
    }}>
      <span style={{ width: 7, height: 7, borderRadius: '50%', background: c.color, boxShadow: onDark ? '0 0 0 3px rgba(255,255,255,0.08)' : 'none' }}></span>
      {c.label}
    </span>
  );
}

// Filter chips — All + one per category. `onDark` swaps to the dark surface styling.
function FilterChips({ active, onChange, onDark }) {
  const cats = Object.keys(window.QZ_CATS);
  const base = {
    font: "var(--fw-medium) 12px/1 var(--font-body)", letterSpacing: '0.04em',
    padding: '9px 15px', borderRadius: 2, cursor: 'pointer', background: 'none',
    transition: 'all 200ms ease', whiteSpace: 'nowrap',
  };
  const chip = (key, label, color) => {
    const on = active === key;
    const idle = onDark ? 'rgba(255,255,255,0.55)' : 'var(--text-secondary)';
    const bd = onDark ? 'rgba(255,255,255,0.18)' : 'var(--border-strong)';
    return (
      <button key={key} onClick={() => onChange(key)} style={{
        ...base,
        border: '1px solid ' + (on ? (color || (onDark ? '#fff' : 'var(--qz-charcoal)')) : bd),
        color: on ? (onDark ? '#fff' : (color ? color : 'var(--qz-charcoal)')) : idle,
        background: on ? (onDark ? 'rgba(255,255,255,0.06)' : (color ? 'transparent' : 'transparent')) : 'transparent',
        fontWeight: on ? 600 : 500,
      }}>
        {key !== 'all' && <span style={{ display: 'inline-block', width: 7, height: 7, borderRadius: '50%', background: color, marginRight: 8, verticalAlign: 'middle' }}></span>}
        {label}
      </button>
    );
  };
  return (
    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10, alignItems: 'center' }}>
      {chip('all', 'All events', null)}
      {cats.map((k) => chip(k, window.QZ_CATS[k].label, window.QZ_CATS[k].color))}
    </div>
  );
}

// Reveal-on-scroll wrapper. Base state is VISIBLE (print / reduced-motion / no-JS safe);
// animates FROM hidden only when motion is allowed and the observer fires.
function Reveal({ children, y = 22, delay = 0, style }) {
  const ref = useRef(null);
  const reduce = typeof window.matchMedia === 'function' && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  const [shown, setShown] = useState(reduce);
  useEffect(() => {
    if (reduce || !ref.current || !('IntersectionObserver' in window)) { setShown(true); return; }
    const ob = new IntersectionObserver((es) => {
      es.forEach((e) => { if (e.isIntersecting) { setShown(true); ob.disconnect(); } });
    }, { threshold: 0.15, rootMargin: '0px 0px -8% 0px' });
    ob.observe(ref.current);
    return () => ob.disconnect();
  }, [reduce]);
  return (
    <div ref={ref} style={{
      ...style,
      opacity: shown ? 1 : 0,
      transform: shown ? 'none' : 'translateY(' + y + 'px)',
      transition: 'opacity 700ms cubic-bezier(.22,.61,.36,1) ' + delay + 'ms, transform 700ms cubic-bezier(.22,.61,.36,1) ' + delay + 'ms',
    }}>{children}</div>
  );
}

// Archival photo frame. Real asset (greyscaled) or an on-brand placeholder.
function EventPhoto({ img, title, ratio = '4 / 3', rounded = 2 }) {
  const frame = {
    position: 'relative', width: '100%', aspectRatio: ratio, overflow: 'hidden',
    borderRadius: rounded, border: '1px solid var(--hairline)', background: 'var(--qz-grey-100)',
  };
  if (img) {
    return (
      <div style={frame}>
        <img src={img} alt={title} style={{ width: '100%', height: '100%', objectFit: 'cover', filter: 'grayscale(1) contrast(1.02)' }} />
      </div>
    );
  }
  return (
    <div style={{ ...frame, display: 'flex', alignItems: 'center', justifyContent: 'center',
      background: 'repeating-linear-gradient(135deg, #ECEBE6 0 14px, #F3F2EE 14px 28px)' }}>
      <div style={{ textAlign: 'center', color: 'var(--qz-grey-500)' }}>
        <svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.4" style={{ margin: '0 auto 6px' }}>
          <rect x="3" y="5" width="18" height="14" rx="1.5"></rect><circle cx="12" cy="12" r="3.2"></circle><path d="M8 5l1.5-2h5L16 5"></path>
        </svg>
        <div style={{ font: "var(--fw-semibold) 9px/1 var(--font-titling)", letterSpacing: '0.2em', textTransform: 'uppercase' }}>Archive photo</div>
      </div>
    </div>
  );
}

Object.assign(window, { CatTag, FilterChips, Reveal, EventPhoto });
