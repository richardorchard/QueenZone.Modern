# Queenzone — Component Source Reference

Full source for every reusable primitive, with its TypeScript prop contract (`.d.ts`) and usage notes (`.prompt.md`). These are React references — recreate them in your framework with the same prop API. Styling is via the CSS custom properties in `styles.css` / `tokens/`.


---

## `components/core/Button.jsx`

```jsx
import React from 'react';

/**
 * Queenzone Button — restrained, editorial. Sharp 3px corners, no gloss.
 * Variants map to the brand's sparing accent usage.
 */
export function Button({
  children,
  variant = 'primary',
  size = 'md',
  iconLeft = null,
  iconRight = null,
  fullWidth = false,
  disabled = false,
  as = 'button',
  href,
  style = {},
  ...rest
}) {
  const sizes = {
    sm: { padding: '8px 16px', fontSize: '13px' },
    md: { padding: '12px 24px', fontSize: '14px' },
    lg: { padding: '16px 34px', fontSize: '15px' },
  };

  const variants = {
    primary: {
      background: 'var(--qz-black)',
      color: 'var(--qz-white)',
      border: '1px solid var(--qz-black)',
    },
    cta: {
      background: 'var(--qz-blue)',
      color: 'var(--qz-white)',
      border: '1px solid var(--qz-blue)',
    },
    secondary: {
      background: 'transparent',
      color: 'var(--qz-charcoal)',
      border: '1px solid var(--border-strong)',
    },
    ghost: {
      background: 'transparent',
      color: 'var(--qz-charcoal)',
      border: '1px solid transparent',
    },
    editorial: {
      background: 'var(--qz-burgundy)',
      color: 'var(--qz-white)',
      border: '1px solid var(--qz-burgundy)',
    },
  };

  const base = {
    display: 'inline-flex',
    alignItems: 'center',
    justifyContent: 'center',
    gap: '8px',
    width: fullWidth ? '100%' : 'auto',
    fontFamily: 'var(--font-body)',
    fontWeight: 'var(--fw-medium)',
    letterSpacing: '0.04em',
    textTransform: 'uppercase',
    borderRadius: 'var(--radius-sm)',
    cursor: disabled ? 'not-allowed' : 'pointer',
    opacity: disabled ? 0.45 : 1,
    transition: 'background var(--dur-fast) var(--ease-out), color var(--dur-fast) var(--ease-out), border-color var(--dur-fast) var(--ease-out), transform var(--dur-fast) var(--ease-out)',
    textDecoration: 'none',
    whiteSpace: 'nowrap',
    ...sizes[size],
    ...variants[variant],
    ...style,
  };

  const Tag = href ? 'a' : as;
  return (
    <Tag
      href={href}
      style={base}
      disabled={disabled}
      onMouseDown={(e) => { if (!disabled) e.currentTarget.style.transform = 'translateY(1px)'; }}
      onMouseUp={(e) => { e.currentTarget.style.transform = 'translateY(0)'; }}
      onMouseLeave={(e) => { e.currentTarget.style.transform = 'translateY(0)'; }}
      {...rest}
    >
      {iconLeft}
      {children}
      {iconRight}
    </Tag>
  );
}
```

---

## `components/core/Button.d.ts`

```ts
import * as React from 'react';

/**
 * Action control props.
 * @startingPoint section="Core" subtitle="Buttons in all five variants & sizes" viewport="700x200"
 */
export interface ButtonProps {
  children?: React.ReactNode;
  /** Visual weight. `cta` = royal blue, `editorial` = burgundy. */
  variant?: 'primary' | 'cta' | 'secondary' | 'ghost' | 'editorial';
  size?: 'sm' | 'md' | 'lg';
  iconLeft?: React.ReactNode;
  iconRight?: React.ReactNode;
  fullWidth?: boolean;
  disabled?: boolean;
  /** Render as a link instead of a button. */
  href?: string;
  as?: 'button' | 'a';
  style?: React.CSSProperties;
  onClick?: (e: React.MouseEvent) => void;
}

/**
 * Primary action control for Queenzone — sharp-cornered, uppercase, editorial.
 */
export function Button(props: ButtonProps): JSX.Element;
```

