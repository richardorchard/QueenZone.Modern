import { ActivityIndicator, Platform, Pressable, Text, type ViewStyle } from 'react-native';
import { fonts, radius, space, useTheme } from '../theme';
import { usePressProps } from './press';

type Props = {
  label: string;
  onPress: () => void;
  variant?: 'primary' | 'outline' | 'ghost';
  size?: 'md' | 'sm';
  disabled?: boolean;
  loading?: boolean;
  accessibilityLabel?: string;
};

export function Button({
  label,
  onPress,
  variant = 'primary',
  size = 'md',
  disabled,
  loading,
  accessibilityLabel,
}: Props) {
  const { c } = useTheme();
  const press = usePressProps();
  const height = size === 'md' ? 48 : 40;
  const base: ViewStyle = {
    height,
    paddingHorizontal: size === 'md' ? space.base : space.md,
    borderRadius: radius.xs,
    alignItems: 'center',
    justifyContent: 'center',
    opacity: disabled ? 0.4 : 1,
  };
  const skin: ViewStyle =
    variant === 'primary'
      ? { backgroundColor: c.accentPrimary }
      : variant === 'outline'
        ? { borderWidth: 1, borderColor: c.borderStrong }
        : {};
  const labelColor =
    variant === 'primary' ? c.textOnAccent : variant === 'outline' ? c.textPrimary : c.accentPrimary;

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={accessibilityLabel ?? label}
      accessibilityState={{ disabled: !!disabled, busy: !!loading }}
      disabled={disabled || loading}
      onPress={onPress}
      {...press}
      style={({ pressed }) => [
        base,
        skin,
        Platform.OS === 'ios' && pressed ? { opacity: 0.85, transform: [{ translateY: 1 }] } : null,
      ]}
    >
      {loading ? (
        <ActivityIndicator size={16} color={labelColor} />
      ) : (
        <Text
          maxFontSizeMultiplier={1.3}
          style={{
            fontFamily: fonts.bodyMedium,
            fontSize: 12,
            letterSpacing: 1.2,
            textTransform: 'uppercase',
            color: labelColor,
          }}
        >
          {label}
        </Text>
      )}
    </Pressable>
  );
}
