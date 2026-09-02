import { memo } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { space, type, useTheme } from '../../theme';
import { Button } from '../../ui/Button';
import { testIds } from '../../test/testIds';
import { watchButtonLabel, watchHint } from './forumThreadMeta';

export const ForumWatchControl = memo(function ForumWatchControl({
  isSignedIn,
  watching,
  watchBusy,
  watchError,
  disabled,
  onToggle,
}: {
  isSignedIn: boolean;
  watching: boolean;
  watchBusy: boolean;
  watchError: string | null;
  disabled: boolean;
  onToggle: () => void;
}) {
  const { c } = useTheme();
  return (
    <View style={styles.watch} testID={testIds.forumThreadWatch}>
      <Button
        label={isSignedIn ? watchButtonLabel(watching) : 'Sign in to watch'}
        variant="outline"
        size="sm"
        loading={watchBusy}
        disabled={disabled}
        onPress={onToggle}
      />
      <Text style={[type.caption, { color: c.textMuted, marginTop: space.sm }]}>{watchHint(watching)}</Text>
      {watchError ? (
        <Text style={[type.caption, { color: c.textMuted, marginTop: space.sm }]}>{watchError}</Text>
      ) : null}
    </View>
  );
});

const styles = StyleSheet.create({
  watch: {
    marginTop: space.lg,
  },
});
