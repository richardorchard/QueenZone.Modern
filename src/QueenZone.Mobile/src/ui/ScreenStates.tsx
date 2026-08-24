import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import { radius, space, type, useTheme } from '../theme';

type StateProps = {
  message: string;
  onRetry?: () => void;
};

export function LoadingBlock({ label = 'Loading…' }: { label?: string }) {
  const { c } = useTheme();
  return (
    <View style={styles.center} accessibilityRole="progressbar" accessibilityLabel={label}>
      <ActivityIndicator color={c.accentPrimary} size="large" />
      <Text style={[type.caption, { color: c.textMuted, marginTop: space.md }]}>{label}</Text>
    </View>
  );
}

export function ErrorBlock({ message, onRetry }: StateProps) {
  const { c } = useTheme();
  return (
    <View style={styles.center}>
      <Text style={[type.cardTitle, { color: c.textPrimary, textAlign: 'center' }]}>Unable to load</Text>
      <Text style={[type.body, { color: c.textSecondary, textAlign: 'center', marginTop: space.sm }]}>
        {message}
      </Text>
      {onRetry ? (
        <Pressable
          accessibilityRole="button"
          accessibilityLabel="Try again"
          onPress={onRetry}
          style={({ pressed }) => [
            styles.retry,
            { borderColor: c.border, opacity: pressed ? 0.85 : 1 },
          ]}
        >
          <Text style={[type.button, { color: c.accentPrimary }]}>Try again</Text>
        </Pressable>
      ) : null}
    </View>
  );
}

/**
 * Compact inline retry for one section of a screen with several independent sections
 * (e.g. Home). Same visual language as `ErrorBlock` but sized to sit within a section's
 * slot instead of filling the screen.
 */
export function SectionErrorBlock({ message, onRetry }: StateProps) {
  const { c } = useTheme();
  return (
    <View style={[styles.section, { paddingHorizontal: space.xl }]}>
      <Text style={[type.body, { color: c.textSecondary }]}>{message}</Text>
      {onRetry ? (
        <Pressable
          accessibilityRole="button"
          accessibilityLabel="Try again"
          onPress={onRetry}
          hitSlop={8}
        >
          <Text style={[type.button, { color: c.accentPrimary, marginTop: space.xs }]}>Retry</Text>
        </Pressable>
      ) : null}
    </View>
  );
}

export function EmptyBlock({ message }: { message: string }) {
  const { c } = useTheme();
  return (
    <View style={styles.center}>
      <Text style={[type.body, { color: c.textSecondary, textAlign: 'center' }]}>{message}</Text>
    </View>
  );
}

export function ListFooterLoading({ visible }: { visible: boolean }) {
  const { c } = useTheme();
  if (!visible) {
    return <View style={styles.footerSpacer} />;
  }
  return (
    <View style={styles.footer}>
      <ActivityIndicator color={c.accentPrimary} />
    </View>
  );
}

const styles = StyleSheet.create({
  center: {
    flexGrow: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: space.xl,
    paddingVertical: space.section,
    gap: space.sm,
  },
  retry: {
    marginTop: space.base,
    minHeight: 48,
    paddingHorizontal: space.base,
    justifyContent: 'center',
    borderWidth: 1,
    borderRadius: radius.xs,
  },
  section: {
    paddingVertical: space.lg,
  },
  footer: {
    paddingVertical: space.xl,
    alignItems: 'center',
  },
  footerSpacer: {
    height: space.xxl,
  },
});
