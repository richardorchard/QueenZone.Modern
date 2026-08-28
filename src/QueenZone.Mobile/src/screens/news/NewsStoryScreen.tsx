import { useCallback, useLayoutEffect } from 'react';
import { Linking, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { fetchNewsDetail, formatPublishedDate } from '../../api';
import { useDetailQuery } from '../../hooks/useDetailQuery';
import { HeaderBackButton } from '../../navigation/headerButtons';
import { leaveStoryScreen } from '../../navigation/nestedTab';
import type { NewsStackParamList } from '../../navigation/types';
import { RichHtmlBody } from '../../ui/RichHtmlBody';
import { ErrorBlock, LoadingBlock } from '../../ui/ScreenStates';
import { testIds } from '../../test/testIds';
import { space, type, useTheme } from '../../theme';

type Props = NativeStackScreenProps<NewsStackParamList, 'Story'>;

export function NewsStoryScreen({ navigation, route }: Props) {
  const { c } = useTheme();
  const { id } = route.params;
  const loadArticle = useCallback((signal: AbortSignal) => fetchNewsDetail(id, signal), [id]);
  const { data: article, error, loading, reload } = useDetailQuery(loadArticle);

  useLayoutEffect(() => {
    navigation.setOptions({
      title: article?.title ?? 'Story',
      headerLeft: () => (
        <HeaderBackButton
          testID={testIds.newsStoryBack}
          onPress={() => leaveStoryScreen(navigation)}
        />
      ),
    });
  }, [navigation, article?.title]);

  if (loading) {
    return <LoadingBlock label="Loading article…" />;
  }

  if (error || !article) {
    return <ErrorBlock message={error ?? 'Article not found.'} onRetry={reload} />;
  }

  const published = formatPublishedDate(article.publishedAt);

  return (
    <ScrollView
      testID={testIds.newsStoryScreen}
      style={[styles.scroll, { backgroundColor: c.surfacePage }]}
      contentContainerStyle={styles.content}
    >
      <Text style={[type.eyebrow, { color: c.accentEditorial }]}>News</Text>
      <Text
        style={[type.articleTitle, { color: c.textPrimary, marginTop: space.sm }]}
        allowFontScaling
        maxFontSizeMultiplier={1.4}
      >
        {article.title}
      </Text>
      {published ? (
        <Text style={[type.meta, { color: c.textMuted, marginTop: space.md }]}>{published}</Text>
      ) : null}
      {article.excerpt ? (
        <Text style={[type.standfirst, { color: c.textSecondary, marginTop: space.lg }]}>
          {article.excerpt}
        </Text>
      ) : null}
      <View style={styles.body}>
        <RichHtmlBody html={article.body} horizontalInset={26} />
      </View>
      {article.sourceUrl ? (
        <Pressable
          accessibilityRole="link"
          accessibilityLabel="Open source"
          onPress={() => Linking.openURL(article.sourceUrl!)}
          style={styles.source}
        >
          <Text style={[type.button, { color: c.accentPrimary }]}>Source</Text>
        </Pressable>
      ) : null}
      <View style={{ height: space.section }} />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  scroll: { flex: 1 },
  content: {
    paddingHorizontal: 26,
    paddingTop: space.xl,
    paddingBottom: space.section,
  },
  body: {
    marginTop: space.xl,
  },
  source: {
    marginTop: space.xxl,
    minHeight: 48,
    justifyContent: 'center',
  },
});
