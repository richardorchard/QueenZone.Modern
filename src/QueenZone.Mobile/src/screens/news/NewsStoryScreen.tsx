import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { PlaceholderScreen } from '../../ui/PlaceholderScreen';
import type { NewsStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<NewsStackParamList, 'Story'>;

export function NewsStoryScreen({ route }: Props) {
  return (
    <PlaceholderScreen
      title="Story"
      epic="Epic 1 — Archive"
      access="public"
      description={`Public news reader placeholder (${route.params.id}).`}
    />
  );
}
