import { ActivityIndicator, Platform, Pressable, Text } from 'react-native';
import Svg, { Path } from 'react-native-svg';
import { radius } from '../theme';
import { usePressProps } from './press';

type Props = {
  label: string;
  onPress: () => void;
  disabled?: boolean;
  loading?: boolean;
};

export function AppleSignInButton({ label, onPress, disabled, loading }: Props) {
  const press = usePressProps();

  return (
    <Pressable
      testID="apple-sign-in-button"
      accessibilityRole="button"
      accessibilityLabel={label}
      accessibilityState={{ disabled: !!disabled, busy: !!loading }}
      disabled={disabled || loading}
      onPress={onPress}
      {...press}
      style={({ pressed }) => [
        {
          height: 48,
          minWidth: 140,
          borderRadius: radius.xs,
          backgroundColor: '#000000',
          alignItems: 'center',
          justifyContent: 'center',
          opacity: disabled ? 0.4 : 1,
        },
        Platform.OS === 'ios' && pressed ? { opacity: 0.85, transform: [{ translateY: 1 }] } : null,
      ]}
    >
      {loading ? (
        <ActivityIndicator size={16} color="#FFFFFF" />
      ) : (
        <>
          <Svg
            width={20}
            height={20}
            viewBox="0 0 24 24"
            accessibilityElementsHidden
            importantForAccessibility="no-hide-descendants"
            style={{ position: 'absolute', left: 16 }}
          >
            <Path
              fill="#FFFFFF"
              d="M17.05 20.28c-.98.95-2.05.8-3.08.35-1.09-.46-2.09-.48-3.24 0-1.44.62-2.2.44-3.06-.35C2.79 15.25 3.51 7.59 9.05 7.31c1.35.07 2.29.74 3.08.79 1.18-.24 2.31-.93 3.57-.84 1.51.12 2.65.72 3.4 1.8-3.12 1.87-2.38 5.98.48 7.13-.57 1.5-1.31 2.99-2.53 4.1M12.03 7.25C11.88 5.02 13.69 3.18 15.77 3c.29 2.58-2.34 4.5-3.74 4.25"
            />
          </Svg>
          <Text
            maxFontSizeMultiplier={1.3}
            style={{
              color: '#FFFFFF',
              fontFamily: Platform.select({ ios: 'System', android: 'sans-serif-medium' }),
              fontSize: 19,
            }}
          >
            {label}
          </Text>
        </>
      )}
    </Pressable>
  );
}
