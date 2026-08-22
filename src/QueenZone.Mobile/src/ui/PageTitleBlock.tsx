import { Text, View } from 'react-native';
import { space, type, useTheme } from '../theme';
import { Eyebrow } from './Eyebrow';

type Props = {
  eyebrow: string;
  title: string;
  subtitle?: string;
};

export function PageTitleBlock({ eyebrow, title, subtitle }: Props) {
  const { c } = useTheme();
  return (
    <View style={{ paddingHorizontal: space.xl, paddingTop: 22, paddingBottom: 16, gap: 8 }}>
      <Eyebrow>{eyebrow}</Eyebrow>
      <Text maxFontSizeMultiplier={1.4} style={[type.pageTitle, { color: c.textPrimary }]}>
        {title}
      </Text>
      {subtitle ? (
        <Text style={[type.caption, { fontSize: 14.5, lineHeight: 23, color: c.textSecondary }]}>{subtitle}</Text>
      ) : null}
    </View>
  );
}
