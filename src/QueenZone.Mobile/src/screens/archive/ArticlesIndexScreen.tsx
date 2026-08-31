import { useCallback } from 'react';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { fetchArticlesPage, formatPublishedDate, type ArticleListItem } from '../../api';
import { usePagedContent } from '../../hooks/usePagedContent';
import type { ArchiveStackParamList } from '../../navigation/types';
import { ArticleRow } from '../../ui/ArticleRow';
import { PageTitleBlock } from '../../ui/PageTitleBlock';
import { PagedListScreen } from '../../ui/PagedListScreen';

type Props = NativeStackScreenProps<ArchiveStackParamList, 'Articles'>;

export function ArticlesIndexScreen({ navigation }: Props) {
  const paged = usePagedContent<ArticleListItem>(
    useCallback((page, signal) => fetchArticlesPage({ page, pageSize: 20, signal }), []),
  );

  const subtitle =
    paged.totalCount > 0 ? `${paged.totalCount.toLocaleString('en-GB')} articles` : undefined;

  return (
    <PagedListScreen
      paged={paged}
      keyExtractor={(item) => String(item.id)}
      loadingLabel="Loading articles…"
      emptyMessage="No articles yet."
      ListHeaderComponent={<PageTitleBlock eyebrow="Long-form" title="Articles" subtitle={subtitle} />}
      renderItem={({ item }) => {
        const published = formatPublishedDate(item.publishedAt);
        const kicker = item.categoryName?.trim();
        const meta = [kicker, published].filter(Boolean).join(' · ') || undefined;
        return (
          <ArticleRow
            title={item.title}
            subtitle={item.excerpt}
            meta={meta}
            onPress={() => navigation.navigate('Story', { id: item.id })}
            accessibilityLabel={`Open article ${item.title}`}
          />
        );
      }}
    />
  );
}
