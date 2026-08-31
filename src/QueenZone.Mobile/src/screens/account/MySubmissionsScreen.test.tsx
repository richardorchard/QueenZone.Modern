import { screen, userEvent, waitFor } from '@testing-library/react-native';
import { submissionsApiUrl } from '../../api/submissions';
import { jsonResponse } from '../../test/fixtures';
import { createMockSession } from '../../test/mockSession';
import { renderWithProviders } from '../../test/render';
import { MySubmissionsScreen } from './MySubmissionsScreen';

const mockSession = createMockSession();

jest.mock('../../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

jest.mock('../../config/appConfig', () => ({
  getAppConfig: () => ({ apiBaseUrl: 'http://qz.test', appEnv: 'development', version: '0.1.0' }),
}));

const fetchMock = jest.fn<Promise<Response>, [RequestInfo | URL, RequestInit?]>();
const photosUrl = submissionsApiUrl('http://qz.test', 'photos');
const newsUrl = submissionsApiUrl('http://qz.test', 'news');
const articlesUrl = submissionsApiUrl('http://qz.test', 'articles');

const photoPayload = {
  items: [
    {
      id: 'photo-1',
      title: 'Live in Montreal',
      submittedAt: '2024-01-15T12:00:00.000Z',
      status: { status: 'pending', statusLabel: 'Pending review', statusTone: 'pending' },
      notes: null,
      thumbnailPath: '/ugc/photos/members/a/thumb.webp',
      promotedPicId: null,
    },
  ],
  page: 1,
  pageSize: 20,
  totalCount: 1,
  totalPages: 1,
};

const newsPayload = {
  items: [
    {
      id: 'news-1',
      url: 'https://example.com/queen',
      truncatedUrl: 'example.com/queen',
      title: 'Tour dates leak',
      submittedAt: '2024-02-01T12:00:00.000Z',
      status: { status: 'review', statusLabel: 'In review', statusTone: 'review' },
      notes: null,
      publishedNewsId: null,
      publishedPath: null,
    },
  ],
  page: 1,
  pageSize: 20,
  totalCount: 1,
  totalPages: 1,
};

const articlePayload = {
  items: [
    {
      id: 'article-1',
      title: 'A Night at the Archive',
      submittedAt: '2024-03-01T12:00:00.000Z',
      status: { status: 'success', statusLabel: 'Published', statusTone: 'success' },
      notes: null,
      canContinueEditing: false,
      editPath: null,
      publishedPath: '/articles/1',
    },
  ],
  page: 1,
  pageSize: 20,
  totalCount: 1,
  totalPages: 1,
};

function mockSubmissionPages() {
  fetchMock.mockImplementation(async (input) => {
    const url = String(input);
    if (url === photosUrl) {
      return jsonResponse(photoPayload);
    }
    if (url === newsUrl) {
      return jsonResponse(newsPayload);
    }
    if (url === articlesUrl) {
      return jsonResponse(articlePayload);
    }
    throw new Error(`unexpected fetch ${url}`);
  });
}

function renderSubmissions() {
  return renderWithProviders(<MySubmissionsScreen />);
}

describe('MySubmissionsScreen', () => {
  beforeEach(() => {
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    fetchMock.mockReset();
    global.fetch = fetchMock as unknown as typeof fetch;
    mockSubmissionPages();
  });

  it('loads photo submissions on the default tab', async () => {
    renderSubmissions();
    await waitFor(() => expect(screen.getByText('Live in Montreal')).toBeOnTheScreen());
    expect(screen.getByText('Pending review')).toBeOnTheScreen();
    const thumb = screen.getByLabelText('Live in Montreal');
    expect(thumb.props.source).toEqual({ uri: 'http://qz.test/ugc/photos/members/a/thumb.webp' });
    expect(thumb.props.priority).toBe('low');
    expect(thumb.props.recyclingKey).toBe('photo-1');
    expect(thumb.props.accessibilityIgnoresInvertColors).toBe(true);
    expect(fetchMock).toHaveBeenCalledWith(photosUrl, {
      headers: { Accept: 'application/json', Authorization: 'Bearer tok' },
    });
    expect(fetchMock).toHaveBeenCalledWith(newsUrl, expect.any(Object));
    expect(fetchMock).toHaveBeenCalledWith(articlesUrl, expect.any(Object));
  });

  it('switches tabs to news and articles', async () => {
    renderSubmissions();
    await waitFor(() => expect(screen.getByText('Live in Montreal')).toBeOnTheScreen());

    const user = userEvent.setup();
    await user.press(screen.getByRole('tab', { name: 'News suggestions' }));
    expect(screen.getByText('Tour dates leak')).toBeOnTheScreen();
    expect(screen.queryByText('Live in Montreal')).toBeNull();

    await user.press(screen.getByRole('tab', { name: 'Articles' }));
    expect(screen.getByText('A Night at the Archive')).toBeOnTheScreen();
    expect(screen.getByText('Published on the website')).toBeOnTheScreen();
    expect(screen.queryByText('Continue editing on the website')).toBeNull();
  });

  it('shows a load error and retries', async () => {
    fetchMock.mockImplementationOnce(async () => jsonResponse({ detail: 'Could not load your submissions.' }, 500));
    fetchMock.mockImplementationOnce(async () => jsonResponse(newsPayload));
    fetchMock.mockImplementationOnce(async () => jsonResponse(articlePayload));
    renderSubmissions();
    await waitFor(() => expect(screen.getByText('Could not load your submissions.')).toBeOnTheScreen());

    mockSubmissionPages();
    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Retry loading submissions' }));
    await waitFor(() => expect(screen.getByText('Live in Montreal')).toBeOnTheScreen());
  });
});
