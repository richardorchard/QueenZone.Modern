/**
 * DTOs mirroring `QueenZone.Web` content API models (`ContentApiModels.cs`).
 * Property names are camelCase to match the JSON contract.
 */

export type ApiPagedResponse<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
};

export type NewsListItem = {
  id: number;
  title: string;
  excerpt: string;
  publishedAt: string;
  detailPath: string;
};

export type NewsDetail = {
  id: number;
  title: string;
  excerpt: string;
  body: string;
  publishedAt: string;
  sourceUrl: string | null;
  detailPath: string;
};

export type BiographyChapterListItem = {
  id: number;
  title: string;
  summary: string;
  displaySequence: number;
  detailPath: string;
};

export type BiographyChapterNav = {
  id: number;
  title: string;
  detailPath: string;
};

export type BiographyChapterDetail = {
  id: number;
  title: string;
  summary: string;
  body: string;
  displaySequence: number;
  detailPath: string;
  previous: BiographyChapterNav | null;
  next: BiographyChapterNav | null;
};

export type AlbumListItem = {
  albumId: number;
  name: string;
  releaseYear: number | null;
  thumbnailUrl: string | null;
  detailPath: string;
};

export type AlbumSong = {
  songId: number;
  title: string;
  isSingle: boolean;
  lyrics: string | null;
  notes: string | null;
};

export type AlbumDetail = {
  albumId: number;
  name: string;
  releaseYear: number | null;
  artistName: string;
  generalNotes: string | null;
  coverUrl: string | null;
  detailPath: string;
  songs: AlbumSong[];
};

export type TimelineEvent = {
  id: number;
  title: string;
  summary: string;
  eventDate: string;
  formattedDate: string;
  category: string;
  categoryLabel: string;
  sourceUrl: string | null;
};

export type FreddieTribute = {
  id: number;
  name: string;
  thought: string;
  country: string | null;
  dateText: string;
  timeText: string | null;
};

export type ForumCategoryListItem = {
  id: number;
  name: string;
  description: string | null;
  postCount: number;
  lastActivityAt: string | null;
  latestThreadTitle: string | null;
  detailPath: string;
};

export type ForumTopicListItem = {
  id: number;
  title: string;
  lastActivityAt: string;
  authorUsername: string;
  replyCount: number;
  lastPostUsername: string | null;
  isSticky: boolean;
  detailPath: string;
};
