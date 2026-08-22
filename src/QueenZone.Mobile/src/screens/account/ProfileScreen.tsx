import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useState } from 'react';
import { Pressable, ScrollView, Text, View } from 'react-native';
import { getAppConfig } from '../../config/appConfig';
import type { HomeStackParamList } from '../../navigation/types';
import { useSession } from '../../session/SessionContext';
import { archiveDisclaimer, fonts, space, type, useTheme } from '../../theme';
import { ArchiveFooter } from '../../ui/ArchiveFooter';
import { BuildStamp } from '../../ui/BuildStamp';
import { Button } from '../../ui/Button';
import { CrestSeal } from '../../ui/CrestSeal';
import { Eyebrow } from '../../ui/Eyebrow';
import { SettingsRow } from '../../ui/SettingsRow';

type Props = NativeStackScreenProps<HomeStackParamList, 'Profile'>;

function initials(name: string | null): string {
  if (!name) {
    return '';
  }
  const parts = name.replace(/_/g, ' ').trim().split(/\s+/);
  if (parts.length === 1) {
    return parts[0].slice(0, 2).toUpperCase();
  }
  return `${parts[0][0] ?? ''}${parts[1][0] ?? ''}`.toUpperCase();
}

export function ProfileScreen({ navigation }: Props) {
  const { c } = useTheme();
  const { isSignedIn, displayName, signIn, signOut } = useSession();
  const { appEnv, apiBaseUrl } = getAppConfig();
  const [onThisDay, setOnThisDay] = useState(true);

  if (!isSignedIn) {
    return (
      <ScrollView
        style={{ flex: 1, backgroundColor: c.surfacePage }}
        contentContainerStyle={{
          paddingHorizontal: space.xl,
          paddingTop: space.section,
          paddingBottom: space.section,
          alignItems: 'center',
          gap: space.lg,
        }}
      >
        <CrestSeal height={48} opacity={0.5} />
        <Text style={[type.pageTitle, { color: c.textPrimary, textAlign: 'center' }]}>Join the archive</Text>
        <Text style={[type.body, { color: c.textSecondary, textAlign: 'center' }]}>
          Sign in to save articles, post on the forum, and keep reading history on this device.
        </Text>
        <View style={{ alignSelf: 'stretch', gap: 10, marginTop: space.sm }}>
          <Button label="Sign in" onPress={signIn} />
          <Button label="Create an account" variant="ghost" onPress={signIn} />
        </View>
        <Pressable accessibilityRole="button" onPress={() => navigation.navigate('Contact')}>
          <Text style={[type.button, { color: c.accentPrimary }]}>Contact</Text>
        </Pressable>
        <Text style={[type.caption, { color: c.textMuted, textAlign: 'center' }]}>
          API {appEnv} → {apiBaseUrl}
        </Text>
        <Text style={[type.caption, { color: c.textMuted, textAlign: 'center' }]}>{archiveDisclaimer}</Text>
        <BuildStamp />
      </ScrollView>
    );
  }

  return (
    <ScrollView style={{ flex: 1, backgroundColor: c.surfacePage }}>
      <View
        style={{
          flexDirection: 'row',
          alignItems: 'center',
          gap: 16,
          paddingHorizontal: space.xl,
          paddingVertical: space.xl,
        }}
      >
        <View
          style={{
            width: 62,
            height: 62,
            borderRadius: 31,
            borderWidth: 1,
            borderColor: c.border,
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          <Text style={[type.articleTitle, { color: c.textPrimary }]}>{initials(displayName)}</Text>
        </View>
        <View style={{ flex: 1, gap: 6 }}>
          <Text style={[type.articleTitle, { color: c.textPrimary }]}>{displayName}</Text>
          <Text style={[type.meta, { color: c.accentPrimary }]}>MEMBER SINCE 2004 · 1,208 POSTS</Text>
        </View>
      </View>

      <View style={{ flexDirection: 'row', borderTopWidth: 1, borderBottomWidth: 1, borderColor: c.hairline }}>
        <View style={{ flex: 1, paddingVertical: space.xl, paddingHorizontal: space.xl, gap: 8 }}>
          <Text style={[type.pageTitle, { fontSize: 34, color: c.textPrimary }]}>34</Text>
          <Text style={[type.eyebrow, { fontSize: 9.5, color: c.textMuted }]}>Saved articles</Text>
        </View>
        <View style={{ width: 1, backgroundColor: c.hairline }} />
        <View style={{ flex: 1, paddingVertical: space.xl, paddingHorizontal: space.xl, gap: 8 }}>
          <Text style={[type.pageTitle, { fontSize: 34, color: c.textPrimary }]}>212</Text>
          <Text style={[type.eyebrow, { fontSize: 9.5, color: c.textMuted }]}>Saved photographs</Text>
        </View>
      </View>

      <View style={{ paddingHorizontal: space.xl, paddingTop: space.xxl, paddingBottom: space.md }}>
        <Eyebrow tone="muted">Library</Eyebrow>
      </View>
      <SettingsRow
        title="Saved articles"
        onPress={() => navigation.navigate('SavedList', { kind: 'articles' })}
      />
      <SettingsRow
        title="Saved photographs"
        onPress={() => navigation.navigate('SavedList', { kind: 'photographs' })}
      />
      <SettingsRow
        title="Downloaded for offline"
        onPress={() => navigation.navigate('SavedList', { kind: 'offline' })}
      />
      <SettingsRow
        title="Reading history"
        onPress={() => navigation.navigate('SavedList', { kind: 'history' })}
      />
      <SettingsRow title="Messages" onPress={() => navigation.navigate('Inbox')} />

      <View style={{ paddingHorizontal: space.xl, paddingTop: space.xxl, paddingBottom: space.md }}>
        <Eyebrow tone="muted">Settings</Eyebrow>
      </View>
      <SettingsRow
        title="On This Day notification"
        subtitle="One entry each morning"
        switchValue={onThisDay}
        onSwitch={setOnThisDay}
      />
      <SettingsRow
        title="Appearance"
        subtitle="Dark · follows system"
        value="Change"
        onPress={() => navigation.navigate('Settings')}
      />
      <SettingsRow title="Text size" onPress={() => navigation.navigate('Settings')} />
      <SettingsRow title="Notifications" onPress={() => navigation.navigate('Settings')} />
      <SettingsRow title="Account & privacy" onPress={() => navigation.navigate('Settings')} />
      <SettingsRow title="About the archive" onPress={() => navigation.getParent()?.navigate('ArchiveTab', { screen: 'AboutArchive' })} />
      <SettingsRow title="Contact" onPress={() => navigation.navigate('Contact')} />
      <View style={{ paddingHorizontal: space.xl, paddingTop: space.xl }}>
        <Button label="Sign out (development)" variant="outline" onPress={signOut} />
        <Text style={[type.caption, { color: c.textMuted, marginTop: space.md, fontFamily: fonts.body }]}>
          API {appEnv} → {apiBaseUrl}
        </Text>
      </View>
      <ArchiveFooter />
      <View style={{ paddingHorizontal: space.xl, paddingBottom: space.xl }}>
        <BuildStamp />
      </View>
    </ScrollView>
  );
}
