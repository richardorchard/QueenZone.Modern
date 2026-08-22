import { ChevronRight } from 'lucide-react-native';
import { Platform, Pressable, Switch, Text, View } from 'react-native';
import { space, type, useTheme } from '../theme';
import { usePressProps } from './press';

type Props = {
  title: string;
  subtitle?: string;
  value?: string;
  onPress?: () => void;
  switchValue?: boolean;
  onSwitch?: (value: boolean) => void;
};

export function SettingsRow({ title, subtitle, value, onPress, switchValue, onSwitch }: Props) {
  const { c } = useTheme();
  const press = usePressProps();
  const content = (
    <View
      style={{
        flexDirection: 'row',
        alignItems: 'center',
        gap: 12,
        paddingVertical: 16,
        paddingHorizontal: space.xl,
        borderTopWidth: 1,
        borderTopColor: c.hairline,
        minHeight: 56,
      }}
    >
      <View style={{ flex: 1, gap: 4 }}>
        <Text style={[type.listTitle, { color: c.textPrimary }]}>{title}</Text>
        {subtitle ? <Text style={[type.caption, { color: c.textMuted }]}>{subtitle}</Text> : null}
      </View>
      {onSwitch ? (
        <Switch
          value={switchValue}
          onValueChange={onSwitch}
          trackColor={{ true: c.accentPrimary, false: c.border }}
          thumbColor={c.textPrimary}
        />
      ) : (
        <>
          {value ? <Text style={[type.caption, { color: c.accentPrimary }]}>{value}</Text> : null}
          <ChevronRight size={17} color={c.textMuted} strokeWidth={1.5} />
        </>
      )}
    </View>
  );

  if (!onPress || onSwitch) {
    return content;
  }

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={title}
      onPress={onPress}
      {...press}
      style={({ pressed }) => (Platform.OS === 'ios' && pressed ? { backgroundColor: 'rgba(255,255,255,0.04)' } : null)}
    >
      {content}
    </Pressable>
  );
}
