import type { BottomTabScreenProps } from '@react-navigation/bottom-tabs';
import type { CompositeScreenProps } from '@react-navigation/native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { ChevronRight, Search } from 'lucide-react-native';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { FlatList, Pressable, RefreshControl, ScrollView, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import {
  fetchForumRecentThreads,
  fetchInbox,
  fetchLiveActivity,
  fetchNewsPage,
  fetchOnThisDay,
  fetchPhotoCategories,
  fetchRandomQuote,
  type ForumRecentThread,
  type InboxConversation,
  type NewsListItem,
  type PhotoCategoryListItem,
} from '../../api';
import { media } from '../../content/media';
import { useHomeSection } from '../../hooks/useHomeSection';
import { usePullToRefresh } from '../../hooks/usePullToRefresh';
import { nestedTabParams } from '../../navigation/nestedTab';
import type { HomeStackParamList, RootTabParamList } from '../../navigation/types';
import { useSession } from '../../session/SessionContext';
import { openSignIn } from '../../session/signInNavigation';
import { fonts, radius, space, type, useTheme } from '../../theme';
import { ArchiveFooter } from '../../ui/ArchiveFooter';
import { ArchiveImage } from '../../ui/ArchiveImage';
import { Chip } from '../../ui/Chip';
import { Eyebrow } from '../../ui/Eyebrow';
import { FeatureBlock } from '../../ui/FeatureBlock';
import { HeroFeature } from '../../ui/HeroFeature';
import { IconButton } from '../../ui/IconButton';
import { initials } from '../../ui/initials';
import { MetaLine } from '../../ui/MetaLine';
import { SectionErrorBlock } from '../../ui/ScreenStates';
import { SectionHeader } from '../../ui/SectionHeader';
import { testIds } from '../../test/testIds';
import { syncHomeWidget } from '../../widgets/widgetSync';
import { profileA11yLabel } from '../messages/inboxMeta';
import { useUnreadConversationCount } from '../messages/useUnreadConversationCount';
import {
  formatForumThreadMeta,
  formatGalleryCardMeta,
  homeFilters,
  liveStripIsVisible,
  liveStripLabel,
  onThisDayIsVisible,
  stockImageIndexForId,
  visibleSectionsForFilter,
  type HomeFilterKey,
} from './homeMeta';

type Props = CompositeScreenProps<
  NativeStackScreenProps<HomeStackParamList, 'Home'>,
  BottomTabScreenProps<RootTabParamList>
>;

const stockImages = [media.hero, media.stage, media.crowd, media.portrait, media.studio];

function stockImageForId(id: number): number {
  return stockImages[stockImageIndexForId(id, stockImages.length)];
}

