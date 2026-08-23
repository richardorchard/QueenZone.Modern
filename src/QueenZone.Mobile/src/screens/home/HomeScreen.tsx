import type { BottomTabScreenProps } from '@react-navigation/bottom-tabs';
import type { CompositeScreenProps } from '@react-navigation/native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { Search } from 'lucide-react-native';
import { Image } from 'expo-image';
import { useCallback } from 'react';
import { FlatList, Pressable, RefreshControl, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { media } from '../../content/media';
import {
  archiveShort,
  featuredStories,
  homeLead,
  onThisDay,
  sampleThreads,
  type ArchiveDestination,
  type FeatureItem,
} from '../../content/sample';
import type { HomeStackParamList, RootTabParamList } from '../../navigation/types';
import { useSession } from '../../session/SessionContext';
import { fonts, space, type, useTheme } from '../../theme';
import { ArchiveFooter } from '../../ui/ArchiveFooter';
import { FeatureBlock } from '../../ui/FeatureBlock';
import { FeatureRail } from '../../ui/FeatureRail';
import { HeroFeature } from '../../ui/HeroFeature';
import { IconButton } from '../../ui/IconButton';
import { MetaLine } from '../../ui/MetaLine';
import { SectionHeader } from '../../ui/SectionHeader';
import { ChevronRight } from 'lucide-react-native';
import { profileA11yLabel } from '../messages/inboxMeta';
import { useUnreadConversationCount } from '../messages/useUnreadConversationCount';

type Props = CompositeScreenProps<
  NativeStackScreenProps<HomeStackParamList, 'Home'>,
  BottomTabScreenProps<RootTabParamList>
>;

function initials(name: string | null): string {
  if (!name) {
    return '';
  }
  const parts = name.replace(/_/g, ' ').trim().split(/\s+/);
  if (parts.length === 1) {
    return parts[0].slice(0, 2).toUpperCase();
  }
  return `${parts[0][0] ?? ''}${parts[1][0] ?? ''}`.toUpperCase();
}

export function HomeScreen({ navigation }: Props) {
  const insets = useSafeAreaInsets();
  const { c } = useTheme();
  const { isSignedIn, displayName } = useSession();
  const unreadCount = useUnreadConversationCount();
  const avatar = isSignedIn ? initials(displayName) : '';

  const openStory = useCallback(() => {
    navigation.navigate('ArchiveTab', { screen: 'Story', params: { id: 0 } });
  }, [navigation]);

  const openFeature = useCallback(
    (_item: FeatureItem) => {
      navigation.navigate('ArchiveTab', { screen: 'Story', params: { id: 0 } });
    },
    [navigation],
  );

  const openArchiveRow = useCallback(
    (row: ArchiveDestination) => {
      if (row.id === 'stories') {
        navigation.navigate('ArchiveTab', { screen: 'Stories' });
        return;
      }
      if (row.id === 'timeline') {
        navigation.navigate('ArchiveTab', { screen: 'Timeline' });
        return;
      }
      if (row.id === 'biography') {
        navigation.navigate('ArchiveTab', { screen: 'Biography' });
        return;
      }
      if (row.id === 'discography') {
        navigation.navigate('ArchiveTab', { screen: 'Discography' });
      }
    },
    [navigation],
  );

  return (
    <FlatList
      style={{ flex: 1, backgroundColor: c.surfacePage }}
      data={[]}
      renderItem={() => null}
      refreshing={false}
      refreshControl={<RefreshControl refreshing={false} tintColor={c.accentPrimary} />}
      ListHeaderComponent={
        <>
          <View>
            <HeroFeature item={homeLead} onPress={openStory} />
            <View
              pointerEvents="box-none"
              style={{
                position: 'absolute',
                top: insets.top + 8,
                left: 20,
                right: 12,
                flexDirection: 'row',
                alignItems: 'center',
                justifyContent: 'space-between',
              }}
            >
              <View style={{ flexDirection: 'row', alignItems: 'center', gap: 9 }}>
                <Image
                  source={media.crestWhite}
                  style={{ height: 26, width: 26 }}
                  contentFit="contain"
                  importantForAccessibility="no"
                  accessibilityElementsHidden
                />
                <Text
                  style={{
                    fontFamily: fonts.titling,
                    fontSize: 13,
                    fontWeight: '600',
                    letterSpacing: 2.3,
                    textTransform: 'uppercase',
                    color: c.textPrimary,
                  }}
                >
                  Queenzone
                </Text>
              </View>
              <View style={{ flexDirection: 'row', alignItems: 'center' }}>
                <IconButton
                  icon={Search}
                  accessibilityLabel="Search"
                  onPress={() => navigation.navigate('Search')}
                />
                <Pressable
                  accessibilityRole="button"
                  accessibilityLabel={profileA11yLabel(isSignedIn ? unreadCount : 0)}
                  onPress={() => navigation.navigate('Profile')}
                  style={{ width: 44, height: 44, alignItems: 'center', justifyContent: 'center' }}
                >
                  <View
                    style={{
                      width: 32,
                      height: 32,
                      borderRadius: 16,
                      backgroundColor: 'rgba(17,17,17,0.55)',
                      borderWidth: 1,
                      borderColor: 'rgba(255,255,255,0.4)',
                      alignItems: 'center',
                      justifyContent: 'center',
                    }}
                  >
                    <Text style={{ fontFamily: fonts.bodyMedium, fontSize: 12, color: c.textPrimary }}>
                      {avatar || '·'}
                    </Text>
                  </View>
                  {isSignedIn && unreadCount > 0 ? (
                    <View
                      style={{
                        position: 'absolute',
                        top: 2,
                        right: 2,
                        minWidth: 16,
                        height: 16,
                        borderRadius: 8,
                        paddingHorizontal: 4,
                        backgroundColor: c.accentPrimary,
                        alignItems: 'center',
                        justifyContent: 'center',
                      }}
                      importantForAccessibility="no"
                      accessibilityElementsHidden
                    >
                      <Text
                        style={{
                          fontFamily: fonts.bodyMedium,
                          fontSize: 9,
                          lineHeight: 11,
                          color: c.textOnAccent,
                        }}
                      >
                        {unreadCount > 99 ? '99+' : unreadCount}
                      </Text>
                    </View>
                  ) : null}
                </Pressable>
              </View>
            </View>
          </View>

          <SectionHeader
            title="Featured stories"
            actionLabel="All"
            onAction={() => navigation.navigate('NewsTab', { screen: 'NewsIndex' })}
          />
          <FeatureRail items={featuredStories} onOpen={openFeature} />

          <FeatureBlock
            eyebrow={onThisDay.eyebrow}
            numeral={onThisDay.numeral}
            body={onThisDay.body}
            actionLabel={onThisDay.actionLabel}
            onAction={openStory}
          />

          <SectionHeader
            title="Explore the archive"
            actionLabel="All"
            onAction={() => navigation.navigate('ArchiveTab', { screen: 'ArchiveHub' })}
          />
          {archiveShort.map((row) => (
            <Pressable
              key={row.id}
              accessible
              accessibilityRole="button"
              accessibilityLabel={`${row.title}. ${row.meta.join(', ')}`}
              onPress={() => openArchiveRow(row)}
              style={{
                marginHorizontal: space.xl,
                paddingVertical: 13,
                borderBottomWidth: 1,
                borderBottomColor: c.hairline,
                flexDirection: 'row',
                alignItems: 'center',
                justifyContent: 'space-between',
                gap: 14,
              }}
            >
              <View style={{ flex: 1, gap: 5 }}>
                <Text style={[type.cardTitle, { fontSize: 20, lineHeight: 24, color: c.textPrimary }]}>
                  {row.title}
                </Text>
                <MetaLine parts={row.meta} />
              </View>
              <ChevronRight size={17} color={c.textMuted} strokeWidth={1.5} />
            </Pressable>
          ))}

          <SectionHeader
            title="Popular discussions"
            actionLabel="Forum"
            onAction={() => navigation.navigate('ForumTab', { screen: 'ForumIndex' })}
          />
          {sampleThreads.slice(0, 3).map((thread) => (
            <Pressable
              key={thread.id}
              accessible
              accessibilityRole="button"
              accessibilityLabel={thread.title}
              onPress={() => navigation.navigate('ForumTab', { screen: 'Thread', params: { id: thread.id } })}
              style={{
                marginHorizontal: space.xl,
                paddingVertical: 14,
                borderBottomWidth: 1,
                borderBottomColor: c.hairline,
                gap: 7,
              }}
            >
              <Text numberOfLines={2} style={[type.listTitle, { color: c.textPrimary }]}>
                {thread.title}
              </Text>
              <MetaLine parts={[thread.author, thread.board]} />
            </Pressable>
          ))}

          <ArchiveFooter />
        </>
      }
    />
  );
}
