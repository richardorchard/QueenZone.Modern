import {
  createContext,
  useContext,
  useMemo,
  type ReactNode,
} from 'react';
import { useColorScheme } from 'react-native';
import {
  chrome,
  dark,
  fonts,
  imagery,
  light,
  motion,
  palette,
  radius,
  shadow,
  space,
  type,
  type ColorScheme,
  type ThemeMode,
} from './tokens';

export type ThemePreference = 'system' | ThemeMode;

type ThemeContextValue = {
  /** Resolved colours for the active mode. */
  c: ColorScheme;
  mode: ThemeMode;
  preference: ThemePreference;
  palette: typeof palette;
  type: typeof type;
  fonts: typeof fonts;
  space: typeof space;
  radius: typeof radius;
  shadow: typeof shadow;
  motion: typeof motion;
  chrome: typeof chrome;
  imagery: typeof imagery;
};

const ThemeContext = createContext<ThemeContextValue | null>(null);

type Props = {
  children: ReactNode;
  /** Default dark-first; pass `system` to follow OS appearance. */
  preference?: ThemePreference;
};

/**
 * Provides design tokens. Dark is the product default; light exists for
 * system preference parity with the web archive.
 */
export function ThemeProvider({ children, preference: preferenceProp = 'dark' }: Props) {
  const systemScheme = useColorScheme();
  const preference = preferenceProp;

  const mode: ThemeMode =
    preference === 'system' ? (systemScheme === 'light' ? 'light' : 'dark') : preference;

  const value = useMemo<ThemeContextValue>(
    () => ({
      c: mode === 'light' ? light : dark,
      mode,
      preference,
      palette,
      type,
      fonts,
      space,
      radius,
      shadow,
      motion,
      chrome,
      imagery,
    }),
    [mode, preference],
  );

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme(): ThemeContextValue {
  const ctx = useContext(ThemeContext);
  if (!ctx) {
    throw new Error('useTheme must be used within ThemeProvider');
  }
  return ctx;
}
