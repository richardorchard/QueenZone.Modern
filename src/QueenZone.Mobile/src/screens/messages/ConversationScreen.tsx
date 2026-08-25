import { useCallback, useEffect, useLayoutEffect, useRef, useState } from 'react';
import {
  FlatList,
  KeyboardAvoidingView,
  Platform,
  RefreshControl,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { ApiError } from '../../api/client';
import {
  fetchConversation,
  replyToConversation,
  reportConversationMessage,
  type ConversationDetail,
  type ConversationMessage,
} from '../../api/messages';
import type { HomeStackParamList } from '../../navigation/types';
import { MemberGate } from '../../session/MemberGate';
import { useSession } from '../../session/SessionContext';
import { fonts, radius, space, type, useTheme } from '../../theme';
import { Button } from '../../ui/Button';
import { ErrorBlock, LoadingBlock } from '../../ui/ScreenStates';
import {
  conversationBodyMaxLength,
  conversationPageSize,
  formatMessageTimestamp,
  parseConversationId,
  reportReasonMaxLength,
  unableToSendMessage,
  validateReplyBody,
  validateReportReason,
} from './inboxMeta';

type Props = NativeStackScreenProps<HomeStackParamList, 'Conversation'>;

function messageFromUnknownError(err: unknown): string {
  return err instanceof ApiError ? err.message : 'Something went wrong.';
}

export function ConversationScreen({ navigation, route }: Props) {
  return (
    <MemberGate title="Conversation">
      <ConversationThread navigation={navigation} route={route} />
    </MemberGate>
  );
}

function ConversationThread({ navigation, route }: Props) {
  const { c } = useTheme();
  const insets = useSafeAreaInsets();
  const { accessToken } = useSession();
  const listRef = useRef<FlatList<ConversationMessage>>(null);
  const conversationId = parseConversationId(route.params.id);
  const [detail, setDetail] = useState<ConversationDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [reloadToken, setReloadToken] = useState(0);
  const [draft, setDraft] = useState('');
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [reportingMessageId, setReportingMessageId] = useState<string | null>(null);
  const [reportReason, setReportReason] = useState('');
  const [reportError, setReportError] = useState<string | null>(null);
  const [reportBusy, setReportBusy] = useState(false);

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
        setError(messageFromUnknownError(err));
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

  const submit = useCallback(async () => {
    const validation = validateReplyBody(draft);
    if (validation) {
      setSubmitError(validation);
      return;
    }
    if (!accessToken || !conversationId) {
      setSubmitError('Sign in to continue.');
      return;
    }

    setSubmitting(true);
    setSubmitError(null);
    try {
      const next = await replyToConversation(accessToken, conversationId, draft.trim());
      setDetail(next);
      setDraft('');
      requestAnimationFrame(() => listRef.current?.scrollToEnd({ animated: true }));
    } catch (err: unknown) {
      setSubmitError(messageFromUnknownError(err));
    } finally {
      setSubmitting(false);
    }
  }, [accessToken, conversationId, draft]);

  const submitReport = useCallback(
    async (messageId: string) => {
      const validation = validateReportReason(reportReason);
      if (validation) {
        setReportError(validation);
        return;
      }
      if (!accessToken || !conversationId) {
        setReportError('Sign in to continue.');
        return;
      }

      setReportBusy(true);
      setReportError(null);
      try {
        await reportConversationMessage(
          accessToken,
          conversationId,
          messageId,
          reportReason.trim() || undefined,
        );
        setDetail((current) =>
          current
            ? {
                ...current,
                messages: current.messages.map((item) =>
                  item.id === messageId ? { ...item, reportedByViewer: true } : item,
                ),
              }
            : current,
        );
        setReportingMessageId(null);
        setReportReason('');
      } catch (err: unknown) {
        setReportError(messageFromUnknownError(err));
      } finally {
        setReportBusy(false);
      }
    },
    [accessToken, conversationId, reportReason],
  );

  if (loading && !detail) {
    return <LoadingBlock label="Loading conversation…" />;
  }

  if (error && !detail) {
    return <ErrorBlock message={error} onRetry={() => setReloadToken((n) => n + 1)} />;
  }

  const canSendReply = detail?.canSendReply === true;

  return (
    <KeyboardAvoidingView
      style={[styles.flex, { backgroundColor: c.surfacePage }]}
      behavior={Platform.OS === 'ios' ? 'padding' : undefined}
    >
      <FlatList
        ref={listRef}
        style={styles.flex}
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
        contentContainerStyle={styles.thread}
        renderItem={({ item }) => (
          <MessageBubble
            item={item}
            reporting={reportingMessageId === item.id}
            reportReason={reportReason}
            reportError={reportingMessageId === item.id ? reportError : null}
            reportBusy={reportBusy}
            onStartReport={() => {
              setReportingMessageId(item.id);
              setReportReason('');
              setReportError(null);
            }}
            onCancelReport={() => {
              setReportingMessageId(null);
              setReportReason('');
              setReportError(null);
            }}
            onChangeReason={setReportReason}
            onSubmitReport={() => {
              void submitReport(item.id);
            }}
          />
        )}
      />
      {canSendReply ? (
        <View
          style={[
            styles.composer,
            {
              borderTopColor: c.hairline,
              backgroundColor: c.surfacePage,
              paddingBottom: Math.max(insets.bottom, space.md),
            },
          ]}
        >
          <TextInput
            value={draft}
            onChangeText={setDraft}
            placeholder="Write a reply"
            placeholderTextColor={c.textMuted}
            accessibilityLabel="Reply"
            multiline
            textAlignVertical="top"
            enterKeyHint="send"
            autoCapitalize="sentences"
            maxLength={conversationBodyMaxLength}
            editable={!submitting}
            style={[
              styles.field,
              {
                color: c.textPrimary,
                borderColor: c.border,
                backgroundColor: c.surfaceCard,
              },
            ]}
          />
          {submitError ? (
            <Text style={[type.caption, { color: c.textSecondary }]}>{submitError}</Text>
          ) : null}
          <Button
            label="Send reply"
            onPress={() => {
              void submit();
            }}
            loading={submitting}
            disabled={!accessToken}
          />
        </View>
      ) : detail ? (
        <View
          style={[
            styles.notice,
            {
              borderTopColor: c.hairline,
              paddingBottom: Math.max(insets.bottom, space.md),
            },
          ]}
        >
          <Text style={[type.body, { color: c.textSecondary }]}>{unableToSendMessage}</Text>
        </View>
      ) : null}
    </KeyboardAvoidingView>
  );
}

function MessageBubble({
  item,
  reporting,
  reportReason,
  reportError,
  reportBusy,
  onStartReport,
  onCancelReport,
  onChangeReason,
  onSubmitReport,
}: {
  item: ConversationMessage;
  reporting: boolean;
  reportReason: string;
  reportError: string | null;
  reportBusy: boolean;
  onStartReport: () => void;
  onCancelReport: () => void;
  onChangeReason: (value: string) => void;
  onSubmitReport: () => void;
}) {
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
      {!item.isMine ? (
        item.reportedByViewer ? (
          <Text style={[type.caption, { color: c.textMuted }]}>Reported</Text>
        ) : reporting ? (
          <View style={{ alignSelf: 'stretch', gap: space.sm }}>
            <TextInput
              value={reportReason}
              onChangeText={onChangeReason}
              placeholder="Optional reason"
              placeholderTextColor={c.textMuted}
              accessibilityLabel="Optional reason"
              maxLength={reportReasonMaxLength}
              editable={!reportBusy}
              style={[
                styles.reportField,
                {
                  color: c.textPrimary,
                  borderColor: c.border,
                  backgroundColor: c.surfaceCard,
                },
              ]}
            />
            {reportError ? (
              <Text style={[type.caption, { color: c.textSecondary }]}>{reportError}</Text>
            ) : null}
            <View style={styles.reportActions}>
              <Button
                label="Submit report"
                size="sm"
                onPress={onSubmitReport}
                loading={reportBusy}
              />
              <Button label="Cancel" size="sm" variant="ghost" onPress={onCancelReport} disabled={reportBusy} />
            </View>
          </View>
        ) : (
          <Button label="Report message" size="sm" variant="ghost" onPress={onStartReport} />
        )
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  thread: { paddingBottom: space.sm },
  composer: {
    paddingHorizontal: space.xl,
    paddingTop: space.md,
    borderTopWidth: StyleSheet.hairlineWidth,
    gap: space.sm,
  },
  notice: {
    paddingHorizontal: space.xl,
    paddingTop: space.md,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  field: {
    minHeight: 88,
    borderWidth: 1,
    borderRadius: radius.xs,
    paddingHorizontal: 12,
    paddingTop: 12,
    fontFamily: fonts.body,
    fontSize: 16,
  },
  reportField: {
    minHeight: 40,
    borderWidth: 1,
    borderRadius: radius.xs,
    paddingHorizontal: 12,
    paddingVertical: 8,
    fontFamily: fonts.body,
    fontSize: 16,
  },
  reportActions: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: space.sm,
  },
});
