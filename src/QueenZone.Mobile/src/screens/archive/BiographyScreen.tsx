import { useCallback } from 'react';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { fetchBiographyPage, type BiographyChapterListItem } from '../../api';
import { usePagedContent } from '../../hooks/usePagedContent';
import type { ArchiveStackParamList } from '../../navigation/types';
import { ArticleRow } from '../../ui/ArticleRow';
import { PagedListScreen } from '../../ui/PagedListScreen';

type Props = NativeStackScreenProps<ArchiveStackParamList, 'Biography'>;

export function BiographyScreen({ navigation }: Props) {
  const paged = usePagedContent<BiographyChapterListItem>(
    useCallback((page, signal) => fetchBiographyPage({ page, pageSize: 50, signal }), []),
    50,
  );

  return (
    <PagedListScreen
      paged={paged}
      keyExtractor={(item) => String(item.id)}
      loadingLabel="Loading biography…"
      emptyMessage="No biography chapters yet."
      renderItem={({ item }) => (
        <ArticleRow
          title={item.title}
          subtitle={item.summary}
          meta={`Chapter ${item.displaySequence}`}
          onPress={() => navigation.navigate('BiographyChapter', { id: item.id })}
          accessibilityLabel={`Open chapter ${item.title}`}
        />
      )}
    />
  );
}
