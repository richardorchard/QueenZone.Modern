import { screen, userEvent, waitFor } from '@testing-library/react-native';
import { fetchPhotoCategory, fetchPhotoCategoryItems } from '../../api';
import { ApiError } from '../../api/client';
import type { PhotoCategoryListItem, PhotoListItem } from '../../api/types';
import { pagedResponse } from '../../test/fixtures';
import { fakeNavigation, flushVirtualizedList, renderWithProviders } from '../../test/render';
import { testIds } from '../../test/testIds';
import { PhotoCategoryScreen } from './PhotoCategoryScreen';

jest.mock('../../api', () => {
  const actual = jest.requireActual('../../api');
  return {
    ...actual,
    fetchPhotoCategory: jest.fn(),
    fetchPhotoCategoryItems: jest.fn(),
  };
});

const fetchCategory = fetchPhotoCategory as jest.MockedFunction<typeof fetchPhotoCategory>;
const fetchItems = fetchPhotoCategoryItems as jest.MockedFunction<typeof fetchPhotoCategoryItems>;

function categoryFixture(overrides: Partial<PhotoCategoryListItem> = {}): PhotoCategoryListItem {
  return {
    catId: 1,
    name: 'Brian May',
    slug: 'brian-may',
    imageCount: 3,
    coverThumbnailUrl: null,
    detailPath: '/photography/brian-may',
    ...overrides,
  };
}

function photoFixture(overrides: Partial<PhotoListItem> = {}): PhotoListItem {
  return {
    picId: 101,
    catId: 1,
    categoryName: 'Brian May',
    categorySlug: 'brian-may',
    title: 'Live Aid',
    thumbnailUrl: 'https://example.com/brian-may/img-101-t.jpg',
    thumbWidth: 200,
    thumbHeight: 150,
    pictureWidth: 1600,
    pictureHeight: 900,
    pictureDimensionsLabel: '1600 x 900',
    year: 1985,
    dateTime: '1985-07-13T00:00:00.000Z',
    detailPath: '/photography/brian-may/101',
    categoryPath: '/photography/brian-may',
    ...overrides,
  };
}

function renderPhotoCategory(navigation = fakeNavigation()) {
  return {
    navigation,
    ...renderWithProviders(
      <PhotoCategoryScreen
        navigation={navigation as never}
        route={
          {
            key: 'photo-category',
            name: 'PhotoCategory',
            params: { slug: 'brian-may', name: 'Brian May' },
          } as never
        }
      />,
    ),
  };
}

describe('PhotoCategoryScreen', () => {
  beforeEach(() => {
    fetchCategory.mockReset();
    fetchItems.mockReset();
  });

  afterEach(async () => {
    await flushVirtualizedList();
  });

  it('loads photos and opens the viewer', async () => {
    fetchCategory.mockResolvedValue(categoryFixture());
    fetchItems.mockResolvedValue(pagedResponse([photoFixture()]));

    const { navigation } = renderPhotoCategory();
    await waitFor(() => expect(screen.getByTestId(testIds.photoCategoryScreen)).toBeOnTheScreen());
    expect(screen.getByRole('button', { name: 'Live Aid' })).toBeOnTheScreen();
    expect(fetchCategory).toHaveBeenCalledWith('brian-may', expect.any(AbortSignal));
    expect(fetchItems).toHaveBeenCalledWith(
      'brian-may',
      expect.objectContaining({ page: 1, pageSize: 24, size: undefined }),
    );

    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Live Aid' }));
    expect(navigation.navigate).toHaveBeenCalledWith('PhotoViewer', { slug: 'brian-may', picId: 101 });
  });

  it('shows an error and retries', async () => {
    fetchCategory.mockResolvedValue(categoryFixture());
    fetchItems
      .mockRejectedValueOnce(new ApiError(500, 'The server had a problem.'))
      .mockResolvedValueOnce(pagedResponse([photoFixture()]));

    renderPhotoCategory();
    await waitFor(() => expect(screen.getByText('The server had a problem.')).toBeOnTheScreen());

    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Try again' }));
    await waitFor(() => expect(screen.getByRole('button', { name: 'Live Aid' })).toBeOnTheScreen());
    expect(fetchItems).toHaveBeenCalledTimes(2);
  });

  it('shows empty copy when the collection has no images', async () => {
    fetchCategory.mockResolvedValue(categoryFixture());
    fetchItems.mockResolvedValue(pagedResponse([], 1, 0));

    renderPhotoCategory();
    await waitFor(() =>
      expect(screen.getByText('No images are available in this collection yet.')).toBeOnTheScreen(),
    );
  });
});
