import { Image } from 'expo-image';
import { Text, View } from 'react-native';
import { media } from '../content/media';
import { fonts, radius, space, type, useTheme } from '../theme';
import { Button } from './Button';
import { Eyebrow } from './Eyebrow';

type Props = {
  eyebrow: string;
  numeral?: string;
  body: string;
  actionLabel: string;
  onAction: () => void;
};

export function FeatureBlock({ eyebrow, numeral, body, actionLabel, onAction }: Props) {
  const { c } = useTheme();
  return (
    <View
      style={{
        marginTop: space.xxl,
        marginHorizontal: space.xl,
        padding: 22,
        backgroundColor: '#181614',
        borderWidth: 1,
        borderColor: 'rgba(184,154,74,0.34)',
        borderRadius: radius.sm,
        overflow: 'hidden',
        gap: space.md,
      }}
    >
      <Image
        source={media.crestWhite}
        style={{
          position: 'absolute',
          right: -30,
          bottom: -34,
          height: 150,
          width: 150,
          opacity: c.crestWatermarkOpacity,
        }}
        contentFit="contain"
        importantForAccessibility="no"
        accessibilityElementsHidden
      />
      <Eyebrow>{eyebrow}</Eyebrow>
      {numeral ? <Text style={[type.numeral, { color: c.textPrimary }]}>{numeral}</Text> : null}
      <Text style={{ fontFamily: fonts.body, fontSize: 15, lineHeight: 24, color: c.textSecondary }}>{body}</Text>
      <View style={{ alignSelf: 'flex-start', marginTop: space.xs }}>
        <Button variant="outline" size="sm" label={actionLabel} onPress={onAction} />
      </View>
    </View>
  );
}
