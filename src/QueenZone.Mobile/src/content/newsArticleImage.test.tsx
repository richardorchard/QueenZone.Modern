import { newsArticleImageSource, newsArticleListImageSource, newsArticlePlaceholder } from './newsArticleImage';

describe('newsArticleImageSource', () => {
  it('returns the bundled placeholder when the article has no image', () => {
    expect(newsArticlePlaceholder).toEqual(expect.not.objectContaining({ uri: expect.stringMatching(/^https?:/) }));
    expect(newsArticleImageSource(null)).toBe(newsArticlePlaceholder);
    expect(newsArticleImageSource(undefined)).toBe(newsArticlePlaceholder);
    expect(newsArticleImageSource('')).toBe(newsArticlePlaceholder);
    expect(newsArticleImageSource('   ')).toBe(newsArticlePlaceholder);
  });

  it('returns a remote uri when the article has an image', () => {
    expect(newsArticleImageSource('/ugc/articles/editors/me/hero.webp')).toEqual({
      uri: '/ugc/articles/editors/me/hero.webp',
    });
  });

  it('prefers the thumbnail for list rows, then falls back to the placeholder', () => {
    expect(
      newsArticleListImageSource({
        imageUrl: '/ugc/articles/full.webp',
        thumbnailUrl: '/ugc/articles/full.webp?size=thumb',
      }),
    ).toEqual({ uri: '/ugc/articles/full.webp?size=thumb' });
    expect(newsArticleListImageSource({ imageUrl: null, thumbnailUrl: null })).toBe(
      newsArticlePlaceholder,
    );
  });
});
