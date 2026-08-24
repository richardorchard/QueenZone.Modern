import { useNavigation } from '@react-navigation/native';
import * as WebBrowser from 'expo-web-browser';
import { Search } from 'lucide-react-native';
import { useCallback, useEffect, useState } from 'react';
import { FlatList, Platform, Pressable, RefreshControl, ScrollView, Text, TextInput, View } from 'react-native';
import { fetchSearchPage, formatPublishedDate } from '../../api';
import type { SearchResult } from '../../api/types';
import { getAppConfig } from '../../config/appConfig';
import { usePagedContent } from '../../hooks/usePagedContent';
import { fonts, space, type, useTheme } from '../../theme';
import { testIds } from '../../test/testIds';
import { ArchiveFooter } from '../../ui/ArchiveFooter';
import { ArticleRow } from '../../ui/ArticleRow';
import { Chip } from '../../ui/Chip';
import { Eyebrow } from '../../ui/Eyebrow';
import { EmptyBlock, ErrorBlock, ListFooterLoading, LoadingBlock } from '../../ui/ScreenStates';
import {
  searchMinQueryLength,
  searchQueryPresets,
  searchTypeFilters,
  searchTypeLabel,
  type SearchTypeFilter,
} from './searchMeta';
import { applySearchTarget, targetForSearchResult, websiteUrl, type SearchOpenTarget } from './searchNavigation';

type Props = {
  onOpen?: (target: SearchOpenTarget, item: SearchResult) => void;
};

