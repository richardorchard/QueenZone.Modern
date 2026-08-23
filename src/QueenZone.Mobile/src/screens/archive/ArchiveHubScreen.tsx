import type { BottomTabScreenProps } from '@react-navigation/bottom-tabs';
import type { CompositeScreenProps } from '@react-navigation/native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { FlatList } from 'react-native';
import {
  archiveDestinations,
  type ArchiveDestination,
} from '../../content/sample';
import type { ArchiveStackParamList, RootTabParamList } from '../../navigation/types';
import { useTheme } from '../../theme';
import { ArchiveFooter } from '../../ui/ArchiveFooter';
import { DestinationRow } from '../../ui/DestinationRow';
import { PageTitleBlock } from '../../ui/PageTitleBlock';

type Props = CompositeScreenProps<
  NativeStackScreenProps<ArchiveStackParamList, 'ArchiveHub'>,
  BottomTabScreenProps<RootTabParamList>
>;

export function ArchiveHubScreen({ navigation }: Props) {
  const { c } = useTheme();

  const open = (row: ArchiveDestination) => {
    switch (row.id) {
      case 'stories':
        navigation.navigate('Stories');
        return;
      case 'timeline':
        navigation.navigate('Timeline');
        return;
      case 'biography':
        navigation.navigate('Biography');
        return;
      case 'discography':
        navigation.navigate('Discography');
        return;
      case 'tribute':
        navigation.navigate('FreddieTribute');
        return;
      case 'fan-performances':
        navigation.navigate('FanPerformances');
        return;
      case 'recently-restored':
        navigation.navigate('PhotosTab', { screen: 'PhotoIndex' });
        return;
      case 'about':
        navigation.navigate('AboutArchive');
        return;
    }
  };

  return (
    <FlatList
      style={{ flex: 1, backgroundColor: c.surfacePage }}
      data={archiveDestinations}
      keyExtractor={(item) => item.id}
      ListHeaderComponent={
        <PageTitleBlock
          eyebrow="The Queenzone.com archive"
          title="Explore the archive"
          subtitle="Four thousand articles, a hundred long-form features, tens of thousands of photographs and the community's own history — preserved and catalogued."
        />
      }
      ListFooterComponent={<ArchiveFooter />}
      renderItem={({ item }) => (
        <DestinationRow
          title={item.title}
          kicker={item.kicker}
          kickerRole={item.kickerRole}
          meta={item.meta}
          image={item.image}
          onPress={() => open(item)}
        />
      )}
    />
  );
}
