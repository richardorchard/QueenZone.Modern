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
  code?: string;
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

export type PhotoCategoryListItem = {
  catId: number;
  name: string;
  slug: string;
  imageCount: number;
  coverThumbnailUrl: string | null;
  detailPath: string;
};

export type PhotoListItem = {
  picId: number;
  catId: number;
  categoryName: string;
  categorySlug: string;
  title: string;
  thumbnailUrl: string;
  thumbWidth: number;
  thumbHeight: number;
  pictureWidth: number;
  pictureHeight: number;
  pictureDimensionsLabel: string | null;
  year: number;
  dateTime: string;
  detailPath: string;
  categoryPath: string;
};

export type PhotoNav = {
  picId: number;
  detailPath: string;
};

export type PhotoDetail = {
  picId: number;
  catId: number;
  categoryName: string;
  categorySlug: string;
  title: string;
  imageUrl: string;
  thumbnailUrl: string;
  thumbWidth: number;
  thumbHeight: number;
  pictureWidth: number;
  pictureHeight: number;
  pictureDimensionsLabel: string | null;
  year: number;
  dateTime: string;
  submittedByDisplayName: string | null;
  detailPath: string;
  categoryPath: string;
  index: number;
  count: number;
  previous: PhotoNav | null;
  next: PhotoNav | null;
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

export type ForumTopicDetail = {
  id: number;
  title: string;
  forumId: number;
  forumName: string;
  categoryPath: string;
  detailPath: string;
  postCount: number;
  hasPoll: boolean | null;
  isLocked: boolean;
};

export type ForumAttachment = {
  fileName: string;
  url: string;
  extension: string;
  formattedSize: string;
  isImage: boolean;
  thumbnailUrl: string | null;
};

export type ForumPost = {
  id: number;
  body: string;
  postedAt: string;
  authorUsername: string;
  signature: string | null;
  authorMemberSince: string | null;
  authorMemberId: string | null;
  editedAt: string | null;
  editCount: number;
  attachments: ForumAttachment[];
};

export type ForumTopicCreated = {
  id: number;
  starterPostId: number;
  title: string;
  detailPath: string;
};

export type ForumPostCreated = {
  id: number;
  topicId: number;
  detailPath: string;
};

export type ForumPollOption = {
  optionId: string;
  optionText: string;
  displayOrder: number;
  voteCount: number;
  percentage: number;
  selectedByViewer: boolean;
};

export type ForumPoll = {
  pollId: string;
  topicId: number;
  question: string;
  isMultiChoice: boolean;
  maxChoices: number | null;
  closesAt: string | null;
  closedAt: string | null;
  createdAt: string;
  totalVotes: number;
  distinctVoters: number;
  viewerHasVoted: boolean;
  isClosed: boolean;
  canViewerVote: boolean;
  canViewerClose: boolean;
  options: ForumPollOption[];
};
