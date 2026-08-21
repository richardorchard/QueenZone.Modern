import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { PlaceholderScreen } from '../../ui/PlaceholderScreen';
import { useSession } from '../../session/SessionContext';
import type { PhotosStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<PhotosStackParamList, 'PhotoIndex'>;

export function PhotosScreen({ navigation }: Props) {
  const { isSignedIn } = useSession();

  return (
    <PlaceholderScreen
      title="Photography"
      epic="Epic 4 — Photo galleries"
      access="public"
      headerShown={false}
      description="Public gallery browse, matching /photography. Submitting a photo is a member action, the same as the website."
      actions={[
        {
          label: 'Open a photograph',
          onPress: () => navigation.navigate('PhotoViewer', { id: 'sample' }),
          variant: 'outline',
        },
        isSignedIn
          ? { label: 'Submit a photo', onPress: () => navigation.navigate('PhotoSubmit') }
          : { label: 'Sign in to submit', onPress: () => navigation.navigate('PhotoSubmit'), variant: 'ghost' },
      ]}
    />
  );
}
