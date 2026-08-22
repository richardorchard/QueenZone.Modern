import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useMemo, useState } from 'react';
import { Dimensions, FlatList, Pressable, ScrollView, Text, View } from 'react-native';
import { photoCategories, samplePhotos } from '../../content/sample';
import type { PhotosStackParamList } from '../../navigation/types';
import { useSession } from '../../session/SessionContext';
import { space, type, useTheme } from '../../theme';
import { ArchiveImage } from '../../ui/ArchiveImage';
import { Button } from '../../ui/Button';
import { Chip } from '../../ui/Chip';
import { PageTitleBlock } from '../../ui/PageTitleBlock';

type Props = NativeStackScreenProps<PhotosStackParamList, 'PhotoIndex'>;

const GAP = 3;
const COLS = 3;

export function PhotosScreen({ navigation }: Props) {
  const { c } = useTheme();
  const { isSignedIn } = useSession();
  const [category, setCategory] = useState<(typeof photoCategories)[number]>('ALL');
  const width = Dimensions.get('window').width;
  const tile = (width - GAP * (COLS - 1) - GAP * 2) / COLS;

  const photos = useMemo(
    () => (category === 'ALL' ? samplePhotos : samplePhotos.filter((photo) => photo.category === category)),
    [category],
  );

  return (
    <FlatList
      style={{ flex: 1, backgroundColor: c.surfacePage }}
      data={photos}
      keyExtractor={(item) => item.id}
      numColumns={COLS}
      ListHeaderComponent={
        <View>
          <PageTitleBlock
            eyebrow="The archive"
            title="Photography"
            subtitle="Tens of thousands of frames · 1,240 restored"
          />
          <ScrollView
            horizontal
            showsHorizontalScrollIndicator={false}
            contentContainerStyle={{ paddingHorizontal: space.xl, gap: 8, paddingBottom: 16 }}
          >
            {photoCategories.map((item) => (
              <Chip key={item} label={item} active={category === item} onPress={() => setCategory(item)} />
            ))}
          </ScrollView>
        </View>
      }
      columnWrapperStyle={{ gap: GAP, paddingHorizontal: GAP }}
      contentContainerStyle={{ paddingBottom: space.section }}
      renderItem={({ item }) => (
        <Pressable
          accessibilityRole="button"
          accessibilityLabel={item.caption}
          onPress={() => navigation.navigate('PhotoViewer', { id: item.id })}
          style={{ width: tile, marginBottom: GAP }}
        >
          <ArchiveImage
            source={item.image}
            label={item.caption}
            recyclingKey={item.id}
            style={{ width: tile, height: tile }}
          />
        </Pressable>
      )}
      ListFooterComponent={
        <View style={{ paddingTop: 26, alignItems: 'center', gap: space.md }}>
          <Text style={[type.eyebrow, { color: c.textMuted }]}>Page 1 of 104</Text>
          {isSignedIn ? (
            <Button
              label="Submit a photo"
              variant="ghost"
              size="sm"
              onPress={() => navigation.navigate('PhotoSubmit')}
            />
          ) : (
            <Button
              label="Sign in to submit"
              variant="ghost"
              size="sm"
              onPress={() => navigation.navigate('PhotoSubmit')}
            />
          )}
        </View>
      }
    />
  );
}
