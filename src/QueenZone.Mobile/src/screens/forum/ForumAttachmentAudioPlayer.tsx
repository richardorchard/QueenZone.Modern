import { useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { useAudioPlayer, useAudioPlayerStatus } from 'expo-audio';
import { Button } from '../../ui/Button';
import { testIds } from '../../test/testIds';
import { space, type, useTheme } from '../../theme';

const DECODE_FAILED = 'This sound could not be played.';

export function ForumAttachmentAudioPlayer({
  fileUri,
  fileName,
  onSaveToFiles,
  saveBusy,
  saveError,
}: {
  fileUri: string;
  fileName: string;
  onSaveToFiles: () => void;
  saveBusy: boolean;
  saveError: string | null;
}) {
  const { c } = useTheme();
  const player = useAudioPlayer({ uri: fileUri });
  const status = useAudioPlayerStatus(player);
  const [decodeError, setDecodeError] = useState<string | null>(null);
  const failed = decodeError ?? decodeFailureMessage(status);

  return (
    <View
      style={styles.player}
      testID={testIds.forumThreadAttachmentAudio}
      accessibilityLabel={fileName}
    >
      <Text style={[type.listTitle, { color: c.textPrimary }]}>{fileName}</Text>
      {failed ? (
        <Text style={[type.caption, { color: c.textMuted, marginTop: space.xs }]}>{failed}</Text>
      ) : null}
      <View style={styles.actions}>
        <Button
          label={status.playing ? 'Pause' : 'Play'}
          onPress={() => {
            try {
              if (status.playing) {
                player.pause();
              } else {
                player.play();
              }
            } catch {
              setDecodeError(DECODE_FAILED);
            }
          }}
          testID={testIds.forumThreadAttachmentAudioPlay}
          disabled={failed != null}
        />
        <Button
          label="Save to Files"
          variant="outline"
          onPress={onSaveToFiles}
          loading={saveBusy}
          testID={testIds.forumThreadAttachmentSaveFile}
        />
      </View>
      {saveError ? (
        <Text style={[type.caption, { color: c.textMuted }]}>{saveError}</Text>
      ) : null}
    </View>
  );
}

function decodeFailureMessage(status: {
  playbackState?: string;
  reasonForWaitingToPlay?: string;
}): string | null {
  const text = `${status.playbackState ?? ''} ${status.reasonForWaitingToPlay ?? ''}`;
  return /fail|error|decode|unsupported/i.test(text) ? DECODE_FAILED : null;
}

const styles = StyleSheet.create({
  player: {
    width: '100%',
    gap: space.md,
  },
  actions: {
    gap: space.sm,
  },
});