---

## `components/core/Button.prompt.md`

```md
Primary action control for Queenzone — sharp-cornered, uppercase, restrained. Use `cta` (royal blue) for the single most important action on a view; `primary` (rich black) elsewhere; `editorial` (burgundy) on featured/premium content.

```jsx
<Button variant="cta" size="lg">Explore the archive</Button>
<Button variant="secondary" iconRight={<Icon name="arrow-right" />}>Read more</Button>
```

Variants: `primary` · `cta` · `secondary` · `ghost` · `editorial`. Sizes: `sm` · `md` · `lg`. Accepts `iconLeft` / `iconRight`, `fullWidth`, `disabled`, and `href` (renders an anchor). Keep one accent button per section — accents are ~10% of the interface.
```

---

## `components/core/IconButton.jsx`

```jsx
import React from 'react';

/**
 * Icon-only control — search, menu, share, bookmark. Square, quiet by default.
 */
export function IconButton({
  children,
  label,
  variant = 'ghost',
  size = 'md',
  active = false,
  onDark = false,
  style = {},
  ...rest
}) {
  const dims = { sm: 32, md: 40, lg: 48 }[size];

  const variants = {
    ghost: {
      background: 'transparent',
      color: onDark ? 'var(--text-on-dark)' : 'var(--qz-charcoal)',
      border: '1px solid transparent',
    },
    outline: {
      background: 'transparent',
      color: onDark ? 'var(--text-on-dark)' : 'var(--qz-charcoal)',
      border: `1px solid ${onDark ? 'var(--border-on-dark)' : 'var(--border-strong)'}`,
    },
    solid: {
      background: 'var(--qz-black)',
      color: 'var(--qz-white)',
      border: '1px solid var(--qz-black)',
    },
  };

  return (
    <button
      aria-label={label}
      aria-pressed={active}
      style={{
        width: dims,
        height: dims,
        display: 'inline-flex',
        alignItems: 'center',
        justifyContent: 'center',
        borderRadius: 'var(--radius-sm)',
        cursor: 'pointer',
        color: active ? 'var(--qz-blue)' : undefined,
        transition: 'background var(--dur-fast) var(--ease-out), color var(--dur-fast) var(--ease-out)',
        ...variants[variant],
        ...(active ? { color: 'var(--qz-blue)' } : {}),
        ...style,
      }}
      onMouseEnter={(e) => { e.currentTarget.style.background = onDark ? 'rgba(255,255,255,0.10)' : 'var(--qz-grey-100)'; }}
      onMouseLeave={(e) => { e.currentTarget.style.background = variants[variant].background; }}
      {...rest}
    >
      {children}
    </button>
  );
}
```

---

## `components/core/IconButton.d.ts`

```ts
import * as React from 'react';

export interface IconButtonProps {
  /** Pass an SVG/icon node as the child. */
  children?: React.ReactNode;
  /** Accessible label (required for icon-only controls). */
  label: string;
  variant?: 'ghost' | 'outline' | 'solid';
  size?: 'sm' | 'md' | 'lg';
  active?: boolean;
  /** Style for placement over dark hero imagery. */
  onDark?: boolean;
  style?: React.CSSProperties;
  onClick?: (e: React.MouseEvent) => void;
}

/** Square icon-only control for nav, search, share and bookmark actions. */
export function IconButton(props: IconButtonProps): JSX.Element;
```

---

## `components/core/IconButton.prompt.md`

```md
Square icon-only control for navigation, search, share and bookmark actions. Supply the icon (e.g. a Lucide SVG) as the child and always pass an accessible `label`.

```jsx
<IconButton label="Search"><i data-lucide="search"></i></IconButton>
<IconButton label="Bookmark" variant="outline" active />
<IconButton label="Menu" onDark variant="ghost"><i data-lucide="menu"></i></IconButton>
```

Variants: `ghost` (default) · `outline` · `solid`. Sizes: `sm` 32 · `md` 40 · `lg` 48. Use `onDark` over hero imagery; `active` tints the icon royal blue.
```

---

## `components/core/Input.jsx`

