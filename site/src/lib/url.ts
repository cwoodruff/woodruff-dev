/**
 * Prefixes an internal path with the configured base path.
 * In preview builds (PREVIEW=true) base is "/woodruff-dev"; in production it is "/".
 * Always use this for internal links and for files served from public/.
 */
const base = import.meta.env.BASE_URL.replace(/\/$/, '');

export function href(path: string): string {
  if (/^(https?:)?\/\//.test(path) || path.startsWith('mailto:') || path.startsWith('tel:')) {
    return path;
  }
  return `${base}${path.startsWith('/') ? path : `/${path}`}`;
}

export function isExternal(path: string): boolean {
  return /^(https?:)?\/\//.test(path);
}
