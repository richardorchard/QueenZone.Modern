import { ScrollView, Text } from 'react-native';
import { space, type, useTheme } from '../../theme';
import { ArchiveFooter } from '../../ui/ArchiveFooter';
import { PageTitleBlock } from '../../ui/PageTitleBlock';

export function AboutArchiveScreen() {
  const { c } = useTheme();
  return (
    <ScrollView style={{ flex: 1, backgroundColor: c.surfacePage }}>
      <PageTitleBlock eyebrow="The old site" title="Queenzone.com, preserved" />
      <Text
        style={[
          type.body,
          {
            color: c.textSecondary,
            paddingHorizontal: 26,
            paddingBottom: space.xl,
          },
        ]}
      >
        Queenzone.org publishes the preserved fan archive of Queen material — news, long-form features,
        photography and forum history — rebuilt as a read-only editorial collection. This screen will
        carry the full account of how the archive was restored.
      </Text>
      <ArchiveFooter />
    </ScrollView>
  );
}
