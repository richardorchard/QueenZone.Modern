import { Pressable, ScrollView, Text } from 'react-native';
import type { FeatureItem } from '../content/sample';
import { radius, space, type, useTheme } from '../theme';
import { ArchiveImage } from './ArchiveImage';
import { Badge } from './Badge';
import { MetaLine } from './MetaLine';
import { usePressProps } from './press';

type Props = {
  items: FeatureItem[];
  onOpen: (item: FeatureItem) => void;
};

export function FeatureRail({ items, onOpen }: Props) {
  const { c } = useTheme();
  const press = usePressProps();

  return (
    <ScrollView
      horizontal
      showsHorizontalScrollIndicator={false}
      snapToInterval={230}
      decelerationRate="fast"
      contentContainerStyle={{ paddingHorizontal: space.xl, gap: 14, paddingVertical: space.base }}
    >
      {items.map((item) => (
        <Pressable
          key={item.id}
          accessible
          accessibilityRole="button"
          accessibilityLabel={`${item.kicker}. ${item.title}. ${item.meta.join(', ')}`}
          onPress={() => onOpen(item)}
          style={{ width: 216, gap: 11 }}
          {...press}
        >
          <ArchiveImage
            source={item.image}
            label={item.title}
            style={{ width: 216, height: 150, borderRadius: radius.xs }}
          />
          <Badge label={item.kicker} role={item.kickerRole} />
          <Text numberOfLines={3} maxFontSizeMultiplier={1.4} style={[type.cardTitle, { color: c.textPrimary }]}>
            {item.title}
          </Text>
          <MetaLine parts={item.meta} />
        </Pressable>
      ))}
    </ScrollView>
  );
}
