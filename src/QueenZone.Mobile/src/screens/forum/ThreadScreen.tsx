import { useCallback, useEffect, useLayoutEffect, useRef, useState } from 'react';
import {
  ActivityIndicator,
  FlatList,
  Image,
  Modal,
  Pressable,
  RefreshControl,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import {
  ApiError,
  closeForumTopicPoll,
  fetchForumTopicPoll,
  fetchForumTopicPostsResult,
  fetchForumTopicResult,
  fetchForumTopicWatch,
  isCookieGatedForumAttachmentPath,
  isOfflineFailure,
  isTimeoutFailure,
  openForumAttachmentFile,
  openForumAttachmentImage,
  unwatchForumTopic,
  voteForumTopicPoll,
  watchForumTopic,
  type CacheSource,
  type ForumAttachment,
  type ForumPoll,
  type ForumPost,
  type ForumTopicDetail,
} from '../../api';
import { getAppConfig } from '../../config';
import { usePagedContent } from '../../hooks/usePagedContent';
import type { ForumStackParamList } from '../../navigation/types';
import { resolvePushMemberId } from '../../notifications/pushMemberId';
import { useOfflineQueue, type OfflineQueueItem } from '../../offlineQueue';
import { isSmokeAttachEnabled } from '../../session/smokeAttach';
import { useSession } from '../../session/SessionContext';
import { openForumComposer, openSignIn } from '../../session/signInNavigation';
import { RichHtmlBody } from '../../ui/RichHtmlBody';
import { Button } from '../../ui/Button';
import { EmptyBlock, ErrorBlock, ListFooterLoading, LoadingBlock, OfflineBanner } from '../../ui/ScreenStates';
import { resolveContentUrl } from '../../ui/html/resolveContentUrl';
import { usePressProps, pressedStyle } from '../../ui/press';
import { testIds } from '../../test/testIds';
import { radius, space, type, useTheme } from '../../theme';
import { ForumPollCard } from './ForumPollCard';
import { pollActionErrorMessage, pollTokenRequiredMessage, shouldLoadPoll } from './forumPollMeta';
import {
  attachmentAction,
  attachmentMeta,
  formatMemberSince,
  formatPostTimestamp,
  forumPostsPageSize,
  imagePreviewUrl,
  parseTopicId,
  topicReplyAllowed,
  watchButtonLabel,
  watchHint,
} from './forumThreadMeta';

type Props = NativeStackScreenProps<ForumStackParamList, 'Thread'>;

function messageFromUnknownError(err: unknown): string {
  return err instanceof ApiError ? err.message : 'Something went wrong.';
}

function isStaleReadFailure(err: unknown): boolean {
  return isOfflineFailure(err) || isTimeoutFailure(err);
}

function smokeAttachAllowed(): boolean {
  return isSmokeAttachEnabled({
    dev: typeof __DEV__ !== 'undefined' ? __DEV__ : false,
    appEnv: getAppConfig().appEnv,
  });
}

export function ThreadScreen({ navigation, route }: Props) {
  const { c } = useTheme();
  const { isSignedIn, accessToken, profile } = useSession();
  const memberId = accessToken ? resolvePushMemberId(accessToken, profile?.memberId) : null;
  const queueItems = useOfflineQueue(memberId);
  const { id: rawId, title } = route.params;
  const id = parseTopicId(rawId);
  const [topic, setTopic] = useState<ForumTopicDetail | null>(null);
  const [topicError, setTopicError] = useState<string | null>(null);
  const [topicReloadToken, setTopicReloadToken] = useState(0);
  const [topicSource, setTopicSource] = useState<CacheSource>('network');
  const [topicCachedAt, setTopicCachedAt] = useState<string | null>(null);
  const [postsSource, setPostsSource] = useState<CacheSource>('network');
  const [postsCachedAt, setPostsCachedAt] = useState<string | null>(null);
  const [poll, setPoll] = useState<ForumPoll | null>(null);
  const [pollBusy, setPollBusy] = useState(false);
  const [pollError, setPollError] = useState<string | null>(null);
  const [watching, setWatching] = useState(false);
  const [watchBusy, setWatchBusy] = useState(false);
  const [watchError, setWatchError] = useState<string | null>(null);
  const topicRef = useRef<ForumTopicDetail | null>(null);
  const topicNetworkOnlyRef = useRef(false);
  topicRef.current = topic;

  const paged = usePagedContent<ForumPost>(
    useCallback(
      async (page, signal, mode) => {
        if (id === null) {
          return { items: [], page: 1, pageSize: forumPostsPageSize, totalCount: 0, totalPages: 0 };
        }
        try {
          const result = await fetchForumTopicPostsResult(id, {
            page,
            pageSize: forumPostsPageSize,
            signal,
            networkOnly: mode === 'refresh',
          });
          setPostsSource(result.source);
          setPostsCachedAt(result.cachedAt);
          return result.data;
        } catch (err: unknown) {
          if (mode === 'refresh' && isStaleReadFailure(err)) {
            setPostsSource('cache');
          }
          throw err;
        }
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
    const networkOnly = topicNetworkOnlyRef.current;
    topicNetworkOnlyRef.current = false;
    setTopicError(null);
    fetchForumTopicResult(id, controller.signal, { networkOnly })
      .then(async (result) => {
        setTopic(result.data);
        setTopicSource(result.source);
        setTopicCachedAt(result.cachedAt);
        if (result.source === 'cache') {
          setWatching(false);
          setWatchError(null);
          setPoll(null);
          setPollError(null);
          return;
        }
        if (accessToken) {
          try {
            const watch = await fetchForumTopicWatch(id, accessToken, controller.signal);
            setWatching(watch.watching);
            setWatchError(null);
          } catch (err: unknown) {
            if (err instanceof Error && err.name === 'AbortError') {
              return;
            }
            setWatching(false);
            if (!(err instanceof ApiError && err.status === 404)) {
              setWatchError(messageFromUnknownError(err));
            }
          }
        } else {
          setWatching(false);
          setWatchError(null);
        }
        if (!shouldLoadPoll(result.data.hasPoll)) {
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
        if (networkOnly && topicRef.current && isStaleReadFailure(err)) {
          setTopicSource('cache');
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

  const retryTopic = useCallback(() => {
    topicNetworkOnlyRef.current = false;
    setTopicReloadToken((n) => n + 1);
  }, []);

  const retry = useCallback(() => {
    retryTopic();
    paged.reload();
  }, [retryTopic, paged]);

  const refresh = useCallback(() => {
    topicNetworkOnlyRef.current = true;
    setTopicReloadToken((n) => n + 1);
    paged.refresh();
  }, [paged]);

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

  const toggleWatch = useCallback(() => {
    if (id === null) {
      return;
    }
    if (!accessToken) {
      openSignIn(navigation);
      return;
    }
    setWatchBusy(true);
    setWatchError(null);
    const action = watching
      ? unwatchForumTopic(id, accessToken)
      : watchForumTopic(id, accessToken);
    void action
      .then((next) => setWatching(next.watching))
      .catch((err: unknown) => setWatchError(messageFromUnknownError(err)))
      .finally(() => setWatchBusy(false));
  }, [accessToken, id, navigation, watching]);

  if (id === null) {
    return (
      <ErrorBlock message={topicError ?? 'This discussion is not available in the archive yet.'} />
    );
  }

  const topicPending = !topic && !topicError;
  if ((paged.loading && paged.items.length === 0) || topicPending) {
    return <LoadingBlock label="Loading thread…" />;
  }

  const offlineSnapshot = topicSource === 'cache' || postsSource === 'cache';
  const snapshotCachedAt = postsCachedAt ?? topicCachedAt;

  if (topicError && !topic) {
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
      {offlineSnapshot ? <OfflineBanner cachedAt={snapshotCachedAt} testID={testIds.offlineBanner} /> : null}
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
      <View style={styles.watch} testID={testIds.forumThreadWatch}>
        <Button
          label={isSignedIn ? watchButtonLabel(watching) : 'Sign in to watch'}
          variant="outline"
          size="sm"
          loading={watchBusy}
          disabled={offlineSnapshot}
          onPress={toggleWatch}
        />
        <Text style={[type.caption, { color: c.textMuted, marginTop: space.sm }]}>
          {watchHint(watching)}
        </Text>
        {watchError ? (
          <Text style={[type.caption, { color: c.textMuted, marginTop: space.sm }]}>{watchError}</Text>
        ) : null}
      </View>
      {poll && !offlineSnapshot ? (
        <View style={styles.poll}>
          <ForumPollCard
            poll={poll}
            isSignedIn={isSignedIn}
            hasAccessToken={Boolean(accessToken)}
            busy={pollBusy}
            error={pollError}
            onVote={votePoll}
            onClose={closePoll}
            onSignIn={() => openSignIn(navigation)}
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
          testID={testIds.forumThreadReply}
          variant="outline"
          onPress={() =>
            openForumComposer(navigation, isSignedIn, {
              threadId: id ?? undefined,
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
      testID={testIds.forumThreadScreen}
      style={[styles.list, { backgroundColor: c.surfacePage }]}
      data={overlayQueuedPosts(paged.items, queueItems, id)}
      keyExtractor={(item) => item.operationId ?? String(item.id)}
      ListHeaderComponent={header}
      ListEmptyComponent={<EmptyBlock message="No posts are available in this thread yet." />}
      ListFooterComponent={footer}
      refreshControl={
        <RefreshControl refreshing={paged.refreshing} onRefresh={refresh} tintColor={c.accentPrimary} />
      }
      onEndReached={paged.loadMore}
      onEndReachedThreshold={0.4}
      renderItem={({ item }) => (
        <ForumPostRow
          post={item}
          isSignedIn={isSignedIn}
          accessToken={accessToken}
          interactionsEnabled={!offlineSnapshot}
        />
      )}
    />
  );
}

type DisplayPost = ForumPost & {
  queueState?: OfflineQueueItem['state'];
  operationId?: string;
};

function overlayQueuedPosts(
  posts: ForumPost[],
  queueItems: OfflineQueueItem[],
  topicId: number | null,
): DisplayPost[] {
  const rows: DisplayPost[] = posts.map((post) => ({ ...post }));
  if (topicId == null) {
    return rows;
  }
  for (const item of queueItems) {
    if (item.kind !== 'forum.reply' || !('topicId' in item.target) || item.target.topicId !== topicId) {
      continue;
    }
    rows.push({
      id: 0,
      body: item.payload.body,
      postedAt: item.createdAt,
      authorUsername: 'You',
      signature: null,
      authorMemberSince: null,
      authorMemberId: item.memberId,
      editedAt: null,
      editCount: 0,
      attachments: [],
      queueState: item.state,
      operationId: item.operationId,
    });
  }
  return rows;
}

function ForumPostRow({
  post,
  isSignedIn,
  accessToken,
  interactionsEnabled,
}: {
  post: DisplayPost;
  isSignedIn: boolean;
  accessToken: string | null;
  interactionsEnabled: boolean;
}) {
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
      {post.queueState ? (
        <Text
          testID={testIds.pendingForumPost}
          style={[type.caption, { color: c.accentPrimary, marginTop: space.xs }]}
        >
          {post.queueState === 'sending'
            ? 'Sending…'
            : post.queueState === 'needs_attention'
              ? 'Needs attention'
              : 'Queued'}
        </Text>
      ) : null}
      {post.attachments.length > 0 ? (
        <ForumAttachmentList
          attachments={post.attachments}
          isSignedIn={isSignedIn}
          accessToken={accessToken}
          interactionsEnabled={interactionsEnabled}
        />
      ) : null}
      {post.signature ? (
        <Text style={[type.caption, { color: c.textMuted, marginTop: space.md }]}>{post.signature}</Text>
      ) : null}
    </View>
  );
}

function ForumAttachmentList({
  attachments,
  isSignedIn,
  accessToken,
  interactionsEnabled,
}: {
  attachments: ForumAttachment[];
  isSignedIn: boolean;
  accessToken: string | null;
  interactionsEnabled: boolean;
}) {
  const { c } = useTheme();
  const press = usePressProps();
  const [viewer, setViewer] = useState<{ uri: string; label: string } | null>(null);
  const [busyKey, setBusyKey] = useState<string | null>(null);
  const [errorKey, setErrorKey] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [opened, setOpened] = useState(false);
  const label = attachments.length === 1 ? 'Attachment' : 'Attachments';

  const openAttachment = useCallback(
    async (attachment: ForumAttachment) => {
      if (!interactionsEnabled) {
        return;
      }
      const action = attachmentAction(attachment, isSignedIn);
      if (action === 'none') {
        return;
      }
      if (!accessToken || isCookieGatedForumAttachmentPath(attachment.downloadUrl)) {
        return;
      }
      const key = `${attachment.downloadUrl}-${attachment.fileName}`;
      setErrorKey(null);
      setErrorMessage(null);
      setBusyKey(key);
      try {
        if (action === 'view-image') {
          const uri = await openForumAttachmentImage(attachment.downloadUrl, accessToken);
          setViewer({ uri, label: attachment.fileName });
          return;
        }
        await openForumAttachmentFile(attachment.downloadUrl, accessToken, attachment.fileName, {
          present: !smokeAttachAllowed(),
        });
        if (smokeAttachAllowed()) {
          setOpened(true);
        }
      } catch (err: unknown) {
        setErrorKey(key);
        setErrorMessage(err instanceof ApiError ? err.message : 'Unable to open this attachment.');
      } finally {
        setBusyKey(null);
      }
    },
    [accessToken, interactionsEnabled, isSignedIn],
  );

  return (
    <View style={styles.attachments}>
      <Text style={[type.meta, { color: c.textMuted }]}>{label}</Text>
      {attachments.map((attachment) => {
        const preview = imagePreviewUrl(attachment);
        const previewUri = preview ? resolveContentUrl(preview, getAppConfig().apiBaseUrl) : null;
        const caption = attachmentMeta(attachment);
        const action = interactionsEnabled ? attachmentAction(attachment, isSignedIn) : 'none';
        const key = `${attachment.downloadUrl}-${attachment.fileName}`;
        const meta = (
          <View style={styles.attachmentMeta} accessibilityLabel={`${attachment.fileName}. ${caption}`}>
            <Text style={[type.listTitle, { color: c.textPrimary }]}>{attachment.fileName}</Text>
            {caption ? (
              <Text style={[type.meta, { color: c.textMuted, marginTop: space.xs }]}>{caption}</Text>
            ) : null}
            {errorKey === key && errorMessage ? (
              <Text style={[type.caption, { color: c.textMuted, marginTop: space.xs }]}>{errorMessage}</Text>
            ) : null}
          </View>
        );
        const body = (
          <>
            {previewUri ? (
              <Image
                source={{ uri: previewUri }}
                style={[styles.thumb, { backgroundColor: c.surfaceCard, borderColor: c.hairline }]}
                resizeMode="cover"
                accessibilityLabel={attachment.fileName}
              />
            ) : null}
            {meta}
            {busyKey === key ? <ActivityIndicator color={c.accentPrimary} /> : null}
          </>
        );
        if (action === 'none') {
          return (
            <View key={key} style={styles.attachment} testID={testIds.forumThreadAttachment}>
              {body}
            </View>
          );
        }
        return (
          <Pressable
            key={key}
            style={({ pressed }) => pressedStyle({ pressed }, styles.attachment)}
            {...press}
            testID={testIds.forumThreadAttachment}
            accessibilityRole="button"
            accessibilityLabel={`${attachment.fileName}. ${caption}. Open`}
            onPress={() => {
              void openAttachment(attachment);
            }}
          >
            {body}
          </Pressable>
        );
      })}
      {opened ? (
        <Text testID={testIds.forumThreadAttachmentOpened} style={[type.caption, { color: c.textMuted }]}>
          Attachment opened
        </Text>
      ) : null}
      <Modal
        visible={viewer != null}
        transparent
        animationType="fade"
        onRequestClose={() => setViewer(null)}
      >
        <Pressable
          style={[styles.viewerBackdrop, { backgroundColor: c.surfaceScrim }]}
          onPress={() => setViewer(null)}
          testID={testIds.forumThreadAttachmentViewer}
          accessibilityRole="button"
          accessibilityLabel="Close attachment"
        >
          {viewer ? (
            <Image
              source={{ uri: viewer.uri }}
              style={styles.viewerImage}
              resizeMode="contain"
              accessibilityLabel={viewer.label}
            />
          ) : null}
        </Pressable>
      </Modal>
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
  watch: {
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
  viewerBackdrop: {
    flex: 1,
    justifyContent: 'center',
    padding: space.xl,
  },
  viewerImage: {
    width: '100%',
    height: '80%',
  },
});
