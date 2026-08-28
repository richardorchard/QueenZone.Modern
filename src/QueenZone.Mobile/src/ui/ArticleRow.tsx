import type { ReactNode } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { space, type, useTheme } from '../theme';

type Props = {
  title: string;
  subtitle?: string;
  meta?: string;
  hint?: string;
  leading?: ReactNode;
  /**
   * When true (default), `leading` stays outside the row press target so a
   * play control can stream without opening detail. Decorative leading
   * (news thumbnails) should pass false so the whole row is tappable.
   */
  leadingInteractive?: boolean;
  onPress?: () => void;
  accessibilityLabel?: string;
  testID?: string;
};

/**
 * Archive list row — STYLE_GUIDE §3 List: hairline separators, type hierarchy, no cards.
 * `leading` sits beside the title and subtitle. Interactive leading (the default)
 * stays outside the row press target so a play control can stream without
 * opening detail. Pass `leadingInteractive={false}` for decorative leading.
 */
export function ArticleRow({
  title,
  subtitle,
  meta,
  hint,
  leading,
  leadingInteractive = true,
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

  if (leading && leadingInteractive) {
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
      {leading ? (
        <View style={styles.track}>
          {leading}
          <View style={styles.copy}>{copy}</View>
        </View>
      ) : (
        copy
      )}
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
