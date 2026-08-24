import { Platform, Pressable } from 'react-native';
import type { LucideIcon } from 'lucide-react-native';
import { useTheme } from '../theme';
import { usePressProps } from './press';

type Props = {
  icon: LucideIcon;
  onPress: () => void;
  accessibilityLabel: string;
  testID?: string;
  tone?: 'onDark' | 'accent';
  size?: 20 | 24;
  active?: boolean;
};

export function IconButton({
  icon: Icon,
  onPress,
  accessibilityLabel,
  testID,
  tone = 'onDark',
  size = 20,
  active = false,
}: Props) {
  const { c } = useTheme();
  const press = usePressProps(true);
  const color = tone === 'accent' || active ? c.accentPrimary : c.textPrimary;

  return (
    <Pressable
      testID={testID}
      accessibilityRole="button"
      accessibilityLabel={accessibilityLabel}
      onPress={onPress}
      {...press}
      style={({ pressed }) => [
        { width: 44, height: 44, alignItems: 'center', justifyContent: 'center', borderRadius: 22 },
        Platform.OS === 'ios' && pressed ? { opacity: 0.6 } : null,
      ]}
    >
      <Icon size={size} color={color} strokeWidth={1.5} />
    </Pressable>
  );
}
