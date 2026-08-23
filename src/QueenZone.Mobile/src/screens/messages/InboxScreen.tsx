import { useCallback, useRef } from 'react';
import { FlatList, Pressable, RefreshControl, Text, View } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { fetchInbox, type InboxConversation } from '../../api/messages';
import { usePagedContent } from '../../hooks/usePagedContent';
import type { HomeStackParamList } from '../../navigation/types';
import { MemberGate } from '../../session/MemberGate';
import { useSession } from '../../session/SessionContext';
import { radius, space, type, useTheme } from '../../theme';
import { Button } from '../../ui/Button';
import { PageTitleBlock } from '../../ui/PageTitleBlock';
import { EmptyBlock, ErrorBlock, ListFooterLoading, LoadingBlock } from '../../ui/ScreenStates';
import {
  formatMessageTimestamp,
  inboxPageSize,
  inboxRowA11yLabel,
  unreadBadgeLabel,
} from './inboxMeta';

type Props = NativeStackScreenProps<HomeStackParamList, 'Inbox'>;

export function InboxScreen({ navigation }: Props) {
  return (
    <MemberGate title="Messages">
      <InboxList navigation={navigation} />
    </MemberGate>
  );
}

function InboxList({ navigation }: Pick<Props, 'navigation'>) {
  const { c } = useTheme();
  const { accessToken } = useSession();
  const skipNextFocusRefresh = useRef(true);
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

  const header = (
    <View>
      <PageTitleBlock
        eyebrow="Community"
        title="Messages"
        subtitle="Private conversations with other members."
      />
      <View style={{ paddingHorizontal: space.xl, paddingBottom: space.lg }}>
        <Button label="New message" onPress={() => navigation.navigate('ComposeMessage')} />
      </View>
    </View>
  );

  if (paged.loading && paged.items.length === 0) {
    return (
      <View style={{ flex: 1, backgroundColor: c.surfacePage }}>
        {header}
        <LoadingBlock label="Loading messages…" />
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
      style={{ flex: 1, backgroundColor: c.surfacePage }}
      data={paged.items}
      keyExtractor={(item) => item.conversationId}
      ListHeaderComponent={header}
      ListEmptyComponent={<EmptyBlock message="You have no private messages yet." />}
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
        <InboxRow
          item={item}
          onPress={() => navigation.navigate('Conversation', { id: item.conversationId })}
        />
      )}
    />
  );
}

function InboxRow({ item, onPress }: { item: InboxConversation; onPress: () => void }) {
  const { c } = useTheme();
  const unread = unreadBadgeLabel(item.unreadCount);
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={inboxRowA11yLabel(item)}
      onPress={onPress}
      style={({ pressed }) => [
        {
          paddingVertical: space.base,
          paddingHorizontal: space.xl,
          borderTopWidth: 1,
          borderTopColor: c.hairline,
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
  );
}
