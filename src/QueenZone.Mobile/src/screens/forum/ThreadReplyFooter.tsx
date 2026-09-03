import { memo } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { space, type, useTheme } from '../../theme';
import { Button } from '../../ui/Button';
import { ListFooterLoading } from '../../ui/ScreenStates';
import { testIds } from '../../test/testIds';

export const ThreadReplyFooter = memo(function ThreadReplyFooter({
  canReply,
  isSignedIn,
  loadingMore,
  onReply,
}: {
  canReply: boolean;
  isSignedIn: boolean;
  loadingMore: boolean;
  onReply: () => void;
}) {
  const { c } = useTheme();
  return (
    <View style={styles.reply}>
      <ListFooterLoading visible={loadingMore} />
      {canReply ? (
        <Button
          label={isSignedIn ? 'Reply' : 'Sign in to reply'}
          testID={testIds.forumThreadReply}
          variant="outline"
          onPress={onReply}
        />
      ) : (
        <Text style={[type.caption, { color: c.textMuted }]}>This topic is locked.</Text>
      )}
    </View>
  );
});

const styles = StyleSheet.create({
  reply: {
    marginHorizontal: space.xl,
    marginTop: space.base,
    marginBottom: space.section,
  },
});
