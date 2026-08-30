import { useCallback, useEffect, useLayoutEffect, useRef, useState } from 'react';
import {
  Alert,
  FlatList,
  KeyboardAvoidingView,
  Platform,
  Pressable,
  RefreshControl,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { Flag, MoreHorizontal } from 'lucide-react-native';
import { ApiError, isOfflineFailure, isTimeoutFailure } from '../../api/client';
import type { CacheSource } from '../../api';
import {
  archiveConversation,
  blockConversationParticipant,
  fetchConversationResult,
  replyToConversation,
  reportConversationMessage,
  type ConversationDetail,
  type ConversationMessage,
} from '../../api/messages';
import type { HomeStackParamList } from '../../navigation/types';
import { resolvePushMemberId } from '../../notifications/pushMemberId';
import {
  enqueueMessageReply,
  flushOfflineQueue,
  removeOfflineItem,
  updateOfflineItem,
  useOfflineQueue,
  type OfflineQueueItem,
} from '../../offlineQueue';
import { MemberGate } from '../../session/MemberGate';
import { useSession } from '../../session/SessionContext';
import { fonts, palette, radius, space, type, useTheme } from '../../theme';
import { Button } from '../../ui/Button';
import { IconButton } from '../../ui/IconButton';
import { ErrorBlock, LoadingBlock, OfflineBanner } from '../../ui/ScreenStates';
import { testIds } from '../../test/testIds';
import {
  buildThreadItems,
  conversationBodyMaxLength,
  conversationPageSize,
  formatDateDividerLabel,
  formatMessageClockTime,
  initialsFor,
  parseConversationId,
  reportReasonMaxLength,
  sendingBlockedNotice,
  validateReplyBody,
  validateReportReason,
  type ThreadListItem,
} from './inboxMeta';

type Props = NativeStackScreenProps<HomeStackParamList, 'Conversation'>;

type DisplayMessage = ConversationMessage & {
  queueState?: OfflineQueueItem['state'];
  queueError?: string | null;
};

function overlayQueuedMessages(
  detail: ConversationDetail | null,
  queueItems: OfflineQueueItem[],
  conversationId: string | null,
  memberId: string | null,
): DisplayMessage[] {
  const messages: DisplayMessage[] = [...(detail?.messages ?? [])];
  if (!conversationId || !memberId) {
    return messages;
  }
  const pending = queueItems.filter(
    (item) =>
      item.kind === 'message.reply' &&
      'conversationId' in item.target &&
      item.target.conversationId === conversationId,
  );
  for (const item of pending) {
    if (messages.some((message) => message.id === item.operationId)) {
      continue;
    }
    messages.push({
      id: item.operationId,
      senderMemberId: memberId,
      senderDisplayName: 'You',
      body: item.payload.body,
      createdAt: item.createdAt,
      isMine: true,
      sortKey: Number.MAX_SAFE_INTEGER,
      reportedByViewer: false,
      queueState: item.state,
      queueError: item.lastError,
    });
  }
  return messages;
}

function queueStatusLabel(state: OfflineQueueItem['state']): string {
  if (state === 'sending') {
    return 'Sending…';
  }
  if (state === 'needs_attention') {
    return 'Needs attention';
  }
  return 'Queued';
}

/**
 * Thread list sits one step darker than the header/composer chrome — a new
 * value from the redesign handoff (`design/design_handoff_private_messages`),
 * not yet a shared theme token.
 */
const threadListBackground = '#0C0C0C';
const outgoingBubbleBackground = '#171717';
const outgoingBubbleBorder = 'rgba(255,255,255,0.28)';
const dividerRule = 'rgba(255,255,255,0.14)';

function messageFromUnknownError(err: unknown): string {
  return err instanceof ApiError ? err.message : 'Something went wrong.';
}

function isStaleReadFailure(err: unknown): boolean {
  return isOfflineFailure(err) || isTimeoutFailure(err);
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
  const { accessToken, profile } = useSession();
  const listRef = useRef<FlatList<ThreadListItem<ConversationMessage>>>(null);
  const conversationId = parseConversationId(route.params.id);
  const memberId = accessToken ? resolvePushMemberId(accessToken, profile?.memberId) : null;
  const queueItems = useOfflineQueue(memberId);
  const [detail, setDetail] = useState<ConversationDetail | null>(null);
  const [source, setSource] = useState<CacheSource>('network');
  const [cachedAt, setCachedAt] = useState<string | null>(null);
  const detailRef = useRef<ConversationDetail | null>(null);
  detailRef.current = detail;
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
  const [archiving, setArchiving] = useState(false);
  const [archiveError, setArchiveError] = useState<string | null>(null);
  const [blocking, setBlocking] = useState(false);
  const [blockError, setBlockError] = useState<string | null>(null);

  const correspondentName = detail?.otherParticipantDisplayName ?? 'Conversation';

  const handleArchive = useCallback(async () => {
    if (!accessToken || !conversationId) {
      return;
    }
    setArchiveError(null);
    setArchiving(true);
    try {
      await archiveConversation(accessToken, conversationId);
      navigation.navigate('Inbox');
    } catch (err: unknown) {
      setArchiveError(messageFromUnknownError(err));
    } finally {
      setArchiving(false);
    }
  }, [accessToken, conversationId, navigation]);

  const confirmArchive = useCallback(() => {
    Alert.alert(
      'Archive conversation',
      `Archive your conversation with ${correspondentName}? You can find it later in Archived messages.`,
      [
        { text: 'Cancel', style: 'cancel' },
        { text: 'Archive', style: 'destructive', onPress: () => void handleArchive() },
      ],
    );
  }, [correspondentName, handleArchive]);

  const handleBlock = useCallback(async () => {
    if (!accessToken || !conversationId) {
      return;
    }
    setBlockError(null);
    setBlocking(true);
    try {
      await blockConversationParticipant(accessToken, conversationId);
      setReloadToken((n) => n + 1);
    } catch (err: unknown) {
      setBlockError(messageFromUnknownError(err));
    } finally {
      setBlocking(false);
    }
  }, [accessToken, conversationId]);

  const confirmBlock = useCallback(() => {
    Alert.alert(
      'Block member',
      `Block ${correspondentName}? They will no longer be able to send you private messages.`,
      [
        { text: 'Cancel', style: 'cancel' },
        { text: 'Block', style: 'destructive', onPress: () => void handleBlock() },
      ],
    );
  }, [correspondentName, handleBlock]);

  const openOverflowMenu = useCallback(() => {
    Alert.alert(correspondentName, undefined, [
      { text: 'Cancel', style: 'cancel' },
      { text: 'Archive conversation', onPress: confirmArchive },
      { text: 'Block member', style: 'destructive', onPress: confirmBlock },
    ]);
  }, [confirmArchive, confirmBlock, correspondentName]);

  useLayoutEffect(() => {
    navigation.setOptions({
      headerTitle: () => (
        <View style={styles.headerTitle}>
          <View style={[styles.avatar, { width: 28, height: 28, backgroundColor: palette.burgundy }]}>
            <Text style={styles.avatarLabel}>{initialsFor(correspondentName)}</Text>
          </View>
          <Text
            numberOfLines={1}
            style={[type.cardTitle, { color: c.textPrimary }]}
          >
            {correspondentName}
          </Text>
        </View>
      ),
      headerRight: () =>
        source === 'cache' ? null : (
          <IconButton
            icon={MoreHorizontal}
            accessibilityLabel="More options"
            onPress={openOverflowMenu}
          />
        ),
    });
  }, [c.textPrimary, correspondentName, navigation, openOverflowMenu, source]);

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
        const next = await fetchConversationResult(accessToken, conversationId, {
          pageSize: conversationPageSize,
          signal,
          memberId,
          networkOnly: mode === 'refresh',
        });
        if (signal.aborted) {
          return;
        }
        setDetail(next.data);
        setSource(next.source);
        setCachedAt(next.cachedAt);
        requestAnimationFrame(() => listRef.current?.scrollToEnd({ animated: false }));
      } catch (err: unknown) {
        if (signal.aborted || (err instanceof Error && err.name === 'AbortError')) {
          return;
        }
        if (mode === 'refresh' && detailRef.current && isStaleReadFailure(err)) {
          setSource('cache');
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
    [accessToken, conversationId, memberId],
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

    if (!memberId) {
      setSubmitError('Sign in to continue.');
      return;
    }

    setSubmitting(true);
    setSubmitError(null);
    try {
      const queued = await enqueueMessageReply({
        memberId,
        conversationId,
        body: draft.trim(),
      });
      void flushOfflineQueue();
      setDraft('');
      requestAnimationFrame(() => listRef.current?.scrollToEnd({ animated: true }));
      try {
        const next = await replyToConversation(
          accessToken,
          conversationId,
          queued.payload.body,
          undefined,
          queued.operationId,
        );
        await removeOfflineItem(queued.operationId);
        setDetail(next);
        setSource('network');
        setCachedAt(new Date().toISOString());
      } catch (err: unknown) {
        if (isOfflineFailure(err) || isTimeoutFailure(err)) {
          return;
        }
        throw err;
      }
    } catch (err: unknown) {
      setSubmitError(messageFromUnknownError(err));
    } finally {
      setSubmitting(false);
    }
  }, [accessToken, conversationId, draft, memberId]);

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

  const offlineSnapshot = source === 'cache';
  const canSendReply = detail?.canSendReply === true;
  const notice = detail
    ? sendingBlockedNotice(detail.hasBlockedOtherParticipant, detail.canSendReply === true)
    : null;
  const pendingMessages = overlayQueuedMessages(detail, queueItems, conversationId, memberId);
  const threadItems = buildThreadItems(pendingMessages);

  return (
    <KeyboardAvoidingView
      style={[styles.flex, { backgroundColor: c.surfacePage }]}
      behavior={Platform.OS === 'ios' ? 'padding' : undefined}
    >
      <FlatList
        ref={listRef}
        style={[styles.flex, { backgroundColor: threadListBackground }]}
        data={threadItems}
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
        ListHeaderComponent={
          offlineSnapshot ? (
            <OfflineBanner cachedAt={cachedAt} testID={testIds.offlineBanner} />
          ) : null
        }
        contentContainerStyle={styles.thread}
        renderItem={({ item }) =>
          item.kind === 'divider' ? (
            <DateDivider label={item.label} />
          ) : (
            <MessageBubble
              item={item.message as DisplayMessage}
              correspondentName={correspondentName}
              isFirstOfRun={item.isFirstOfRun}
              reporting={reportingMessageId === item.message.id}
              reportReason={reportReason}
              reportError={reportingMessageId === item.message.id ? reportError : null}
              reportBusy={reportBusy}
              interactionsEnabled={!offlineSnapshot && !(item.message as DisplayMessage).queueState}
              onStartReport={() => {
                setReportingMessageId(item.message.id);
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
                void submitReport(item.message.id);
              }}
            />
          )
        }
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
          <View style={styles.composerContext}>
            <Text style={[styles.attribution, { color: 'rgba(255,255,255,0.45)' }]} numberOfLines={1}>
              REPLYING TO {correspondentName.toUpperCase()}
            </Text>
            <Pressable
              accessibilityRole="button"
              accessibilityLabel="Archive conversation"
              accessibilityState={{ disabled: archiving || offlineSnapshot, busy: archiving }}
              disabled={archiving || offlineSnapshot}
              onPress={confirmArchive}
              hitSlop={8}
              style={[styles.archiveTap, archiving ? { opacity: 0.5 } : null]}
            >
              <Text style={[styles.attribution, { color: palette.gold }]}>
                {archiving ? 'ARCHIVING…' : 'ARCHIVE'}
              </Text>
            </Pressable>
          </View>
          <TextInput
            value={draft}
            onChangeText={setDraft}
            placeholder="Write a reply"
            placeholderTextColor="rgba(255,255,255,0.45)"
            accessibilityLabel="Reply"
            multiline
            textAlignVertical="top"
            enterKeyHint="send"
            autoCapitalize="sentences"
            maxLength={conversationBodyMaxLength}
            editable={!submitting}
            style={styles.field}
          />
          {submitError ? (
            <Text style={[type.caption, { color: c.textSecondary }]}>{submitError}</Text>
          ) : null}
          {archiveError ? (
            <Text style={[type.caption, { color: c.textSecondary }]}>{archiveError}</Text>
          ) : null}
          {blockError ? (
            <Text style={[type.caption, { color: c.textSecondary }]}>{blockError}</Text>
          ) : null}
          <Button
            label="Send reply"
            onPress={() => {
              void submit();
            }}
            loading={submitting}
            disabled={!accessToken || draft.trim().length === 0}
          />
        </View>
      ) : notice ? (
        <View
          style={[
            styles.notice,
            {
              borderTopColor: c.hairline,
              backgroundColor: c.surfacePage,
              paddingBottom: Math.max(insets.bottom, space.md),
            },
          ]}
        >
          <Text style={[type.body, { color: c.textSecondary }]}>{notice}</Text>
          {blockError ? (
            <Text style={[type.caption, { color: c.textSecondary }]}>{blockError}</Text>
          ) : null}
        </View>
      ) : null}
    </KeyboardAvoidingView>
  );
}

