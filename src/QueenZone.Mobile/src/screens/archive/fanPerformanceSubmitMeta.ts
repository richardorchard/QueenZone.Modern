import type { AudioUploadFile } from '../../api/fanPerformanceSubmissions';

export const fanPerformanceTitleMaxLength = 200;
export const fanPerformanceDescriptionMaxLength = 2000;
export const fanPerformanceMaxUploadBytes = 25 * 1024 * 1024;

export const fanPerformanceSubmitCopy = {
  eyebrow: 'Community',
  title: 'Submit a fan performance',
  intro:
    'Pick an existing audio file from your device. Submissions are reviewed before they appear on the fan stage. Recording in the app is not available.',
  help: 'MP3 or FLAC. Max 25 MB. You must confirm this is your own performance of a Queen song.',
  confirmationTitle: 'Fan performance submitted',
  confirmationMessage: 'Your fan performance is under review.',
  submitAction: 'Submit for review',
  anotherAction: 'Submit another performance',
  rightsDeclaration:
    'I confirm this recording is my own performance of a Queen song and I agree to it being published on QueenZone.',
} as const;

export type FanPerformanceSubmitFields = {
  title: string;
  coveredSong: string;
  performedBy: string;
  description: string;
  rightsDeclarationAccepted: boolean;
  audio: AudioUploadFile | null;
  fileSize?: number | null;
};

export function validateFanPerformanceSubmit(input: FanPerformanceSubmitFields): string | null {
  if (!input.title.trim()) {
    return 'Title is required.';
  }

  if (input.title.trim().length > fanPerformanceTitleMaxLength) {
    return `Title must be ${fanPerformanceTitleMaxLength} characters or fewer.`;
  }

  if (!input.coveredSong.trim()) {
    return 'Covered song is required.';
  }

  if (input.coveredSong.trim().length > fanPerformanceTitleMaxLength) {
    return `Covered song must be ${fanPerformanceTitleMaxLength} characters or fewer.`;
  }

  if (!input.performedBy.trim()) {
    return 'Performed by is required.';
  }

  if (input.performedBy.trim().length > fanPerformanceTitleMaxLength) {
    return `Performed by must be ${fanPerformanceTitleMaxLength} characters or fewer.`;
  }

  if (input.description.trim().length > fanPerformanceDescriptionMaxLength) {
    return `Description must be ${fanPerformanceDescriptionMaxLength} characters or fewer.`;
  }

  if (!input.rightsDeclarationAccepted) {
    return 'You must confirm this is your own performance and agree to publication.';
  }

  if (!input.audio) {
    return 'Choose an audio file to upload.';
  }

  if (input.fileSize != null && input.fileSize > fanPerformanceMaxUploadBytes) {
    return 'Audio must be 25 MB or smaller.';
  }

  return null;
}

export function audioFromDocumentAsset(asset: {
  uri: string;
  name?: string | null;
  mimeType?: string | null;
  size?: number | null;
}): { file: AudioUploadFile; fileSize: number | null } {
  const name = asset.name?.trim() || 'performance.mp3';
  const type = asset.mimeType?.trim() || mimeFromName(name);
  return {
    file: { uri: asset.uri, name, type },
    fileSize: typeof asset.size === 'number' ? asset.size : null,
  };
}

function mimeFromName(name: string): string {
  const lower = name.toLowerCase();
  if (lower.endsWith('.flac')) {
    return 'audio/flac';
  }

  return 'audio/mpeg';
}
