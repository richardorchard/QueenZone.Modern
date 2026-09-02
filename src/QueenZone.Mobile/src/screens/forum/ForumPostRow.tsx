import { memo } from 'react';
import { Alert, Pressable, StyleSheet, Text, View } from 'react-native';
import type { ForumPost } from '../../api';
import { flushOfflineQueue, removeOfflineItem, updateOfflineItem, type OfflineQueueItem } from '../../offlineQueue';
import { RichHtmlBody } from '../../ui/RichHtmlBody';
import { testIds } from '../../test/testIds';
import { space, type, useTheme } from '../../theme';
import { formatMemberSince, formatPostTimestamp } from './forumThreadMeta';
import { ForumAttachmentList } from './ForumAttachmentList';

export type DisplayPost = ForumPost & {
  queueState?: OfflineQueueItem['state'];
  operationId?: string;
};

export function postKeyExtractor(item: DisplayPost): string {
  return item.operationId ?? String(item.id);
}

export const ForumPostRow = memo(function ForumPostRow({
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
        <Pressable
          accessibilityRole="button"
          accessibilityLabel={
            post.queueState === 'sending'
              ? 'Sending…'
              : post.queueState === 'needs_attention'
                ? 'Needs attention'
                : 'Queued'
          }
          testID={testIds.pendingForumPost}
          onPress={() => {
            if (post.queueState !== 'needs_attention' || !post.operationId) {
              return;
            }
            Alert.alert('This reply could not be sent.', undefined, [
              { text: 'Dismiss', style: 'cancel' },
              {
                text: 'Discard',
                style: 'destructive',
                onPress: () => {
                  void removeOfflineItem(post.operationId!);
                },
              },
              {
                text: 'Retry',
                onPress: () => {
                  void updateOfflineItem(post.operationId!, {
                    state: 'queued',
                    nextRetryAt: new Date().toISOString(),
                    lastError: null,
                  }).then(() => {
                    void flushOfflineQueue();
                  });
                },
              },
            ]);
          }}
        >
          <Text style={[type.caption, { color: c.accentPrimary, marginTop: space.xs }]}>
            {post.queueState === 'sending'
              ? 'Sending…'
              : post.queueState === 'needs_attention'
                ? 'Needs attention'
                : 'Queued'}
          </Text>
        </Pressable>
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
});

const styles = StyleSheet.create({
  post: {
    paddingHorizontal: space.xl,
    paddingVertical: space.lg,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  body: {
    marginTop: space.md,
  },
});