```jsx
import React from 'react';

/**
 * Text input / search field — hairline border, generous padding, quiet focus.
 */
export function Input({
  type = 'text',
  placeholder = '',
  value,
  defaultValue,
  iconLeft = null,
  size = 'md',
  invalid = false,
  disabled = false,
  fullWidth = true,
  style = {},
  onChange,
  ...rest
}) {
  const pad = { sm: '9px 12px', md: '13px 16px', lg: '16px 18px' }[size];
  const fs = { sm: '14px', md: '15px', lg: '16px' }[size];

  return (
    <div style={{ position: 'relative', width: fullWidth ? '100%' : 'auto', display: 'inline-flex', alignItems: 'center' }}>
      {iconLeft && (
        <span style={{ position: 'absolute', left: 14, display: 'inline-flex', color: 'var(--text-muted)', pointerEvents: 'none' }}>
          {iconLeft}
        </span>
      )}
      <input
        type={type}
        placeholder={placeholder}
        value={value}
        defaultValue={defaultValue}
        disabled={disabled}
        onChange={onChange}
        style={{
          width: '100%',
          padding: pad,
          paddingLeft: iconLeft ? 42 : undefined,
          font: `var(--fw-regular) ${fs}/1.4 var(--font-body)`,
          color: 'var(--text-primary)',
          background: disabled ? 'var(--qz-grey-100)' : 'var(--qz-white)',
          border: `1px solid ${invalid ? 'var(--qz-burgundy)' : 'var(--border-strong)'}`,
          borderRadius: 'var(--radius-xs)',
          outline: 'none',
          transition: 'border-color var(--dur-fast) var(--ease-out), box-shadow var(--dur-fast) var(--ease-out)',
          ...style,
        }}
        onFocus={(e) => {
          e.currentTarget.style.borderColor = invalid ? 'var(--qz-burgundy)' : 'var(--qz-blue)';
          e.currentTarget.style.boxShadow = 'var(--shadow-focus)';
        }}
        onBlur={(e) => {
          e.currentTarget.style.borderColor = invalid ? 'var(--qz-burgundy)' : 'var(--border-strong)';
          e.currentTarget.style.boxShadow = 'none';
        }}
        {...rest}
      />
    </div>
  );
}
```

---

## `components/core/Input.d.ts`

```ts
import * as React from 'react';

export interface InputProps {
  type?: string;
  placeholder?: string;
  value?: string;
  defaultValue?: string;
  /** Leading icon node (e.g. a search glyph). */
  iconLeft?: React.ReactNode;
  size?: 'sm' | 'md' | 'lg';
  invalid?: boolean;
  disabled?: boolean;
  fullWidth?: boolean;
  style?: React.CSSProperties;
  onChange?: (e: React.ChangeEvent<HTMLInputElement>) => void;
}

/** Hairline-bordered text / search field with quiet royal-blue focus ring. */
export function Input(props: InputProps): JSX.Element;
```

---

## `components/core/Input.prompt.md`

```md
Hairline-bordered text and search field. Quiet by default; focus brings a royal-blue border and soft ring. Use `iconLeft` for search.

```jsx
<Input placeholder="Search 4,000+ articles…" iconLeft={<i data-lucide="search"></i>} />
<Input size="lg" invalid placeholder="Email" />
```

Sizes: `sm` · `md` · `lg`. `invalid` switches the border to burgundy. Defaults to `fullWidth`.
```

---

## `components/editorial/Badge.jsx`

