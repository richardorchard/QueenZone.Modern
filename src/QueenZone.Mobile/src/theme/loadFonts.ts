import {
  Cinzel_400Regular,
} from '@expo-google-fonts/cinzel';
import {
  CormorantGaramond_500Medium,
  CormorantGaramond_600SemiBold,
} from '@expo-google-fonts/cormorant-garamond';
import {
  Inter_400Regular,
  Inter_500Medium,
  Inter_600SemiBold,
} from '@expo-google-fonts/inter';
import { useFonts } from 'expo-font';
import { fonts } from './tokens';

/**
 * Maps theme.fonts family names onto bundled Google Font TTFs
 * (same three families as wwwroot/design-system/tokens/fonts.css).
 */
export const fontAssetMap = {
  [fonts.display]: CormorantGaramond_500Medium,
  [fonts.displaySemi]: CormorantGaramond_600SemiBold,
  [fonts.body]: Inter_400Regular,
  [fonts.bodyMedium]: Inter_500Medium,
  [fonts.bodySemi]: Inter_600SemiBold,
  [fonts.titling]: Cinzel_400Regular,
} as const;

/** Load Queenzone display / body / titling faces before first paint. */
export function useQueenzoneFonts(): [boolean, Error | null] {
  return useFonts(fontAssetMap);
}
