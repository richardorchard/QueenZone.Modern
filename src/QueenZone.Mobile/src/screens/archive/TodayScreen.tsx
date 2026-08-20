import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type { CompositeScreenProps } from '@react-navigation/native';
import type { BottomTabScreenProps } from '@react-navigation/bottom-tabs';
import { PlaceholderScreen } from '../../ui/PlaceholderScreen';
import type { ArchiveStackParamList, RootTabParamList } from '../../navigation/types';

type Props = CompositeScreenProps<
  NativeStackScreenProps<ArchiveStackParamList, 'Today'>,
  BottomTabScreenProps<RootTabParamList>
>;

export function TodayScreen({ navigation }: Props) {
  return (
    <PlaceholderScreen
      title="Today"
      epic="Epic 1 — Archive"
      access="public"
      headerShown={false}
      description="Home for the public archive. Later this becomes the designed Today feed. Biography, discography, timeline, the Freddie Tribute, and fan performances attach here; News and photography have their own tabs."
      actions={[
        { label: 'News', onPress: () => navigation.navigate('NewsTab', { screen: 'NewsIndex' }), variant: 'outline' },
        { label: 'Biography', onPress: () => navigation.navigate('Biography'), variant: 'outline' },
        { label: 'Discography', onPress: () => navigation.navigate('Discography'), variant: 'outline' },
        { label: 'Timeline', onPress: () => navigation.navigate('Timeline'), variant: 'outline' },
        { label: 'Freddie Tribute', onPress: () => navigation.navigate('FreddieTribute'), variant: 'outline' },
        { label: 'Fan performances', onPress: () => navigation.navigate('FanPerformances'), variant: 'outline' },
        { label: 'Search', onPress: () => navigation.navigate('Search'), variant: 'ghost' },
      ]}
    />
  );
}
