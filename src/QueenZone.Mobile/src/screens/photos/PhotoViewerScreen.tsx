import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { Bookmark, BookmarkCheck, Share2, X } from 'lucide-react-native';
import { useMemo, useState } from 'react';
import { Pressable, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { samplePhotos } from '../../content/sample';
import type { PhotosStackParamList } from '../../navigation/types';
import { type, useTheme } from '../../theme';
import { ArchiveImage } from '../../ui/ArchiveImage';
import { IconButton } from '../../ui/IconButton';
import { MetaLine } from '../../ui/MetaLine';

type Props = NativeStackScreenProps<PhotosStackParamList, 'PhotoViewer'>;

export function PhotoViewerScreen({ navigation, route }: Props) {
  const insets = useSafeAreaInsets();
  const { c } = useTheme();
  const [chromeVisible, setChromeVisible] = useState(true);
  const [saved, setSaved] = useState(false);
  const index = Math.max(
    0,
    samplePhotos.findIndex((photo) => photo.id === route.params.id),
  );
  const photo = samplePhotos[index] ?? samplePhotos[0];
  const label = useMemo(() => `${index + 1} of ${samplePhotos.length}`, [index]);

  return (
    <View style={{ flex: 1, backgroundColor: '#000' }}>
      <Pressable style={{ flex: 1 }} onPress={() => setChromeVisible((value) => !value)}>
        <ArchiveImage
          source={photo.image}
          label={photo.caption}
          contentFit="contain"
          style={{ flex: 1, width: '100%' }}
        />
      </Pressable>
      {chromeVisible ? (
        <>
          <View
            style={{
              position: 'absolute',
              top: insets.top,
              left: 4,
              right: 4,
              flexDirection: 'row',
              alignItems: 'center',
              justifyContent: 'space-between',
            }}
          >
            <IconButton icon={X} accessibilityLabel="Close" onPress={() => navigation.goBack()} />
            <Text style={[type.eyebrow, { color: c.textMuted }]}>{label}</Text>
            <View style={{ flexDirection: 'row' }}>
              <IconButton
                icon={saved ? BookmarkCheck : Bookmark}
                accessibilityLabel={saved ? 'Saved' : 'Not saved'}
                active={saved}
                onPress={() => setSaved((value) => !value)}
              />
              <IconButton icon={Share2} accessibilityLabel="Share" onPress={() => undefined} />
            </View>
          </View>
          <View
            style={{
              position: 'absolute',
              left: 24,
              right: 24,
              bottom: insets.bottom + 24,
              gap: 8,
            }}
          >
            <Text style={[type.cardTitle, { color: c.textPrimary }]}>{photo.caption}</Text>
            <MetaLine parts={photo.meta} />
          </View>
        </>
      ) : null}
    </View>
  );
}
