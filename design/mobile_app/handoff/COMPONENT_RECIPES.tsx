/**
 * Queenzone — component recipes
 *
 * Copy-paste starting points for the primitives described in
 * QUEENZONE_APP_SPEC.md §3. These are deliberately plain: no styling library,
 * no animation library beyond Reanimated-free RN APIs. Adapt to your codebase
 * conventions, but keep the VALUES — they are the design system.
 *
 * Assumes:
 *   theme.ts  in the same folder
 *   lucide-react-native, expo-image, expo-blur, expo-linear-gradient
 *   react-native-safe-area-context
 */

import React from 'react';
import {
  View, Text, Pressable, Platform, FlatList, StyleSheet,
  type ViewStyle, type TextStyle,
} from 'react-native';
import { Image } from 'expo-image';
import { LinearGradient } from 'expo-linear-gradient';
import { Bookmark, Share2, Search, ChevronRight, type LucideIcon } from 'lucide-react-native';
import theme from './theme';

/* ------------------------------------------------------------------ *
 * Theme access
 * ------------------------------------------------------------------ */

const c = theme.dark;                          // swap for a context-driven hook
const chrome = theme.chrome[Platform.OS === 'ios' ? 'ios' : 'android'];
const { type, space, radius } = theme;

/** Platform press feedback in one place — never branch inside a screen. */
export function pressProps(borderless = false) {
  return Platform.OS === 'android'
    ? { android_ripple: { color: c.accentTintWeak, borderless } }
    : {};
}
const pressedOpacity = ({ pressed }: { pressed: boolean }) =>
  Platform.OS === 'ios' && pressed ? { opacity: 0.85 } : null;

/* ------------------------------------------------------------------ *
 * Eyebrow · MetaLine — the two labels every section uses
 * ------------------------------------------------------------------ */

export function Eyebrow({
  children, tone = 'accent', size = 10,
}: { children: string; tone?: 'accent' | 'primary' | 'muted'; size?: number }) {
  const color = tone === 'accent' ? c.accentPrimary : tone === 'primary' ? c.textPrimary : c.textSecondary;
  return (
    <Text
      maxFontSizeMultiplier={1.4}
      style={[type.eyebrow, { fontSize: size, letterSpacing: size * 0.22, color }]}
    >
      {children}
    </Text>
  );
}

export function MetaLine({ parts, muted = true }: { parts: string[]; muted?: boolean }) {
  return (
    <Text maxFontSizeMultiplier={1.6} style={[type.meta, { color: muted ? c.textMuted : c.textSecondary }]}>
      {parts.join(' · ').toUpperCase()}
    </Text>
  );
}

/* ------------------------------------------------------------------ *
 * Button · IconButton · Chip
 * ------------------------------------------------------------------ */

type ButtonProps = {
  label: string;
  onPress: () => void;
  variant?: 'primary' | 'outline' | 'ghost';
  size?: 'md' | 'sm';
  disabled?: boolean;
};

export function Button({ label, onPress, variant = 'primary', size = 'md', disabled }: ButtonProps) {
  const height = size === 'md' ? 48 : 40;
  const base: ViewStyle = {
    height,
    paddingHorizontal: size === 'md' ? space.base : space.md,
    borderRadius: radius.xs,
    alignItems: 'center',
    justifyContent: 'center',
    opacity: disabled ? 0.4 : 1,
  };
  const skin: ViewStyle =
    variant === 'primary' ? { backgroundColor: c.accentPrimary }
    : variant === 'outline' ? { borderWidth: 1, borderColor: c.borderStrong }
    : {};
  const labelColor =
    variant === 'primary' ? c.textOnAccent : variant === 'outline' ? c.textPrimary : c.accentPrimary;

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ disabled: !!disabled }}
      disabled={disabled}
      onPress={onPress}
      {...pressProps()}
      style={({ pressed }) => [
        base, skin,
        Platform.OS === 'ios' && pressed ? { opacity: 0.85, transform: [{ translateY: 1 }] } : null,
      ]}
    >
      <Text
        maxFontSizeMultiplier={1.3}
        style={{
          fontFamily: theme.fonts.bodyMedium, fontSize: 12, letterSpacing: 1.2,
          textTransform: 'uppercase', color: labelColor,
        }}
      >
        {label}
      </Text>
    </Pressable>
  );
}

