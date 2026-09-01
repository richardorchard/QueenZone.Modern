import { useCallback, useEffect, useLayoutEffect, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import {
  ApiError,
  fetchBiographyChapter,
  toPlainText,
  type BiographyChapterDetail,
} from '../../api';
import type { ArchiveStackParamList } from '../../navigation/types';
import { ErrorBlock, LoadingBlock } from '../../ui/ScreenStates';
import { space, type, useTheme } from '../../theme';

type Props = NativeStackScreenProps<ArchiveStackParamList, 'BiographyChapter'>;

export function BiographyChapterScreen({ navigation, route }: Props) {
  const { c } = useTheme();
  const { id } = route.params;
  const [chapter, setChapter] = useState<BiographyChapterDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [reloadToken, setReloadToken] = useState(0);

  useLayoutEffect(() => {
    navigation.setOptions({ title: chapter?.title ?? 'Chapter' });
  }, [navigation, chapter?.title]);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setError(null);
    fetchBiographyChapter(id, controller.signal)
      .then((detail) => {
        setChapter(detail);
        setLoading(false);
      })
      .catch((err: unknown) => {
        if (err instanceof Error && err.name === 'AbortError') {
          return;
        }
        setChapter(null);
        setError(err instanceof ApiError ? err.message : 'Something went wrong.');
        setLoading(false);
      });
    return () => controller.abort();
  }, [id, reloadToken]);

  const retry = useCallback(() => setReloadToken((n) => n + 1), []);

  if (loading) {
    return <LoadingBlock label="Loading chapter…" />;
  }

  if (error || !chapter) {
    return <ErrorBlock message={error ?? 'Chapter not found.'} onRetry={retry} />;
  }

  const body = toPlainText(chapter.body);

  return (
    <ScrollView
      style={[styles.scroll, { backgroundColor: c.surfacePage }]}
      contentContainerStyle={styles.content}
    >
      <Text style={[type.eyebrow, { color: c.accentArchive }]}>Biography</Text>
      <Text
        style={[type.articleTitle, { color: c.textPrimary, marginTop: space.sm }]}
        allowFontScaling
        maxFontSizeMultiplier={1.4}
      >
        {chapter.title}
      </Text>
      <Text style={[type.meta, { color: c.textMuted, marginTop: space.md }]}>
        Chapter {chapter.displaySequence}
      </Text>
      <Text style={[type.longform, { color: c.textPrimary, marginTop: space.xl }]}>{body}</Text>
      <View style={styles.nav}>
        {chapter.previous ? (
          <Pressable
            accessibilityRole="button"
            accessibilityLabel={`Previous: ${chapter.previous.title}`}
            onPress={() => navigation.replace('BiographyChapter', { id: chapter.previous!.id })}
            style={({ pressed }) => [styles.navButton, { borderColor: c.border, opacity: pressed ? 0.85 : 1 }]}
          >
            <Text style={[type.meta, { color: c.textMuted }]}>Previous</Text>
            <Text style={[type.listTitle, { color: c.accentPrimary }]} numberOfLines={2}>
              {chapter.previous.title}
            </Text>
          </Pressable>
        ) : (
          <View style={styles.navSpacer} />
        )}
        {chapter.next ? (
          <Pressable
            accessibilityRole="button"
            accessibilityLabel={`Next: ${chapter.next.title}`}
            onPress={() => navigation.replace('BiographyChapter', { id: chapter.next!.id })}
            style={({ pressed }) => [styles.navButton, { borderColor: c.border, opacity: pressed ? 0.85 : 1 }]}
          >
            <Text style={[type.meta, { color: c.textMuted }]}>Next</Text>
            <Text style={[type.listTitle, { color: c.accentPrimary }]} numberOfLines={2}>
              {chapter.next.title}
            </Text>
          </Pressable>
        ) : (
          <View style={styles.navSpacer} />
        )}
      </View>
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
  nav: {
    marginTop: space.xxl,
    gap: space.md,
  },
  navButton: {
    borderWidth: 1,
    padding: space.base,
    gap: space.xs,
    minHeight: 48,
  },
  navSpacer: {
    flex: 1,
  },
});
