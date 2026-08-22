import { Pressable, Text, View } from 'react-native';
import { fonts, space, useTheme } from '../theme';
import { Eyebrow } from './Eyebrow';

type Props = {
  title: string;
  actionLabel?: string;
  onAction?: () => void;
};

export function SectionHeader({ title, actionLabel, onAction }: Props) {
  const { c } = useTheme();
  return (
    <View
      style={{
        marginTop: space.xxl,
        marginHorizontal: space.xl,
        paddingBottom: space.md,
        borderBottomWidth: 1,
        borderBottomColor: c.hairline,
        flexDirection: 'row',
        alignItems: 'flex-end',
        justifyContent: 'space-between',
      }}
    >
      <Eyebrow tone="primary" size={11}>
        {title}
      </Eyebrow>
      {actionLabel ? (
        <Pressable onPress={onAction} accessibilityRole="button" hitSlop={10}>
          <Text
            style={{
              fontFamily: fonts.bodyMedium,
              fontSize: 12,
              letterSpacing: 0.7,
              textTransform: 'uppercase',
              color: c.accentPrimary,
            }}
          >
            {actionLabel}
          </Text>
        </Pressable>
      ) : null}
    </View>
  );
}