export function IconButton({
  icon: Icon, onPress, accessibilityLabel, tone = 'onDark', size = 20, active = false,
}: {
  icon: LucideIcon; onPress: () => void; accessibilityLabel: string;
  tone?: 'onDark' | 'accent'; size?: number; active?: boolean;
}) {
  const color = tone === 'accent' || active ? c.accentPrimary : c.textPrimary;
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={accessibilityLabel}
      onPress={onPress}
      {...pressProps(true)}
      style={({ pressed }) => [
        { width: 44, height: 44, alignItems: 'center', justifyContent: 'center', borderRadius: 22 },
        Platform.OS === 'ios' && pressed ? { opacity: 0.6 } : null,
      ]}
    >
      <Icon size={size} color={color} strokeWidth={1.5} fill={active ? c.accentPrimary : 'transparent'} />
    </Pressable>
  );
}

export function Chip({ label, active, onPress }: { label: string; active: boolean; onPress: () => void }) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ selected: active }}
      hitSlop={{ top: 7, bottom: 7 }}
      onPress={onPress}
      {...pressProps()}
      style={[
        {
          height: 34, paddingHorizontal: 15, borderRadius: radius.pill,
          alignItems: 'center', justifyContent: 'center',
        },
        active
          ? { backgroundColor: c.accentPrimary }
          : { borderWidth: 1, borderColor: c.border },
      ]}
    >
      <Text
        maxFontSizeMultiplier={1.3}
        style={{
          fontFamily: theme.fonts.bodyMedium, fontSize: 11, letterSpacing: 1.1,
          textTransform: 'uppercase', color: active ? c.textOnAccent : c.textSecondary,
        }}
      >
        {label}
      </Text>
    </Pressable>
  );
}

/* ------------------------------------------------------------------ *
 * Badge — role → accent meaning. The ONLY sanctioned accent-on-text map.
 * ------------------------------------------------------------------ */

const BADGE_COLOR = {
  restored: c.accentSpecial,
  anniversary: c.accentSpecial,
  featured: c.accentEditorial,
  archive: c.accentArchive,
  community: c.textSecondary,
} as const;

export function Badge({ label, role }: { label: string; role: keyof typeof BADGE_COLOR }) {
  return (
    <Text style={[type.eyebrow, { fontSize: 9, letterSpacing: 1.8, color: BADGE_COLOR[role] }]}>
      {label.toUpperCase()}
    </Text>
  );
}

/* ------------------------------------------------------------------ *
 * Monochrome image — every archival photograph goes through this.
 * Pick ONE greyscale strategy app-wide (see SPEC §7.3) and put it here.
 * ------------------------------------------------------------------ */

export function ArchiveImage({
  uri, style, label, recyclingKey,
}: { uri: string; style: ViewStyle; label: string; recyclingKey?: string }) {
  return (
    <Image
      source={{ uri }}
      style={style as any}
      contentFit="cover"
      transition={theme.motion.slow}
      recyclingKey={recyclingKey ?? uri}
      accessibilityLabel={label}
      // Greyscale: colour matrix / filter-kit / pre-processed CDN derivative.
      // Do NOT ship colour photography.
    />
  );
}

/* ------------------------------------------------------------------ *
 * Section header — use for EVERY new section (STYLE_GUIDE §2)
 * ------------------------------------------------------------------ */

