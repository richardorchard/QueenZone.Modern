import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { PlaceholderScreen } from '../../ui/PlaceholderScreen';
import type { NewsStackParamList } from '../../navigation/types';

type Props = NativeStackScreenProps<NewsStackParamList, 'NewsIndex'>;

export function NewsIndexScreen({ navigation }: Props) {
  return (
    <PlaceholderScreen
      title="News"
      epic="Epic 1 — Archive"
      access="public"
      headerShown={false}
      description="Public news index. Articles will come from /api/v1/content/news. This tab is the designed News stack; the reader is a pushed Story screen."
      actions={[
        {
          label: 'Open a story',
          onPress: () => navigation.navigate('Story', { id: 'sample' }),
          variant: 'outline',
        },
      ]}
    />
  );
}
