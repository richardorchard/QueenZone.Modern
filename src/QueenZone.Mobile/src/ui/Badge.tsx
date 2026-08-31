import { Text } from 'react-native';
import { type, useTheme } from '../theme';

export type BadgeRole = 'restored' | 'anniversary' | 'featured' | 'archive' | 'community';

export function Badge({ label, role }: { label: string; role: BadgeRole }) {
  const { c } = useTheme();
  const color = {
    restored: c.accentSpecial,
    anniversary: c.accentSpecial,
    featured: c.accentEditorial,
    archive: c.accentArchive,
    community: c.textSecondary,
  }[role];

  return (
    <Text style={[type.eyebrow, { fontSize: 9, letterSpacing: 1.8, color }]}>{label.toUpperCase()}</Text>
  );
}
