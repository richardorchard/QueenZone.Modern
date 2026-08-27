import type { ReactNode } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { space, type, useTheme } from '../theme';

type Props = {
  title: string;
  subtitle?: string;
  meta?: string;
  hint?: string;
  leading?: ReactNode;
  onPress?: () => void;
  accessibilityLabel?: string;
  testID?: string;
};

/**
 * Archive list row — STYLE_GUIDE §3 List: hairline separators, type hierarchy, no cards.
 * `leading` sits beside the title and subtitle and is not inside the row press target,
 * so a play control can stream without blocking open-detail.
 */
export function ArticleRow({
  title,
  subtitle,
  meta,
  hint,
  leading,
  onPress,
  accessibilityLabel,
  testID,
}: Props) {
  const { c } = useTheme();
  const copy = (
    <>
      <Text
        style={[type.listTitle, { color: c.textPrimary }]}
        numberOfLines={2}
        allowFontScaling
        maxFontSizeMultiplier={1.4}
      >
        {title}
      </Text>
      {subtitle ? (
        <Text
          style={[type.caption, { color: c.textSecondary, marginTop: space.xs }]}
          numberOfLines={2}
          allowFontScaling
        >
          {subtitle}
        </Text>
      ) : null}
    </>
  );

  const metaLine = meta ? (
    <Text style={[type.meta, { color: c.textMuted, marginBottom: space.xs }]} numberOfLines={1}>
      {meta}
    </Text>
  ) : null;

  const hintLine = hint ? (
    <Text style={[type.meta, { color: c.textMuted, marginTop: space.xs }]}>{hint}</Text>
  ) : null;

  if (leading) {
    const titleBlock = onPress ? (
      <Pressable
        testID={testID}
        accessibilityRole="button"
        accessibilityLabel={accessibilityLabel ?? title}
        onPress={onPress}
        style={({ pressed }) => [styles.copy, pressed ? styles.pressed : null]}
      >
        {copy}
      </Pressable>
    ) : (
      <View style={styles.copy}>{copy}</View>
    );

    return (
      <View style={[styles.row, { borderTopColor: c.hairline }]}>
        {metaLine}
        <View style={styles.track}>
          {leading}
          {titleBlock}
        </View>
        {hintLine}
      </View>
    );
  }

  const body = (
    <View style={[styles.row, { borderTopColor: c.hairline }]}>
      {metaLine}
      {copy}
      {hintLine}
    </View>
  );

  if (!onPress) {
    return body;
  }

  return (
    <Pressable
      testID={testID}
      accessibilityRole="button"
      accessibilityLabel={accessibilityLabel ?? title}
      onPress={onPress}
      style={({ pressed }) => (pressed ? styles.pressed : null)}
    >
      {body}
    </Pressable>
  );
}

const styles = StyleSheet.create({
  row: {
    paddingVertical: space.base,
    paddingHorizontal: space.xl,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  track: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: space.base,
  },
  copy: {
    flex: 1,
    minWidth: 0,
  },
  pressed: {
    opacity: 0.72,
  },
});