export function SectionHeader({
  title, actionLabel, onAction,
}: { title: string; actionLabel?: string; onAction?: () => void }) {
  return (
    <View
      style={{
        marginTop: space.xxl, marginHorizontal: space.xl, paddingBottom: space.md,
        borderBottomWidth: 1, borderBottomColor: c.hairline,
        flexDirection: 'row', alignItems: 'flex-end', justifyContent: 'space-between',
      }}
    >
      <Eyebrow tone="primary" size={11}>{title}</Eyebrow>
      {actionLabel ? (
        <Pressable onPress={onAction} accessibilityRole="button" hitSlop={10}>
          <Text style={{
            fontFamily: theme.fonts.bodyMedium, fontSize: 12, letterSpacing: 0.7,
            textTransform: 'uppercase', color: c.accentPrimary,
          }}>
            {actionLabel}
          </Text>
        </Pressable>
      ) : null}
    </View>
  );
}

/* ------------------------------------------------------------------ *
 * HeroFeature — the "one strong image" pattern
 * ------------------------------------------------------------------ */

export function HeroFeature({
  item, onPress, height = 468,
}: {
  item: { kicker: string; title: string; standfirst: string; meta: string[]; imageUri: string };
  onPress: () => void; height?: number;
}) {
  return (
    <Pressable
      accessible
      accessibilityRole="button"
      accessibilityLabel={`${item.kicker}. ${item.title}. ${item.standfirst}`}
      onPress={onPress}
      style={{ height }}
      {...pressProps()}
    >
      <ArchiveImage uri={item.imageUri} label={item.title} style={StyleSheet.absoluteFillObject as ViewStyle} />
      <LinearGradient
        colors={theme.imagery.scrimBottom as unknown as string[]}
        locations={theme.imagery.scrimStops as unknown as number[]}
        style={StyleSheet.absoluteFillObject}
      />
      <View style={{ position: 'absolute', left: space.xl, right: space.xl, bottom: 28, gap: space.md }}>
        <Eyebrow>{item.kicker}</Eyebrow>
        <Text numberOfLines={3} maxFontSizeMultiplier={1.4} style={[type.heroTitle, { color: c.textPrimary }]}>
          {item.title}
        </Text>
        <Text numberOfLines={3} style={{ fontFamily: theme.fonts.body, fontSize: 15, lineHeight: 23, color: c.textSecondary }}>
          {item.standfirst}
        </Text>
        <MetaLine parts={item.meta} />
      </View>
    </Pressable>
  );
}

/* ------------------------------------------------------------------ *
 * ArticleRow + FeatureCard — the List and Rail shapes
 * ------------------------------------------------------------------ */

export type Article = {
  id: string; title: string; kicker: string;
  kickerRole: keyof typeof BADGE_COLOR; meta: string[]; thumbUri: string;
};

export function ArticleRow({ item, onPress }: { item: Article; onPress: () => void }) {
  return (
    <Pressable
      accessible
      accessibilityRole="button"
      accessibilityLabel={`${item.kicker}. ${item.title}. ${item.meta.join(', ')}`}
      onPress={onPress}
      {...pressProps()}
      style={({ pressed }) => [
        {
          flexDirection: 'row', gap: 15, paddingVertical: space.base, paddingHorizontal: space.xl,
          borderTopWidth: 1, borderTopColor: c.hairline,
        },
        Platform.OS === 'ios' && pressed ? { backgroundColor: 'rgba(255,255,255,0.04)' } : null,
      ]}
    >
      <ArchiveImage uri={item.thumbUri} label={item.title} style={{ width: 92, height: 92, borderRadius: radius.xs }} />
      <View style={{ flex: 1, gap: 7 }}>
        <Badge label={item.kicker} role={item.kickerRole} />
        <Text numberOfLines={2} maxFontSizeMultiplier={1.4} style={[type.cardTitle, { fontSize: 20, lineHeight: 23.5, color: c.textPrimary }]}>
          {item.title}
        </Text>
        <MetaLine parts={item.meta} />
      </View>
    </Pressable>
  );
}