function DateDivider({ label }: { label: string }) {
  return (
    <View style={styles.dividerRow}>
      <View style={styles.dividerRule} />
      <Text style={[type.eyebrow, { color: 'rgba(255,255,255,0.5)' }]}>{label}</Text>
      <View style={styles.dividerRule} />
    </View>
  );
}

function MessageBubble({
  item,
  correspondentName,
  isFirstOfRun,
  reporting,
  reportReason,
  reportError,
  reportBusy,
  interactionsEnabled,
  onStartReport,
  onCancelReport,
  onChangeReason,
  onSubmitReport,
}: {
  item: DisplayMessage;
  correspondentName: string;
  isFirstOfRun: boolean;
  reporting: boolean;
  reportReason: string;
  reportError: string | null;
  reportBusy: boolean;
  interactionsEnabled: boolean;
  onStartReport: () => void;
  onCancelReport: () => void;
  onChangeReason: (value: string) => void;
  onSubmitReport: () => void;
}) {
  const time = formatMessageClockTime(item.createdAt);
  const attribution = item.isMine
    ? `YOU · ${time}`
    : isFirstOfRun
      ? `${item.senderDisplayName.toUpperCase()} · ${time}`
      : time;
  const attributionColor = item.isMine ? 'rgba(255,255,255,0.62)' : 'rgba(255,255,255,0.72)';

  if (item.isMine) {
    return (
      <View style={styles.outgoingRow}>
        <Text style={[styles.attribution, { color: attributionColor }]}>{attribution}</Text>
        <View style={[styles.bubble, styles.outgoingBubble]}>
          <Text style={styles.outgoingText}>{item.body}</Text>
        </View>
        {item.queueState ? (
          <Pressable
            accessibilityRole="button"
            accessibilityLabel={queueStatusLabel(item.queueState)}
            testID={testIds.pendingMessage}
            onPress={() => {
              if (item.queueState !== 'needs_attention') {
                return;
              }
              Alert.alert(item.queueError ?? 'This message could not be sent.', undefined, [
                { text: 'Dismiss', style: 'cancel' },
                {
                  text: 'Discard',
                  style: 'destructive',
                  onPress: () => {
                    void removeOfflineItem(item.id);
                  },
                },
                {
                  text: 'Retry',
                  onPress: () => {
                    void updateOfflineItem(item.id, {
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
            <Text style={[styles.attribution, { color: palette.gold }]}>
              {queueStatusLabel(item.queueState)}
            </Text>
          </Pressable>
        ) : null}
      </View>
    );
  }

  return (
    <View style={styles.incomingRow}>
      {isFirstOfRun ? (
        <View style={[styles.avatar, styles.incomingAvatar, { backgroundColor: palette.burgundy }]}>
          <Text style={styles.avatarLabelSmall}>{initialsFor(correspondentName)}</Text>
        </View>
      ) : (
        <View style={styles.avatarSpacer} />
      )}
      <View style={styles.incomingContent}>
        <Text style={[styles.attribution, { color: attributionColor }]}>{attribution}</Text>
        <View style={[styles.bubble, styles.incomingBubble]}>
          <Text style={styles.incomingText}>{item.body}</Text>
        </View>
        {item.reportedByViewer ? (
          <View style={styles.reportedRow}>
            <Flag size={11} strokeWidth={2} color={palette.gold} />
            <Text style={styles.reportedLabel}>REPORTED</Text>
          </View>
        ) : !interactionsEnabled ? null : reporting ? (
          <View style={styles.reportForm}>
            <TextInput
              value={reportReason}
              onChangeText={onChangeReason}
              placeholder="Optional reason"
              placeholderTextColor="rgba(255,255,255,0.45)"
              accessibilityLabel="Optional reason"
              maxLength={reportReasonMaxLength}
              editable={!reportBusy}
              style={styles.reportField}
            />
            {reportError ? (
              <Text style={[type.caption, { color: 'rgba(255,255,255,0.66)' }]}>{reportError}</Text>
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
          <Pressable
            accessibilityRole="button"
            accessibilityLabel="Report message"
            onPress={onStartReport}
            hitSlop={8}
          >
            <Text style={styles.reportTrigger}>Report message</Text>
          </Pressable>
        )}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  thread: { paddingHorizontal: space.base, paddingTop: space.lg, paddingBottom: space.sm, gap: 20 },
  headerTitle: { flexDirection: 'row', alignItems: 'center', gap: 9, maxWidth: 220 },
  avatar: { borderRadius: 999, alignItems: 'center', justifyContent: 'center' },
  avatarLabel: {
    fontFamily: fonts.titling,
    fontSize: 11,
    letterSpacing: 0.7,
    color: palette.white,
  },
  avatarLabelSmall: {
    fontFamily: fonts.titling,
    fontSize: 10,
    letterSpacing: 0.6,
    color: palette.white,
  },
  avatarSpacer: { width: 28, height: 28 },
  incomingAvatar: { width: 28, height: 28, marginTop: 18 },
  dividerRow: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  dividerRule: { flex: 1, height: 1, backgroundColor: dividerRule },
  attribution: {
    fontFamily: fonts.titling,
    fontSize: 9.5,
    letterSpacing: 1.9,
  },
  outgoingRow: { alignItems: 'flex-end', gap: 6 },
  incomingRow: { flexDirection: 'row', alignItems: 'flex-start', gap: 10 },
  incomingContent: { flex: 1, maxWidth: '80%', alignItems: 'flex-start', gap: 6 },
  bubble: { borderRadius: radius.md, paddingHorizontal: 14, paddingVertical: 12, maxWidth: '80%' },
  outgoingBubble: { backgroundColor: outgoingBubbleBackground, borderWidth: 1, borderColor: outgoingBubbleBorder },
  incomingBubble: { backgroundColor: palette.warmWhite, alignSelf: 'stretch', maxWidth: undefined },
  outgoingText: { fontFamily: fonts.body, fontSize: 16.5, lineHeight: 24.75, color: palette.white },
  incomingText: { fontFamily: fonts.body, fontSize: 16.5, lineHeight: 24.75, color: palette.charcoal },
  reportedRow: { flexDirection: 'row', alignItems: 'center', gap: 6, paddingLeft: 2 },
  reportedLabel: { fontFamily: fonts.titling, fontSize: 9, letterSpacing: 1.6, color: palette.gold },
  reportTrigger: {
    fontFamily: fonts.body,
    fontSize: 13,
    color: 'rgba(255,255,255,0.5)',
    textDecorationLine: 'underline',
  },
  reportForm: { alignSelf: 'stretch', gap: space.sm },
  reportField: {
    minHeight: 40,
    borderWidth: 1,
    borderColor: 'rgba(255,255,255,0.2)',
    borderRadius: radius.md,
    paddingHorizontal: 12,
    paddingVertical: 8,
    fontFamily: fonts.body,
    fontSize: 16,
    color: palette.white,
  },
  reportActions: { flexDirection: 'row', flexWrap: 'wrap', gap: space.sm },
  composer: { paddingHorizontal: space.base, paddingTop: space.md, borderTopWidth: StyleSheet.hairlineWidth, gap: 10 },
  composerContext: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  archiveTap: { minHeight: 44, justifyContent: 'center' },
  notice: { paddingHorizontal: space.base, paddingTop: space.md, borderTopWidth: StyleSheet.hairlineWidth },
  field: {
    minHeight: 44,
    maxHeight: 120,
    borderWidth: 1,
    borderColor: 'rgba(255,255,255,0.2)',
    backgroundColor: 'rgba(255,255,255,0.06)',
    borderRadius: radius.md,
    paddingHorizontal: 14,
    paddingVertical: 12,
    fontFamily: fonts.body,
    fontSize: 16,
    color: palette.white,
  },
});
