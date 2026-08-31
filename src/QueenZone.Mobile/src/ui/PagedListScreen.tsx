import type { ComponentType, ReactNode } from 'react';
import {
  FlatList,
  RefreshControl,
  StyleSheet,
  View,
  type FlatListProps,
  type ListRenderItem,
} from 'react-native';
import type { PagedState } from '../hooks/usePagedContent';
import { useTheme } from '../theme';
import { EmptyBlock, ErrorBlock, ListFooterLoading, LoadingBlock } from './ScreenStates';

type OwnedListProp =
  | 'data'
  | 'renderItem'
  | 'keyExtractor'
  | 'onEndReached'
  | 'onEndReachedThreshold'
  | 'refreshControl'
  | 'ListFooterComponent'
  | 'ListEmptyComponent';

/** Shared infinite-scroll trigger. Owned here so list screens cannot drift. */
const END_REACHED_THRESHOLD = 0.4;

export type PagedListScreenProps<T> = {
  paged: PagedState<T>;
  renderItem: ListRenderItem<T>;
  keyExtractor: (item: T, index: number) => string;
  loadingLabel: string;
  emptyMessage: string;
  /** Extra footer content after the owned loading spinner (e.g. Search `ArchiveFooter`). */
  footerAfter?: ReactNode;
} & Omit<FlatListProps<T>, OwnedListProp>;

function renderListHeader(header: FlatListProps<unknown>['ListHeaderComponent']): ReactNode {
  if (header == null) {
    return null;
  }
  if (typeof header === 'function') {
    const Header = header as ComponentType;
    return <Header />;
  }
  return header;
}

export function PagedListScreen<T>({
  paged,
  renderItem,
  keyExtractor,
  loadingLabel,
  emptyMessage,
  footerAfter,
  style,
  ListHeaderComponent,
  ...listProps
}: PagedListScreenProps<T>) {
  const { c } = useTheme();
  const listStyle = [styles.list, { backgroundColor: c.surfacePage }, style];

  if (paged.loading && paged.items.length === 0) {
    return (
      <View style={listStyle}>
        {renderListHeader(ListHeaderComponent)}
        <LoadingBlock label={loadingLabel} />
      </View>
    );
  }

  if (paged.error && paged.items.length === 0) {
    return (
      <View style={listStyle}>
        {renderListHeader(ListHeaderComponent)}
        <ErrorBlock message={paged.error} onRetry={paged.reload} />
      </View>
    );
  }

  return (
    <FlatList
      {...listProps}
      style={listStyle}
      data={paged.items}
      keyExtractor={keyExtractor}
      renderItem={renderItem}
      ListHeaderComponent={ListHeaderComponent}
      ListEmptyComponent={<EmptyBlock message={emptyMessage} />}
      ListFooterComponent={
        <>
          <ListFooterLoading visible={paged.loadingMore} />
          {footerAfter}
        </>
      }
      refreshControl={
        <RefreshControl
          refreshing={paged.refreshing}
          onRefresh={paged.refresh}
          tintColor={c.accentPrimary}
        />
      }
      onEndReached={paged.loadMore}
      onEndReachedThreshold={END_REACHED_THRESHOLD}
    />
  );
}

const styles = StyleSheet.create({
  list: { flex: 1 },
});
