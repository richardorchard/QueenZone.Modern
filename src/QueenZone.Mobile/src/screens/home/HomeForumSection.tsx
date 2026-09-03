import { memo } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import type { ForumRecentThread } from '../../api';
import type { SectionView } from '../../hooks/useHomeSection';
import { Eyebrow } from '../../ui/Eyebrow';
import { MetaLine } from '../../ui/MetaLine';
import { SectionErrorBlock } from '../../ui/ScreenStates';
import { initials } from '../../ui/initials';
import { fonts, radius, space, type } from '../../theme';
import { formatForumThreadMeta } from './homeMeta';

export const HomeForumSection = memo(function HomeForumSection({
  forumView,
  onOpenThread,
  onEnterForum,
  onReloadForum,
}: {
  forumView: SectionView<ForumRecentThread[]>;
  onOpenThread: (thread: ForumRecentThread) => void;
  onEnterForum: () => void;
  onReloadForum: () => void;
}) {
  return (
    <View style={styles.section}>
      <View style={styles.header}>
        <View style={styles.headerText}>
          <Eyebrow tone="onDark" size={10}>
            The community
          </Eyebrow>
          <Text style={[type.pageTitle, styles.title]}>In the forum</Text>
        </View>
        <Pressable accessibilityRole="button" onPress={onEnterForum} hitSlop={8}>
          <Text style={styles.enter}>Enter</Text>
        </Pressable>
      </View>

      {forumView.kind === 'skeleton' ? (
        <View style={styles.skeletonList}>
          {[0, 1, 2].map((key) => (
            <View key={key} style={styles.skeletonRow} />
          ))}
        </View>
      ) : forumView.kind === 'error' ? (
        <SectionErrorBlock message={forumView.message} onRetry={onReloadForum} />
      ) : (
        forumView.data.map((thread, index) => (
          <Pressable
            key={thread.topicId}
            accessible
            accessibilityRole="button"
            accessibilityLabel={thread.title}
            onPress={() => onOpenThread(thread)}
            style={styles.row}
          >
            <View style={styles.avatar}>
              <Text style={styles.avatarLabel}>{initials(thread.categoryName)}</Text>
            </View>
            <View style={styles.rowText}>
              <Text numberOfLines={2} style={styles.rowTitle}>
                {thread.title}
              </Text>
              <MetaLine parts={formatForumThreadMeta(thread)} />
            </View>
            {index === 0 ? <View style={styles.newDot} /> : null}
          </Pressable>
        ))
      )}
    </View>
  );
});

const styles = StyleSheet.create({
  section: {
    marginTop: space.xxl,
    backgroundColor: '#181614',
    paddingVertical: 26,
    paddingHorizontal: space.xl,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'flex-end',
    justifyContent: 'space-between',
    marginBottom: space.md,
  },
  headerText: { gap: 6 },
  title: { color: '#F2F1ED', fontSize: 23 },
  enter: {
    fontFamily: fonts.bodyMedium,
    fontSize: 12,
    letterSpacing: 0.7,
    textTransform: 'uppercase',
    color: 'rgba(255,255,255,0.66)',
  },
  skeletonList: { gap: 12 },
  skeletonRow: { height: 44, backgroundColor: 'rgba(255,255,255,0.06)', borderRadius: radius.xs },
  row: {
    paddingVertical: 14,
    borderTopWidth: 1,
    borderTopColor: 'rgba(255,255,255,0.16)',
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
  },
  avatar: {
    width: space.avatar,
    height: space.avatar,
    borderRadius: radius.avatar,
    backgroundColor: 'rgba(255,255,255,0.10)',
    borderWidth: 1,
    borderColor: 'rgba(255,255,255,0.18)',
    alignItems: 'center',
    justifyContent: 'center',
  },
  avatarLabel: { fontFamily: fonts.display, fontSize: 12, color: 'rgba(255,255,255,0.85)' },
  rowText: { flex: 1, gap: 4 },
  rowTitle: { fontFamily: fonts.bodyMedium, fontSize: 14.5, color: '#FFFFFF' },
  newDot: { width: 6, height: 6, borderRadius: 3, backgroundColor: '#B89A4A' },
});