```jsx
import React from 'react';

/**
 * Editorial content marker — anniversary, premium, archive, etc.
 * Mirrors the brand's accent-by-meaning system.
 */
export function Badge({ children, tone = 'neutral', variant = 'soft', style = {}, ...rest }) {
  const tones = {
    neutral:   { c: 'var(--qz-charcoal)',  soft: 'var(--qz-grey-100)',    solid: 'var(--qz-charcoal)' },
    archive:   { c: 'var(--qz-purple)',    soft: 'var(--qz-purple-tint)', solid: 'var(--qz-purple)' },
    editorial: { c: 'var(--qz-burgundy)',  soft: 'var(--qz-burgundy-tint)', solid: 'var(--qz-burgundy)' },
    cta:       { c: 'var(--qz-blue)',      soft: 'var(--qz-blue-tint)',   solid: 'var(--qz-blue)' },
    special:   { c: 'var(--qz-gold-deep)', soft: 'var(--qz-gold-tint)',   solid: 'var(--qz-gold)' },
  };
  const t = tones[tone] || tones.neutral;

  const looks = {
    soft:    { background: t.soft, color: t.c, border: '1px solid transparent' },
    solid:   { background: t.solid, color: 'var(--qz-white)', border: `1px solid ${t.solid}` },
    outline: { background: 'transparent', color: t.c, border: `1px solid ${t.c}` },
  };

  return (
    <span
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: '6px',
        padding: '4px 10px',
        fontFamily: 'var(--font-titling)',
        fontSize: '10.5px',
        fontWeight: 'var(--fw-semibold)',
        letterSpacing: 'var(--ls-eyebrow)',
        textTransform: 'uppercase',
        borderRadius: 'var(--radius-xs)',
        lineHeight: 1,
        ...looks[variant],
        ...style,
      }}
      {...rest}
    >
      {children}
    </span>
  );
}
```

---

## `components/editorial/Badge.d.ts`

```ts
import * as React from 'react';

/**
 * Content marker props.
 * @startingPoint section="Editorial" subtitle="Content markers in every tone" viewport="700x150"
 */
export interface BadgeProps {
  children?: React.ReactNode;
  /** Accent meaning: archive=purple, editorial=burgundy, cta=blue, special=gold. */
  tone?: 'neutral' | 'archive' | 'editorial' | 'cta' | 'special';
  variant?: 'soft' | 'solid' | 'outline';
  style?: React.CSSProperties;
}

/** Small Cinzel content marker for featured, anniversary and archive items. */
export function Badge(props: BadgeProps): JSX.Element;
```

---

## `components/editorial/Badge.prompt.md`

```md
Small Cinzel content marker. Tone carries meaning — match the brand's accent system: `editorial` (burgundy) for featured/premium, `archive` (purple) for historical, `special` (gold) for anniversaries, `cta` (blue) for new/active.

```jsx
<Badge tone="editorial">Featured Story</Badge>
<Badge tone="special" variant="solid">40th Anniversary</Badge>
<Badge tone="archive" variant="outline">Restored 2026</Badge>
```

Variants: `soft` (default) · `solid` · `outline`. Use gold sparingly — anniversaries and key highlights only.
```

---

## `components/editorial/Tag.jsx`

```jsx
import React from 'react';

/**
 * Quiet category tag — Inter, hairline, for taxonomies (albums, eras, topics).
 * Lighter-weight than Badge; used in clusters.
 */
export function Tag({ children, href, active = false, onDark = false, style = {}, ...rest }) {
  const Tag_ = href ? 'a' : 'span';
  const idleText = onDark ? 'rgba(255,255,255,0.72)' : 'var(--qz-grey-700)';
  const idleBorder = onDark ? 'var(--border-on-dark)' : 'var(--border-strong)';
  const hoverText = onDark ? 'var(--qz-white)' : 'var(--qz-charcoal)';
  const hoverBorder = onDark ? 'rgba(255,255,255,0.5)' : 'var(--qz-charcoal)';
  const activeBg = onDark ? 'var(--qz-white)' : 'var(--qz-charcoal)';
  const activeText = onDark ? 'var(--qz-charcoal)' : 'var(--qz-white)';
  return (
    <Tag_
      href={href}
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        padding: '5px 12px',
        font: `var(--fw-medium) 12px/1 var(--font-body)`,
        letterSpacing: '0.04em',
        color: active ? activeText : idleText,
        background: active ? activeBg : 'transparent',
        border: `1px solid ${active ? activeBg : idleBorder}`,
        borderRadius: 'var(--radius-pill)',
        cursor: href ? 'pointer' : 'default',
        transition: 'all var(--dur-fast) var(--ease-out)',
        textDecoration: 'none',
        ...style,
      }}
      onMouseEnter={(e) => { if (!active && href) { e.currentTarget.style.borderColor = hoverBorder; e.currentTarget.style.color = hoverText; } }}
      onMouseLeave={(e) => { if (!active && href) { e.currentTarget.style.borderColor = idleBorder; e.currentTarget.style.color = idleText; } }}
      {...rest}
    >
      {children}
    </Tag_>
  );
}
```

