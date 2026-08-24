/** Two-letter initials for an avatar circle, from a display name (or username). */
export function initials(name: string | null | undefined): string {
  if (!name) {
    return '';
  }
  const parts = name.replace(/_/g, ' ').trim().split(/\s+/);
  if (parts.length === 0 || parts[0].length === 0) {
    return '';
  }
  if (parts.length === 1) {
    return parts[0].slice(0, 2).toUpperCase();
  }
  return `${parts[0][0] ?? ''}${parts[1][0] ?? ''}`.toUpperCase();
}
