import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { ScrollView } from 'react-native';
import type { ArchiveStackParamList } from '../../navigation/types';
import { useTheme } from '../../theme';
import { PageTitleBlock } from '../../ui/PageTitleBlock';
import { EmptyBlock } from '../../ui/ScreenStates';

type Props = NativeStackScreenProps<ArchiveStackParamList, 'Articles'>;

/** Empty until the articles API lands (#1186). Do not point this at fetchNewsPage. */
export function ArticlesIndexScreen(_props: Props) {
  const { c } = useTheme();
  return (
    <ScrollView
      style={{ flex: 1, backgroundColor: c.surfacePage }}
      contentContainerStyle={{ flexGrow: 1 }}
    >
      <PageTitleBlock eyebrow="Long-form" title="Articles" subtitle="104 features · Editorial" />
      <EmptyBlock message="No articles yet." />
    </ScrollView>
  );
}
