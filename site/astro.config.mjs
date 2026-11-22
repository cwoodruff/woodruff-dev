// @ts-check
import { createReadStream, existsSync, readdirSync, statSync } from 'node:fs';
import { extname, join, normalize } from 'node:path';
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
  const categories = ['rust', 'genetic-algorithms', 'efcore', 'terraform', 'patterns', 'htmx', 'http-rest', 'network-book-sample', 'biz-software', 'simplicity-first', 'career-life', 'developer-experience'];
  for (const c of categories) map[`/category/${c}`] = `${base}/blog/category/${c}/`;
  // Retired categories whose posts were folded into a broader one.
  const retired = {
    speaking: 'career-life',
    ai: 'biz-software',
    'fun-tech': 'developer-experience',
    'random-csharp': 'network-book-sample',
    'network-programming': 'network-book-sample',
  };
  for (const [from, to] of Object.entries(retired)) {
    map[`/category/${from}`] = `${base}/blog/category/${to}/`;
    map[`/blog/category/${from}`] = `${base}/blog/category/${to}/`;
  }
  map['/blog/category/blog'] = `${base}/blog/`;
  return map;
}

/**
 * Blog search uses Pagefind, whose index is written to dist/pagefind after
 * `npm run build`. `astro dev` has no build output, so this serves the index
 * from the last build at /pagefind/ during development. Rebuild to refresh it.
 * @param {string} base
 * @returns {import('astro').AstroIntegration}
 */
function pagefindDevServer(base) {
  const root = new URL('./dist/pagefind/', import.meta.url).pathname;
  const types = { '.js': 'text/javascript', '.css': 'text/css', '.json': 'application/json' };
  return {
    name: 'pagefind-dev-server',
    hooks: {
      'astro:server:setup': ({ server }) => {
        server.middlewares.use(`${base}/pagefind`, (req, res, next) => {
          const file = normalize(join(root, decodeURIComponent((req.url ?? '/').split('?')[0])));
          if (!file.startsWith(root) || !existsSync(file) || !statSync(file).isFile()) return next();
          res.setHeader('Content-Type', types[/** @type {keyof typeof types} */ (extname(file))] ?? 'application/octet-stream');
          createReadStream(file).pipe(res);
        });
      },
    },
  };
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
  integrations: [sitemap(), pagefindDevServer(preview ? '/woodruff-dev' : '')],
  image: {
    service: {
      entrypoint: 'astro/assets/services/sharp',
      // Default encoder quality for every generated image, including images in post bodies.
      config: { webp: { quality: 70 }, jpeg: { quality: 75 } },
    },
  },
  markdown: {
    shikiConfig: { theme: 'github-light' },
  },
});
