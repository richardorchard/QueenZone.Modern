import { memo } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { type } from '../../theme';

/**
 * Thread list sits one step darker than the header/composer chrome — a new
 * value from the redesign handoff (`design/design_handoff_private_messages`),
 * not yet a shared theme token.
 */
const dividerRule = 'rgba(255,255,255,0.14)';

export const DateDivider = memo(function DateDivider({ label }: { label: string }) {
  return (
    <View style={styles.dividerRow}>
      <View style={styles.dividerRule} />
      <Text style={[type.eyebrow, { color: 'rgba(255,255,255,0.5)' }]}>{label}</Text>
      <View style={styles.dividerRule} />
    </View>
  );
});

const styles = StyleSheet.create({
  dividerRow: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  dividerRule: { flex: 1, height: 1, backgroundColor: dividerRule },
});
