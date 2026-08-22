import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { FlatList } from 'react-native';
import { featuredStories, homeLead } from '../../content/sample';
import type { ArchiveStackParamList } from '../../navigation/types';
import { useTheme } from '../../theme';
import { DestinationRow } from '../../ui/DestinationRow';
import { PageTitleBlock } from '../../ui/PageTitleBlock';

type Props = NativeStackScreenProps<ArchiveStackParamList, 'Stories'>;

const stories = [
  {
    id: 'lead',
    title: homeLead.title,
    kicker: homeLead.kicker,
    kickerRole: 'restored' as const,
    meta: [...homeLead.meta],
    image: homeLead.image,
  },
  ...featuredStories,
];

export function StoriesIndexScreen({ navigation }: Props) {
  const { c } = useTheme();
  return (
    <FlatList
      style={{ flex: 1, backgroundColor: c.surfacePage }}
      data={stories}
      keyExtractor={(item) => item.id}
      ListHeaderComponent={
        <PageTitleBlock eyebrow="Long-form" title="Stories" subtitle="104 features · Editorial" />
      }
      renderItem={({ item }) => (
        <DestinationRow
          title={item.title}
          kicker={item.kicker}
          kickerRole={item.kickerRole}
          meta={item.meta}
          image={item.image}
          onPress={() => navigation.navigate('Story', { id: 0 })}
        />
      )}
    />
  );
}
