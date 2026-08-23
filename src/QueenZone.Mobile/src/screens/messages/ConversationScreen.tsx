import { useCallback, useEffect, useLayoutEffect, useState } from 'react';
import { FlatList, RefreshControl, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { ApiError } from '../../api/client';
import { fetchConversation, type ConversationDetail, type ConversationMessage } from '../../api/messages';
import type { HomeStackParamList } from '../../navigation/types';
import { MemberGate } from '../../session/MemberGate';
import { useSession } from '../../session/SessionContext';
import { space, type, useTheme } from '../../theme';
import { ErrorBlock, LoadingBlock } from '../../ui/ScreenStates';
import { conversationPageSize, formatMessageTimestamp, parseConversationId } from './inboxMeta';

type Props = NativeStackScreenProps<HomeStackParamList, 'Conversation'>;

export function ConversationScreen({ navigation, route }: Props) {
  return (
    <MemberGate title="Conversation">
      <ConversationThread navigation={navigation} route={route} />
    </MemberGate>
  );
}

function ConversationThread({ navigation, route }: Props) {
  const { c } = useTheme();
  const { accessToken } = useSession();
  const conversationId = parseConversationId(route.params.id);
  const [detail, setDetail] = useState<ConversationDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [reloadToken, setReloadToken] = useState(0);

  useLayoutEffect(() => {
    navigation.setOptions({
      title: detail?.otherParticipantDisplayName ?? 'Conversation',
    });
  }, [detail?.otherParticipantDisplayName, navigation]);

  const load = useCallback(
    async (signal: AbortSignal, mode: 'initial' | 'refresh') => {
      if (!accessToken || !conversationId) {
        setDetail(null);
        setError(conversationId ? 'Sign in to continue.' : 'This conversation is not available.');
        setLoading(false);
        setRefreshing(false);
        return;
      }

      if (mode === 'initial') {
        setLoading(true);
      } else {
        setRefreshing(true);
      }
      setError(null);

      try {
        const next = await fetchConversation(accessToken, conversationId, {
          pageSize: conversationPageSize,
          signal,
        });
        if (signal.aborted) {
          return;
        }
        setDetail(next);
      } catch (err: unknown) {
        if (signal.aborted || (err instanceof Error && err.name === 'AbortError')) {
          return;
        }
        setDetail(null);
        setError(err instanceof ApiError ? err.message : 'Something went wrong.');
      } finally {
        if (!signal.aborted) {
          setLoading(false);
          setRefreshing(false);
        }
      }
    },
    [accessToken, conversationId],
  );

  useEffect(() => {
    const controller = new AbortController();
    void load(controller.signal, 'initial');
    return () => controller.abort();
  }, [load, reloadToken]);

  if (loading && !detail) {
    return <LoadingBlock label="Loading conversation…" />;
  }

  if (error && !detail) {
    return <ErrorBlock message={error} onRetry={() => setReloadToken((n) => n + 1)} />;
  }

  return (
    <FlatList
      style={{ flex: 1, backgroundColor: c.surfacePage }}
      data={detail?.messages ?? []}
      keyExtractor={(item) => item.id}
      refreshControl={
        <RefreshControl
          refreshing={refreshing}
          onRefresh={() => {
            const controller = new AbortController();
            void load(controller.signal, 'refresh');
          }}
          tintColor={c.accentPrimary}
        />
      }
      contentContainerStyle={{ paddingBottom: space.section }}
      renderItem={({ item }) => <MessageBubble item={item} />}
    />
  );
}

function MessageBubble({ item }: { item: ConversationMessage }) {
  const { c } = useTheme();
  const stamp = formatMessageTimestamp(item.createdAt);
  return (
    <View
      style={{
        paddingHorizontal: space.xl,
        paddingVertical: space.base,
        borderTopWidth: 1,
        borderTopColor: c.hairline,
        alignItems: item.isMine ? 'flex-end' : 'flex-start',
        gap: 6,
      }}
    >
      <Text style={[type.meta, { color: c.textMuted }]}>
        {item.senderDisplayName}
        {stamp ? ` · ${stamp}` : ''}
      </Text>
      <Text
        style={[
          type.body,
          {
            color: c.textPrimary,
            textAlign: item.isMine ? 'right' : 'left',
          },
        ]}
      >
        {item.body}
      </Text>
    </View>
  );
}
