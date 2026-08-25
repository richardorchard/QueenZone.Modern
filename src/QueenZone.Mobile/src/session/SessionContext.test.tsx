import { useState } from 'react';
import { Text } from 'react-native';
import { screen, waitFor, userEvent } from '@testing-library/react-native';
import { fetchJson } from '../api/client';
import { authTokensFixture, memberProfilePayload } from '../test/fixtures';
import { renderWithProviders } from '../test/render';
import { SessionProvider, useSession } from './SessionContext';
import * as oauth from './oauth';
import * as tokenStore from './tokenStore';
import * as notifications from '../notifications';

const mockAppConfig = {
  apiBaseUrl: 'http://qz.test',
  appEnv: 'development' as string,
  version: '0.1.0',
};

jest.mock('../config/appConfig', () => ({
  getAppConfig: () => mockAppConfig,
}));

jest.mock('../api/client', () => ({
  fetchJson: jest.fn(),
}));

jest.mock('./oauth', () => ({
  signInWithProvider: jest.fn(),
  refreshAccessToken: jest.fn(),
  revokeRefreshToken: jest.fn(),
  logoutRemote: jest.fn(),
}));

jest.mock('./tokenStore', () => ({
  readStoredSession: jest.fn(),
  writeStoredSession: jest.fn(async (tokens: { accessToken: string; refreshToken: string; expiresIn: number }) => ({
    ...tokens,
    expiresAt: Date.now() + 60_000,
  })),
  clearStoredSession: jest.fn(async () => {}),
}));

jest.mock('../notifications', () => ({
  syncPushRegistration: jest.fn(async () => {}),
  refreshPushRegistration: jest.fn(async () => {}),
  clearPushRegistration: jest.fn(async () => {}),
}));

const fetchJsonMock = fetchJson as jest.MockedFunction<typeof fetchJson>;
const readStored = tokenStore.readStoredSession as jest.MockedFunction<typeof tokenStore.readStoredSession>;
const writeStored = tokenStore.writeStoredSession as jest.MockedFunction<typeof tokenStore.writeStoredSession>;
const clearStored = tokenStore.clearStoredSession as jest.MockedFunction<typeof tokenStore.clearStoredSession>;
const signInWithProvider = oauth.signInWithProvider as jest.MockedFunction<typeof oauth.signInWithProvider>;
const refreshAccessToken = oauth.refreshAccessToken as jest.MockedFunction<typeof oauth.refreshAccessToken>;
const logoutRemote = oauth.logoutRemote as jest.MockedFunction<typeof oauth.logoutRemote>;
const revokeRefreshToken = oauth.revokeRefreshToken as jest.MockedFunction<typeof oauth.revokeRefreshToken>;
const syncPushRegistration = notifications.syncPushRegistration as jest.MockedFunction<
  typeof notifications.syncPushRegistration
>;
const clearPushRegistration = notifications.clearPushRegistration as jest.MockedFunction<
  typeof notifications.clearPushRegistration
>;

function Probe() {
  const session = useSession();
  const [smokeResult, setSmokeResult] = useState('smoke-idle');
  return (
    <>
      <Text>{session.isRestoring ? 'restoring' : session.isSignedIn ? 'signed-in' : 'signed-out'}</Text>
      <Text>{session.displayName ?? 'anonymous'}</Text>
      <Text>{session.accessToken ?? 'no-token'}</Text>
      <Text>{smokeResult}</Text>
      <Text
        onPress={() => {
          void session.signIn('Google').catch(() => {});
        }}
      >
        do-sign-in
      </Text>
      <Text onPress={() => void session.signOut()}>do-sign-out</Text>
      <Text
        onPress={() => {
          void session.applySmokeSession('smoke-access').then((ok) => {
            setSmokeResult(ok ? 'smoke-applied' : 'smoke-rejected');
          });
        }}
      >
        do-smoke-auth
      </Text>
      <Text onPress={() => session.setAccessToken('manual-token')}>set-token</Text>
      <Text onPress={() => session.setAccessToken(null)}>clear-token</Text>
      <Text onPress={() => session.setAccessToken('')}>set-empty-token</Text>
    </>
  );
}

function renderSession() {
  return renderWithProviders(
    <SessionProvider>
      <Probe />
    </SessionProvider>,
    { navigation: false },
  );
}

