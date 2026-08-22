import { Text } from 'react-native';
import { type, useTheme } from '../theme';

type Props = {
  parts: string[];
  muted?: boolean;
};

export function MetaLine({ parts, muted = true }: Props) {
  const { c } = useTheme();
  return (
    <Text maxFontSizeMultiplier={1.6} style={[type.meta, { color: muted ? c.textMuted : c.textSecondary }]}>
      {parts.join(' · ').toUpperCase()}
    </Text>
  );
}
