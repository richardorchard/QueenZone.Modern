import { useCallback, useMemo, useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import type { ForumPoll } from '../../api';
import { radius, space, type, useTheme } from '../../theme';
import { Button } from '../../ui/Button';
import {
  canCastPollVote,
  formatPollResultMeta,
  formatPollStatus,
  pollAuthPrompt,
  pollTokenRequiredMessage,
} from './forumPollMeta';

type Props = {
  poll: ForumPoll;
  isSignedIn: boolean;
  hasAccessToken: boolean;
  busy: boolean;
  error: string | null;
  onVote: (optionIds: string[]) => void;
  onClose: () => void;
  onSignIn: () => void;
};

export function ForumPollCard({
  poll,
  isSignedIn,
  hasAccessToken,
  busy,
  error,
  onVote,
  onClose,
  onSignIn,
}: Props) {
  const { c } = useTheme();
  const [selected, setSelected] = useState<string[]>([]);
  const canVote = canCastPollVote({ canViewerVote: poll.canViewerVote, hasAccessToken });
  const prompt = pollAuthPrompt({
    canViewerVote: poll.canViewerVote,
    viewerHasVoted: poll.viewerHasVoted,
    isClosed: poll.isClosed,
    isSignedIn,
    hasAccessToken,
  });
  const maxChoices = poll.isMultiChoice
    ? poll.maxChoices != null && poll.maxChoices > 0
      ? poll.maxChoices
      : poll.options.length
    : 1;

  const toggle = useCallback(
    (optionId: string) => {
      setSelected((current) => {
        if (current.includes(optionId)) {
          return current.filter((id) => id !== optionId);
        }
        if (!poll.isMultiChoice) {
          return [optionId];
        }
        if (current.length >= maxChoices) {
          return current;
        }
        return [...current, optionId];
      });
    },
    [maxChoices, poll.isMultiChoice],
  );

  const voteDisabled = busy || selected.length === 0 || !canVote;
  const status = useMemo(() => formatPollStatus(poll), [poll]);

  return (
    <View
      style={[styles.card, { backgroundColor: c.surfaceCard, borderColor: c.border }]}
      accessibilityLabel={`Poll. ${poll.question}. ${status}`}
    >
      <Text style={[type.eyebrow, { color: c.accentPrimary }]}>Poll</Text>
      <Text
        style={[type.cardTitle, { color: c.textPrimary, marginTop: space.sm }]}
        allowFontScaling
        maxFontSizeMultiplier={1.4}
      >
        {poll.question}
      </Text>
      <Text style={[type.meta, { color: c.textMuted, marginTop: space.sm }]}>{status}</Text>

      {!canVote ? (
        <View style={styles.list}>
          {poll.options.map((option) => (
            <View key={option.optionId} style={styles.result}>
              <View style={styles.resultHead}>
                <Text style={[type.listTitle, { color: c.textPrimary, flex: 1 }]}>
                  {option.optionText}
                </Text>
                <Text style={[type.meta, { color: c.textMuted }]}>
                  {formatPollResultMeta(option.voteCount, option.percentage)}
                </Text>
              </View>
              <View
                style={[styles.bar, { backgroundColor: c.border }]}
                accessibilityRole="image"
                accessibilityLabel={`${formatPollResultMeta(option.voteCount, option.percentage).replace(' · ', ', ')}`}
              >
                <View
                  style={[
                    styles.barFill,
                    {
                      backgroundColor: c.accentPrimary,
                      width: `${Math.max(0, Math.min(100, option.percentage))}%`,
                    },
                  ]}
                />
              </View>
              {option.selectedByViewer ? (
                <Text style={[type.meta, { color: c.textMuted, marginTop: space.xs }]}>Your vote</Text>
              ) : null}
            </View>
          ))}
        </View>
      ) : (
        <View style={styles.list}>
          {poll.options.map((option) => {
            const checked = selected.includes(option.optionId);
            return (
              <Pressable
                key={option.optionId}
                accessibilityRole={poll.isMultiChoice ? 'checkbox' : 'radio'}
                accessibilityState={{ checked }}
                accessibilityLabel={option.optionText}
                onPress={() => toggle(option.optionId)}
                style={[
                  styles.option,
                  { borderColor: checked ? c.accentPrimary : c.hairline, backgroundColor: c.surfaceRaised },
                ]}
              >
                <View
                  style={[
                    poll.isMultiChoice ? styles.box : styles.radio,
                    { borderColor: c.borderStrong },
                    checked ? { backgroundColor: c.accentPrimary, borderColor: c.accentPrimary } : null,
                  ]}
                />
                <Text style={[type.listTitle, { color: c.textPrimary, flex: 1 }]}>
                  {option.optionText}
                </Text>
              </Pressable>
            );
          })}
          <Text style={[type.caption, { color: c.textMuted }]}>
            Votes are final and cannot be changed.
          </Text>
          <Button
            label="Vote"
            onPress={() => onVote(selected)}
            loading={busy}
            disabled={voteDisabled}
          />
        </View>
      )}

      {prompt !== 'none' ? (
        <Text style={[type.caption, { color: c.textSecondary }]}>{pollTokenRequiredMessage}</Text>
      ) : null}

      {prompt === 'signIn' ? (
        <Button label="Sign in (development)" variant="outline" onPress={onSignIn} />
      ) : null}

      {poll.canViewerClose && hasAccessToken ? (
        <Button label="Close poll" variant="ghost" onPress={onClose} loading={busy} disabled={busy} />
      ) : null}

      {error ? <Text style={[type.caption, { color: c.danger }]}>{error}</Text> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    marginHorizontal: space.xl,
    marginBottom: space.base,
    padding: space.base,
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: radius.md,
    gap: space.sm,
  },
  list: {
    marginTop: space.md,
    gap: space.md,
  },
  option: {
    minHeight: 48,
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: radius.xs,
    paddingHorizontal: space.md,
    flexDirection: 'row',
    alignItems: 'center',
    gap: space.md,
  },
  radio: {
    width: 16,
    height: 16,
    borderRadius: radius.pill,
    borderWidth: 1,
  },
  box: {
    width: 16,
    height: 16,
    borderRadius: radius.xs,
    borderWidth: 1,
  },
  result: {
    gap: space.xs,
  },
  resultHead: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    gap: space.md,
    alignItems: 'flex-start',
  },
  bar: {
    height: 9,
    borderRadius: radius.pill,
    overflow: 'hidden',
  },
  barFill: {
    height: '100%',
    borderRadius: radius.pill,
  },
});
