import { useCallback, useEffect, useLayoutEffect, useRef, useState } from 'react';
import { FlatList, Image, RefreshControl, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import {
  ApiError,
  closeForumTopicPoll,
  fetchForumTopic,
  fetchForumTopicPoll,
  fetchForumTopicPosts,
  voteForumTopicPoll,
  type ForumAttachment,
  type ForumPoll,
  type ForumPost,
  type ForumTopicDetail,
} from '../../api';
import { getAppConfig } from '../../config';
import { usePagedContent } from '../../hooks/usePagedContent';
import type { ForumStackParamList } from '../../navigation/types';
import { useSession } from '../../session/SessionContext';
import { RichHtmlBody } from '../../ui/RichHtmlBody';
import { Button } from '../../ui/Button';
import { EmptyBlock, ErrorBlock, ListFooterLoading, LoadingBlock } from '../../ui/ScreenStates';
import { resolveContentUrl } from '../../ui/html/resolveContentUrl';
import { radius, space, type, useTheme } from '../../theme';
import { ForumPollCard } from './ForumPollCard';
import { pollActionErrorMessage, pollTokenRequiredMessage, shouldLoadPoll } from './forumPollMeta';
import {
  attachmentMeta,
  formatMemberSince,
  formatPostTimestamp,
  forumPostsPageSize,
  imagePreviewUrl,
  parseTopicId,
  topicReplyAllowed,
} from './forumThreadMeta';

type Props = NativeStackScreenProps<ForumStackParamList, 'Thread'>;

function messageFromUnknownError(err: unknown): string {
  return err instanceof ApiError ? err.message : 'Something went wrong.';
}

export function ThreadScreen({ navigation, route }: Props) {
  const { c } = useTheme();
  const { isSignedIn, accessToken, signIn } = useSession();
  const { id: rawId, title } = route.params;
  const id = parseTopicId(rawId);
  const [topic, setTopic] = useState<ForumTopicDetail | null>(null);
  const [topicError, setTopicError] = useState<string | null>(null);
  const [topicReloadToken, setTopicReloadToken] = useState(0);
  const [poll, setPoll] = useState<ForumPoll | null>(null);
  const [pollBusy, setPollBusy] = useState(false);
  const [pollError, setPollError] = useState<string | null>(null);

  const paged = usePagedContent<ForumPost>(
    useCallback(
      (page, signal) => {
        if (id === null) {
          return Promise.resolve({ items: [], page: 1, pageSize: forumPostsPageSize, totalCount: 0, totalPages: 0 });
        }
        return fetchForumTopicPosts(id, { page, pageSize: forumPostsPageSize, signal });
      },
      [id],
    ),
    forumPostsPageSize,
  );

  useEffect(() => {
    if (id === null) {
      setTopic(null);
      setPoll(null);
      setTopicError('This discussion is not available in the archive yet.');
      return;
    }
    const controller = new AbortController();
    setTopicError(null);
    fetchForumTopic(id, controller.signal)
      .then(async (item) => {
        setTopic(item);
        if (!shouldLoadPoll(item.hasPoll)) {
          setPoll(null);
          setPollError(null);
          return;
        }
        try {
          const nextPoll = await fetchForumTopicPoll(id, accessToken, controller.signal);
          setPoll(nextPoll);
          setPollError(null);
        } catch (err: unknown) {
          if (err instanceof Error && err.name === 'AbortError') {
            return;
          }
          setPoll(null);
          if (err instanceof ApiError && err.status === 404) {
            setPollError(null);
            return;
          }
          setPollError(messageFromUnknownError(err));
        }
      })
      .catch((err: unknown) => {
        if (err instanceof Error && err.name === 'AbortError') {
          return;
        }
        setTopic(null);
        setPoll(null);
        setTopicError(messageFromUnknownError(err));
      });
    return () => controller.abort();
  }, [accessToken, id, topicReloadToken]);

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

  const skipFocusRefresh = useRef(true);
  useEffect(() => {
    return navigation.addListener('focus', () => {
      if (skipFocusRefresh.current) {
        skipFocusRefresh.current = false;
        return;
      }
      refresh();
    });
  }, [navigation, refresh]);

  const runPollAction = useCallback(
    async (action: () => Promise<ForumPoll>) => {
      if (id === null) {
        return;
      }
      setPollBusy(true);
      setPollError(null);
      try {
        setPoll(await action());
      } catch (err: unknown) {
        setPollError(pollActionErrorMessage(err));
      } finally {
        setPollBusy(false);
      }
    },
    [id],
  );

  const votePoll = useCallback(
    (optionIds: string[]) => {
      if (id === null || !accessToken) {
        setPollError(
          accessToken
            ? 'Something went wrong.'
            : pollTokenRequiredMessage,
        );
        return;
      }
      void runPollAction(() => voteForumTopicPoll(id, optionIds, accessToken));
    },
    [accessToken, id, runPollAction],
  );

  const closePoll = useCallback(() => {
    if (id === null || !accessToken) {
      setPollError('Closing a poll requires a mobile Bearer token.');
      return;
    }
    void runPollAction(() => closeForumTopicPoll(id, accessToken));
  }, [accessToken, id, runPollAction]);

  if (id === null) {
    return (
      <ErrorBlock message={topicError ?? 'This discussion is not available in the archive yet.'} />
    );
  }

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
      {poll ? (
        <View style={styles.poll}>
          <ForumPollCard
            poll={poll}
            isSignedIn={isSignedIn}
            hasAccessToken={Boolean(accessToken)}
            busy={pollBusy}
            error={pollError}
            onVote={votePoll}
            onClose={closePoll}
            onSignIn={signIn}
          />
        </View>
      ) : null}
      {pollError && !poll ? (
        <ErrorBlock message={pollError} onRetry={retry} />
      ) : null}
    </View>
  );

  const canReply = topicReplyAllowed(topic);

  const footer = (
    <View style={styles.reply}>
      <ListFooterLoading visible={paged.loadingMore} />
      {canReply ? (
        <Button
          label={isSignedIn ? 'Reply' : 'Sign in to reply'}
          variant="outline"
          onPress={() =>
            navigation.navigate('Composer', {
              threadId: id,
              threadTitle: topic?.title ?? title,
              isLocked: topic?.isLocked,
            })
          }
        />
      ) : (
        <Text style={[type.caption, { color: c.textMuted }]}>This topic is locked.</Text>
      )}
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
              <Image
                source={{ uri: previewUri }}
                style={[styles.thumb, { backgroundColor: c.surfaceCard, borderColor: c.hairline }]}
                resizeMode="cover"
                accessibilityLabel={attachment.fileName}
              />
            ) : null}
            {/*
              Do not Linking.openURL cookie-gated /forum/attachment/... from the app.
              A Bearer-authenticated download API is a follow-up before #733 uploads
              rely on opening attachments.
            */}
            <View style={styles.attachmentMeta} accessibilityLabel={`${attachment.fileName}. ${caption}`}>
              <Text style={[type.listTitle, { color: c.textPrimary }]}>{attachment.fileName}</Text>
              {caption ? (
                <Text style={[type.meta, { color: c.textMuted, marginTop: space.xs }]}>{caption}</Text>
              ) : null}
            </View>
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
  poll: {
    marginHorizontal: -space.xl,
    marginTop: space.lg,
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
  attachmentMeta: {
    minHeight: 48,
    justifyContent: 'center',
  },
  reply: {
    marginHorizontal: space.xl,
    marginTop: space.base,
    marginBottom: space.section,
  },
});
