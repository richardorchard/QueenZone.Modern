import { screen, userEvent, waitFor } from '@testing-library/react-native';
import * as ImagePicker from 'expo-image-picker';
import { fetchPhotoCategories } from '../../api';
import { pagedResponse } from '../../test/fixtures';
import { createMockSession } from '../../test/mockSession';
import { fakeNavigation, renderWithProviders } from '../../test/render';
import { PhotoSubmitScreen } from './PhotoSubmitScreen';

const mockSession = createMockSession();

jest.mock('../../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

jest.mock('@react-navigation/native', () => {
  const actual = jest.requireActual('@react-navigation/native');
  return {
    ...actual,
    useNavigation: () => ({ navigate: jest.fn() }),
  };
});

jest.mock('../../api', () => {
  const actual = jest.requireActual('../../api');
  return {
    ...actual,
    fetchPhotoCategories: jest.fn(),
  };
});

jest.mock('expo-image-picker', () => ({
  requestMediaLibraryPermissionsAsync: jest.fn(),
  requestCameraPermissionsAsync: jest.fn(),
  launchImageLibraryAsync: jest.fn(),
  launchCameraAsync: jest.fn(),
  UIImagePickerPreferredAssetRepresentationMode: { Compatible: 'compatible' },
}));

const fetchCategories = fetchPhotoCategories as jest.MockedFunction<typeof fetchPhotoCategories>;

function renderSubmit() {
  return renderWithProviders(
    <PhotoSubmitScreen
      navigation={fakeNavigation() as never}
      route={{ key: 'submit', name: 'PhotoSubmit' } as never}
    />,
  );
}

describe('PhotoSubmitScreen', () => {
  beforeEach(() => {
    mockSession.isSignedIn = false;
    mockSession.accessToken = null;
    fetchCategories.mockResolvedValue(pagedResponse([], 1, 0));
  });

  it('gates unsigned visitors', () => {
    renderSubmit();
    expect(screen.getByText('Submit a photo')).toBeOnTheScreen();
    expect(screen.getByRole('button', { name: 'Sign in' })).toBeOnTheScreen();
  });

  it('validates a missing photo and reports library permission denial', async () => {
    const user = userEvent.setup();
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    (ImagePicker.requestMediaLibraryPermissionsAsync as jest.Mock).mockResolvedValue({ granted: false });
    renderSubmit();
    await waitFor(() => expect(screen.getByRole('button', { name: 'Submit for review' })).toBeOnTheScreen());
    await user.press(screen.getByRole('button', { name: 'Submit for review' }));
    expect(screen.getByText('Title is required.')).toBeOnTheScreen();
    await user.press(screen.getByRole('button', { name: 'Choose from library' }));
    await waitFor(() =>
      expect(screen.getByText('Photo library permission is required to choose a photo.')).toBeOnTheScreen(),
    );
  });
});
