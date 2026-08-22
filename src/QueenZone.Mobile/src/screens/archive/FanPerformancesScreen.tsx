import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { PlaceholderScreen } from '../../ui/PlaceholderScreen';
import { useSession } from '../../session/SessionContext';
import type { ArchiveStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<ArchiveStackParamList, 'FanPerformances'>;

export function FanPerformancesScreen({ navigation }: Props) {
  const { isSignedIn } = useSession();

  return (
    <PlaceholderScreen
      title="Fan performances"
      epic="Epic 5 — Fan performances"
      access="public"
      description="The listing is public, matching /fan-performances on the website. Streaming audio stays member-only, the same as the web archive."
      actions={[
        {
          label: 'Open a recording',
          onPress: () => navigation.navigate('FanPerformanceDetail', { id: 'sample' }),
          variant: 'outline',
        },
        ...(isSignedIn
          ? []
          : [
              {
                label: 'Sign in to stream audio',
                onPress: () => navigation.getParent()?.navigate('HomeTab', { screen: 'SignIn' }),
              },
            ]),
      ]}
    />
  );
}
