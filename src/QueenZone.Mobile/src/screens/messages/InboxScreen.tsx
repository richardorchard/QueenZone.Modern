import { memo, useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Alert, FlatList, Pressable, RefreshControl, Text, View, type ListRenderItem } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { archiveConversation, fetchInbox, type InboxConversation } from '../../api/messages';
import { getContentCache, inboxCacheKey } from '../../cache';
import { usePagedContent } from '../../hooks/usePagedContent';
import type { HomeStackParamList } from '../../navigation/types';
import { resolvePushMemberId } from '../../notifications/pushMemberId';
import {
  flushOfflineQueue,
  removeOfflineItem,
  updateOfflineItem,
  useOfflineQueue,
  type OfflineQueueItem,
} from '../../offlineQueue';
import { MemberGate } from '../../session/MemberGate';
import { useSession } from '../../session/SessionContext';
import { radius, space, type, useTheme } from '../../theme';
import { Button } from '../../ui/Button';
import { PageTitleBlock } from '../../ui/PageTitleBlock';
import { EmptyBlock, ErrorBlock, ListFooterLoading, LoadingBlock } from '../../ui/ScreenStates';
import { testIds } from '../../test/testIds';
import {
  formatMessageTimestamp,
  inboxPageSize,
  inboxRowA11yLabel,
  unreadBadgeLabel,
} from './inboxMeta';

type Props = NativeStackScreenProps<HomeStackParamList, 'Inbox'>;

function overlayQueuedComposes(
  items: InboxConversation[],
  queueItems: OfflineQueueItem[],
): InboxConversation[] {
  const pending = queueItems.filter((item) => item.kind === 'message.compose');
  if (pending.length === 0) {
    return items;
  }
  const extra = pending.map((item) => ({
    conversationId: `pending:${item.operationId}`,
    otherParticipantId: 'recipientMemberId' in item.target ? item.target.recipientMemberId : '',
    otherParticipantDisplayName:
      item.state === 'needs_attention'
        ? 'Needs attention'
        : item.state === 'sending'
          ? 'Sending…'
          : 'Queued message',
    lastMessagePreview: item.payload.body,
    lastMessageAt: item.createdAt,
    hasUnread: false,
    unreadCount: 0,
    detailPath: '',
  }));
  return [...extra, ...items];
}

function inboxKeyExtractor(item: InboxConversation): string {
  return item.conversationId;
}

export function InboxScreen({ navigation }: Props) {
  return (
    <MemberGate title="Messages">
      <InboxList navigation={navigation} />
    </MemberGate>
  );
}

