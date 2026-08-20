import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { PlaceholderScreen } from '../../ui/PlaceholderScreen';
import type { PhotosStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<PhotosStackParamList, 'PhotoViewer'>;

export function PhotoViewerScreen({ route }: Props) {
  return (
    <PlaceholderScreen
      title="Photograph"
      epic="Epic 4 — Photo galleries"
      access="public"
      description={`Public photo viewer placeholder (${route.params.id}).`}
    />
  );
}
