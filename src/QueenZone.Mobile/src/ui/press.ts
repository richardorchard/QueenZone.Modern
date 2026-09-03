import { Platform, type PressableStateCallbackType, type StyleProp, type ViewStyle } from 'react-native';
import { useTheme } from '../theme';

/** iOS pressed opacity used by `pressedStyle`. */
export const pressedOpacity = 0.85;

/** Platform press feedback in one place — never branch inside a screen. */
export function usePressProps(borderless = false) {
  const { c, chrome } = useTheme();
  const feedback = Platform.OS === 'android' ? chrome.android.pressFeedback : chrome.ios.pressFeedback;

  if (feedback === 'ripple') {
    return { android_ripple: { color: c.accentTintWeak, borderless } };
  }

  return {};
}

export function pressedStyle(
  { pressed }: PressableStateCallbackType,
  extra?: StyleProp<ViewStyle>,
): StyleProp<ViewStyle> {
  if (Platform.OS !== 'ios' || !pressed) {
    return extra ?? null;
  }
  return [{ opacity: pressedOpacity }, extra];
}
