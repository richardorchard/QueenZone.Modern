export const subjectMinLength = 5;
export const subjectMaxLength = 200;

export type ComposerMode = 'reply' | 'newTopic';

export type ComposerParams = {
  threadId?: number;
  threadTitle?: string;
  categoryId?: number;
  categoryName?: string;
  isLocked?: boolean;
};

export function composerMode(params: ComposerParams | undefined): ComposerMode {
  return params?.threadId != null ? 'reply' : 'newTopic';
}

export function validateComposer(input: {
  mode: ComposerMode;
  title: string;
  body: string;
  categoryId?: number;
  isLocked?: boolean;
}): string | null {
  if (input.mode === 'reply' && input.isLocked) {
    return 'This topic is locked.';
  }

  if (!input.body.trim()) {
    return 'Write a post before publishing.';
  }

  if (input.mode === 'newTopic') {
    if (input.categoryId == null || input.categoryId <= 0) {
      return 'Choose a board for this topic.';
    }

    const title = input.title.trim();
    if (title.length < subjectMinLength || title.length > subjectMaxLength) {
      return 'Title must be between 5 and 200 characters.';
    }
  }

  return null;
}

export function composerCopy(mode: ComposerMode): { title: string; action: string } {
  return mode === 'reply'
    ? { title: 'Reply', action: 'Post reply' }
    : { title: 'New topic', action: 'Post topic' };
}
