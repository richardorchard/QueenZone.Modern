import { Image, type ImageStyle } from 'expo-image';
import type { StyleProp } from 'react-native';
import { motion } from '../theme';

type Props = {
  source: number | { uri: string };
  label: string;
  style: StyleProp<ImageStyle>;
  recyclingKey?: string;
  contentFit?: 'cover' | 'contain';
  priority?: 'low' | 'normal' | 'high';
};

/**
 * Every archival photograph goes through this component.
 * Remote gallery URIs must already be `cdn.queenzone.org` (never App Service).
 * expo-image lazy-decodes off-screen cells; grids should pass `priority="low"`.
 */
export function ArchiveImage({
  source,
  label,
  style,
  recyclingKey,
  contentFit = 'cover',
  priority = 'normal',
}: Props) {
  const key = recyclingKey ?? (typeof source === 'number' ? String(source) : source.uri);
  return (
    <Image
      source={source}
      style={style}
      contentFit={contentFit}
      transition={motion.slow}
      recyclingKey={key}
      cachePolicy="memory-disk"
      priority={priority}
      accessibilityLabel={label}
    />
  );
}
