import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { PlaceholderScreen } from '../../ui/PlaceholderScreen';
import type { ArchiveStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<ArchiveStackParamList, 'Story'>;

export function StoryScreen({ route }: Props) {
  return (
    <PlaceholderScreen
      title="Article"
      epic="Epic 1 — Archive"
      access="public"
      description={`Public article reader placeholder (${route.params.id}). Tab bar is hidden on this pushed route.`}
    />
  );
}
