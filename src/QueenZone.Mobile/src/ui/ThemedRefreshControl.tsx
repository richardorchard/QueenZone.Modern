import { RefreshControl, type RefreshControlProps } from 'react-native';
import { palette, useTheme } from '../theme';

type ThemedRefreshControlProps = Omit<
  RefreshControlProps,
  'tintColor' | 'colors' | 'progressBackgroundColor'
>;

/**
 * Shared pull-to-refresh chrome (#1383 Option A).
 * Owns iOS tint and Android spinner/progress-disc colours so screens cannot drift.
 * Android `progressBackgroundColor` is a lifted grey that contrasts on `surfacePage`
 * in both schemes (dark #111 / light #FFF).
 */
export function ThemedRefreshControl(props: ThemedRefreshControlProps) {
  const { c, mode } = useTheme();
  return (
    <RefreshControl
      {...props}
      tintColor={c.accentPrimary}
      colors={[c.accentPrimary]}
      progressBackgroundColor={mode === 'dark' ? palette.grey800 : palette.grey100}
    />
  );
}
