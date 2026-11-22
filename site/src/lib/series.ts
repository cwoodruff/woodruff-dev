import { getPosts, type Post } from './posts';

/**
 * Multi-part series. A post belongs to a series when it is in the series'
 * category and published inside its date window (both inclusive, UTC).
 */
const SERIES = [
  { slug: 'network-programming', title: 'Network Programming Fundamentals', category: 'network-book-sample', from: '2025-01-03', to: '2025-01-15', blurb: 'Protocols, addressing, and sockets in C#, from the client-server model to graceful shutdowns.' },
  { slug: 'csharp-to-rust', title: 'From C# to Rust', category: 'rust', from: '2025-01-16', to: '2025-05-31', blurb: 'A C# developer learns Rust, from syntax and ownership through to shipping a CLI tool.' },
  { slug: 'ef-core-performance', title: 'EF Core in Depth', category: 'efcore', from: '2025-01-01', to: '2025-02-28', blurb: 'Pooling, compiled models, raw SQL, and the techniques that make EF Core apps fast.' },
  { slug: 'terraform', title: 'Terraform from Zero', category: 'terraform', from: '2025-02-16', to: '2025-03-09', blurb: 'Infrastructure as code from your first HCL file to disaster-proof cloud automation.' },
  { slug: 'http-rest', title: 'HTTP & REST Fundamentals', category: 'http-rest', from: '2025-03-10', to: '2025-03-24', blurb: 'How HTTP really works, and each of REST’s six constraints explained.' },
  { slug: 'htmx-razor-pages', title: 'htmx with Razor Pages', category: 'htmx', from: '2025-03-25', to: '2025-04-30', blurb: 'Server-driven interactivity for ASP.NET Core Razor Pages, one htmx feature at a time.' },
  { slug: 'genetic-algorithms', title: 'Genetic Algorithms Bootcamp', category: 'genetic-algorithms', from: '2025-05-24', to: '2025-09-30', blurb: 'A 35-day bootcamp on evolving solutions in C#, from chromosomes to neural-network tuning.' },
  { slug: 'enterprise-patterns', title: 'Enterprise Patterns in ASP.NET Core', category: 'patterns', from: '2025-11-28', to: '2026-01-31', blurb: 'Fowler’s enterprise application patterns implemented with real ASP.NET Core code.' },
] as const;

export type Series = (typeof SERIES)[number] & { posts: Post[] };

const day = (iso: string) => new Date(`${iso}T00:00:00Z`).valueOf();
const inSeries = (s: (typeof SERIES)[number], p: Post) =>
  p.data.categories.includes(s.category) && p.data.date.valueOf() >= day(s.from) && p.data.date.valueOf() < day(s.to) + 86400000;

let cache: Series[] | undefined;

/** Every series with its posts in reading order (oldest first). */
export async function getSeries(): Promise<Series[]> {
  if (cache) return cache;
  const posts = await getPosts();
  cache = SERIES.map((s) => ({
    ...s,
    posts: posts.filter((p) => inSeries(s, p)).sort((a, b) => a.data.date.valueOf() - b.data.date.valueOf()),
  })).filter((s) => s.posts.length > 1);
  return cache;
}

/** The series a post belongs to and its 1-based part number, if any. */
export async function seriesFor(post: Post): Promise<{ series: Series; part: number } | undefined> {
  for (const series of await getSeries()) {
    const i = series.posts.findIndex((p) => p.id === post.id);
    if (i >= 0) return { series, part: i + 1 };
  }
}
