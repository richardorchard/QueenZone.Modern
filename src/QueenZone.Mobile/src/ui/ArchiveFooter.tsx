import { StyleSheet, Text, View } from 'react-native';
import { archiveDisclaimer, space, type, useTheme } from '../theme';
import { CrestSeal } from './CrestSeal';

export function ArchiveFooter() {
  const { c } = useTheme();
  return (
    <View style={styles.footer}>
      <CrestSeal height={38} opacity={0.34} />
      <Text style={[type.caption, { color: c.textMuted, textAlign: 'center', maxWidth: 250 }]}>
        {archiveDisclaimer}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  footer: {
    paddingTop: 40,
    paddingBottom: space.section,
    paddingHorizontal: space.xl,
    alignItems: 'center',
    gap: space.md,
  },
});