---

## `components/editorial/Tag.d.ts`

```ts
import * as React from 'react';

export interface TagProps {
  children?: React.ReactNode;
  /** Renders as a clickable link with hover. */
  href?: string;
  active?: boolean;
  /** Light styling for placement on rich-black sections. */
  onDark?: boolean;
  style?: React.CSSProperties;
}

/** Quiet pill-shaped category tag for taxonomies (albums, eras, topics). */
export function Tag(props: TagProps): JSX.Element;
```

---

## `components/editorial/Tag.prompt.md`

```md
Quiet pill-shaped category tag for taxonomies — albums, eras, topics. Used in clusters; lighter than Badge.

```jsx
<Tag href="#" active>All</Tag>
<Tag href="#">A Night at the Opera</Tag>
<Tag href="#">Live Aid</Tag>
```

Pass `active` for the selected filter. With `href` it becomes a hoverable link.
```

---

## `components/editorial/SectionHeader.jsx`

```jsx
import React from 'react';

/**
 * Editorial section header — Cinzel eyebrow, Cormorant serif title, optional
 * trailing action. The primary rhythm device between homepage sections.
 */
export function SectionHeader({
  eyebrow,
  title,
  action = null,
  align = 'left',
  onDark = false,
  style = {},
}) {
  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'flex-end',
        justifyContent: align === 'center' ? 'center' : 'space-between',
        gap: 'var(--space-5)',
        borderBottom: align === 'center' ? 'none' : `1px solid ${onDark ? 'var(--border-on-dark)' : 'var(--hairline)'}`,
        paddingBottom: align === 'center' ? 0 : 'var(--space-4)',
        textAlign: align,
        ...style,
      }}
    >
      <div style={{ width: align === 'center' ? '100%' : 'auto' }}>
        {eyebrow && (
          <div
            style={{
              fontFamily: 'var(--font-titling)',
              fontSize: 'var(--fs-eyebrow)',
              fontWeight: 'var(--fw-semibold)',
              letterSpacing: 'var(--ls-eyebrow)',
              textTransform: 'uppercase',
              color: onDark ? 'var(--qz-gold)' : 'var(--accent-archive)',
              marginBottom: 'var(--space-3)',
            }}
          >
            {eyebrow}
          </div>
        )}
        <h2
          style={{
            font: 'var(--type-h2)',
            color: onDark ? 'var(--text-on-dark)' : 'var(--text-primary)',
            margin: 0,
          }}
        >
          {title}
        </h2>
      </div>
      {action && align !== 'center' && (
        <div style={{ flexShrink: 0, paddingBottom: '4px' }}>{action}</div>
      )}
    </div>
  );
}
```

---

## `components/editorial/SectionHeader.d.ts`

```ts
import * as React from 'react';

export interface SectionHeaderProps {
  /** Cinzel uppercase kicker above the title. */
  eyebrow?: string;
  title: React.ReactNode;
  /** Trailing action node (e.g. a "View all" link) — left-aligned only. */
  action?: React.ReactNode;
  align?: 'left' | 'center';
  /** Style for placement on rich-black sections. */
  onDark?: boolean;
  style?: React.CSSProperties;
}

/** Section heading with Cinzel eyebrow + Cormorant title — the homepage rhythm device. */
export function SectionHeader(props: SectionHeaderProps): JSX.Element;
```

---

## `components/editorial/SectionHeader.prompt.md`

```md
Section heading — Cinzel eyebrow over a Cormorant Garamond title, with a hairline underline and optional trailing action. The primary device separating homepage sections.

```jsx
<SectionHeader
  eyebrow="Explore the Archive"
  title="This Day in Queen History"
  action={<Button variant="ghost" size="sm">View all</Button>} />

<SectionHeader eyebrow="Featured" title="Articles" align="center" />
```

Use `onDark` on rich-black sections (eyebrow turns gold). `align="center"` drops the underline and action for hero-style intros.
```

---

## `components/editorial/ArticleCard.jsx`

