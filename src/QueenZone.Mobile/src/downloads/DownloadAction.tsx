import { Pressable, StyleSheet, Text } from 'react-native';
import { Download, Check, CircleAlert, LoaderCircle } from 'lucide-react-native';
import type { FanPerformance } from '../api';
import { useSession } from '../session/SessionContext';
import { testIds } from '../test/testIds';
import { space, type, useTheme } from '../theme';
import { formatByteSize } from './formatBytes';
import { enqueueDownload, removeDownload } from './manager';
import { useDownloadMemberId, useDownloadUi } from './useDownloadUi';

type Props = {
  track: FanPerformance;
  compact?: boolean;
  onNeedSignIn?: () => void;
};

export function downloadStatusLabel(
  status: string | undefined,
  title: string,
  sizeLabel: string,
): string {
  switch (status) {
    case 'queued':
      return `Download queued for ${title}`;
    case 'downloading':
      return sizeLabel ? `Downloading ${title}, ${sizeLabel}` : `Downloading ${title}`;
    case 'downloaded':
      return sizeLabel ? `${title} downloaded, ${sizeLabel}` : `${title} downloaded`;
    case 'failed':
      return `Download failed for ${title}. Double tap to retry`;
    case 'removing':
      return `Removing download of ${title}`;
    default:
      return `Download ${title} for offline playback`;
  }
}

export function DownloadAction({ track, compact = false, onNeedSignIn }: Props) {
  const { c } = useTheme();
  const { accessToken, isRestoring, ensureAccessToken } = useSession();
  const memberId = useDownloadMemberId();
  const snapshot = useDownloadUi(track.id);
  const status = snapshot?.status;
  const sizeLabel = formatByteSize(snapshot?.byteSize ?? snapshot?.expectedBytes);
  const label = downloadStatusLabel(status, track.title, sizeLabel);

  const onPress = () => {
    if (isRestoring) {
      return;
    }
    if (!memberId) {
      onNeedSignIn?.();
      return;
    }
    if (status === 'downloaded') {
      void removeDownload(memberId, String(track.id));
      return;
    }
    if (status === 'queued' || status === 'downloading' || status === 'removing') {
      return;
    }
    enqueueDownload(track, memberId, ensureAccessToken);
  };

  const Icon =
    status === 'downloaded'
      ? Check
      : status === 'failed'
        ? CircleAlert
        : status === 'queued' || status === 'downloading' || status === 'removing'
          ? LoaderCircle
          : Download;

  return (
    <Pressable
      testID={`${testIds.fanPerformanceDownloadPrefix}${track.id}`}
      accessibilityRole="button"
      accessibilityLabel={label}
      accessibilityHint={
        status === 'downloaded' ? 'Removes the downloaded recording from this device' : undefined
      }
      accessibilityState={{
        busy: status === 'downloading' || status === 'removing' || status === 'queued',
        disabled: status === 'queued' || status === 'downloading' || status === 'removing',
      }}
      onPress={onPress}
      style={[
        styles.button,
        compact ? styles.compact : null,
        { borderColor: c.borderStrong, backgroundColor: c.surfaceRaised },
      ]}
    >
      <Icon
        size={18}
        color={status === 'failed' ? c.danger : status === 'downloaded' ? c.accentPrimary : c.textPrimary}
      />
      {compact ? null : (
        <Text style={[type.caption, { color: c.textPrimary }]}>
          {status === 'downloaded'
            ? sizeLabel
              ? `Downloaded · ${sizeLabel}`
              : 'Downloaded'
            : status === 'downloading'
              ? sizeLabel
                ? `Downloading · ${sizeLabel}`
                : 'Downloading'
              : status === 'queued'
                ? 'Queued'
                : status === 'failed'
                  ? 'Retry download'
                  : status === 'removing'
                    ? 'Removing'
                    : accessToken
                      ? 'Download'
                      : 'Download'}
        </Text>
      )}
    </Pressable>
  );
}

const styles = StyleSheet.create({
  button: {
    minHeight: 40,
    paddingHorizontal: space.md,
    borderRadius: 20,
    borderWidth: 1,
    flexDirection: 'row',
    alignItems: 'center',
    gap: space.sm,
  },
  compact: {
    width: 40,
    minHeight: 40,
    paddingHorizontal: 0,
    justifyContent: 'center',
  },
});
