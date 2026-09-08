import { useCallback, useState } from 'react';
import { Image as ExpoImage } from 'expo-image';
import { ActivityIndicator, Modal, Pressable, StyleSheet, Text, View } from 'react-native';
import {
  ApiError,
  cacheForumAttachment,
  openForumAttachmentFile,
  openForumAttachmentImage,
  saveForumAttachmentImage,
  type ForumAttachment,
} from '../../api';
import { getAppConfig } from '../../config';
import { SaveToPhotosError } from '../../media/saveToPhotos';
import { isSmokeAttachEnabled } from '../../session/smokeAttach';
import { ArchiveImage } from '../../ui/ArchiveImage';
import { Button } from '../../ui/Button';
import { resolveContentUrl } from '../../ui/html/resolveContentUrl';
import { usePressProps, pressedStyle } from '../../ui/press';
import { testIds } from '../../test/testIds';
import { radius, space, type, useTheme } from '../../theme';
import { ForumAttachmentAudioPlayer } from './ForumAttachmentAudioPlayer';
import { attachmentAction, attachmentMeta, imagePreviewUrl } from './forumThreadMeta';

function smokeAttachAllowed(): boolean {
  const config = getAppConfig();
  return isSmokeAttachEnabled({
    dev: typeof __DEV__ !== 'undefined' ? __DEV__ : false,
    appEnv: config.appEnv,
    smokeEmbed: config.smokeEmbed,
  });
}

