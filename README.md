# woodruff.dev

The personal site of Chris "Woody" Woodruff: fractional architect, strategic technology advisor, agentic developer relations, and software forensic expert witness. Built with [Astro](https://astro.build) and published to GitHub Pages at https://woodruff.dev.

## Layout

| Path | What it is |
|---|---|
| `site/` | The Astro project. `npm run dev` for local development, `npm run build` for a production build. |
| `site/src/content/` | All content as Markdown collections: `blog/`, `media/` (Press & Media), `portfolio/`, `services/`, `training/`, `testimonials/`. |
| `incoming/blog/`, `incoming/media/` | Drop folders. Push a Markdown file plus its image here and the **Import content** Action turns it into a content entry, commits, and deploys. See the README in each folder. |
| `site/scripts/` | The importers, the one-off WordPress migration helpers, and the prebuild step that copies the resume into `public/`. |
| `docs/` | The migration plan (`ASTRO-SITE-PLAN.md`), the cutover runbook (`CUTOVER.md`), course outlines, the resume, and the book cover placeholder. |
| `.github/workflows/` | `deploy.yml` builds and publishes on every push to `main` that touches the site; `import-content.yml` handles the drop folders. |

## Publishing a blog post

1. Write `incoming/blog/my-post.md` (front matter optional; see `incoming/blog/README.md`).
2. Add the featured image as `incoming/blog/my-post.png` (or `.jpg`, `.jpeg`, `.webp`).
3. Commit and push to `main`. The Action imports the post to `site/src/content/blog/YYYY/MM/my-post/`, builds, and deploys. The post appears at `https://woodruff.dev/blog/my-post/`.

Opening a pull request that touches `incoming/` runs the import and build as a dry run without committing.

## Adding a Press & Media item

Drop `incoming/media/item.md` with `title`, `url`, and `image` front matter plus the logo file, then push. Details in `incoming/media/README.md`.

## Local development

```bash
cd site
npm ci
npm run dev        # http://localhost:4321
npm run build      # full production build into site/dist
PREVIEW=true npm run build   # build with the /woodruff-dev base path (GitHub Pages preview URL)
```

Node 24 is used in CI. The build processes roughly 1,800 images on a cold run (about 30 seconds locally).

## Configuration

- `site/src/lib/site.ts`: name, contact details, social links, navigation, the Web3Forms access key for the contact form.
- `site/src/styles/tokens.css`: colors, type scale, spacing.
- `site/astro.config.mjs`: `site`/`base` (switched by the `PREVIEW` environment variable) and the redirect stubs for old WordPress URLs.
- `site/public/CNAME`: the custom domain.
