import { useCallback, useLayoutEffect, useMemo, useRef } from 'react';
import {
  Alert,
  FlatList,
  KeyboardAvoidingView,
  Platform,
  RefreshControl,
  StyleSheet,
  Text,
  View,
  type ListRenderItem,
} from 'react-native';
import { useHeaderHeight } from '@react-navigation/elements';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { MoreHorizontal } from 'lucide-react-native';
import { resolvePushMemberId } from '../../notifications/pushMemberId';
import { useOfflineQueue } from '../../offlineQueue';
import { MemberGate } from '../../session/MemberGate';
import { useSession } from '../../session/SessionContext';
import { fonts, palette, space, type, useTheme } from '../../theme';
import { IconButton } from '../../ui/IconButton';
import { ErrorBlock, LoadingBlock, OfflineBanner } from '../../ui/ScreenStates';
import { testIds } from '../../test/testIds';
import {
  buildThreadItems,
  initialsFor,
  parseConversationId,
  sendingBlockedNotice,
  type ThreadListItem,
} from './inboxMeta';
import { overlayQueuedMessages, type DisplayMessage } from './conversationMeta';
import { ConversationComposer } from './ConversationComposer';
import { DateDivider } from './DateDivider';
import { MessageBubble } from './MessageBubble';
import { useConversation } from './useConversation';
import type { HomeStackParamList } from '../../navigation/types';

export { messageBubbleRenderProbe } from './MessageBubble';

type Props = NativeStackScreenProps<HomeStackParamList, 'Conversation'>;

function threadKeyExtractor(item: ThreadListItem<DisplayMessage>): string {
  return item.id;
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
  const headerHeight = useHeaderHeight();
  const { accessToken, profile } = useSession();
  const listRef = useRef<FlatList<ThreadListItem<DisplayMessage>>>(null);
  const conversationId = parseConversationId(route.params.id);
  const memberId = accessToken ? resolvePushMemberId(accessToken, profile?.memberId) : null;
  const queueItems = useOfflineQueue(memberId);

  const scrollToEnd = useCallback(() => {
    listRef.current?.scrollToEnd({ animated: false });
  }, []);
  const onArchived = useCallback(() => {
    navigation.navigate('Inbox');
  }, [navigation]);

  const conversation = useConversation(conversationId, accessToken, memberId, {
    scrollToEnd,
    onArchived,
  });
  const { detail, source, cachedAt, error, loading, refreshing } = conversation;

  const correspondentName = detail?.otherParticipantDisplayName ?? 'Conversation';
  const offlineSnapshot = source === 'cache';

  const confirmArchive = useCallback(() => {
    Alert.alert(
      'Archive conversation',
      `Archive your conversation with ${correspondentName}? You can find it later in Archived messages.`,
      [
        { text: 'Cancel', style: 'cancel' },
        { text: 'Archive', style: 'destructive', onPress: () => void conversation.archive() },
      ],
    );
  }, [conversation, correspondentName]);

  const confirmBlock = useCallback(() => {
    Alert.alert(
      'Block member',
      `Block ${correspondentName}? They will no longer be able to send you private messages.`,
      [
        { text: 'Cancel', style: 'cancel' },
        { text: 'Block', style: 'destructive', onPress: () => void conversation.block() },
      ],
    );
  }, [conversation, correspondentName]);

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

  const pendingMessages = useMemo(
    () => overlayQueuedMessages(detail, queueItems, conversationId, memberId),
    [conversationId, detail, memberId, queueItems],
  );
  const threadItems = useMemo(() => buildThreadItems(pendingMessages), [pendingMessages]);

  const renderItem = useCallback<ListRenderItem<ThreadListItem<DisplayMessage>>>(
    ({ item }) =>
      item.kind === 'divider' ? (
        <DateDivider label={item.label} />
      ) : (
        <MessageBubble
          item={item.message}
          correspondentName={correspondentName}
          isFirstOfRun={item.isFirstOfRun}
          interactionsEnabled={!offlineSnapshot && !item.message.queueState}
          onSubmitReport={conversation.submitReport}
        />
      ),
    [conversation.submitReport, correspondentName, offlineSnapshot],
  );

  if (loading && !detail) {
    return <LoadingBlock label="Loading conversation…" />;
  }

  if (error && !detail) {
    return <ErrorBlock message={error} onRetry={conversation.reload} />;
  }

  const canSendReply = detail?.canSendReply === true;
  const notice = detail
    ? sendingBlockedNotice(detail.hasBlockedOtherParticipant, detail.canSendReply === true)
    : null;

  return (
    <KeyboardAvoidingView
      style={[styles.flex, { backgroundColor: c.surfacePage }]}
      behavior={Platform.OS === 'ios' ? 'padding' : undefined}
      keyboardVerticalOffset={Platform.OS === 'ios' ? headerHeight : 0}
    >
      <FlatList
        ref={listRef}
        style={[styles.flex, { backgroundColor: threadListBackground }]}
        data={threadItems}
        keyExtractor={threadKeyExtractor}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={conversation.refresh}
            tintColor={c.accentPrimary}
          />
        }
        ListHeaderComponent={
          offlineSnapshot ? (
            <OfflineBanner cachedAt={cachedAt} testID={testIds.offlineBanner} />
          ) : null
        }
        contentContainerStyle={styles.thread}
        renderItem={renderItem}
      />
      {canSendReply ? (
        <ConversationComposer
          correspondentName={correspondentName}
          canSend={Boolean(accessToken)}
          archiving={conversation.archiving}
          archiveDisabled={offlineSnapshot}
          archiveError={conversation.archiveError}
          blockError={conversation.blockError}
          onArchive={confirmArchive}
          onSend={conversation.sendReply}
        />
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
          {conversation.blockError ? (
            <Text style={[type.caption, { color: c.textSecondary }]}>{conversation.blockError}</Text>
          ) : null}
        </View>
      ) : null}
    </KeyboardAvoidingView>
  );
}

/**
 * Thread list sits one step darker than the header/composer chrome — a new
 * value from the redesign handoff (`design/design_handoff_private_messages`),
 * not yet a shared theme token.
 */
const threadListBackground = '#0C0C0C';

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
  notice: { paddingHorizontal: space.base, paddingTop: space.md, borderTopWidth: StyleSheet.hairlineWidth },
});
