import { Pressable, Text } from 'react-native';
import { fonts, radius, useTheme } from '../theme';
import { usePressProps } from './press';

type Props = {
  label: string;
  active: boolean;
  onPress: () => void;
};

export function Chip({ label, active, onPress }: Props) {
  const { c } = useTheme();
  const press = usePressProps();

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ selected: active }}
      hitSlop={{ top: 7, bottom: 7 }}
      onPress={onPress}
      {...press}
      style={{
        height: 34,
        paddingHorizontal: 15,
        borderRadius: radius.pill,
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: active ? c.accentPrimary : 'transparent',
        borderWidth: active ? 0 : 1,
        borderColor: c.border,
      }}
    >
      <Text
        maxFontSizeMultiplier={1.3}
        style={{
          fontFamily: fonts.bodyMedium,
          fontSize: 11,
          letterSpacing: 1.1,
          textTransform: 'uppercase',
          color: active ? c.textOnAccent : c.textSecondary,
        }}
      >
        {label}
      </Text>
    </Pressable>
  );
}
