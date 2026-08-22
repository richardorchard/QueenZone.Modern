import { Image } from 'expo-image';
import { media } from '../content/media';
import { useTheme } from '../theme';

type Variant = 'white' | 'black' | 'silver';

type Props = {
  variant?: Variant;
  height: number;
  opacity?: number;
};

const sources = {
  white: media.crestWhite,
  black: media.crestBlack,
  silver: media.crestSilver,
} as const;

export function CrestSeal({ variant, height, opacity = 0.3 }: Props) {
  const { mode } = useTheme();
  const resolved = variant ?? (mode === 'light' ? 'black' : 'white');

  return (
    <Image
      source={sources[resolved]}
      style={{ height, width: height, opacity }}
      contentFit="contain"
      importantForAccessibility="no"
      accessibilityElementsHidden
    />
  );
}
