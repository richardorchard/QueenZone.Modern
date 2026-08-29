import { screen, userEvent, waitFor } from '@testing-library/react-native';
import * as DocumentPicker from 'expo-document-picker';
import * as ImagePicker from 'expo-image-picker';
import { ApiError, createForumReply, createForumTopic, fetchForumCategories } from '../../api';
import { pagedResponse } from '../../test/fixtures';
import { createMockSession } from '../../test/mockSession';
import { fakeNavigation, renderWithProviders } from '../../test/render';
import { testIds } from '../../test/testIds';
import { ComposerScreen } from './ComposerScreen';

const mockSession = createMockSession();

jest.mock('../../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

jest.mock('../../api', () => {
  const actual = jest.requireActual('../../api');
  return {
    ...actual,
    createForumReply: jest.fn(),
    createForumTopic: jest.fn(),
    fetchForumCategories: jest.fn(),
  };
});

jest.mock('expo-image-picker', () => ({
  requestMediaLibraryPermissionsAsync: jest.fn(),
  launchImageLibraryAsync: jest.fn(),
  UIImagePickerPreferredAssetRepresentationMode: { Compatible: 'compatible' },
}));

jest.mock('expo-document-picker', () => ({
  getDocumentAsync: jest.fn(),
}));

const createForumReplyMock = createForumReply as jest.MockedFunction<typeof createForumReply>;
const createForumTopicMock = createForumTopic as jest.MockedFunction<typeof createForumTopic>;
const fetchForumCategoriesMock = fetchForumCategories as jest.MockedFunction<typeof fetchForumCategories>;

function renderComposer(
  params: {
    threadId?: number;
    threadTitle?: string;
    categoryId?: number;
    categoryName?: string;
    isLocked?: boolean;
  } = {},
  navigation = fakeNavigation(),
) {
  return {
    navigation,
    ...renderWithProviders(
      <ComposerScreen
        navigation={navigation as never}
        route={{ key: 'composer', name: 'Composer', params } as never}
      />,
    ),
  };
}

describe('ComposerScreen', () => {
  beforeEach(() => {
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    createForumReplyMock.mockReset();
    createForumTopicMock.mockReset();
    fetchForumCategoriesMock.mockReset();
    (ImagePicker.requestMediaLibraryPermissionsAsync as jest.Mock).mockReset();
    (ImagePicker.launchImageLibraryAsync as jest.Mock).mockReset();
    (DocumentPicker.getDocumentAsync as jest.Mock).mockReset();
    fetchForumCategoriesMock.mockResolvedValue(
      pagedResponse([
        {
          id: 1,
          name: 'The Music',
          description: null,
          postCount: 10,
          lastActivityAt: null,
          latestThreadTitle: null,
          detailPath: '/forum/1/the-music',
        },
      ]),
    );
  });

  it('publishes a reply and goes back', async () => {
    createForumReplyMock.mockResolvedValueOnce({
      id: 88,
      topicId: 1002,
      detailPath: '/forum/topic/1002',
    });
    const { navigation } = renderComposer({ threadId: 1002, threadTitle: 'Ranking every studio album' });

    await waitFor(() => expect(screen.getByLabelText('Reply body')).toBeOnTheScreen());
    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Reply body'), 'A reply from mobile');
    await user.press(screen.getByRole('button', { name: 'Post reply' }));

    await waitFor(() =>
      expect(createForumReplyMock).toHaveBeenCalledWith(1002, { body: 'A reply from mobile' }, 'tok'),
    );
    expect(navigation.goBack).toHaveBeenCalled();
    expect(createForumTopicMock).not.toHaveBeenCalled();
  });

  it('publishes a new topic and replaces with Thread', async () => {
    createForumTopicMock.mockResolvedValueOnce({
      id: 2001,
      starterPostId: 1,
      title: 'Fresh forum news',
      detailPath: '/forum/topic/2001/fresh-forum-news',
    });
    const { navigation } = renderComposer();

    await waitFor(() => expect(screen.getByRole('button', { name: 'Post to The Music' })).toBeOnTheScreen());
    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Post to The Music' }));
    await user.type(screen.getByLabelText('Topic title'), 'Fresh forum news');
    await user.type(screen.getByLabelText('Topic body'), 'Hello fans');
    await user.press(screen.getByRole('button', { name: 'Post topic' }));

    await waitFor(() =>
      expect(createForumTopicMock).toHaveBeenCalledWith(1, { title: 'Fresh forum news', body: 'Hello fans' }, 'tok'),
    );
    expect(navigation.replace).toHaveBeenCalledWith('Thread', { id: 2001, title: 'Fresh forum news' });
    expect(createForumReplyMock).not.toHaveBeenCalled();
  });

  it('surfaces an empty-body validation error without calling the API', async () => {
    renderComposer({ threadId: 1002, threadTitle: 'Ranking every studio album' });
    await waitFor(() => expect(screen.getByRole('button', { name: 'Post reply' })).toBeOnTheScreen());

    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Post reply' }));

    await waitFor(() => expect(screen.getByText('Write a post before publishing.')).toBeOnTheScreen());
    expect(createForumReplyMock).not.toHaveBeenCalled();
    expect(createForumTopicMock).not.toHaveBeenCalled();
  });

  it('does not call the API when the topic is locked', async () => {
    renderComposer({ threadId: 1002, threadTitle: 'Locked topic', isLocked: true });
    await waitFor(() => expect(screen.getByText('This topic is locked.')).toBeOnTheScreen());
    expect(screen.queryByRole('button', { name: 'Post reply' })).toBeNull();
    expect(createForumReplyMock).not.toHaveBeenCalled();
  });

  it('hides attach controls when signed out', () => {
    mockSession.isSignedIn = false;
    mockSession.accessToken = null;
    renderComposer({ threadId: 1002, threadTitle: 'Ranking every studio album' });
    expect(screen.getByRole('button', { name: 'Sign in' })).toBeOnTheScreen();
    expect(screen.queryByTestId(testIds.forumComposerAttachPhotos)).toBeNull();
    expect(screen.queryByTestId(testIds.forumComposerAttachFiles)).toBeNull();
    expect(screen.queryByRole('button', { name: 'Photos' })).toBeNull();
    expect(screen.queryByRole('button', { name: 'Files' })).toBeNull();
  });

  it('hides attach controls when the topic is locked', async () => {
    renderComposer({ threadId: 1002, threadTitle: 'Locked topic', isLocked: true });
    await waitFor(() => expect(screen.getByText('This topic is locked.')).toBeOnTheScreen());
    expect(screen.queryByTestId(testIds.forumComposerAttachPhotos)).toBeNull();
    expect(screen.queryByTestId(testIds.forumComposerAttachFiles)).toBeNull();
  });

  it('asks for photo permission only when Photos is tapped', async () => {
    (ImagePicker.requestMediaLibraryPermissionsAsync as jest.Mock).mockResolvedValue({ granted: false });
    renderComposer({ threadId: 1002, threadTitle: 'Ranking every studio album' });
    await waitFor(() => expect(screen.getByRole('button', { name: 'Photos' })).toBeOnTheScreen());
    expect(ImagePicker.requestMediaLibraryPermissionsAsync).not.toHaveBeenCalled();

    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Photos' }));
    await waitFor(() =>
      expect(screen.getByText('Photo library permission is required to choose a photo.')).toBeOnTheScreen(),
    );
    expect(ImagePicker.launchImageLibraryAsync).not.toHaveBeenCalled();
    expect(DocumentPicker.getDocumentAsync).not.toHaveBeenCalled();
  });

  it('does not ask for photo permission when Files is tapped', async () => {
    (DocumentPicker.getDocumentAsync as jest.Mock).mockResolvedValue({ canceled: true, assets: null });
    renderComposer({ threadId: 1002, threadTitle: 'Ranking every studio album' });
    await waitFor(() => expect(screen.getByRole('button', { name: 'Files' })).toBeOnTheScreen());

    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Files' }));
    await waitFor(() => expect(DocumentPicker.getDocumentAsync).toHaveBeenCalledWith({
      copyToCacheDirectory: true,
      multiple: false,
    }));
    expect(ImagePicker.requestMediaLibraryPermissionsAsync).not.toHaveBeenCalled();
  });

  it('replaces the attached file instead of stacking', async () => {
    (ImagePicker.requestMediaLibraryPermissionsAsync as jest.Mock).mockResolvedValue({ granted: true });
    (ImagePicker.launchImageLibraryAsync as jest.Mock).mockResolvedValue({
      canceled: false,
      assets: [{ uri: 'file:///tmp/crowd.jpg', fileName: 'crowd.jpg', mimeType: 'image/jpeg' }],
    });
    (DocumentPicker.getDocumentAsync as jest.Mock).mockResolvedValue({
      canceled: false,
      assets: [{ uri: 'file:///tmp/notes.pdf', name: 'notes.pdf', mimeType: 'application/pdf' }],
    });
    createForumReplyMock.mockResolvedValueOnce({
      id: 88,
      topicId: 1002,
      detailPath: '/forum/topic/1002',
    });

    renderComposer({ threadId: 1002, threadTitle: 'Ranking every studio album' });
    await waitFor(() => expect(screen.getByLabelText('Reply body')).toBeOnTheScreen());
    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Photos' }));
    await waitFor(() => expect(screen.getByText('crowd.jpg')).toBeOnTheScreen());
    expect(ImagePicker.launchImageLibraryAsync).toHaveBeenCalledWith(
      expect.objectContaining({
        quality: 1,
        allowsEditing: false,
        preferredAssetRepresentationMode: 'compatible',
      }),
    );
    await user.press(screen.getByRole('button', { name: 'Files' }));
    await waitFor(() => expect(screen.getByText('notes.pdf')).toBeOnTheScreen());
    expect(screen.queryByText('crowd.jpg')).toBeNull();

    await user.type(screen.getByLabelText('Reply body'), 'A reply from mobile');
    await user.press(screen.getByRole('button', { name: 'Post reply' }));
    await waitFor(() =>
      expect(createForumReplyMock).toHaveBeenCalledWith(
        1002,
        {
          body: 'A reply from mobile',
          file: { uri: 'file:///tmp/notes.pdf', name: 'notes.pdf', type: 'application/pdf' },
        },
        'tok',
      ),
    );
  });

  it('posts a new topic with the selected file', async () => {
    (DocumentPicker.getDocumentAsync as jest.Mock).mockResolvedValue({
      canceled: false,
      assets: [{ uri: 'file:///tmp/setlist.txt', name: 'setlist.txt', mimeType: 'text/plain' }],
    });
    createForumTopicMock.mockResolvedValueOnce({
      id: 2001,
      starterPostId: 1,
      title: 'Fresh forum news',
      detailPath: '/forum/topic/2001/fresh-forum-news',
    });
    renderComposer({ categoryId: 1, categoryName: 'The Music' });
    await waitFor(() => expect(screen.getByLabelText('Topic body')).toBeOnTheScreen());
    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Topic title'), 'Fresh forum news');
    await user.type(screen.getByLabelText('Topic body'), 'Hello fans');
    await user.press(screen.getByRole('button', { name: 'Files' }));
    await waitFor(() => expect(screen.getByText('setlist.txt')).toBeOnTheScreen());
    await user.press(screen.getByRole('button', { name: 'Post topic' }));

    await waitFor(() =>
      expect(createForumTopicMock).toHaveBeenCalledWith(
        1,
        {
          title: 'Fresh forum news',
          body: 'Hello fans',
          file: { uri: 'file:///tmp/setlist.txt', name: 'setlist.txt', type: 'text/plain' },
        },
        'tok',
      ),
    );
  });

  it('clears the attachment and posts JSON again', async () => {
    (DocumentPicker.getDocumentAsync as jest.Mock).mockResolvedValue({
      canceled: false,
      assets: [{ uri: 'file:///tmp/notes.pdf', name: 'notes.pdf', mimeType: 'application/pdf' }],
    });
    createForumReplyMock.mockResolvedValueOnce({
      id: 88,
      topicId: 1002,
      detailPath: '/forum/topic/1002',
    });
    renderComposer({ threadId: 1002, threadTitle: 'Ranking every studio album' });
    await waitFor(() => expect(screen.getByLabelText('Reply body')).toBeOnTheScreen());
    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Files' }));
    await waitFor(() => expect(screen.getByText('notes.pdf')).toBeOnTheScreen());
    await user.press(screen.getByRole('button', { name: 'Remove attachment' }));
    await waitFor(() => expect(screen.queryByText('notes.pdf')).toBeNull());
    await user.type(screen.getByLabelText('Reply body'), 'A reply from mobile');
    await user.press(screen.getByRole('button', { name: 'Post reply' }));
    await waitFor(() =>
      expect(createForumReplyMock).toHaveBeenCalledWith(1002, { body: 'A reply from mobile' }, 'tok'),
    );
  });

  it('ignores a canceled picker and reports a library that cannot open', async () => {
    (ImagePicker.requestMediaLibraryPermissionsAsync as jest.Mock).mockResolvedValue({ granted: true });
    (ImagePicker.launchImageLibraryAsync as jest.Mock)
      .mockResolvedValueOnce({ canceled: true, assets: [] })
      .mockRejectedValueOnce(new Error('unavailable'));
    renderComposer({ threadId: 1002, threadTitle: 'Ranking every studio album' });
    await waitFor(() => expect(screen.getByRole('button', { name: 'Photos' })).toBeOnTheScreen());
    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Photos' }));
    expect(screen.queryByTestId(testIds.forumComposerAttachment)).toBeNull();
    await user.press(screen.getByRole('button', { name: 'Photos' }));
    await waitFor(() => expect(screen.getByText('Could not open the photo library.')).toBeOnTheScreen());
  });

  it('shows the picker read error when the asset has no uri', async () => {
    (DocumentPicker.getDocumentAsync as jest.Mock).mockResolvedValue({
      canceled: false,
      assets: [{ uri: '', name: 'notes.pdf', mimeType: 'application/pdf' }],
    });
    renderComposer({ threadId: 1002, threadTitle: 'Ranking every studio album' });
    await waitFor(() => expect(screen.getByRole('button', { name: 'Files' })).toBeOnTheScreen());
    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Files' }));
    await waitFor(() =>
      expect(screen.getByText('Could not read the selected file. Try choosing it again.')).toBeOnTheScreen(),
    );
  });

  it('reports a file picker that cannot open', async () => {
    (DocumentPicker.getDocumentAsync as jest.Mock).mockRejectedValueOnce(new Error('unavailable'));
    renderComposer({ threadId: 1002, threadTitle: 'Ranking every studio album' });
    await waitFor(() => expect(screen.getByRole('button', { name: 'Files' })).toBeOnTheScreen());
    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Files' }));
    await waitFor(() => expect(screen.getByText('Could not open the file picker.')).toBeOnTheScreen());
  });

  it('shows the API validation message when the attachment is rejected', async () => {
    (DocumentPicker.getDocumentAsync as jest.Mock).mockResolvedValue({
      canceled: false,
      assets: [{ uri: 'file:///tmp/notes.exe', name: 'notes.exe', mimeType: 'application/octet-stream' }],
    });
    createForumReplyMock.mockRejectedValueOnce(
      new ApiError(400, "'notes.exe' has a type that is not allowed (application/octet-stream)."),
    );
    renderComposer({ threadId: 1002, threadTitle: 'Ranking every studio album' });
    await waitFor(() => expect(screen.getByLabelText('Reply body')).toBeOnTheScreen());
    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Reply body'), 'A reply from mobile');
    await user.press(screen.getByRole('button', { name: 'Files' }));
    await waitFor(() => expect(screen.getByText('notes.exe')).toBeOnTheScreen());
    await user.press(screen.getByRole('button', { name: 'Post reply' }));
    await waitFor(() =>
      expect(screen.getByText("'notes.exe' has a type that is not allowed (application/octet-stream).")).toBeOnTheScreen(),
    );
  });

  it('keeps the composer on screen when publish fails', async () => {
    createForumReplyMock.mockRejectedValueOnce(new ApiError(500, 'The server had a problem.'));
    const { navigation } = renderComposer({ threadId: 1002, threadTitle: 'Ranking every studio album' });

    await waitFor(() => expect(screen.getByLabelText('Reply body')).toBeOnTheScreen());
    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Reply body'), 'A reply from mobile');
    await user.press(screen.getByRole('button', { name: 'Post reply' }));

    await waitFor(() => expect(screen.getByText('The server had a problem.')).toBeOnTheScreen());
    expect(navigation.goBack).not.toHaveBeenCalled();
    expect(screen.getByRole('button', { name: 'Post reply' })).toBeOnTheScreen();
  });
});
