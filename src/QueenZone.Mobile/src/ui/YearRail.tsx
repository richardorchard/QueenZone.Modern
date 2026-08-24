import { useMemo, useRef, useState } from 'react';
import {
  PanResponder,
  Pressable,
  StyleSheet,
  Text,
  View,
  type GestureResponderEvent,
  type LayoutChangeEvent,
} from 'react-native';
import { fonts, radius, space, useTheme } from '../theme';
import { usePressProps } from './press';

export type YearRailOption = {
  label: string;
};

type Props<T extends YearRailOption> = {
  options: readonly T[];
  value: T;
  onChange: (option: T) => void;
  testID?: string;
};

/**
 * Vertical "Google Photos"-style scrubber for jumping through the News archive (issue #886).
 * News rows are variable height (title + excerpt), so unlike Photos' uniform thumbnails a
 * pixel-accurate drag-to-scroll-offset mapping would drift. Each tick instead maps to a discrete
 * decade and drives the same server-side filter the decade chips use (issue #838) — dragging
 * only shows a floating preview label; the fetch happens once on release.
 *
 * Ticks are individually tappable `button`s so TalkBack/VoiceOver users (and anyone who can't
 * perform the drag gesture) get the same jump without dragging; the decade chips above the list
 * remain as a second, already-accessible way to change decade.
 */
export function YearRail<T extends YearRailOption>({ options, value, onChange, testID }: Props<T>) {
  const { c } = useTheme();
  const press = usePressProps();
  const [dragIndex, setDragIndex] = useState<number | null>(null);
  const railHeightRef = useRef(0);
  const optionsRef = useRef(options);
  optionsRef.current = options;
  const onChangeRef = useRef(onChange);
  onChangeRef.current = onChange;

  const indexForOffset = (offsetY: number) => {
    const count = optionsRef.current.length;
    const height = railHeightRef.current;
    if (count <= 1 || height <= 0) {
      return 0;
    }
    const ratio = Math.min(1, Math.max(0, offsetY / height));
    return Math.round(ratio * (count - 1));
  };

  const panResponder = useMemo(
    () =>
      PanResponder.create({
        onStartShouldSetPanResponder: () => true,
        onMoveShouldSetPanResponder: () => true,
        onPanResponderGrant: (evt: GestureResponderEvent) => {
          setDragIndex(indexForOffset(evt.nativeEvent.locationY));
        },
        onPanResponderMove: (evt: GestureResponderEvent) => {
          setDragIndex(indexForOffset(evt.nativeEvent.locationY));
        },
        onPanResponderRelease: (evt: GestureResponderEvent) => {
          const index = indexForOffset(evt.nativeEvent.locationY);
          setDragIndex(null);
          const option = optionsRef.current[index];
          if (option) {
            onChangeRef.current(option);
          }
        },
        onPanResponderTerminate: () => setDragIndex(null),
      }),
    [],
  );

  const handleLayout = (evt: LayoutChangeEvent) => {
    railHeightRef.current = evt.nativeEvent.layout.height;
  };

  const activeIndex = dragIndex ?? options.findIndex((option) => option.label === value.label);
  const previewOption = dragIndex !== null ? options[dragIndex] : null;
  const labelTop =
    dragIndex !== null && options.length > 1
      ? (dragIndex / (options.length - 1)) * railHeightRef.current
      : 0;

  return (
    <View style={styles.wrapper} testID={testID} pointerEvents="box-none">
      {previewOption ? (
        <View
          pointerEvents="none"
          style={[
            styles.previewBubble,
            {
              backgroundColor: c.accentPrimary,
              top: Math.max(0, labelTop - 16),
            },
          ]}
        >
          <Text maxFontSizeMultiplier={1.3} style={[styles.previewText, { color: c.textOnAccent }]}>
            {previewOption.label}
          </Text>
        </View>
      ) : null}
      <View style={styles.rail} onLayout={handleLayout} {...panResponder.panHandlers}>
        {options.map((option, index) => {
          const active = index === activeIndex;
          return (
            <Pressable
              key={option.label}
              accessibilityRole="button"
              accessibilityLabel={`Jump to ${option.label}`}
              accessibilityState={{ selected: active }}
              hitSlop={{ top: 10, bottom: 10, left: 14, right: 14 }}
              onPress={() => onChange(option)}
              {...press}
              style={styles.tick}
            >
              <View
                style={[
                  styles.dot,
                  {
                    backgroundColor: active ? c.accentPrimary : c.border,
                    width: active ? 8 : 6,
                    height: active ? 8 : 6,
                  },
                ]}
              />
            </Pressable>
          );
        })}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  wrapper: {
    position: 'absolute',
    right: 0,
    top: 0,
    bottom: 0,
    alignItems: 'flex-end',
    justifyContent: 'center',
  },
  rail: {
    paddingVertical: space.lg,
    paddingHorizontal: space.sm,
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  tick: {
    paddingVertical: 6,
    alignItems: 'center',
    justifyContent: 'center',
  },
  dot: {
    borderRadius: radius.pill,
  },
  previewBubble: {
    position: 'absolute',
    right: 34,
    minWidth: 56,
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: radius.pill,
    alignItems: 'center',
    justifyContent: 'center',
  },
  previewText: {
    fontFamily: fonts.bodyMedium,
    fontSize: 12,
    letterSpacing: 0.6,
  },
});