export function FeatureRail({ items, onOpen }: { items: Article[]; onOpen: (a: Article) => void }) {
  return (
    <FlatList
      horizontal
      data={items}
      keyExtractor={(i) => i.id}
      showsHorizontalScrollIndicator={false}
      snapToInterval={230}
      decelerationRate="fast"
      contentContainerStyle={{ paddingHorizontal: space.xl, gap: 14, paddingVertical: space.base }}
      renderItem={({ item }) => (
        <Pressable
          accessible
          accessibilityRole="button"
          accessibilityLabel={`${item.kicker}. ${item.title}`}
          onPress={() => onOpen(item)}
          style={{ width: 216, gap: 11 }}
          {...pressProps()}
        >
          <ArchiveImage uri={item.thumbUri} label={item.title} style={{ width: 216, height: 150, borderRadius: radius.xs }} />
          <Badge label={item.kicker} role={item.kickerRole} />
          <Text numberOfLines={3} maxFontSizeMultiplier={1.4} style={[type.cardTitle, { color: c.textPrimary }]}>
            {item.title}
          </Text>
          <MetaLine parts={item.meta} />
        </Pressable>
      )}
    />
  );
}

/* ------------------------------------------------------------------ *
 * FeatureBlock — the gold-bordered editorial panel. Max ONE per screen.
 * ------------------------------------------------------------------ */

export function FeatureBlock({
  eyebrow, numeral, body, actionLabel, onAction, crestSource,
}: {
  eyebrow: string; numeral?: string; body: string;
  actionLabel: string; onAction: () => void; crestSource: any;
}) {
  return (
    <View
      style={{
        marginTop: space.xxl, marginHorizontal: space.xl, padding: 22,
        backgroundColor: '#181614', borderWidth: 1, borderColor: 'rgba(184,154,74,0.34)',
        borderRadius: radius.sm, overflow: 'hidden', gap: space.md,
      }}
    >
      <Image
        source={crestSource}
        style={{ position: 'absolute', right: -30, bottom: -34, height: 150, width: 150, opacity: c.crestWatermarkOpacity }}
        contentFit="contain"
        importantForAccessibility="no"
        accessibilityElementsHidden
      />
      <Eyebrow>{eyebrow}</Eyebrow>
      {numeral ? <Text style={[type.numeral, { color: c.textPrimary }]}>{numeral}</Text> : null}
      <Text style={{ fontFamily: theme.fonts.body, fontSize: 15, lineHeight: 24, color: c.textSecondary }}>
        {body}
      </Text>
      <View style={{ alignSelf: 'flex-start', marginTop: space.xs }}>
        <Button variant="outline" size="sm" label={actionLabel} onPress={onAction} />
      </View>
    </View>
  );
}

/* ------------------------------------------------------------------ *
 * Example screen — how the pieces assemble (STYLE_GUIDE §9)
 * ------------------------------------------------------------------ */

export function TodayScreenExample({
  lead, vault, onOpenStory, onOpenNews, crest,
}: {
  lead: Parameters<typeof HeroFeature>[0]['item'];
  vault: Article[];
  onOpenStory: (a?: Article) => void;
  onOpenNews: () => void;
  crest: any;
}) {
  return (
    <FlatList
      style={{ backgroundColor: c.surfacePage }}
      data={[]}
      renderItem={null}
      ListHeaderComponent={
        <>
          <HeroFeature item={lead} onPress={() => onOpenStory()} />
          <SectionHeader title="From the vaults" actionLabel="All" onAction={onOpenNews} />
          <FeatureRail items={vault} onOpen={onOpenStory} />
          <FeatureBlock
            eyebrow="This day in Queen history"
            numeral="MCMLXXV"
            body="20 August 1975 — the band begin sessions at Rockfield Studios for the album that would become A Night at the Opera."
            actionLabel="Read the entry"
            onAction={() => onOpenStory()}
            crestSource={crest}
          />
        </>
      }
    />
  );
}
