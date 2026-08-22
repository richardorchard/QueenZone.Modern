import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { MemberGate } from '../../session/MemberGate';
import type { HomeStackParamList } from '../../navigation/types';
import { PlaceholderScreen } from '../../ui/PlaceholderScreen';

type Props = NativeStackScreenProps<HomeStackParamList, 'SavedList'>;

const titles: Record<HomeStackParamList['SavedList']['kind'], string> = {
  articles: 'Saved articles',
  photographs: 'Saved photographs',
  offline: 'Downloaded for offline',
  history: 'Reading history',
};

export function SavedListScreen({ route }: Props) {
  const title = titles[route.params.kind];
  return (
    <MemberGate title={title}>
      <PlaceholderScreen
        title={title}
        epic="Library"
        access="member"
        description="Saved and offline library lists attach here once the member API is wired."
      />
    </MemberGate>
  );
}
