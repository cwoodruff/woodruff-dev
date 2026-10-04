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
      category: z.enum(['open-source', 'book', 'course', 'workshop', 'podcast', 'article']),
      description: z.string(),
      image: image().optional(),
      url: z.string().url().optional(),
      tech: z.array(z.string()).default([]),
      year: z.number().optional(),
      featured: z.boolean().default(false),
      order: z.number().default(100),
    }),
});

/** Services: six detail pages. Body is the long-form page content. */
const services = defineCollection({
  loader: glob({ pattern: '*.md', base: './src/content/services' }),
  schema: z.object({
    title: z.string(),
    navTitle: z.string().optional(),
    short: z.string(),
    tagline: z.string(),
    order: z.number(),
    ctaHeading: z.string(),
    ctaText: z.string(),
    ctaButton: z.string().default('Start the conversation'),
    externalLink: z.object({ label: z.string(), url: z.string().url() }).optional(),
  }),
});

/** Training: in-person workshops and courses. Drafts are hidden from the index. */
const training = defineCollection({
  loader: glob({ pattern: '*.md', base: './src/content/training' }),
  schema: ({ image }) =>
    z.object({
      title: z.string(),
      short: z.string(),
      format: z.enum(['workshop', 'course', 'bootcamp']),
      duration: z.string(),
      level: z.enum(['intermediate', 'advanced']),
      audience: z.string(),
      prerequisites: z.array(z.string()).default([]),
      modules: z.array(z.object({ title: z.string(), length: z.string().optional() })).default([]),
      image: image().optional(),
      accent: z.string().optional(),
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

export const collections = { blog, media, portfolio, services, training, testimonials };