```jsx
import React from 'react';

/**
 * Editorial story card — archival b&w image, Cormorant title, quiet meta.
 * The workhorse of the Queenzone homepage and archive grids.
 */
export function ArticleCard({
  image,
  category,
  title,
  excerpt,
  meta,
  badge = null,
  href = '#',
  layout = 'vertical',
  monochrome = true,
  onDark = false,
  style = {},
}) {
  const horizontal = layout === 'horizontal';
  const titleColor = onDark ? 'var(--qz-white)' : 'var(--text-primary)';
  const excerptColor = onDark ? 'rgba(255,255,255,0.66)' : 'var(--text-secondary)';
  const metaColor = onDark ? 'rgba(255,255,255,0.5)' : 'var(--text-muted)';
  const catColor = onDark ? 'var(--qz-gold)' : 'var(--accent-archive)';

  return (
    <a
      href={href}
      style={{
        display: horizontal ? 'grid' : 'flex',
        gridTemplateColumns: horizontal ? '40% 1fr' : undefined,
        flexDirection: 'column',
        gap: horizontal ? 'var(--space-5)' : '0',
        background: 'transparent',
        textDecoration: 'none',
        color: 'inherit',
        ...style,
      }}
      className="qz-article-card"
      onMouseEnter={(e) => { const im = e.currentTarget.querySelector('img'); if (im) { im.style.transform = 'scale(1.04)'; im.style.filter = monochrome ? 'grayscale(0)' : 'none'; } }}
      onMouseLeave={(e) => { const im = e.currentTarget.querySelector('img'); if (im) { im.style.transform = 'scale(1)'; im.style.filter = monochrome ? 'grayscale(1)' : 'none'; } }}
    >
      {image && (
        <div style={{ position: 'relative', overflow: 'hidden', borderRadius: 'var(--radius-md)', aspectRatio: horizontal ? '4 / 3' : '3 / 2', background: 'var(--qz-grey-200)' }}>
          <img
            src={image}
            alt=""
            style={{
              width: '100%', height: '100%', objectFit: 'cover',
              filter: monochrome ? 'grayscale(1)' : 'none',
              transition: 'transform var(--dur-slow) var(--ease-out), filter var(--dur-slow) var(--ease-out)',
            }}
          />
          {badge && <div style={{ position: 'absolute', top: 12, left: 12 }}>{badge}</div>}
        </div>
      )}
      <div style={{ paddingTop: image && !horizontal ? 'var(--space-4)' : 0 }}>
        {category && (
          <div style={{
            fontFamily: 'var(--font-titling)', fontSize: '11px', fontWeight: 'var(--fw-semibold)',
            letterSpacing: 'var(--ls-eyebrow)', textTransform: 'uppercase',
            color: catColor, marginBottom: 'var(--space-2)',
          }}>{category}</div>
        )}
        <h3 style={{
          font: 'var(--fw-semibold) 1.5rem/1.18 var(--font-display)',
          letterSpacing: 'var(--ls-display)', color: titleColor,
          margin: '0 0 var(--space-2)',
        }}>{title}</h3>
        {excerpt && (
          <p style={{ font: 'var(--type-body)', color: excerptColor, margin: '0 0 var(--space-3)', fontSize: '15px' }}>{excerpt}</p>
        )}
        {meta && (
          <div style={{ font: 'var(--type-meta)', color: metaColor, textTransform: 'uppercase', letterSpacing: 'var(--ls-caps)' }}>{meta}</div>
        )}
      </div>
    </a>
  );
}
```

---

## `components/editorial/ArticleCard.d.ts`

```ts
import * as React from 'react';

/**
 * Editorial story card props.
 * @startingPoint section="Editorial" subtitle="Story card — vertical & horizontal" viewport="700x420"
 */
export interface ArticleCardProps {
  /** Image URL — rendered black & white by default, easing to colour on hover. */
  image?: string;
  /** Cinzel category kicker. */
  category?: string;
  title: React.ReactNode;
  excerpt?: string;
  /** Meta line, e.g. "12 June 2026 · 6 min read". */
  meta?: React.ReactNode;
  /** A <Badge> node overlaid on the image. */
  badge?: React.ReactNode;
  href?: string;
  layout?: 'vertical' | 'horizontal';
  /** Keep image monochrome (default true) per photography direction. */
  monochrome?: boolean;
  /** Light text + gold kicker for placement on rich-black sections. */
  onDark?: boolean;
  style?: React.CSSProperties;
}

/** Editorial story card with archival b&w imagery and a Cormorant title. */
export function ArticleCard(props: ArticleCardProps): JSX.Element;
```

