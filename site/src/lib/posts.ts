import { getCollection, type CollectionEntry } from 'astro:content';
import { href } from './url';

export type Post = CollectionEntry<'blog'>;

const CATEGORY_LABELS: Record<string, string> = {
  ai: 'AI',
  'biz-software': 'Business & Software',
  blog: 'Blog',
  efcore: 'EF Core',
  'fun-tech': 'Fun Tech',
  'genetic-algorithms': 'Genetic Algorithms',
  htmx: 'htmx',
  'http-rest': 'HTTP & REST',
  'network-book-sample': 'Network Programming',
  patterns: 'Patterns',
  'random-csharp': 'C#',
  rust: 'Rust',
  'simplicity-first': 'Simplicity-First',
  speaking: 'Speaking',
  terraform: 'Terraform',
};

const TAG_LABELS: Record<string, string> = {
  net: '.NET',
  dotnet: '.NET',
  c: 'C#',
  csharp: 'C#',
  efcore: 'EF Core',
  'ef-core': 'EF Core',
  sql: 'SQL',
  api: 'API',
  ai: 'AI',
  http: 'HTTP',
  rest: 'REST',
  htmx: 'htmx',
  aspnet: 'ASP.NET',
  'asp-net-core': 'ASP.NET Core',
};

export function categoryLabel(slug: string): string {
  return CATEGORY_LABELS[slug] ?? titleCase(slug);
}

export function tagLabel(slug: string): string {
  return TAG_LABELS[slug] ?? titleCase(slug);
}

function titleCase(slug: string): string {
  return slug
    .split('-')
    .map((w) => w.charAt(0).toUpperCase() + w.slice(1))
    .join(' ');
}

let cache: Post[] | undefined;

/** All published posts, newest first. */
export async function getPosts(): Promise<Post[]> {
  if (cache) return cache;
  const posts = await getCollection('blog', ({ data }) => !data.draft);
  cache = posts.sort((a, b) => b.data.date.valueOf() - a.data.date.valueOf());
  return cache;
}

export function postHref(post: Post): string {
  return href(`/blog/${post.id}/`);
}

export function categoryHref(slug: string): string {
  return href(`/blog/category/${slug}/`);
}

export function tagHref(slug: string): string {
  return href(`/blog/tag/${slug}/`);
}

export function primaryCategory(post: Post): string | undefined {
  return post.data.categories[0];
}

/** Plain-text excerpt: front matter description, else the first paragraph(s) of the body. */
export function excerpt(post: Post, max = 170): string {
  if (post.data.description) return post.data.description;
  let src = post.body ?? '';
  const more = src.indexOf('<!--more-->');
  if (more > 0) src = src.slice(0, more);

  const text = src
    .replace(/```[\s\S]*?```/g, ' ')
    .replace(/<!--[\s\S]*?-->/g, ' ')
    .replace(/<[^>]+>/g, ' ')
    .replace(/^#{1,6}\s.*$/gm, ' ')
    .replace(/!\[[^\]]*\]\([^)]*\)/g, ' ')
    .replace(/\[([^\]]*)\]\([^)]*\)/g, '$1')
    .replace(/[*_`>#]+/g, '')
    .replace(/\s+/g, ' ')
    .trim();

  if (text.length <= max) return text;
  const cut = text.slice(0, max);
  const at = cut.lastIndexOf(' ');
  return (at > max * 0.6 ? cut.slice(0, at) : cut).replace(/[,;:]$/, '') + '…';
}

export function readingTime(body: string | undefined): number {
  const words = (body ?? '')
    .replace(/```[\s\S]*?```/g, ' ')
    .replace(/<[^>]+>/g, ' ')
    .split(/\s+/)
    .filter(Boolean).length;
  return Math.max(1, Math.round(words / 230));
}

const dateFmt = new Intl.DateTimeFormat('en-US', { year: 'numeric', month: 'short', day: 'numeric', timeZone: 'UTC' });

export function formatDate(d: Date): string {
  return dateFmt.format(d);
}

export function isoDate(d: Date): string {
  return d.toISOString().slice(0, 10);
}

/** Map of category slug -> post count, sorted by count desc. */
export async function getCategories(): Promise<Array<{ slug: string; label: string; count: number }>> {
  const posts = await getPosts();
  const counts = new Map<string, number>();
  for (const p of posts) for (const c of p.data.categories) counts.set(c, (counts.get(c) ?? 0) + 1);
  return [...counts]
    .map(([slug, count]) => ({ slug, label: categoryLabel(slug), count }))
    .sort((a, b) => b.count - a.count || a.label.localeCompare(b.label));
}

export async function getTags(): Promise<Array<{ slug: string; label: string; count: number }>> {
  const posts = await getPosts();
  const counts = new Map<string, number>();
  for (const p of posts) for (const t of p.data.tags) counts.set(t, (counts.get(t) ?? 0) + 1);
  return [...counts]
    .map(([slug, count]) => ({ slug, label: tagLabel(slug), count }))
    .sort((a, b) => b.count - a.count || a.label.localeCompare(b.label));
}

export const POSTS_PER_PAGE = 12;
