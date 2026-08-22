import { ChevronRight } from 'lucide-react-native';
import { Platform, Pressable, Text, View } from 'react-native';
import type { BadgeRole } from '../content/sample';
import { radius, space, type, useTheme } from '../theme';
import { ArchiveImage } from './ArchiveImage';
import { Badge } from './Badge';
import { MetaLine } from './MetaLine';
import { usePressProps } from './press';

type Props = {
  title: string;
  kicker: string;
  kickerRole: BadgeRole;
  meta: string[];
  image: number;
  onPress: () => void;
  showChevron?: boolean;
};

export function DestinationRow({
  title,
  kicker,
  kickerRole,
  meta,
  image,
  onPress,
  showChevron = true,
}: Props) {
  const { c } = useTheme();
  const press = usePressProps();

  return (
    <Pressable
      accessible
      accessibilityRole="button"
      accessibilityLabel={`${kicker}. ${title}. ${meta.join(', ')}`}
      onPress={onPress}
      {...press}
      style={({ pressed }) => [
        {
          flexDirection: 'row',
          alignItems: 'center',
          gap: 15,
          paddingVertical: space.base,
          paddingHorizontal: space.xl,
          borderTopWidth: 1,
          borderTopColor: c.hairline,
        },
        Platform.OS === 'ios' && pressed ? { backgroundColor: 'rgba(255,255,255,0.04)' } : null,
      ]}
    >
      <ArchiveImage
        source={image}
        label={title}
        style={{ width: 84, height: 84, borderRadius: radius.xs }}
      />
      <View style={{ flex: 1, gap: 7 }}>
        <Badge label={kicker} role={kickerRole} />
        <Text
          numberOfLines={2}
          maxFontSizeMultiplier={1.4}
          style={[type.cardTitle, { fontSize: 23, lineHeight: 26, color: c.textPrimary }]}
        >
          {title}
        </Text>
        <MetaLine parts={meta} />
      </View>
      {showChevron ? <ChevronRight size={17} color={c.textMuted} strokeWidth={1.5} /> : null}
    </Pressable>
  );
}
