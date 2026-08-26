import { sendJson } from './client';
import { newsSuggestionsPath, parseNewsSuggestionCreated } from './newsSuggestionResponse';
import type { NewsSuggestionCreated } from './types';

export type { NewsSuggestionCreated } from './types';
export { newsSuggestionsPath, parseNewsSuggestionCreated } from './newsSuggestionResponse';

export type NewsSuggestionWrite = {
  url: string;
  title: string | null;
  notes: string | null;
};

export async function createNewsSuggestion(
  input: NewsSuggestionWrite,
  accessToken: string,
  signal?: AbortSignal,
): Promise<NewsSuggestionCreated> {
  return parseNewsSuggestionCreated(
    await sendJson(newsSuggestionsPath, {
      method: 'POST',
      body: {
        url: input.url,
        title: input.title,
        notes: input.notes,
      },
      accessToken,
      signal,
    }),
  );
}
