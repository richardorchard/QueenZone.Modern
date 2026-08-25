import { useMemo, useRef, useState } from 'react';
import {
  type GestureResponderEvent,
  type LayoutChangeEvent,
  PanResponder,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { fonts, radius, space, useTheme } from '../theme';
import { testIds } from '../test/testIds';
import { buildYearTicks, offsetForYear, stepYear, yearForOffset } from './yearRailMeta';

type Props = {
  minYear: number | null;
  maxYear: number | null;
  /** The year currently applied to the list, or null when no year filter is active. */
  activeYear: number | null;
  onSelectYear: (year: number) => void;
};

const railWidth = 28;
const railInset = space.xs;

/**
 * Google Photos-style vertical scrubber for jumping the news archive to a year (issue #886).
 * Drag or tap anywhere on the rail to preview a year in a floating bubble; releasing jumps the
 * list there via the server-side year filter (see `NewsArchiveFilter`). Exposed as an
 * accessibility "adjustable" control so VoiceOver/TalkBack users can step year-by-year without
 * performing the drag gesture; the decade chips above the list remain as a non-gesture fallback.
 */
export function YearRail({ minYear, maxYear, activeYear, onSelectYear }: Props) {
  const { c } = useTheme();
  const years = useMemo(() => buildYearTicks(minYear, maxYear), [minYear, maxYear]);
  const [railHeight, setRailHeight] = useState(0);
  const [draggingYear, setDraggingYear] = useState<number | null>(null);

  // PanResponder.create runs once (captured by the useRef below), so callbacks that need the
  // latest years/height/onSelectYear must read them from refs rather than closing over render
  // values that would otherwise go stale after the first render.
  const yearsRef = useRef(years);
  yearsRef.current = years;
  const railHeightRef = useRef(railHeight);
  railHeightRef.current = railHeight;
  const onSelectYearRef = useRef(onSelectYear);
  onSelectYearRef.current = onSelectYear;

  const onLayout = (event: LayoutChangeEvent) => {
    setRailHeight(event.nativeEvent.layout.height);
  };

  const resolveYear = (event: GestureResponderEvent) =>
    yearForOffset(event.nativeEvent.locationY, railHeightRef.current, yearsRef.current);

  const panResponder = useRef(
    PanResponder.create({
      onStartShouldSetPanResponder: () => true,
      onMoveShouldSetPanResponder: () => true,
      onPanResponderGrant: (event) => setDraggingYear(resolveYear(event)),
      onPanResponderMove: (event) => setDraggingYear(resolveYear(event)),
      onPanResponderRelease: (event) => {
        const year = resolveYear(event);
        setDraggingYear(null);
        if (year !== null) {
          onSelectYearRef.current(year);
        }
      },
      onPanResponderTerminate: () => setDraggingYear(null),
    }),
  ).current;

  if (years.length < 2) {
    return null;
  }

  const displayedYear = draggingYear ?? activeYear ?? years[0];
  const bubbleTop = railHeight > 0 ? offsetForYear(displayedYear, railHeight, years) : 0;

  const onAccessibilityAction = (event: { nativeEvent: { actionName: string } }) => {
    const current = activeYear ?? years[0];
    if (event.nativeEvent.actionName === 'increment') {
      onSelectYear(stepYear(current, -1, years));
    } else if (event.nativeEvent.actionName === 'decrement') {
      onSelectYear(stepYear(current, 1, years));
    }
  };

  return (
    <View style={styles.container} pointerEvents="box-none">
      {draggingYear !== null ? (
        <View
          pointerEvents="none"
          style={[
            styles.bubble,
            { backgroundColor: c.accentPrimary, top: Math.max(0, bubbleTop - 16), right: railWidth + railInset },
          ]}
        >
          <Text maxFontSizeMultiplier={1.3} style={[styles.bubbleText, { color: c.textOnAccent }]}>
            {draggingYear}
          </Text>
        </View>
      ) : null}
      <View
        testID={testIds.newsYearRail}
        onLayout={onLayout}
        accessible
        accessibilityRole="adjustable"
        accessibilityLabel="Jump to year"
        accessibilityHint="Swipe up or down to move between years, or drag to scrub the archive."
        accessibilityValue={{ min: years[years.length - 1], max: years[0], now: displayedYear, text: String(displayedYear) }}
        accessibilityActions={[{ name: 'increment' }, { name: 'decrement' }]}
        onAccessibilityAction={onAccessibilityAction}
        style={[styles.rail, { backgroundColor: c.surfaceCard, borderColor: c.border }]}
        {...panResponder.panHandlers}
      >
        {years.map((year) => (
          <View
            key={year}
            style={[
              styles.tick,
              { backgroundColor: year === activeYear ? c.accentPrimary : c.border },
            ]}
          />
        ))}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    right: railInset,
    justifyContent: 'center',
  },
  rail: {
    width: railWidth,
    paddingVertical: space.md,
    borderRadius: radius.pill,
    borderWidth: 1,
    alignItems: 'center',
    justifyContent: 'space-between',
    maxHeight: '90%',
  },
  tick: {
    width: 4,
    height: 4,
    borderRadius: 2,
  },
  bubble: {
    position: 'absolute',
    minWidth: 56,
    paddingHorizontal: space.sm,
    paddingVertical: space.xs,
    borderRadius: radius.md,
    alignItems: 'center',
    justifyContent: 'center',
  },
  bubbleText: {
    fontFamily: fonts.bodyMedium,
    fontSize: 15,
  },
});
