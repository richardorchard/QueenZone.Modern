import { Pressable, StyleSheet, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { testIds } from '../test/testIds';
import { fonts, radius, shadow, space, type, useTheme } from '../theme';
import { pressedStyle, usePressProps } from '../ui/press';
import { noticeEyebrow, type NotificationDestination } from './payload';

export type ForegroundBannerProps = {
  title: string;
  body: string;
  destination: NotificationDestination;
  onPress: () => void;
  onDismiss: () => void;
};

export function ForegroundBanner({ title, body, destination, onPress, onDismiss }: ForegroundBannerProps) {
  const { c } = useTheme();
  const insets = useSafeAreaInsets();
  const press = usePressProps();

  return (
    <View
      pointerEvents="box-none"
      style={[styles.wrap, { paddingTop: Math.max(insets.top, space.sm) + space.xs }]}
    >
      <Pressable
        {...press}
        accessibilityRole="button"
        accessibilityLabel={`${noticeEyebrow(destination.category)}. ${title}. ${body}`}
        testID={testIds.notificationBanner}
        onPress={onPress}
        style={(state) =>
          pressedStyle(state, [
            styles.card,
            shadow.card,
            { backgroundColor: c.surfaceCard, borderColor: c.hairline },
          ])
        }
      >
        <Text style={[type.eyebrow, { color: c.accentPrimary }]}>{noticeEyebrow(destination.category)}</Text>
        <Text style={[styles.title, { color: c.textPrimary }]} numberOfLines={1}>
          {title}
        </Text>
        <Text style={[type.caption, { color: c.textSecondary }]} numberOfLines={2}>
          {body}
        </Text>
      </Pressable>
      <Pressable
        accessibilityRole="button"
        accessibilityLabel="Dismiss notification"
        onPress={onDismiss}
        hitSlop={12}
        style={styles.dismissHit}
      >
        <Text style={[type.meta, { color: c.textMuted }]}>Dismiss</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    position: 'absolute',
    left: space.base,
    right: space.base,
    top: 0,
    zIndex: 20,
    gap: space.xs,
  },
  card: {
    borderRadius: radius.md,
    borderWidth: 1,
    paddingHorizontal: space.base,
    paddingVertical: space.md,
    gap: space.xs,
  },
  title: {
    fontFamily: fonts.bodySemi,
    fontSize: 15,
    lineHeight: 20,
  },
  dismissHit: {
    alignSelf: 'flex-end',
    paddingHorizontal: space.xs,
    paddingVertical: space.xs,
  },
});
