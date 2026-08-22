import { useCallback, useEffect, useLayoutEffect, useState } from 'react';
import {
  FlatList,
  Image,
  Linking,
  Pressable,
  RefreshControl,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import {
  ApiError,
  fetchForumTopic,
  fetchForumTopicPosts,
  type ForumAttachment,
  type ForumPost,
  type ForumTopicDetail,
} from '../../api';
import { getAppConfig } from '../../config';
import { usePagedContent } from '../../hooks/usePagedContent';
import type { ForumStackParamList } from '../../navigation/types';
import { useSession } from '../../session/SessionContext';
import { RichHtmlBody } from '../../ui/RichHtmlBody';
import { EmptyBlock, ErrorBlock, ListFooterLoading, LoadingBlock } from '../../ui/ScreenStates';
import { resolveContentUrl } from '../../ui/html/resolveContentUrl';
import { radius, space, type, useTheme } from '../../theme';
import {
  attachmentMeta,
  formatMemberSince,
  formatPostTimestamp,
  forumPostsPageSize,
  imagePreviewUrl,
} from './forumThreadMeta';

type Props = NativeStackScreenProps<ForumStackParamList, 'Thread'>;

function messageFromUnknownError(err: unknown): string {
  return err instanceof ApiError ? err.message : 'Something went wrong.';
}

function openResolvedUrl(href: string): void {
  const resolved = resolveContentUrl(href, getAppConfig().apiBaseUrl);
  if (resolved) {
    void Linking.openURL(resolved);
  }
}

export function ThreadScreen({ navigation, route }: Props) {
  const { c } = useTheme();
  const { isSignedIn } = useSession();
  const { id, title } = route.params;
  const [topic, setTopic] = useState<ForumTopicDetail | null>(null);
  const [topicError, setTopicError] = useState<string | null>(null);
  const [topicReloadToken, setTopicReloadToken] = useState(0);

  const paged = usePagedContent<ForumPost>(
    useCallback((page, signal) => fetchForumTopicPosts(id, { page, pageSize: forumPostsPageSize, signal }), [id]),
    forumPostsPageSize,
  );

  useEffect(() => {
    const controller = new AbortController();
    setTopicError(null);
    fetchForumTopic(id, controller.signal)
      .then((item) => {
        setTopic(item);
        setTopicError(null);
      })
      .catch((err: unknown) => {
        if (err instanceof Error && err.name === 'AbortError') {
          return;
        }
        setTopic(null);
        setTopicError(messageFromUnknownError(err));
      });
    return () => controller.abort();
  }, [id, topicReloadToken]);

  useLayoutEffect(() => {
    navigation.setOptions({ title: topic?.title ?? title ?? 'Thread' });
  }, [navigation, topic?.title, title]);

  const retryTopic = useCallback(() => setTopicReloadToken((n) => n + 1), []);

  const retry = useCallback(() => {
    retryTopic();
    paged.reload();
  }, [retryTopic, paged]);

  const refresh = useCallback(() => {
    retryTopic();
    paged.refresh();
  }, [retryTopic, paged]);

  const topicPending = !topic && !topicError;
  if ((paged.loading && paged.items.length === 0) || topicPending) {
    return <LoadingBlock label="Loading thread…" />;
  }

  if (topicError) {
    return <ErrorBlock message={topicError} onRetry={retry} />;
  }

  if (paged.error && paged.items.length === 0) {
    return <ErrorBlock message={paged.error} onRetry={retry} />;
  }

  const stats = topic
    ? `${topic.postCount.toLocaleString()} posts · ${topic.forumName}`
    : null;

  const header = (
    <View style={styles.header}>
      <Text style={[type.eyebrow, { color: c.accentPrimary }]}>{topic?.forumName ?? 'Forum'}</Text>
      <Text
        style={[type.articleTitle, { color: c.textPrimary, marginTop: space.sm }]}
        allowFontScaling
        maxFontSizeMultiplier={1.4}
      >
        {topic?.title ?? title ?? 'Thread'}
      </Text>
      {stats ? (
        <Text style={[type.meta, { color: c.textMuted, marginTop: space.md }]}>{stats}</Text>
      ) : null}
    </View>
  );

  const footer = (
    <View>
      <ListFooterLoading visible={paged.loadingMore} />
      <Pressable
        accessibilityRole="button"
        accessibilityLabel={isSignedIn ? 'Reply' : 'Sign in to reply'}
        onPress={() => navigation.navigate('Composer', { threadId: String(id) })}
        style={({ pressed }) => [
          styles.reply,
          {
            borderColor: c.border,
            opacity: pressed ? 0.85 : 1,
          },
        ]}
      >
        <Text style={[type.button, { color: c.accentPrimary }]}>
          {isSignedIn ? 'Reply' : 'Sign in to reply'}
        </Text>
      </Pressable>
    </View>
  );

  return (
    <FlatList
      style={[styles.list, { backgroundColor: c.surfacePage }]}
      data={paged.items}
      keyExtractor={(item) => String(item.id)}
      ListHeaderComponent={header}
      ListEmptyComponent={<EmptyBlock message="No posts are available in this thread yet." />}
      ListFooterComponent={footer}
      refreshControl={
        <RefreshControl refreshing={paged.refreshing} onRefresh={refresh} tintColor={c.accentPrimary} />
      }
      onEndReached={paged.loadMore}
      onEndReachedThreshold={0.4}
      renderItem={({ item }) => <ForumPostRow post={item} />}
    />
  );
}

function ForumPostRow({ post }: { post: ForumPost }) {
  const { c } = useTheme();
  const posted = formatPostTimestamp(post.postedAt);
  const memberSince = formatMemberSince(post.authorMemberSince);
  const meta = [posted, memberSince ? `Member since ${memberSince}` : null].filter(Boolean).join(' · ');

  return (
    <View style={[styles.post, { borderTopColor: c.hairline }]}>
      <Text style={[type.listTitle, { color: c.textPrimary }]} allowFontScaling>
        {post.authorUsername}
      </Text>
      {meta ? (
        <Text style={[type.meta, { color: c.textMuted, marginTop: space.xs }]}>{meta}</Text>
      ) : null}
      <View style={styles.body}>
        <RichHtmlBody html={post.body} horizontalInset={space.xl} />
      </View>
      {post.attachments.length > 0 ? <ForumAttachmentList attachments={post.attachments} /> : null}
      {post.signature ? (
        <Text style={[type.caption, { color: c.textMuted, marginTop: space.md }]}>{post.signature}</Text>
      ) : null}
    </View>
  );
}

function ForumAttachmentList({ attachments }: { attachments: ForumAttachment[] }) {
  const { c } = useTheme();
  const label = attachments.length === 1 ? 'Attachment' : 'Attachments';

  return (
    <View style={styles.attachments}>
      <Text style={[type.meta, { color: c.textMuted }]}>{label}</Text>
      {attachments.map((attachment) => {
        const preview = imagePreviewUrl(attachment);
        const previewUri = preview ? resolveContentUrl(preview, getAppConfig().apiBaseUrl) : null;
        const caption = attachmentMeta(attachment);
        return (
          <View key={`${attachment.url}-${attachment.fileName}`} style={styles.attachment}>
            {previewUri ? (
              <Pressable
                accessibilityRole="imagebutton"
                accessibilityLabel={`Open image ${attachment.fileName}`}
                onPress={() => openResolvedUrl(attachment.url)}
              >
                <Image
                  source={{ uri: previewUri }}
                  style={[styles.thumb, { backgroundColor: c.surfaceCard, borderColor: c.hairline }]}
                  resizeMode="cover"
                />
              </Pressable>
            ) : null}
            <Pressable
              accessibilityRole="link"
              accessibilityLabel={`Download ${attachment.fileName}`}
              onPress={() => openResolvedUrl(attachment.url)}
              style={styles.attachmentLink}
            >
              <Text style={[type.listTitle, { color: c.accentPrimary }]}>{attachment.fileName}</Text>
              {caption ? (
                <Text style={[type.meta, { color: c.textMuted, marginTop: space.xs }]}>{caption}</Text>
              ) : null}
            </Pressable>
          </View>
        );
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  list: { flex: 1 },
  header: {
    paddingHorizontal: space.xl,
    paddingTop: space.xl,
    paddingBottom: space.base,
  },
  post: {
    paddingHorizontal: space.xl,
    paddingVertical: space.lg,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  body: {
    marginTop: space.md,
  },
  attachments: {
    marginTop: space.lg,
    gap: space.md,
  },
  attachment: {
    gap: space.sm,
  },
  thumb: {
    width: 120,
    height: 120,
    borderRadius: radius.xs,
    borderWidth: StyleSheet.hairlineWidth,
  },
  attachmentLink: {
    minHeight: 48,
    justifyContent: 'center',
  },
  reply: {
    marginHorizontal: space.xl,
    marginTop: space.base,
    marginBottom: space.section,
    minHeight: 48,
    justifyContent: 'center',
    alignItems: 'center',
    borderWidth: 1,
    borderRadius: radius.xs,
  },
});