export function HomeScreen({ navigation }: Props) {
  const insets = useSafeAreaInsets();
  const { c, mode } = useTheme();
  const { isSignedIn, displayName, accessToken } = useSession();
  const unreadCount = useUnreadConversationCount();
  const avatar = isSignedIn ? initials(displayName) : '';
  const [filter, setFilter] = useState<HomeFilterKey>('all');
  const visibleSections = useMemo(() => visibleSectionsForFilter(filter), [filter]);

  const news = useHomeSection(
    useCallback((signal) => fetchNewsPage({ page: 1, pageSize: 4, signal }), []),
  );
  const forum = useHomeSection(
    useCallback((signal) => fetchForumRecentThreads(3, signal), []),
  );
  const gallery = useHomeSection(
    useCallback((signal) => fetchPhotoCategories({ page: 1, pageSize: 3, signal }), []),
  );
  const onThisDay = useHomeSection(useCallback((signal) => fetchOnThisDay(signal), []));
  const quote = useHomeSection(useCallback((signal) => fetchRandomQuote(signal), []));
  const liveActivity = useHomeSection(useCallback((signal) => fetchLiveActivity(signal), []));
  const messages = useHomeSection(
    useCallback(
      (signal) =>
        isSignedIn && accessToken
          ? fetchInbox(accessToken, { pageSize: 2, signal })
          : Promise.resolve(null),
      [isSignedIn, accessToken],
    ),
  );

  const openNewsStory = useCallback(
    (id: number) => {
      navigation.navigate('Story', { id });
    },
    [navigation],
  );

  const openThread = useCallback(
    (thread: ForumRecentThread) => {
      navigation.navigate('ForumTab', nestedTabParams('Thread', { id: thread.topicId, title: thread.title }));
    },
    [navigation],
  );

  const openGalleryCategory = useCallback(
    (category: PhotoCategoryListItem) => {
      navigation.navigate(
        'PhotosTab',
        nestedTabParams('PhotoCategory', { slug: category.slug, name: category.name }),
      );
    },
    [navigation],
  );

  const pull = usePullToRefresh([
    news.refresh,
    forum.refresh,
    gallery.refresh,
    onThisDay.refresh,
    quote.refresh,
    liveActivity.refresh,
    messages.refresh,
  ]);

  useEffect(() => {
    if (onThisDay.view.kind === 'skeleton' || quote.view.kind === 'skeleton') {
      return;
    }
    syncHomeWidget({
      onThisDay: onThisDay.view.kind === 'content' ? onThisDay.view.data : null,
      quote: quote.view.kind === 'content' ? quote.view.data : null,
    }).catch(() => {
      /* widget sync is best-effort */
    });
  }, [onThisDay.view, quote.view]);

  const newsItems = news.view.kind === 'content' ? news.view.data.items : [];
  const hero = newsItems[0] ?? null;
  const latestNews = newsItems.slice(1, 4);
  const totalNewsCount = news.view.kind === 'content' ? news.view.data.totalCount : 0;

  return (
    <FlatList
      testID={testIds.homeScreen}
      style={{ flex: 1, backgroundColor: c.surfacePage }}
      data={[]}
      renderItem={() => null}
      refreshControl={
        <RefreshControl refreshing={pull.refreshing} onRefresh={pull.onRefresh} tintColor={c.accentPrimary} />
      }
      ListHeaderComponent={
        <>
          <View
            style={{
              paddingTop: insets.top + 10,
              paddingHorizontal: space.xl,
              paddingBottom: space.md,
              flexDirection: 'row',
              alignItems: 'center',
              justifyContent: 'space-between',
              backgroundColor: c.surfacePage,
            }}
          >
            <View style={{ flexDirection: 'row', alignItems: 'center', gap: 9 }}>
              <ArchiveImage
                source={mode === 'dark' ? media.crestWhite : media.crestBlack}
                label="Queenzone crest"
                style={{ height: 24, width: 24 }}
                contentFit="contain"
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
                testID={testIds.homeSearch}
                accessibilityLabel="Search"
                onPress={() => navigation.navigate('Search')}
              />
              <Pressable
                testID={testIds.homeProfile}
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
                    backgroundColor: c.surfaceCard,
                    borderWidth: 1,
                    borderColor: c.border,
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

          {liveActivity.view.kind === 'content' &&
          liveStripIsVisible(liveActivity.view.data.newForumRepliesToday) ? (
            <View
              style={{
                backgroundColor: '#181614',
                paddingVertical: 9,
                paddingHorizontal: space.xl,
                flexDirection: 'row',
                alignItems: 'center',
                gap: 8,
              }}
            >
              <View style={{ width: 6, height: 6, borderRadius: 3, backgroundColor: '#B89A4A' }} />
              <Text
                numberOfLines={1}
                style={{ fontFamily: fonts.body, fontSize: 12, color: 'rgba(255,255,255,0.72)' }}
              >
                {liveStripLabel(liveActivity.view.data.newForumRepliesToday)}
              </Text>
            </View>
          ) : null}

          <ScrollView
            horizontal
            showsHorizontalScrollIndicator={false}
            contentContainerStyle={{
              paddingHorizontal: space.xl,
              paddingTop: space.md,
              paddingBottom: space.md,
              gap: 8,
            }}
          >
            {homeFilters.map((option) => (
              <Chip
                key={option.key}
                label={option.label}
                active={filter === option.key}
                onPress={() => setFilter(option.key)}
              />
            ))}
          </ScrollView>

          {visibleSections.has('hero') && (
            <>
              {news.view.kind === 'skeleton' ? (
                <View style={{ height: 300, backgroundColor: c.surfaceCard }} />
              ) : news.view.kind === 'error' ? (
                <SectionErrorBlock message={news.view.message} onRetry={news.reload} />
              ) : hero ? (
                <HeroFeature
                  testID={testIds.homeHero}
                  priority="high"
                  height={300}
                  item={{
                    kicker: 'Lead story',
                    title: hero.title,
                    standfirst: hero.excerpt,
                    meta: [],
                    image: stockImageForId(hero.id),
                  }}
                  onPress={() => openNewsStory(hero.id)}
                />
              ) : null}
            </>
          )}

          {visibleSections.has('news') && (
            <>
              <SectionHeader
                title="Latest news"
                actionLabel={totalNewsCount > 0 ? `All ${totalNewsCount.toLocaleString()}+` : 'All'}
                onAction={() => navigation.navigate('NewsTab', { screen: 'NewsIndex' })}
              />
              {news.view.kind === 'skeleton' ? (
                <View style={{ paddingHorizontal: space.xl, gap: 14 }}>
                  {[0, 1, 2].map((key) => (
                    <View key={key} style={{ height: 76, backgroundColor: c.surfaceCard, borderRadius: radius.xs }} />
                  ))}
                </View>
              ) : news.view.kind === 'error' ? (
                <SectionErrorBlock message={news.view.message} onRetry={news.reload} />
              ) : (
                latestNews.map((item: NewsListItem) => (
                  <Pressable
                    key={item.id}
                    accessible
                    accessibilityRole="button"
                    accessibilityLabel={item.title}
                    onPress={() => openNewsStory(item.id)}
                    style={{
                      marginHorizontal: space.xl,
                      paddingVertical: 14,
                      borderTopWidth: 1,
                      borderTopColor: c.hairline,
                      flexDirection: 'row',
                      alignItems: 'center',
                      gap: 14,
                    }}
                  >
                    <View style={{ flex: 1, gap: 6 }}>
                      <Eyebrow tone="accent" size={10}>
                        {new Date(item.publishedAt).toLocaleDateString(undefined, {
                          day: 'numeric',
                          month: 'long',
                          year: 'numeric',
                        })}
                      </Eyebrow>
                      <Text numberOfLines={2} style={[type.listTitle, { color: c.textPrimary }]}>
                        {item.title}
                      </Text>
                    </View>
                    <ArchiveImage
                      source={stockImageForId(item.id)}
                      label={item.title}
                      priority="low"
                      style={{ width: 76, height: 76, borderRadius: radius.xs }}
                    />
                  </Pressable>
                ))
              )}
            </>
          )}

          {visibleSections.has('forum') && (
            <View style={{ marginTop: space.xxl, backgroundColor: '#181614', paddingVertical: 26, paddingHorizontal: space.xl }}>
              <View style={{ flexDirection: 'row', alignItems: 'flex-end', justifyContent: 'space-between', marginBottom: space.md }}>
                <View style={{ gap: 6 }}>
                  <Eyebrow tone="onDark" size={10}>The community</Eyebrow>
                  <Text style={[type.pageTitle, { color: '#F2F1ED', fontSize: 23 }]}>In the forum</Text>
                </View>
                <Pressable
                  accessibilityRole="button"
                  onPress={() => navigation.navigate('ForumTab', { screen: 'ForumIndex' })}
                  hitSlop={8}
                >
                  <Text
                    style={{
                      fontFamily: fonts.bodyMedium,
                      fontSize: 12,
                      letterSpacing: 0.7,
                      textTransform: 'uppercase',
                      color: 'rgba(255,255,255,0.66)',
                    }}
                  >
                    Enter
                  </Text>
                </Pressable>
              </View>

              {forum.view.kind === 'skeleton' ? (
                <View style={{ gap: 12 }}>
                  {[0, 1, 2].map((key) => (
                    <View key={key} style={{ height: 44, backgroundColor: 'rgba(255,255,255,0.06)', borderRadius: radius.xs }} />
                  ))}
                </View>
              ) : forum.view.kind === 'error' ? (
                <SectionErrorBlock message={forum.view.message} onRetry={forum.reload} />
              ) : (
                forum.view.data.map((thread, index) => (
                  <Pressable
                    key={thread.topicId}
                    accessible
                    accessibilityRole="button"
                    accessibilityLabel={thread.title}
                    onPress={() => openThread(thread)}
                    style={{
                      paddingVertical: 14,
                      borderTopWidth: 1,
                      borderTopColor: 'rgba(255,255,255,0.16)',
                      flexDirection: 'row',
                      alignItems: 'center',
                      gap: 12,
                    }}
                  >
                    <View
                      style={{
                        width: 34,
                        height: 34,
                        borderRadius: 17,
                        backgroundColor: 'rgba(255,255,255,0.10)',
                        borderWidth: 1,
                        borderColor: 'rgba(255,255,255,0.18)',
                        alignItems: 'center',
                        justifyContent: 'center',
                      }}
                    >
                      <Text style={{ fontFamily: fonts.display, fontSize: 12, color: 'rgba(255,255,255,0.85)' }}>
                        {initials(thread.categoryName)}
                      </Text>
                    </View>
                    <View style={{ flex: 1, gap: 4 }}>
                      <Text numberOfLines={2} style={{ fontFamily: fonts.bodyMedium, fontSize: 14.5, color: '#FFFFFF' }}>
                        {thread.title}
                      </Text>
                      <MetaLine parts={formatForumThreadMeta(thread)} />
                    </View>
                    {index === 0 ? (
                      <View style={{ width: 6, height: 6, borderRadius: 3, backgroundColor: '#B89A4A' }} />
                    ) : null}
                  </Pressable>
                ))
              )}
            </View>
          )}

          {visibleSections.has('gallery') && (
            <>
              <SectionHeader
                title="New in the gallery"
                actionLabel="Browse"
                onAction={() => navigation.navigate('PhotosTab', { screen: 'PhotoIndex' })}
              />
              {gallery.view.kind === 'skeleton' ? (
                <View style={{ flexDirection: 'row', paddingHorizontal: space.xl, gap: 10 }}>
                  {[0, 1, 2].map((key) => (
                    <View key={key} style={{ width: 148, height: 148, backgroundColor: c.surfaceCard, borderRadius: radius.xs }} />
                  ))}
                </View>
              ) : gallery.view.kind === 'error' ? (
                <SectionErrorBlock message={gallery.view.message} onRetry={gallery.reload} />
              ) : (
                <ScrollView
                  horizontal
                  showsHorizontalScrollIndicator={false}
                  contentContainerStyle={{ paddingHorizontal: space.xl, gap: 10 }}
                >
                  {gallery.view.data.items.map((category) => (
                    <Pressable
                      key={category.catId}
                      accessible
                      accessibilityRole="button"
                      accessibilityLabel={category.name}
                      onPress={() => openGalleryCategory(category)}
                      style={{ width: 148, gap: 9 }}
                    >
                      {category.coverThumbnailUrl ? (
                        <ArchiveImage
                          source={{ uri: category.coverThumbnailUrl }}
                          label={category.name}
                          priority="low"
                          style={{ width: 148, height: 148, borderRadius: radius.xs }}
                        />
                      ) : (
                        <View style={{ width: 148, height: 148, borderRadius: radius.xs, backgroundColor: c.surfaceCard }} />
                      )}
                      <Text style={[type.cardTitle, { fontSize: 14, color: c.textPrimary }]}>{category.name}</Text>
                      <MetaLine parts={[formatGalleryCardMeta(category)]} />
                    </Pressable>
                  ))}
                </ScrollView>
              )}
            </>
          )}

          {isSignedIn ? (
            <View
              style={{
                marginTop: space.xxl,
                backgroundColor: c.surfaceRaised,
                paddingVertical: 26,
                paddingHorizontal: space.xl,
                borderTopWidth: 1,
                borderBottomWidth: 1,
                borderColor: c.hairline,
              }}
            >
              <SectionHeader
                title="Your messages"
                actionLabel="Inbox"
                onAction={() => navigation.navigate('Inbox')}
              />
              {messages.view.kind === 'skeleton' ? (
                <View style={{ gap: 12 }}>
                  {[0, 1].map((key) => (
                    <View key={key} style={{ height: 44, backgroundColor: c.surfaceCard, borderRadius: radius.xs }} />
                  ))}
                </View>
              ) : messages.view.kind === 'error' ? (
                <SectionErrorBlock message={messages.view.message} onRetry={messages.reload} />
              ) : messages.view.kind === 'content' &&
                messages.view.data !== null &&
                messages.view.data.items.length > 0 ? (
                messages.view.data.items.map((conversation: InboxConversation) => (
                  <Pressable
                    key={conversation.conversationId}
                    accessible
                    accessibilityRole="button"
                    accessibilityLabel={conversation.otherParticipantDisplayName}
                    onPress={() => navigation.navigate('Conversation', { id: conversation.conversationId })}
                    style={{
                      paddingVertical: 13,
                      borderTopWidth: 1,
                      borderTopColor: c.hairline,
                      flexDirection: 'row',
                      alignItems: 'center',
                      gap: 12,
                    }}
                  >
                    <View
                      style={{
                        width: 34,
                        height: 34,
                        borderRadius: 17,
                        backgroundColor: c.surfaceSheet,
                        borderWidth: 1,
                        borderColor: c.border,
                        alignItems: 'center',
                        justifyContent: 'center',
                      }}
                    >
                      <Text style={{ fontFamily: fonts.display, fontSize: 12, color: c.textPrimary }}>
                        {initials(conversation.otherParticipantDisplayName)}
                      </Text>
                    </View>
                    <View style={{ flex: 1, gap: 4 }}>
                      <Text style={{ fontFamily: fonts.bodySemi, fontSize: 14.5, color: c.textPrimary }}>
                        {conversation.otherParticipantDisplayName}
                      </Text>
                      <Text numberOfLines={1} style={[type.body, { fontSize: 13, color: c.textSecondary }]}>
                        {conversation.lastMessagePreview}
                      </Text>
                    </View>
                    {conversation.hasUnread ? (
                      <View style={{ width: 7, height: 7, borderRadius: 3.5, backgroundColor: c.accentPrimary }} />
                    ) : null}
                  </Pressable>
                ))
              ) : null}
            </View>
          ) : (
            <Pressable
              accessibilityRole="button"
              accessibilityLabel="Member sign in"
              onPress={() => openSignIn(navigation, { tab: 'HomeTab', screen: 'Profile' })}
              style={{
                marginTop: space.xxl,
                backgroundColor: c.surfaceRaised,
                paddingVertical: 18,
                paddingHorizontal: space.xl,
                borderTopWidth: 1,
                borderBottomWidth: 1,
                borderColor: c.hairline,
                flexDirection: 'row',
                alignItems: 'center',
                justifyContent: 'space-between',
              }}
            >
              <Text style={[type.body, { color: c.textSecondary }]}>Member sign in</Text>
              <ChevronRight size={17} color={c.textMuted} strokeWidth={1.5} />
            </Pressable>
          )}

          {visibleSections.has('onThisDay') &&
          onThisDay.view.kind === 'content' &&
          onThisDayIsVisible(onThisDay.view.data) ? (
            <FeatureBlock
              eyebrow="On this day"
              numeral={onThisDay.view.data.formattedDate.toUpperCase()}
              body={onThisDay.view.data.summary}
              quote={
                quote.view.kind === 'content' && quote.view.data
                  ? { text: quote.view.data.text, whoSaid: quote.view.data.whoSaid }
                  : undefined
              }
              actionLabel="View timeline"
              onAction={() => navigation.navigate('ArchiveTab', { screen: 'Timeline' })}
            />
          ) : null}

          <ArchiveFooter />
        </>
      }
    />
  );
}