---

## `components/editorial/ArticleCard.prompt.md`

```md
Editorial story card — the workhorse of homepage and archive grids. Archival black-and-white imagery eases gently to colour on hover; Cormorant title, Cinzel category kicker, quiet uppercase meta.

```jsx
<ArticleCard
  image="/assets/photo.jpg"
  category="Live History"
  title="The Day Queen Stole Live Aid"
  excerpt="Twenty-one minutes that rewrote the rules of the stadium show."
  meta="13 July 1985 · 8 min read"
  badge={<Badge tone="editorial">Featured</Badge>} />
```

`layout="horizontal"` for list rows (image left). Keep `monochrome` on for the archival look. Pair the kicker tone with the section's accent.
```

---

## `components/brand/CrestSeal.jsx`

```jsx
import React from 'react';

/**
 * The Queen crest as a seal / emblem / watermark — the site's visual anchor.
 * Supply `src` pointing to a crest asset (black, white, silver or line-art).
 */
export function CrestSeal({
  src,
  size = 72,
  treatment = 'seal',
  alt = 'Queen crest',
  style = {},
}) {
  const treatments = {
    seal:      { opacity: 1, filter: 'none' },
    watermark: { opacity: 0.06, filter: 'none' },
    ghost:     { opacity: 0.14, filter: 'none' },
    divider:   { opacity: 0.9, filter: 'none' },
  };
  const t = treatments[treatment] || treatments.seal;

  if (treatment === 'divider') {
    return (
      <div style={{ display: 'flex', alignItems: 'center', gap: 'var(--space-5)', ...style }}>
        <span style={{ flex: 1, height: 1, background: 'var(--hairline)' }} />
        <img src={src} alt={alt} style={{ width: size, height: 'auto', opacity: 0.85 }} />
        <span style={{ flex: 1, height: 1, background: 'var(--hairline)' }} />
      </div>
    );
  }

  return (
    <img
      src={src}
      alt={alt}
      style={{
        width: size,
        height: 'auto',
        opacity: t.opacity,
        filter: t.filter,
        userSelect: 'none',
        pointerEvents: 'none',
        ...style,
      }}
    />
  );
}
```

---

## `components/brand/CrestSeal.d.ts`

```ts
import * as React from 'react';

/**
 * Crest emblem props.
 * @startingPoint section="Brand" subtitle="Crest seal, watermark & divider" viewport="700x260"
 */
export interface CrestSealProps {
  /** URL to a crest asset (crest-black/white/silver/lineart.png). */
  src: string;
  /** Rendered width in px (height auto). */
  size?: number;
  /** seal = full emblem · watermark = 6% ghost · ghost = 14% · divider = crest between hairlines. */
  treatment?: 'seal' | 'watermark' | 'ghost' | 'divider';
  alt?: string;
  style?: React.CSSProperties;
}

/** The Queen crest as a seal, watermark or section divider — the brand anchor. */
export function CrestSeal(props: CrestSealProps): JSX.Element;
```

---

## `components/brand/CrestSeal.prompt.md`

```md
The Queen crest used as a seal, watermark or divider — the brand's recurring anchor. Treat it as an emblem, never a clickable logo. Use the colour version that suits the surface: `crest-black.png` on white, `crest-white.png` on rich-black, `crest-silver.png` as a premium hero feature.

```jsx
<CrestSeal src="/assets/crest-black.png" size={64} />
<CrestSeal src="/assets/crest-white.png" treatment="watermark" size={520} />
<CrestSeal src="/assets/crest-black.png" treatment="divider" size={44} />
```

Treatments: `seal` (full) · `watermark` (6% — behind heroes/footers) · `ghost` (14%) · `divider` (crest centred between hairlines). Keep watermarks subtle so they read as a seal, not decoration.
```
