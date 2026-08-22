import { useCallback, useEffect, useLayoutEffect, useState } from 'react';
import {
  KeyboardAvoidingView,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import {
  ApiError,
  createForumReply,
  createForumTopic,
  fetchForumCategories,
  type ForumCategoryListItem,
} from '../../api';
import type { ForumStackParamList } from '../../navigation/types';
import { MemberGate } from '../../session/MemberGate';
import { useSession } from '../../session/SessionContext';
import { fonts, radius, space, type, useTheme } from '../../theme';
import { Button } from '../../ui/Button';
import { ErrorBlock, LoadingBlock } from '../../ui/ScreenStates';
import { composerCopy, composerMode, validateComposer } from './composerMeta';

type Props = NativeStackScreenProps<ForumStackParamList, 'Composer'>;

function messageFromUnknownError(err: unknown): string {
  return err instanceof ApiError ? err.message : 'Something went wrong.';
}

export function ComposerScreen({ navigation, route }: Props) {
  return (
    <MemberGate title="Compose">
      <ComposerForm navigation={navigation} route={route} />
    </MemberGate>
  );
}

function ComposerForm({ navigation, route }: Props) {
  const { c } = useTheme();
  const { accessToken } = useSession();
  const mode = composerMode(route.params);
  const copy = composerCopy(mode);
  const [title, setTitle] = useState('');
  const [body, setBody] = useState('');
  const [categoryId, setCategoryId] = useState(route.params?.categoryId);
  const [categoryName, setCategoryName] = useState(route.params?.categoryName);
  const [boards, setBoards] = useState<ForumCategoryListItem[]>([]);
  const [boardsError, setBoardsError] = useState<string | null>(null);
  const [boardsLoading, setBoardsLoading] = useState(mode === 'newTopic' && categoryId == null);
  const [boardsReloadToken, setBoardsReloadToken] = useState(0);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useLayoutEffect(() => {
    navigation.setOptions({ title: copy.title });
  }, [copy.title, navigation]);

  useEffect(() => {
    if (mode !== 'newTopic' || categoryId != null) {
      return;
    }

    const controller = new AbortController();
    setBoardsLoading(true);
    setBoardsError(null);
    fetchForumCategories({ page: 1, pageSize: 50, signal: controller.signal })
      .then((page) => {
        setBoards(page.items);
        setBoardsError(null);
      })
      .catch((err: unknown) => {
        if (err instanceof Error && err.name === 'AbortError') {
          return;
        }
        setBoardsError(messageFromUnknownError(err));
      })
      .finally(() => setBoardsLoading(false));

    return () => controller.abort();
  }, [boardsReloadToken, categoryId, mode]);

  const retryBoards = useCallback(() => {
    setBoardsLoading(true);
    setBoardsError(null);
    setBoardsReloadToken((n) => n + 1);
  }, []);

  const submit = useCallback(async () => {
    const validation = validateComposer({
      mode,
      title,
      body,
      categoryId,
      isLocked: route.params?.isLocked,
    });
    if (validation) {
      setSubmitError(validation);
      return;
    }
    if (!accessToken) {
      setSubmitError('Sign in with a mobile session to publish. The development toggle cannot post.');
      return;
    }

    setSubmitting(true);
    setSubmitError(null);
    try {
      if (mode === 'reply' && route.params?.threadId != null) {
        await createForumReply(route.params.threadId, { body: body.trim() }, accessToken);
        navigation.goBack();
        return;
      }

      if (categoryId == null) {
        setSubmitError('Choose a board for this topic.');
        return;
      }

      const created = await createForumTopic(
        categoryId,
        { title: title.trim(), body: body.trim() },
        accessToken,
      );
      navigation.replace('Thread', { id: created.id, title: created.title });
    } catch (err: unknown) {
      setSubmitError(messageFromUnknownError(err));
    } finally {
      setSubmitting(false);
    }
  }, [accessToken, body, categoryId, mode, navigation, route.params?.isLocked, route.params?.threadId, title]);

  const context =
    mode === 'reply'
      ? route.params?.threadTitle
      : categoryName;

  if (boardsLoading) {
    return <LoadingBlock label="Loading boards…" />;
  }

  if (mode === 'reply' && route.params?.isLocked) {
    return <ErrorBlock message="This topic is locked." />;
  }

  if (boardsError) {
    return <ErrorBlock message={boardsError} onRetry={retryBoards} />;
  }

  return (
    <KeyboardAvoidingView
      style={[styles.flex, { backgroundColor: c.surfacePage }]}
      behavior={Platform.OS === 'ios' ? 'padding' : undefined}
    >
      <ScrollView
        contentContainerStyle={styles.content}
        keyboardShouldPersistTaps="handled"
      >
        <Text style={[type.eyebrow, { color: c.accentPrimary }]}>
          {mode === 'reply' ? 'Reply' : 'Post to the community'}
        </Text>
        {context ? (
          <Text
            style={[type.listTitle, { color: c.textPrimary, marginTop: space.sm }]}
            allowFontScaling
          >
            {context}
          </Text>
        ) : null}

        {mode === 'newTopic' && categoryId == null ? (
          <View style={styles.boards}>
            <Text style={[type.meta, { color: c.textMuted }]}>Board</Text>
            {boards.map((board) => (
              <Pressable
                key={board.id}
                accessibilityRole="button"
                accessibilityLabel={`Post to ${board.name}`}
                onPress={() => {
                  setCategoryId(board.id);
                  setCategoryName(board.name);
                }}
                style={[
                  styles.board,
                  { borderColor: c.hairline, backgroundColor: c.surfaceCard },
                ]}
              >
                <Text style={[type.listTitle, { color: c.textPrimary }]}>{board.name}</Text>
              </Pressable>
            ))}
          </View>
        ) : null}

        {mode === 'newTopic' ? (
          <TextInput
            value={title}
            onChangeText={setTitle}
            placeholder="Topic title"
            placeholderTextColor={c.textMuted}
            accessibilityLabel="Topic title"
            maxLength={200}
            style={[
              styles.field,
              {
                color: c.textPrimary,
                borderColor: c.border,
                backgroundColor: '#1D1D1D',
              },
            ]}
          />
        ) : null}

        <TextInput
          value={body}
          onChangeText={setBody}
          placeholder={mode === 'reply' ? 'Write a reply' : 'Write the first post'}
          placeholderTextColor={c.textMuted}
          accessibilityLabel={mode === 'reply' ? 'Reply body' : 'Topic body'}
          multiline
          textAlignVertical="top"
          style={[
            styles.field,
            styles.body,
            {
              color: c.textPrimary,
              borderColor: c.border,
              backgroundColor: '#1D1D1D',
            },
          ]}
        />

        {!accessToken ? (
          <Text style={[type.caption, { color: c.textSecondary }]}>
            Publishing uses your mobile sign-in token. The development toggle cannot post to the
            site.
          </Text>
        ) : null}

        {submitError ? (
          <Text style={[type.caption, { color: c.textSecondary }]}>{submitError}</Text>
        ) : null}

        <Button
          label={copy.action}
          onPress={() => {
            void submit();
          }}
          loading={submitting}
          disabled={!accessToken}
        />
      </ScrollView>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  content: {
    paddingHorizontal: space.xl,
    paddingTop: space.xl,
    paddingBottom: space.section,
    gap: space.base,
  },
  boards: {
    gap: space.sm,
  },
  board: {
    minHeight: 48,
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: radius.xs,
    paddingHorizontal: space.md,
    justifyContent: 'center',
  },
  field: {
    minHeight: 48,
    borderWidth: 1,
    borderRadius: radius.xs,
    paddingHorizontal: 12,
    fontFamily: fonts.body,
    fontSize: 16,
  },
  body: {
    minHeight: 160,
    paddingTop: 12,
  },
});
