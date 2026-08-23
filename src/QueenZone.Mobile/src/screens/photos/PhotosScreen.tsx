import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type { CompositeScreenProps } from '@react-navigation/native';
import type { BottomTabScreenProps } from '@react-navigation/bottom-tabs';
import { PlaceholderScreen } from '../../ui/PlaceholderScreen';
import { useSession } from '../../session/SessionContext';
import type { PhotosStackParamList, RootTabParamList } from '../../navigation/types';

type Props = CompositeScreenProps<
  NativeStackScreenProps<PhotosStackParamList, 'PhotoIndex'>,
  BottomTabScreenProps<RootTabParamList>
>;

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
        ...(isSignedIn
          ? [
              {
                label: 'My submissions',
                onPress: () => navigation.navigate('YouTab', { screen: 'MySubmissions' }),
                variant: 'outline' as const,
              },
            ]
          : []),
      ]}
    />
  );
}
