import { Image, type ImageStyle } from 'expo-image';
import type { StyleProp } from 'react-native';
import { motion } from '../theme';

type Props = {
  source: number | { uri: string };
  label: string;
  style: StyleProp<ImageStyle>;
  recyclingKey?: string;
  contentFit?: 'cover' | 'contain';
};

/**
 * Every archival photograph goes through this component.
 * Placeholder assets are already monochrome; remote photography should use the
 * same greyscale pipeline once the CDN derivatives exist (SPEC §7.3).
 */
export function ArchiveImage({ source, label, style, recyclingKey, contentFit = 'cover' }: Props) {
  const key = recyclingKey ?? (typeof source === 'number' ? String(source) : source.uri);
  return (
    <Image
      source={source}
      style={style}
      contentFit={contentFit}
      transition={motion.slow}
      recyclingKey={key}
      accessibilityLabel={label}
    />
  );
}
