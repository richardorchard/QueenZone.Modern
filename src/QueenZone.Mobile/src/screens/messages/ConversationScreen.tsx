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
import { ApiError } from '../../api/client';
import {
  archiveConversation,
  blockConversationParticipant,
  fetchConversation,
  replyToConversation,
  reportConversationMessage,
  type ConversationDetail,
  type ConversationMessage,
} from '../../api/messages';
import { getMessagesCache } from '../../cache/messagesCache';
import type { HomeStackParamList } from '../../navigation/types';
import { MemberGate } from '../../session/MemberGate';
import { useSession } from '../../session/SessionContext';
import { fonts, palette, radius, space, type, useTheme } from '../../theme';
import { Button } from '../../ui/Button';
import { IconButton } from '../../ui/IconButton';
import { ErrorBlock, LoadingBlock } from '../../ui/ScreenStates';
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
  const cacheKey = profile && conversationId ? `conversation:${profile.memberId}:${conversationId}` : null;
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
      headerRight: () => (
        <IconButton
          icon={MoreHorizontal}
          accessibilityLabel="More options"
          onPress={openOverflowMenu}
        />
      ),
    });
  }, [c.textPrimary, correspondentName, navigation, openOverflowMenu]);

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
        requestAnimationFrame(() => listRef.current?.scrollToEnd({ animated: false }));
        if (cacheKey) {
          void getMessagesCache().put(cacheKey, next).catch(() => {});
        }
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
    [accessToken, conversationId, cacheKey],
  );

  // Render the last-seen version of this conversation immediately (e.g. from a
  // push-notification tap) while the fresh fetch below runs in the background.
  useEffect(() => {
    if (!cacheKey) {
      return;
    }
    let cancelled = false;
    getMessagesCache()
      .get<ConversationDetail>(cacheKey)
      .then((cached) => {
        if (!cancelled && cached) {
          setDetail((current) => current ?? cached);
        }
      })
      .catch(() => {});
    return () => {
      cancelled = true;
    };
  }, [cacheKey]);

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
  const notice = detail
    ? sendingBlockedNotice(detail.hasBlockedOtherParticipant, canSendReply)
    : null;
  const threadItems = buildThreadItems(detail?.messages ?? []);

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
        contentContainerStyle={styles.thread}
        renderItem={({ item }) =>
          item.kind === 'divider' ? (
            <DateDivider label={item.label} />
          ) : (
            <MessageBubble
              item={item.message}
              correspondentName={correspondentName}
              isFirstOfRun={item.isFirstOfRun}
              reporting={reportingMessageId === item.message.id}
              reportReason={reportReason}
              reportError={reportingMessageId === item.message.id ? reportError : null}
              reportBusy={reportBusy}
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
              accessibilityState={{ disabled: archiving, busy: archiving }}
              disabled={archiving}
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
  onStartReport,
  onCancelReport,
  onChangeReason,
  onSubmitReport,
}: {
  item: ConversationMessage;
  correspondentName: string;
  isFirstOfRun: boolean;
  reporting: boolean;
  reportReason: string;
  reportError: string | null;
  reportBusy: boolean;
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
        ) : reporting ? (
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
