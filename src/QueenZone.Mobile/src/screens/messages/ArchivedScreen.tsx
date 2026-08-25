import { useCallback, useRef, useState } from 'react';
import { FlatList, Pressable, RefreshControl, Text, View } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { fetchArchivedInbox, unarchiveConversation, type InboxConversation } from '../../api/messages';
import { usePagedContent } from '../../hooks/usePagedContent';
import type { HomeStackParamList } from '../../navigation/types';
import { MemberGate } from '../../session/MemberGate';
import { useSession } from '../../session/SessionContext';
import { space, type, useTheme } from '../../theme';
import { Button } from '../../ui/Button';
import { PageTitleBlock } from '../../ui/PageTitleBlock';
import { EmptyBlock, ErrorBlock, ListFooterLoading, LoadingBlock } from '../../ui/ScreenStates';
import { testIds } from '../../test/testIds';
import { formatMessageTimestamp, inboxPageSize, inboxRowA11yLabel, unreadBadgeLabel } from './inboxMeta';

type Props = NativeStackScreenProps<HomeStackParamList, 'Archived'>;

export function ArchivedScreen({ navigation }: Props) {
  return (
    <MemberGate title="Archived messages">
      <ArchivedList navigation={navigation} />
    </MemberGate>
  );
}

function ArchivedList({ navigation }: Pick<Props, 'navigation'>) {
  const { c } = useTheme();
  const { accessToken } = useSession();
  const skipNextFocusRefresh = useRef(true);
  const [unarchivingId, setUnarchivingId] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
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
        return fetchArchivedInbox(accessToken, { page, pageSize: inboxPageSize, signal });
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

  const handleUnarchive = useCallback(
    async (conversationId: string) => {
      if (!accessToken) {
        return;
      }
      setActionError(null);
      setUnarchivingId(conversationId);
      try {
        await unarchiveConversation(accessToken, conversationId);
        paged.refresh();
      } catch {
        setActionError('Unable to unarchive this conversation. Try again.');
      } finally {
        setUnarchivingId(null);
      }
    },
    [accessToken, paged.refresh],
  );

  const header = (
    <View>
      <PageTitleBlock
        eyebrow="Community"
        title="Archived messages"
        subtitle="Conversations you have archived."
      />
      {actionError ? (
        <View style={{ paddingHorizontal: space.xl, paddingBottom: space.md }}>
          <Text style={[type.caption, { color: c.textSecondary }]}>{actionError}</Text>
        </View>
      ) : null}
    </View>
  );

  if (paged.loading && paged.items.length === 0) {
    return (
      <View style={{ flex: 1, backgroundColor: c.surfacePage }}>
        {header}
        <LoadingBlock label="Loading archived messages…" />
      </View>
    );
  }

  if (paged.error && paged.items.length === 0) {
    return (
      <View style={{ flex: 1, backgroundColor: c.surfacePage }}>
        {header}
        <ErrorBlock message={paged.error} onRetry={paged.reload} />
      </View>
    );
  }

  return (
    <FlatList
      testID={testIds.archivedScreen}
      style={{ flex: 1, backgroundColor: c.surfacePage }}
      data={paged.items}
      keyExtractor={(item) => item.conversationId}
      ListHeaderComponent={header}
      ListEmptyComponent={<EmptyBlock message="You have no archived conversations." />}
      ListFooterComponent={<ListFooterLoading visible={paged.loadingMore} />}
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
        <ArchivedRow
          item={item}
          unarchiving={unarchivingId === item.conversationId}
          onPress={() => navigation.navigate('Conversation', { id: item.conversationId })}
          onUnarchive={() => {
            void handleUnarchive(item.conversationId);
          }}
        />
      )}
    />
  );
}

function ArchivedRow({
  item,
  unarchiving,
  onPress,
  onUnarchive,
}: {
  item: InboxConversation;
  unarchiving: boolean;
  onPress: () => void;
  onUnarchive: () => void;
}) {
  const { c } = useTheme();
  const unread = unreadBadgeLabel(item.unreadCount);
  return (
    <View style={{ borderTopWidth: 1, borderTopColor: c.hairline }}>
      <Pressable
        accessibilityRole="button"
        accessibilityLabel={inboxRowA11yLabel(item)}
        onPress={onPress}
        style={({ pressed }) => [
          { paddingVertical: space.base, paddingHorizontal: space.xl, gap: 6, opacity: pressed ? 0.72 : 1 },
        ]}
      >
        <Text numberOfLines={1} style={[type.listTitle, { color: c.textPrimary }]}>
          {item.otherParticipantDisplayName}
        </Text>
        {item.lastMessagePreview ? (
          <Text numberOfLines={2} style={[type.caption, { color: c.textSecondary }]}>
            {item.lastMessagePreview}
          </Text>
        ) : null}
        <Text style={[type.meta, { color: c.textMuted }]}>
          {formatMessageTimestamp(item.lastMessageAt)}
          {unread ? ` · ${unread}` : ''}
        </Text>
      </Pressable>
      <View style={{ paddingHorizontal: space.xl, paddingBottom: space.md }}>
        <Button label="Unarchive" size="sm" variant="ghost" onPress={onUnarchive} loading={unarchiving} />
      </View>
    </View>
  );
}
