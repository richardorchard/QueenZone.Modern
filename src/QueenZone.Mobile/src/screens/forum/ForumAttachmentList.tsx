import { useCallback, useState } from 'react';
import { Image as ExpoImage } from 'expo-image';
import { ActivityIndicator, Modal, Pressable, StyleSheet, Text, View } from 'react-native';
import { ApiError, openForumAttachmentFile, openForumAttachmentImage, type ForumAttachment } from '../../api';
import { getAppConfig } from '../../config';
import { isSmokeAttachEnabled } from '../../session/smokeAttach';
import { ArchiveImage } from '../../ui/ArchiveImage';
import { resolveContentUrl } from '../../ui/html/resolveContentUrl';
import { usePressProps, pressedStyle } from '../../ui/press';
import { testIds } from '../../test/testIds';
import { radius, space, type, useTheme } from '../../theme';
import { attachmentAction, attachmentMeta, imagePreviewUrl } from './forumThreadMeta';

function smokeAttachAllowed(): boolean {
  return isSmokeAttachEnabled({
    dev: typeof __DEV__ !== 'undefined' ? __DEV__ : false,
    appEnv: getAppConfig().appEnv,
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
  const [viewer, setViewer] = useState<{ uri: string; label: string } | null>(null);
  const [busyKey, setBusyKey] = useState<string | null>(null);
  const [errorKey, setErrorKey] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
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
      setBusyKey(key);
      try {
        if (!accessToken) {
          throw ApiError.http(401, 'Sign in to continue.');
        }
        if (action === 'view-image') {
          const uri = await openForumAttachmentImage(attachment.downloadUrl, accessToken);
          setViewer({ uri, label: attachment.fileName });
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
        <Pressable
          style={[styles.viewerBackdrop, { backgroundColor: c.surfaceScrim }]}
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
  viewerImage: {
    width: '100%',
    height: '80%',
  },
});
