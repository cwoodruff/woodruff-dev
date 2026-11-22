import { defineCollection, z } from 'astro:content';
import { glob } from 'astro/loaders';

/**
 * Blog posts live in src/content/blog/YYYY/MM/<slug>/index.md with a sibling
 * images/ folder. The public URL is flat (/blog/<slug>/), so the entry id is
 * the folder name only. Uniqueness is asserted by scripts/migrate-aspnet-content.mjs
 * and scripts/import-blog.mjs; a duplicate here would silently overwrite.
 */
const blog = defineCollection({
  loader: glob({
    pattern: '**/index.md',
    base: './src/content/blog',
    generateId: ({ entry }) => entry.split('/').at(-2)!,
  }),
  schema: ({ image }) =>
    z.object({
      title: z.string(),
      date: z.coerce.date(),
      updated: z.coerce.date().optional(),
      description: z.string().optional(),
      categories: z.array(z.string()).default([]),
      tags: z.array(z.string()).default([]),
      coverImage: image().optional(),
      draft: z.boolean().default(false),
    }),
});

/**
 * Essays from The Simplicity-First Philosophy Substack, fetched at build time.
 * If Substack can't be reached, the committed snapshot in src/data keeps the build working;
 * refresh it with `npm run sync:essays`.
 */
const SUBSTACK = 'https://simplicityfirstphilosophy.substack.com';
// Substack rejects requests without a browser-like User-Agent (HTTP 403 in CI).
const SUBSTACK_HEADERS = {
  'User-Agent': 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0 Safari/537.36',
  Accept: 'application/json',
};

type SubstackPost = {
  slug: string;
  title: string;
  subtitle?: string | null;
  post_date: string;
  canonical_url: string;
  wordcount?: number | null;
  type: string;
};

const essays = defineCollection({
  loader: {
    name: 'substack-essays',
    load: async ({ store, logger, parseData }) => {
      let posts: SubstackPost[];
      try {
        const res = await fetch(`${SUBSTACK}/api/v1/posts?limit=50`, { headers: SUBSTACK_HEADERS, signal: AbortSignal.timeout(15000) });
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const body = await res.json();
        // The endpoint returns either a bare array or { posts: [...] }.
        posts = Array.isArray(body) ? body : body.posts;
        if (!Array.isArray(posts)) throw new Error('unexpected response shape');
        // The API caps a page at 50 and rejects offsets, so a full page may mean older essays are missing.
        if (posts.length >= 50) logger.warn('Substack returned a full page of 50 posts; older essays may be missing.');
      } catch (err) {
        logger.warn(`Substack fetch failed (${err}); using the committed snapshot.`);
        posts = (await import('./data/substack-posts.json')).default as SubstackPost[];
      }
      store.clear();
      for (const p of posts.filter((p) => p.type === 'newsletter' && p.slug !== 'coming-soon')) {
        const data = await parseData({
          id: p.slug,
          data: {
            title: p.title,
            subtitle: p.subtitle || undefined,
            date: p.post_date,
            url: p.canonical_url,
            minutes: p.wordcount ? Math.max(1, Math.round(p.wordcount / 238)) : undefined,
          },
        });
        store.set({ id: p.slug, data });
      }
    },
  },
  schema: z.object({
    title: z.string(),
    subtitle: z.string().optional(),
    date: z.coerce.date(),
    url: z.string().url(),
    minutes: z.number().optional(),
  }),
});

/** Press & Media: external appearances shown as logo cards. */
const media = defineCollection({
  loader: glob({ pattern: '*.md', base: './src/content/media' }),
  schema: ({ image }) =>
    z.object({
      title: z.string(),
      url: z.string().url(),
      image: image(),
      outlet: z.string().optional(),
      type: z.enum(['podcast', 'video', 'article', 'talk']).default('article'),
      date: z.coerce.date().optional(),
      description: z.string().optional(),
      featured: z.boolean().default(false),
      order: z.number().default(100),
    }),
});

/** Portfolio: books, courses, workshops, podcasts, open source. */
const portfolio = defineCollection({
  loader: glob({ pattern: '*.md', base: './src/content/portfolio' }),
  schema: ({ image }) =>
    z.object({
      title: z.string(),
      category: z.enum(['open-source', 'book', 'course', 'workshop', 'podcast', 'article', 'website', 'newsletter', 'app']),
      description: z.string(),
      image: image().optional(),
      url: z.string().url().optional(),
      externalLink: z.object({ label: z.string(), url: z.string().url() }).optional(),
      tech: z.array(z.string()).default([]),
      year: z.number().optional(),
      featured: z.boolean().default(false),
      order: z.number().default(100),
    }),
});

/** Services: six detail pages. Body is the long-form page content. */
const services = defineCollection({
  loader: glob({ pattern: '*.md', base: './src/content/services' }),
  schema: ({ image }) =>
    z.object({
      title: z.string(),
      navTitle: z.string().optional(),
      short: z.string(),
      tagline: z.string(),
      group: z.enum(['ongoing', 'defined', 'specialist']),
      engagement: z.string(),
      image: image().optional(),
      order: z.number(),
      ctaHeading: z.string(),
      ctaText: z.string(),
      ctaButton: z.string().default('Start the conversation'),
      externalLink: z.object({ label: z.string(), url: z.string().url() }).optional(),
    }),
});

/** Training: hands-on workshops and courses. Drafts are hidden from the index. */
const training = defineCollection({
  loader: glob({ pattern: '*.md', base: './src/content/training' }),
  schema: ({ image }) =>
    z.object({
      title: z.string(),
      short: z.string(),
      format: z.enum(['workshop', 'course', 'bootcamp']),
      duration: z.string().optional(),
      level: z.enum(['beginner', 'intermediate', 'advanced']),
      audience: z.string(),
      prerequisites: z.array(z.string()).default([]),
      modules: z.array(z.object({ title: z.string(), length: z.string().optional() })).default([]),
      outcomes: z.array(z.string()).default([]),
      build: z.string().optional(),
      tech: z.array(z.string()).default([]),
      image: image().optional(),
      accent: z.string().optional(),
      site: z.string().url().optional(),
      repo: z.string().url().optional(),
      nextDates: z
        .array(z.object({ date: z.coerce.date(), city: z.string(), url: z.string().url().optional() }))
        .default([]),
      draft: z.boolean().default(true),
      order: z.number().default(100),
    }),
});

/** Testimonials: body is the quote. */
const testimonials = defineCollection({
  loader: glob({ pattern: '*.md', base: './src/content/testimonials' }),
  schema: ({ image }) =>
    z.object({
      name: z.string(),
      role: z.string(),
      avatar: image().optional(),
      order: z.number().default(100),
    }),
});

export const collections = { blog, essays, media, portfolio, services, training, testimonials };
