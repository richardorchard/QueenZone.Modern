import type { ImageSourcePropType } from 'react-native';

/** Bundled default graphic — same artwork as the website static SVG. */
export const newsArticlePlaceholder = require('../../assets/news-article-placeholder.png') as number;

export type NewsArticleImageFields = {
  imageUrl?: string | null;
  thumbnailUrl?: string | null;
};

/**
 * Image source for article photos. Returns the bundled placeholder when the
 * article has no image — never a network URL for the empty state.
 */
export function newsArticleImageSource(
  imageUrl: string | null | undefined,
): ImageSourcePropType {
  const trimmed = imageUrl?.trim();
  return trimmed ? { uri: trimmed } : newsArticlePlaceholder;
}

/** Prefer the thumbnail, then the full image, then the bundled placeholder. */
export function newsArticleListImageSource(
  item: NewsArticleImageFields,
): ImageSourcePropType {
  return newsArticleImageSource(item.thumbnailUrl ?? item.imageUrl);
}
