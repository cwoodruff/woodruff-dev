// @ts-check
import { existsSync, readdirSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { defineConfig } from 'astro/config';
import sitemap from '@astrojs/sitemap';

/**
 * Redirects from the WordPress URL structure. GitHub Pages has no server-side
 * redirects, so Astro emits a small HTML page with a meta refresh and a
 * canonical link for each one.
 *
 * WordPress served posts at /<slug>/; the Astro site serves them at /blog/<slug>/.
 * The slug list is read from the content folder so it never goes stale.
 */
function wordpressRedirects(base = '') {
  /** @type {Record<string, string>} */
  const map = {
    '/category/blog': `${base}/blog/`,
    '/fractional-architect': `${base}/services/fractional-architect/`,
    '/expert-witness': `${base}/services/expert-witness/`,
    '/micro-consulting': `${base}/services/micro-consulting/`,
    '/project-based-contracts': `${base}/services/project-based/`,
    '/retainer-based-services': `${base}/services/agentic-developer-relations/`,
    '/advisory-board-roles': `${base}/services/advisory/`,
  };
  // /feed/ -> /rss.xml is a hand-written stub in src/pages/feed/index.astro,
  // because trailingSlash: 'always' would rewrite the target to /rss.xml/.
  const blog = new URL('./src/content/blog', import.meta.url).pathname;
  if (!existsSync(blog)) return map;
  const reserved = new Set(['about', 'contact', 'services', 'portfolio', 'press-media', 'blog', 'training', 'rss.xml', '404']);
  for (const year of readdirSync(blog)) {
    const yDir = join(blog, year);
    if (!statSync(yDir).isDirectory()) continue;
    for (const month of readdirSync(yDir)) {
      const mDir = join(yDir, month);
      if (!statSync(mDir).isDirectory()) continue;
      for (const slug of readdirSync(mDir)) {
        if (!reserved.has(slug) && existsSync(join(mDir, slug, 'index.md'))) {
          map[`/${slug}`] = `${base}/blog/${slug}/`;
        }
      }
    }
  }
  const categories = ['rust', 'genetic-algorithms', 'efcore', 'terraform', 'patterns', 'htmx', 'http-rest', 'network-book-sample', 'biz-software', 'simplicity-first', 'speaking', 'ai', 'fun-tech', 'random-csharp'];
  for (const c of categories) map[`/category/${c}`] = `${base}/blog/category/${c}/`;
  return map;
}

/**
 * Until the woodruff.dev DNS cutover, builds run in PREVIEW mode and deploy
 * to https://cwoodruff.github.io/woodruff-dev/. PREVIEW=true is set in the
 * deploy workflow; remove it (and add public/CNAME) at cutover.
 *
 * Every internal link must go through `href()` from src/lib/url.ts so the
 * base path is applied in preview and dropped in production.
 */
const preview = process.env.PREVIEW === 'true';

export default defineConfig({
  site: preview ? 'https://cwoodruff.github.io' : 'https://woodruff.dev',
  base: preview ? '/woodruff-dev' : undefined,
  trailingSlash: 'always',
  redirects: wordpressRedirects(preview ? '/woodruff-dev' : ''),
  integrations: [sitemap()],
  image: {
    service: { entrypoint: 'astro/assets/services/sharp' },
  },
  markdown: {
    shikiConfig: { theme: 'github-light' },
  },
});
