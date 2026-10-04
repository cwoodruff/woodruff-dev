// @ts-check
import { defineConfig } from 'astro/config';
import sitemap from '@astrojs/sitemap';

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
  integrations: [sitemap()],
  image: {
    service: { entrypoint: 'astro/assets/services/sharp' },
  },
  markdown: {
    shikiConfig: { theme: 'github-light' },
  },
});