function SearchResults({
  query,
  typeFilter,
  onOpen,
}: {
  query: string;
  typeFilter: SearchTypeFilter;
  onOpen?: Props['onOpen'];
}) {
  const { c } = useTheme();
  const paged = usePagedContent<SearchResult>(
    useCallback(
      (page, signal) => fetchSearchPage({ q: query, type: typeFilter, page, pageSize: 20, signal }),
      [query, typeFilter],
    ),
    20,
    `${query}|${typeFilter ?? ''}`,
  );

  if (paged.loading && paged.items.length === 0) {
    return <LoadingBlock label="Searching the archive…" />;
  }

  if (paged.error && paged.items.length === 0) {
    return <ErrorBlock message={paged.error} onRetry={paged.reload} />;
  }

  const countLine =
    paged.totalCount === 1
      ? `1 result for “${query}”`
      : `${paged.totalCount.toLocaleString('en-GB')} results for “${query}”`;

  return (
    <FlatList
      testID="search-results"
      style={{ flex: 1 }}
      data={paged.items}
      keyExtractor={(item) => item.sourceKey}
      keyboardShouldPersistTaps="handled"
      keyboardDismissMode="on-drag"
      ListHeaderComponent={
        <View style={{ paddingHorizontal: space.xl, paddingBottom: space.md }}>
          <Eyebrow tone="muted">{paged.totalCount > 0 ? countLine : 'Results'}</Eyebrow>
        </View>
      }
      ListEmptyComponent={
        <EmptyBlock message={`No results found for “${query}”. Try different keywords.`} />
      }
      ListFooterComponent={
        <>
          <ListFooterLoading visible={paged.loadingMore} />
          <ArchiveFooter />
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
      onEndReachedThreshold={0.4}
      renderItem={({ item }) => (
        <ArticleRow
          testID={`search-result-${item.sourceKey.replace(/[^a-z0-9]+/gi, '-').toLowerCase()}`}
          title={item.title}
          subtitle={item.summary}
          meta={[searchTypeLabel(item.contentType), formatPublishedDate(item.publishedAt ?? '')]
            .filter(Boolean)
            .join(' · ')}
          accessibilityLabel={`${item.title}. ${searchTypeLabel(item.contentType)}`}
          onPress={() => onOpen?.(targetForSearchResult(item, getAppConfig().apiBaseUrl), item)}
        />
      )}
    />
  );
}

export function SearchScreen({ onOpen }: Props) {
  const { c, chrome } = useTheme();
  const [query, setQuery] = useState('');
  const [committedQuery, setCommittedQuery] = useState('');
  const [typeFilter, setTypeFilter] = useState<SearchTypeFilter>(null);
  const fieldRadius = Platform.OS === 'ios' ? chrome.ios.searchFieldRadius : chrome.android.searchFieldRadius;
  const shouldSearch = committedQuery.length >= searchMinQueryLength;

  useEffect(() => {
    const handle = setTimeout(() => setCommittedQuery(query.trim()), 300);
    return () => clearTimeout(handle);
  }, [query]);

  const applyPreset = (preset: string) => {
    setQuery(preset);
    setCommittedQuery(preset);
  };

  return (
    <View testID={testIds.searchScreen} style={{ flex: 1, backgroundColor: c.surfacePage }}>
      <View style={{ paddingHorizontal: space.xl, paddingTop: space.md, paddingBottom: space.lg }}>
        <View
          style={{
            height: 44,
            borderRadius: fieldRadius,
            backgroundColor: c.surfaceRaised,
            borderWidth: 1,
            borderColor: c.border,
            flexDirection: 'row',
            alignItems: 'center',
            paddingHorizontal: 12,
            gap: 10,
          }}
        >
          <Search size={18} color={c.textMuted} strokeWidth={1.5} />
          <TextInput
            testID={testIds.searchInput}
            autoFocus
            value={query}
            onChangeText={setQuery}
            placeholder="Search news, articles and discussions"
            placeholderTextColor={c.textMuted}
            accessibilityLabel="Search the archive"
            autoCorrect={false}
            autoCapitalize="none"
            returnKeyType="search"
            style={{
              flex: 1,
              color: c.textPrimary,
              fontFamily: fonts.body,
              fontSize: 16,
            }}
          />
        </View>
      </View>

      {shouldSearch ? (
        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          keyboardShouldPersistTaps="handled"
          contentContainerStyle={{ paddingHorizontal: space.xl, gap: 8, paddingBottom: space.md }}
        >
          {searchTypeFilters.map((filter) => (
            <Chip
              key={filter.label}
              label={filter.label}
              active={typeFilter === filter.type}
              onPress={() => setTypeFilter(filter.type)}
            />
          ))}
        </ScrollView>
      ) : (
        <View style={{ paddingHorizontal: space.xl, paddingBottom: space.md }}>
          <Eyebrow tone="muted">Suggested</Eyebrow>
        </View>
      )}

      {shouldSearch ? (
        <SearchResults query={committedQuery} typeFilter={typeFilter} onOpen={onOpen} />
      ) : (
        <View style={{ flex: 1 }}>
          {searchQueryPresets.map((preset) => (
            <Pressable
              key={preset}
              testID={`search-preset-${preset.replace(/[^a-z0-9]+/gi, '-').toLowerCase()}`}
              accessibilityRole="button"
              accessibilityLabel={`Search for ${preset}`}
              onPress={() => applyPreset(preset)}
              style={{
                paddingHorizontal: space.xl,
                paddingVertical: 16,
                borderTopWidth: 1,
                borderTopColor: c.hairline,
              }}
            >
              <Text style={[type.listTitle, { color: c.textPrimary }]}>{preset}</Text>
            </Pressable>
          ))}
          <ArchiveFooter />
        </View>
      )}
    </View>
  );
}

export function SearchRouteScreen() {
  const navigation = useNavigation();
  return (
    <SearchScreen
      onOpen={(target, item) => {
        applySearchTarget(
          target,
          (tab, params) => {
            const parent = navigation.getParent();
            if (parent) {
              parent.navigate(tab, params);
              return;
            }
            const url = websiteUrl(getAppConfig().apiBaseUrl, item.url);
            if (url) {
              void WebBrowser.openBrowserAsync(url);
            }
          },
          (url) => {
            void WebBrowser.openBrowserAsync(url);
          },
        );
      }}
    />
  );
}
