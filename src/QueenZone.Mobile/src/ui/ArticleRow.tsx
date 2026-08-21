import { Pressable, StyleSheet, Text, View } from 'react-native';
import { space, type, useTheme } from '../theme';

type Props = {
  title: string;
  subtitle?: string;
  meta?: string;
  onPress?: () => void;
  accessibilityLabel?: string;
};

/**
 * Archive list row — STYLE_GUIDE §3 List: hairline separators, type hierarchy, no cards.
 */
export function ArticleRow({ title, subtitle, meta, onPress, accessibilityLabel }: Props) {
  const { c } = useTheme();
  const content = (
    <View style={[styles.row, { borderTopColor: c.hairline }]}>
      {meta ? (
        <Text style={[type.meta, { color: c.textMuted, marginBottom: space.xs }]} numberOfLines={1}>
          {meta}
        </Text>
      ) : null}
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
    </View>
  );

  if (!onPress) {
    return content;
  }

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={accessibilityLabel ?? title}
      onPress={onPress}
      style={({ pressed }) => (pressed ? styles.pressed : null)}
    >
      {content}
    </Pressable>
  );
}

const styles = StyleSheet.create({
  row: {
    paddingVertical: space.base,
    paddingHorizontal: space.xl,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  pressed: {
    opacity: 0.72,
  },
});