function InboxList({ navigation }: Pick<Props, 'navigation'>) {
  const { c } = useTheme();
  const { accessToken, profile } = useSession();
  const memberId = accessToken ? resolvePushMemberId(accessToken, profile?.memberId) : null;
  const queueItems = useOfflineQueue(memberId);
  const skipNextFocusRefresh = useRef(true);
  const [archivingId, setArchivingId] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const cacheKey = profile ? inboxCacheKey(profile.memberId) : null;
  const [cachedItems, setCachedItems] = useState<InboxConversation[] | null>(null);

  useEffect(() => {
    if (!cacheKey) {
      return;
    }
    let cancelled = false;
    getContentCache()
      .get<InboxConversation[]>(cacheKey)
      .then((cached) => {
        if (!cancelled && cached && cached.length > 0) {
          setCachedItems(cached);
        }
      })
      .catch(() => {});
    return () => {
      cancelled = true;
    };
  }, [cacheKey]);

  const paged = usePagedContent<InboxConversation>(
    useCallback(
      (page, signal) => {
        if (!accessToken) {
          return Promise.resolve({
            items: [],
            page: 1,
            pageSize: inboxPageSize,
            totalCount: 0,
            totalPages: 0,
          });
        }
        return fetchInbox(accessToken, { page, pageSize: inboxPageSize, signal });
      },
      [accessToken],
    ),
    inboxPageSize,
    accessToken ?? '',
  );

  useFocusEffect(
    useCallback(() => {
      if (skipNextFocusRefresh.current) {
        skipNextFocusRefresh.current = false;
        return;
      }
      paged.refresh();
    }, [paged.refresh]),
  );

  // Persist the freshest first page so the next cold start can render instantly.
  useEffect(() => {
    if (!cacheKey || paged.page !== 1 || paged.loading || paged.error) {
      return;
    }
    void getContentCache().put(cacheKey, paged.items).catch(() => {});
  }, [cacheKey, paged.page, paged.loading, paged.error, paged.items]);

  const showingCacheOnly = paged.loading && paged.items.length === 0 && !!cachedItems && cachedItems.length > 0;
  const sourceItems = showingCacheOnly && cachedItems ? cachedItems : paged.items;
  const displayItems = useMemo(
    () => overlayQueuedComposes(sourceItems, queueItems),
    [queueItems, sourceItems],
  );

  const handleArchive = useCallback(
    async (conversationId: string) => {
      if (!accessToken) {
        return;
      }
      setActionError(null);
      setArchivingId(conversationId);
      try {
        await archiveConversation(accessToken, conversationId);
        paged.refresh();
      } catch {
        setActionError('Unable to archive this conversation. Try again.');
      } finally {
        setArchivingId(null);
      }
    },
    [accessToken, paged.refresh],
  );

  const openConversation = useCallback(
    (item: InboxConversation) => {
      if (item.conversationId.startsWith('pending:')) {
        const operationId = item.conversationId.slice('pending:'.length);
        Alert.alert(item.otherParticipantDisplayName, item.lastMessagePreview, [
          { text: 'Dismiss', style: 'cancel' },
          {
            text: 'Discard',
            style: 'destructive',
            onPress: () => {
              void removeOfflineItem(operationId);
            },
          },
          {
            text: 'Retry',
            onPress: () => {
              void updateOfflineItem(operationId, {
                state: 'queued',
                nextRetryAt: new Date().toISOString(),
                lastError: null,
              }).then(() => {
                void flushOfflineQueue();
              });
            },
          },
        ]);
        return;
      }
      navigation.navigate('Conversation', { id: item.conversationId });
    },
    [navigation],
  );

  const archiveItem = useCallback(
    (conversationId: string) => {
      if (conversationId.startsWith('pending:')) {
        return;
      }
      void handleArchive(conversationId);
    },
    [handleArchive],
  );

  const renderItem = useCallback<ListRenderItem<InboxConversation>>(
    ({ item }) => (
      <InboxRow
        item={item}
        archiving={archivingId === item.conversationId}
        onPress={openConversation}
        onArchive={archiveItem}
      />
    ),
    [archivingId, archiveItem, openConversation],
  );

  const header = (
    <View>
      <PageTitleBlock
        eyebrow="Community"
        title="Messages"
        subtitle="Private conversations with other members."
      />
      <View style={{ paddingHorizontal: space.xl, paddingBottom: space.lg, flexDirection: 'row', gap: space.sm }}>
        <Button label="New message" onPress={() => navigation.navigate('ComposeMessage')} />
        <Button label="Archived" variant="ghost" onPress={() => navigation.navigate('Archived')} />
      </View>
      {actionError ? (
        <View style={{ paddingHorizontal: space.xl, paddingBottom: space.md }}>
          <Text style={[type.caption, { color: c.textSecondary }]}>{actionError}</Text>
        </View>
      ) : null}
    </View>
  );

  if (paged.loading && displayItems.length === 0) {
    return (
      <View style={{ flex: 1, backgroundColor: c.surfacePage }}>
        {header}
        <LoadingBlock label="Loading messages…" />
      </View>
    );
  }

  if (paged.error && displayItems.length === 0) {
    return (
      <View style={{ flex: 1, backgroundColor: c.surfacePage }}>
        {header}
        <ErrorBlock message={paged.error} onRetry={paged.reload} />
      </View>
    );
  }

  return (
    <FlatList
      testID={testIds.inboxScreen}
      style={{ flex: 1, backgroundColor: c.surfacePage }}
      data={displayItems}
      keyExtractor={inboxKeyExtractor}
      ListHeaderComponent={header}
      ListEmptyComponent={<EmptyBlock message="You have no private messages yet." />}
      ListFooterComponent={<ListFooterLoading visible={paged.loadingMore} />}
      refreshControl={
        <RefreshControl
          refreshing={paged.refreshing || showingCacheOnly}
          onRefresh={paged.refresh}
          tintColor={c.accentPrimary}
        />
      }
      onEndReached={paged.loadMore}
      onEndReachedThreshold={0.4}
      renderItem={renderItem}
    />
  );
}

const InboxRow = memo(function InboxRow({
  item,
  archiving,
  onPress,
  onArchive,
}: {
  item: InboxConversation;
  archiving: boolean;
  onPress: (item: InboxConversation) => void;
  onArchive: (conversationId: string) => void;
}) {
  const { c } = useTheme();
  const unread = unreadBadgeLabel(item.unreadCount);
  return (
    <View style={{ borderTopWidth: 1, borderTopColor: c.hairline }}>
      <Pressable
        accessibilityRole="button"
        accessibilityLabel={inboxRowA11yLabel(item)}
        onPress={() => onPress(item)}
        style={({ pressed }) => [
          {
            paddingVertical: space.base,
            paddingHorizontal: space.xl,
            gap: 6,
            opacity: pressed ? 0.72 : 1,
          },
        ]}
      >
        <View style={{ flexDirection: 'row', alignItems: 'center', gap: 10 }}>
          <Text
            numberOfLines={1}
            style={[
              type.listTitle,
              { color: c.textPrimary, flex: 1 },
            ]}
          >
            {item.otherParticipantDisplayName}
          </Text>
          {unread ? (
            <View
              style={{
                backgroundColor: c.accentPrimary,
                borderRadius: radius.pill,
                paddingHorizontal: 8,
                paddingVertical: 3,
              }}
            >
              <Text style={[type.meta, { color: c.textOnAccent, letterSpacing: 0.4 }]}>{unread}</Text>
            </View>
          ) : null}
        </View>
        {item.lastMessagePreview ? (
          <Text numberOfLines={2} style={[type.caption, { color: c.textSecondary }]}>
            {item.lastMessagePreview}
          </Text>
        ) : null}
        <Text style={[type.meta, { color: c.textMuted }]}>{formatMessageTimestamp(item.lastMessageAt)}</Text>
      </Pressable>
      <View style={{ paddingHorizontal: space.xl, paddingBottom: space.md }}>
        <Button
          label="Archive"
          size="sm"
          variant="ghost"
          onPress={() => onArchive(item.conversationId)}
          loading={archiving}
        />
      </View>
    </View>
  );
});
