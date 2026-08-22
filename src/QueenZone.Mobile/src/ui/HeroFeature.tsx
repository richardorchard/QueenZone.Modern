import { LinearGradient } from 'expo-linear-gradient';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { fonts, imagery, space, type, useTheme } from '../theme';
import { ArchiveImage } from './ArchiveImage';
import { Eyebrow } from './Eyebrow';
import { MetaLine } from './MetaLine';
import { usePressProps } from './press';

type Props = {
  item: {
    kicker: string;
    title: string;
    standfirst: string;
    meta: readonly string[] | string[];
    image: number | { uri: string };
  };
  onPress: () => void;
  height?: number;
};

export function HeroFeature({ item, onPress, height = 468 }: Props) {
  const { c } = useTheme();
  const press = usePressProps();

  return (
    <Pressable
      accessible
      accessibilityRole="button"
      accessibilityLabel={`${item.kicker}. ${item.title}. ${item.standfirst}`}
      onPress={onPress}
      style={{ height }}
      {...press}
    >
      <ArchiveImage source={item.image} label={item.title} style={StyleSheet.absoluteFill} />
      <LinearGradient
        colors={imagery.scrimBottom as unknown as [string, string, ...string[]]}
        locations={imagery.scrimStops as unknown as [number, number, ...number[]]}
        style={StyleSheet.absoluteFill}
      />
      <View style={{ position: 'absolute', left: space.xl, right: space.xl, bottom: 28, gap: space.md }}>
        <Eyebrow>{item.kicker}</Eyebrow>
        <Text numberOfLines={3} maxFontSizeMultiplier={1.4} style={[type.heroTitle, { color: c.textPrimary }]}>
          {item.title}
        </Text>
        <Text
          numberOfLines={3}
          style={{ fontFamily: fonts.body, fontSize: 15, lineHeight: 23, color: c.textSecondary }}
        >
          {item.standfirst}
        </Text>
        <MetaLine parts={[...item.meta]} />
      </View>
    </Pressable>
  );
}
