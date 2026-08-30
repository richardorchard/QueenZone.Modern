import { useState } from 'react';
import { Pressable, Text, View } from 'react-native';
import type { HomePoll } from '../../api';
import { voteHomePoll } from '../../api';
import { fonts, radius, space, type, useTheme } from '../../theme';
import { Eyebrow } from '../../ui/Eyebrow';
import { testIds } from '../../test/testIds';

type Props = {
  poll: HomePoll;
  isSignedIn: boolean;
  accessToken: string | null;
  onVoted: () => Promise<void> | void;
  onSignIn: () => void;
};

export function HomePollCard({ poll, isSignedIn, accessToken, onVoted, onSignIn }: Props) {
  const { c } = useTheme();
  const [pendingOptionId, setPendingOptionId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const canVote = isSignedIn && !poll.viewerHasVoted && !poll.isClosed && Boolean(accessToken);

  const submit = async (optionId: string) => {
    if (!accessToken || pendingOptionId) {
      return;
    }

    setPendingOptionId(optionId);
    setError(null);
    try {
      await voteHomePoll(optionId, accessToken);
      await onVoted();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Could not cast that vote.');
    } finally {
      setPendingOptionId(null);
    }
  };

  return (
    <View
      testID={testIds.homePoll}
      style={{
        marginTop: space.xxl,
        marginHorizontal: space.xl,
        paddingVertical: 22,
        paddingHorizontal: space.lg,
        borderTopWidth: 1,
        borderBottomWidth: 1,
        borderColor: c.hairline,
        backgroundColor: c.surfaceRaised,
        gap: 10,
      }}
    >
      <Eyebrow tone="accent" size={10}>
        Poll
      </Eyebrow>
      <Text style={[type.pageTitle, { color: c.textPrimary, fontSize: 22 }]}>{poll.question}</Text>
      <Text style={[type.caption, { color: c.textSecondary }]}>
        {poll.isClosed ? 'Closed' : 'Open'} · {poll.totalVotes.toLocaleString()}{' '}
        {poll.totalVotes === 1 ? 'vote' : 'votes'}
      </Text>
      {error ? <Text style={[type.caption, { color: c.danger }]}>{error}</Text> : null}

      {poll.options.map((option) => {
        const selected = poll.selectedOptionId === option.id;
        const label = `${option.text} ${option.count} · ${formatPercent(option.percentage)}%`;
        return (
          <View key={option.id} style={{ gap: 6 }}>
            {canVote ? (
              <Pressable
                testID={`${testIds.homePollVote}-${option.id}`}
                accessibilityRole="button"
                accessibilityLabel={label}
                disabled={pendingOptionId !== null}
                onPress={() => {
                  void submit(option.id);
                }}
                style={{
                  paddingVertical: 12,
                  paddingHorizontal: 12,
                  borderWidth: 1,
                  borderColor: c.border,
                  borderRadius: radius.xs,
                  flexDirection: 'row',
                  justifyContent: 'space-between',
                  gap: 12,
                  opacity: pendingOptionId && pendingOptionId !== option.id ? 0.6 : 1,
                }}
              >
                <Text style={{ fontFamily: fonts.bodyMedium, fontSize: 15, color: c.textPrimary, flex: 1 }}>
                  {option.text}
                </Text>
                <Text style={[type.caption, { color: c.textSecondary }]}>
                  {option.count} · {formatPercent(option.percentage)}%
                </Text>
              </Pressable>
            ) : (
              <View
                accessibilityRole="text"
                accessibilityLabel={selected ? `${label}. Your vote` : label}
                style={{ gap: 6 }}
              >
                <View style={{ flexDirection: 'row', justifyContent: 'space-between', gap: 12 }}>
                  <Text
                    style={{
                      fontFamily: selected ? fonts.bodySemi : fonts.bodyMedium,
                      fontSize: 15,
                      color: c.textPrimary,
                      flex: 1,
                    }}
                  >
                    {option.text}
                  </Text>
                  <Text style={[type.caption, { color: c.textSecondary }]}>
                    {option.count} · {formatPercent(option.percentage)}%
                  </Text>
                </View>
                {selected ? (
                  <Text style={[type.caption, { color: c.accentPrimary }]}>Your vote</Text>
                ) : null}
              </View>
            )}
            <View
              style={{
                height: 6,
                borderRadius: 3,
                backgroundColor: c.hairline,
                overflow: 'hidden',
              }}
            >
              <View
                style={{
                  width: `${Math.max(0, Math.min(100, option.percentage))}%`,
                  height: 6,
                  backgroundColor: c.accentPrimary,
                }}
              />
            </View>
          </View>
        );
      })}

      {!isSignedIn && !poll.isClosed ? (
        <Pressable
          testID={testIds.homePollSignIn}
          accessibilityRole="button"
          accessibilityLabel="Sign in to vote"
          onPress={onSignIn}
        >
          <Text style={[type.body, { color: c.accentPrimary }]}>Sign in to vote</Text>
        </Pressable>
      ) : null}

      {canVote ? (
        <Text style={[type.caption, { color: c.textMuted }]}>Votes are final and cannot be changed.</Text>
      ) : null}
    </View>
  );
}

function formatPercent(value: number): string {
  return Number.isInteger(value) ? String(value) : value.toFixed(1);
}