beforeEach(() => {
  mockAppConfig.appEnv = 'development';
  fetchJsonMock.mockReset();
  readStored.mockReset();
  writeStored.mockReset();
  writeStored.mockImplementation(async (tokens) => ({
    ...tokens,
    expiresAt: Date.now() + 60_000,
  }));
  clearStored.mockReset();
  signInWithProvider.mockReset();
  refreshAccessToken.mockReset();
  logoutRemote.mockReset();
  revokeRefreshToken.mockReset();
  syncPushRegistration.mockReset();
  clearPushRegistration.mockReset();
  fetchJsonMock.mockResolvedValue(memberProfilePayload());
  logoutRemote.mockResolvedValue(undefined);
  revokeRefreshToken.mockResolvedValue(undefined);
  syncPushRegistration.mockResolvedValue(undefined);
  clearPushRegistration.mockResolvedValue(undefined);
});

describe('SessionProvider', () => {
  it('finishes restore with no stored session as signed out', async () => {
    readStored.mockResolvedValue(null);
    renderSession();
    await waitFor(() => expect(screen.getByText('signed-out')).toBeOnTheScreen());
    expect(screen.getByText('no-token')).toBeOnTheScreen();
  });

  it('rejects the smoke session when __DEV__ is false', async () => {
    const user = userEvent.setup();
    const previous = (globalThis as { __DEV__: boolean }).__DEV__;
    (globalThis as { __DEV__: boolean }).__DEV__ = false;
    try {
      readStored.mockResolvedValue(null);
      renderSession();
      await waitFor(() => expect(screen.getByText('signed-out')).toBeOnTheScreen());

      await user.press(screen.getByText('do-smoke-auth'));
      await waitFor(() => expect(screen.getByText('smoke-rejected')).toBeOnTheScreen());
      expect(screen.getByText('signed-out')).toBeOnTheScreen();
      expect(writeStored).not.toHaveBeenCalled();
    } finally {
      (globalThis as { __DEV__: boolean }).__DEV__ = previous;
    }
  });

  it('restores a valid stored access token and profile', async () => {
    readStored.mockResolvedValue({
      ...authTokensFixture(),
      expiresAt: Date.now() + 60_000,
    });
    renderSession();
    await waitFor(() => expect(screen.getByText('signed-in')).toBeOnTheScreen());
    expect(screen.getByText('Freddie')).toBeOnTheScreen();
    expect(refreshAccessToken).not.toHaveBeenCalled();
  });

  it('refreshes an expired stored token', async () => {
    readStored.mockResolvedValue({
      ...authTokensFixture(),
      expiresAt: Date.now() - 1_000,
    });
    refreshAccessToken.mockResolvedValue(authTokensFixture({ accessToken: 'next' }));
    renderSession();
    await waitFor(() => expect(screen.getByText('signed-in')).toBeOnTheScreen());
    expect(refreshAccessToken).toHaveBeenCalledWith('http://qz.test', 'refresh-token');
  });

  it('clears local state when refresh fails', async () => {
    readStored.mockResolvedValue({
      ...authTokensFixture(),
      expiresAt: Date.now() - 1_000,
    });
    refreshAccessToken.mockRejectedValue(new Error('expired'));
    renderSession();
    await waitFor(() => expect(screen.getByText('signed-out')).toBeOnTheScreen());
    expect(clearStored).toHaveBeenCalled();
  });

  it('signs in through the provider hop and can sign out', async () => {
    const user = userEvent.setup();
    readStored.mockResolvedValue(null);
    signInWithProvider.mockResolvedValue(authTokensFixture());
    renderSession();
    await waitFor(() => expect(screen.getByText('signed-out')).toBeOnTheScreen());

    await user.press(screen.getByText('do-sign-in'));
    await waitFor(() => expect(screen.getByText('signed-in')).toBeOnTheScreen());
    await waitFor(() => expect(screen.getByText('Freddie')).toBeOnTheScreen());
    expect(signInWithProvider).toHaveBeenCalledWith('http://qz.test', 'Google');
    await waitFor(() => expect(syncPushRegistration).toHaveBeenCalledWith(authTokensFixture().accessToken));

    await user.press(screen.getByText('do-sign-out'));
    await waitFor(() => expect(screen.getByText('signed-out')).toBeOnTheScreen());
    expect(clearPushRegistration).toHaveBeenCalledWith(authTokensFixture().accessToken);
    expect(logoutRemote).toHaveBeenCalled();
    expect(revokeRefreshToken).toHaveBeenCalled();
    expect(clearStored).toHaveBeenCalled();
  });

  it('does not register for push before sign-in', async () => {
    readStored.mockResolvedValue(null);
    renderSession();
    await waitFor(() => expect(screen.getByText('signed-out')).toBeOnTheScreen());
    expect(syncPushRegistration).not.toHaveBeenCalled();
  });

  it('registers for push when a stored session is restored', async () => {
    readStored.mockResolvedValue({
      ...authTokensFixture(),
      expiresAt: Date.now() + 60_000,
    });
    renderSession();
    await waitFor(() => expect(screen.getByText('signed-in')).toBeOnTheScreen());
    await waitFor(() => expect(syncPushRegistration).toHaveBeenCalledWith(authTokensFixture().accessToken));
  });

  it('stays signed out when the provider hop is cancelled', async () => {
    const user = userEvent.setup();
    readStored.mockResolvedValue(null);
    signInWithProvider.mockRejectedValue(new Error('Sign-in was cancelled.'));
    renderSession();
    await waitFor(() => expect(screen.getByText('signed-out')).toBeOnTheScreen());

    await user.press(screen.getByText('do-sign-in'));
    expect(signInWithProvider).toHaveBeenCalledWith('http://qz.test', 'Google');
    expect(screen.getByText('signed-out')).toBeOnTheScreen();
    expect(clearStored).not.toHaveBeenCalled();
  });

  it('applies a Debug smoke access token without the OAuth hop when appEnv is development', async () => {
    const user = userEvent.setup();
    readStored.mockResolvedValue(null);
    renderSession();
    await waitFor(() => expect(screen.getByText('signed-out')).toBeOnTheScreen());

    await user.press(screen.getByText('do-smoke-auth'));
    await waitFor(() => expect(screen.getByText('signed-in')).toBeOnTheScreen());
    expect(screen.getByText('smoke-applied')).toBeOnTheScreen();
    expect(signInWithProvider).not.toHaveBeenCalled();
    expect(writeStored).toHaveBeenCalledWith({
      accessToken: 'smoke-access',
      refreshToken: 'smoke-debug-no-refresh',
      expiresIn: 3600,
    });
  });

  it.each(['staging', 'production'] as const)(
    'rejects the smoke session when appEnv is %s',
    async (appEnv) => {
      const user = userEvent.setup();
      mockAppConfig.appEnv = appEnv;
      readStored.mockResolvedValue(null);
      renderSession();
      await waitFor(() => expect(screen.getByText('signed-out')).toBeOnTheScreen());

      await user.press(screen.getByText('do-smoke-auth'));
      await waitFor(() => expect(screen.getByText('smoke-rejected')).toBeOnTheScreen());
      expect(screen.getByText('signed-out')).toBeOnTheScreen();
      expect(screen.getByText('no-token')).toBeOnTheScreen();
      expect(writeStored).not.toHaveBeenCalled();
    },
  );

  it('derives isSignedIn from setAccessToken and stays signed out without a token', async () => {
    const user = userEvent.setup();
    readStored.mockResolvedValue(null);
    renderSession();
    await waitFor(() => expect(screen.getByText('signed-out')).toBeOnTheScreen());

    await user.press(screen.getByText('clear-token'));
    expect(screen.getByText('signed-out')).toBeOnTheScreen();
    expect(screen.getByText('no-token')).toBeOnTheScreen();

    await user.press(screen.getByText('set-empty-token'));
    expect(screen.getByText('signed-out')).toBeOnTheScreen();
    expect(screen.getByText('no-token')).toBeOnTheScreen();

    await user.press(screen.getByText('set-token'));
    expect(screen.getByText('signed-in')).toBeOnTheScreen();
    expect(screen.getByText('manual-token')).toBeOnTheScreen();

    await user.press(screen.getByText('clear-token'));
    expect(screen.getByText('signed-out')).toBeOnTheScreen();
    expect(screen.getByText('no-token')).toBeOnTheScreen();
  });

  it('stays signed in when profile load fails', async () => {
    readStored.mockResolvedValue({
      ...authTokensFixture(),
      expiresAt: Date.now() + 60_000,
    });
    fetchJsonMock.mockRejectedValue(new Error('offline'));
    renderSession();
    await waitFor(() => expect(screen.getByText('signed-in')).toBeOnTheScreen());
    expect(screen.getByText('anonymous')).toBeOnTheScreen();
  });
});