export function ForumAttachmentList({
  attachments,
  isSignedIn,
  accessToken,
  interactionsEnabled,
}: {
  attachments: ForumAttachment[];
  isSignedIn: boolean;
  accessToken: string | null;
  interactionsEnabled: boolean;
}) {
  const { c } = useTheme();
  const press = usePressProps();
  const [viewer, setViewer] = useState<{
    uri: string;
    label: string;
    downloadUrl: string;
    fileName: string;
  } | null>(null);
  const [audio, setAudio] = useState<{
    fileUri: string;
    fileName: string;
    downloadUrl: string;
  } | null>(null);
  const [busyKey, setBusyKey] = useState<string | null>(null);
  const [errorKey, setErrorKey] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [saveBusy, setSaveBusy] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [opened, setOpened] = useState(false);
  const label = attachments.length === 1 ? 'Attachment' : 'Attachments';

  const openAttachment = useCallback(
    async (attachment: ForumAttachment) => {
      if (!interactionsEnabled) {
        return;
      }
      const action = attachmentAction(attachment, isSignedIn);
      if (action === 'none') {
        return;
      }
      const key = `${attachment.downloadUrl}-${attachment.fileName}`;
      setErrorKey(null);
      setErrorMessage(null);
      setSaveError(null);
      setBusyKey(key);
      try {
        if (!accessToken) {
          throw ApiError.http(401, 'Sign in to continue.');
        }
        if (action === 'view-image') {
          const uri = await openForumAttachmentImage(attachment.downloadUrl, accessToken);
          setViewer({
            uri,
            label: attachment.fileName,
            downloadUrl: attachment.downloadUrl,
            fileName: attachment.fileName,
          });
          return;
        }
        if (action === 'play-audio') {
          const cached = await cacheForumAttachment(
            attachment.downloadUrl,
            accessToken,
            attachment.fileName,
          );
          setAudio({
            fileUri: cached.fileUri,
            fileName: attachment.fileName,
            downloadUrl: attachment.downloadUrl,
          });
          if (smokeAttachAllowed()) {
            setOpened(true);
          }
          return;
        }
        await openForumAttachmentFile(attachment.downloadUrl, accessToken, attachment.fileName, {
          present: !smokeAttachAllowed(),
        });
        if (smokeAttachAllowed()) {
          setOpened(true);
        }
      } catch (err: unknown) {
        setErrorKey(key);
        setErrorMessage(err instanceof ApiError ? err.message : 'Unable to open this attachment.');
      } finally {
        setBusyKey(null);
      }
    },
    [accessToken, interactionsEnabled, isSignedIn],
  );

  const saveImage = useCallback(async () => {
    if (!viewer || !accessToken) {
      return;
    }
    setSaveError(null);
    setSaveBusy(true);
    try {
      await saveForumAttachmentImage(viewer.downloadUrl, accessToken, viewer.fileName);
    } catch (err: unknown) {
      setSaveError(
        err instanceof SaveToPhotosError || err instanceof ApiError
          ? err.message
          : 'Unable to open this attachment.',
      );
    } finally {
      setSaveBusy(false);
    }
  }, [accessToken, viewer]);

  const saveAudioFile = useCallback(async () => {
    if (!audio || !accessToken) {
      return;
    }
    setSaveError(null);
    setSaveBusy(true);
    try {
      await openForumAttachmentFile(audio.downloadUrl, accessToken, audio.fileName, {
        present: !smokeAttachAllowed(),
      });
    } catch (err: unknown) {
      setSaveError(err instanceof ApiError ? err.message : 'Unable to open this attachment.');
    } finally {
      setSaveBusy(false);
    }
  }, [accessToken, audio]);

  return (
    <View style={styles.attachments}>
      <Text style={[type.meta, { color: c.textMuted }]}>{label}</Text>
      {attachments.map((attachment) => {
        const preview = imagePreviewUrl(attachment);
        const previewUri = preview ? resolveContentUrl(preview, getAppConfig().apiBaseUrl) : null;
        const caption = attachmentMeta(attachment);
        const action = interactionsEnabled ? attachmentAction(attachment, isSignedIn) : 'none';
        const key = `${attachment.downloadUrl}-${attachment.fileName}`;
        const meta = (
          <View style={styles.attachmentMeta} accessibilityLabel={`${attachment.fileName}. ${caption}`}>
            <Text style={[type.listTitle, { color: c.textPrimary }]}>{attachment.fileName}</Text>
            {caption ? (
              <Text style={[type.meta, { color: c.textMuted, marginTop: space.xs }]}>{caption}</Text>
            ) : null}
            {errorKey === key && errorMessage ? (
              <Text style={[type.caption, { color: c.textMuted, marginTop: space.xs }]}>{errorMessage}</Text>
            ) : null}
          </View>
        );
        const body = (
          <>
            {previewUri ? (
              <ArchiveImage
                source={{ uri: previewUri }}
                style={[styles.thumb, { backgroundColor: c.surfaceCard, borderColor: c.hairline }]}
                priority="low"
                recyclingKey={attachment.downloadUrl}
                label={attachment.fileName}
              />
            ) : null}
            {meta}
            {busyKey === key ? <ActivityIndicator color={c.accentPrimary} /> : null}
          </>
        );
        if (action === 'none') {
          return (
            <View key={key} style={styles.attachment} testID={testIds.forumThreadAttachment}>
              {body}
            </View>
          );
        }
        return (
          <Pressable
            key={key}
            style={({ pressed }) => pressedStyle({ pressed }, styles.attachment)}
            {...press}
            testID={testIds.forumThreadAttachment}
            accessibilityRole="button"
            accessibilityLabel={`${attachment.fileName}. ${caption}. Open`}
            onPress={() => {
              void openAttachment(attachment);
            }}
          >
            {body}
          </Pressable>
        );
      })}
      {opened ? (
        <Text testID={testIds.forumThreadAttachmentOpened} style={[type.caption, { color: c.textMuted }]}>
          Attachment opened
        </Text>
      ) : null}
      <Modal
        visible={viewer != null}
        transparent
        animationType="fade"
        onRequestClose={() => setViewer(null)}
      >
        <View style={[styles.viewerBackdrop, { backgroundColor: c.surfaceScrim }]}>
          <Pressable
            style={styles.viewerClose}
            onPress={() => setViewer(null)}
            testID={testIds.forumThreadAttachmentViewer}
            accessibilityRole="button"
            accessibilityLabel="Close attachment"
          >
            {viewer ? (
              <ExpoImage
                source={{ uri: viewer.uri }}
                style={styles.viewerImage}
                contentFit="contain"
                accessibilityLabel={viewer.label}
              />
            ) : null}
          </Pressable>
          {viewer ? (
            <View style={styles.viewerActions}>
              <Button
                label="Save to Photos"
                onPress={() => {
                  void saveImage();
                }}
                loading={saveBusy}
                testID={testIds.forumThreadAttachmentSave}
              />
              {saveError ? (
                <Text style={[type.caption, { color: c.textMuted, textAlign: 'center' }]}>{saveError}</Text>
              ) : null}
            </View>
          ) : null}
        </View>
      </Modal>
      <Modal
        visible={audio != null}
        transparent
        animationType="fade"
        onRequestClose={() => setAudio(null)}
      >
        <View style={[styles.viewerBackdrop, { backgroundColor: c.surfaceScrim }]}>
          <Pressable
            style={StyleSheet.absoluteFill}
            onPress={() => setAudio(null)}
            accessibilityRole="button"
            accessibilityLabel="Close attachment"
          />
          {audio ? (
            <View style={[styles.audioSheet, { backgroundColor: c.surfaceSheet }]}>
              <ForumAttachmentAudioPlayer
                fileUri={audio.fileUri}
                fileName={audio.fileName}
                onSaveToFiles={() => {
                  void saveAudioFile();
                }}
                saveBusy={saveBusy}
                saveError={saveError}
              />
            </View>
          ) : null}
        </View>
      </Modal>
    </View>
  );
}

const styles = StyleSheet.create({
  attachments: {
    marginTop: space.lg,
    gap: space.md,
  },
  attachment: {
    gap: space.sm,
  },
  thumb: {
    width: 120,
    height: 120,
    borderRadius: radius.xs,
    borderWidth: StyleSheet.hairlineWidth,
  },
  attachmentMeta: {
    minHeight: 48,
    justifyContent: 'center',
  },
  viewerBackdrop: {
    flex: 1,
    justifyContent: 'center',
    padding: space.xl,
  },
  viewerClose: {
    flex: 1,
    justifyContent: 'center',
  },
  viewerImage: {
    width: '100%',
    height: '80%',
  },
  viewerActions: {
    gap: space.sm,
    paddingTop: space.md,
  },
  audioSheet: {
    padding: space.lg,
    borderRadius: radius.sm,
    gap: space.md,
  },
});
