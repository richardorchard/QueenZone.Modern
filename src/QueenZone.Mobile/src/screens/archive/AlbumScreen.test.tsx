import { screen, waitFor } from '@testing-library/react-native';
import { fetchAlbumDetail } from '../../api';
import type { AlbumDetail } from '../../api/types';
import { fakeNavigation, renderWithProviders } from '../../test/render';
import { AlbumScreen } from './AlbumScreen';

jest.mock('../../api', () => {
  const actual = jest.requireActual('../../api');
  return {
    ...actual,
    fetchAlbumDetail: jest.fn(),
  };
});

const fetchDetail = fetchAlbumDetail as jest.MockedFunction<typeof fetchAlbumDetail>;

function albumDetailFixture(overrides: Partial<AlbumDetail> = {}): AlbumDetail {
  return {
    albumId: 7,
    name: 'A Night at the Opera',
    releaseYear: 1975,
    artistName: 'Queen',
    generalNotes: 'Studio album.',
    coverUrl: 'https://cdn.queenzone.org/discography/7-cover.jpg',
    detailPath: '/discography/7',
    songs: [{ songId: 1, title: 'Bohemian Rhapsody', isSingle: true, lyrics: null, notes: null }],
    ...overrides,
  };
}

function renderAlbum(id = 7) {
  return renderWithProviders(
    <AlbumScreen
      navigation={fakeNavigation() as never}
      route={{ key: 'album', name: 'Album', params: { id } } as never}
    />,
    { navigation: false },
  );
}

describe('AlbumScreen', () => {
  beforeEach(() => {
    fetchDetail.mockReset();
    fetchDetail.mockResolvedValue(albumDetailFixture());
  });

  it('renders the cover through ArchiveImage at normal priority', async () => {
    renderAlbum();
    await waitFor(() => expect(screen.getByText('Bohemian Rhapsody')).toBeOnTheScreen());

    const cover = screen.getByLabelText('A Night at the Opera cover');
    expect(cover.props.source).toEqual({
      uri: 'https://cdn.queenzone.org/discography/7-cover.jpg',
    });
    expect(cover.props.priority).toBe('normal');
    expect(cover.props.recyclingKey).toBe('7');
    expect(cover.props.accessibilityIgnoresInvertColors).toBe(true);
  });
});
