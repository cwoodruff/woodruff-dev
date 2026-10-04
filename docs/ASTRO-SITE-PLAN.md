# woodruff.dev on Astro: Migration and Build Plan

Status: approved with decisions (2026-10-04, see section 10)
Owner: Chris "Woody" Woodruff
Target: static Astro site deployed to GitHub Pages from `cwoodruff/woodruff-dev`, served at `https://woodruff.dev`

This plan covers the review of the current WordPress site and the unused ASP.NET Core solution, the outline and design of the new Astro site, the content and image migration, the two GitHub Actions content importers (blog posts and Press & Media), the reserved In-Person Training section, and the cutover to GitHub Pages.

---

## 1. What exists today

### 1.1 The live WordPress site (woodruff.dev)

Reviewed on 2026-10-04. Outline, in nav order:

| Nav item | WordPress URL | Notes |
|---|---|---|
| Home | `/` | Hero, About Me, credentials, contact strip, Blog & Insights (3 posts), Essential Services (6 cards), Portfolio (Books, Podcasts, Workshops, Courses, Open-Source), Testimonials (3), "Any Project On Mind" CTA with contact form |
| Services (dropdown) | `/fractional-architect/`, `/expert-witness/`, `/micro-consulting/`, `/project-based-contracts/`, `/retainer-based-services/`, `/advisory-board-roles/` | Six service detail pages |
| Blog & Insights | `/category/blog/` and per-post permalinks | 217 posts (already exported to this repo, see 1.2) |
| Contact | `/contact/` | Phone, email, Wyoming MI 49418, "respond within 24h", form (Name, Email, Phone, Company, Subject select, Message), Akismet |
| Press & Media | `/press-media/` | 12 cards, each a logo + title + link (list in Appendix A) |
| About | `/about/` | Mission / Vision / Goal, bio, MVP (2008, 2025), .NET Foundation board, 50+ talks, books, The Breakpoint Show, Simplicity-First Review newsletter, Agentic Relations, "Download CV" |
| Download CV | button in header | PDF |

Social links in the header: LinkedIn, GitHub, YouTube, RSS (`/feed/`), WhatsApp, Reddit, Podcast.
Credential badges: Microsoft MVP 2025, JetBrains (JBCC).
Footer: copyright only.

### 1.2 The ASP.NET Core solution (`woodruffdev.web/`)

A .NET 10 Razor Pages app that was built to replace WordPress. It is a useful source for three things and should be treated as the content of record where it is newer than WordPress.

**Content already in a migratable form**

- `BlogPosts/YYYY/MM/<slug>/index.md` plus `images/` per post. 217 posts, every one with `title`, `date`, `categories`, `tags`, `coverImage` front matter. Images are 226 PNG, 18 WebP, 5 JPG. This is the exact shape an Astro content collection wants, so the blog migration is mostly a move.
- `Services/PortfolioService.cs` and `Services/MediaService.cs` hold the Portfolio and Media seed data as C# object initialisers. They move into Markdown/JSON content entries.
- `Pages/Services/*.cshtml` (six services), `Pages/Projects/*.cshtml` (five project detail pages), `Pages/About.cshtml`, `Pages/Contact.cshtml`, `Pages/AgenticRelations.cshtml`, and the home page sections hold finished copy. That copy is better than the WordPress copy in most places (it was rewritten in commits `270eebc` and `d9090ce`) and is what the Astro pages should carry.
- `docs/COURSES.md` holds three full course outlines (EF Core, C# Network Programming, htmx with Razor Pages). These are the obvious seed for the Training section later.

**Structure worth keeping**

- Nav: Home, Services (6), Portfolio, Media, Blog, Network (Simplicity-First, Agentic Relations), About, Get In Touch.
- Home section order: Hero, Why Work With Me (4 benefits), Services (6), Simplicity-First companion block, Portfolio (featured), How I Work (4 steps), Blog & Insights (3 latest), Testimonials (2), CTA.
- Portfolio filters (All / Open Source / Writing) and Media filters (All / Podcasts / Videos / Articles) as client-side tabs.
- A single `page-header` pattern for inner pages and a `service-detail` body pattern.

**Things that do not carry over**

- The runtime Markdown pipeline (Markdig, YamlDotNet, `BlogService`). Astro's content layer replaces it at build time.
- The POST contact form handler. GitHub Pages has no server, see 6.3.
- Instrument Serif + DM Sans and the warm gold design system. The new design follows kalogirourania.com, see section 3.
- The `wwwroot/lib` vendored jQuery and Bootstrap. Not needed.

**Differences between WordPress and the ASP.NET solution that need a decision**

| Topic | WordPress | ASP.NET | Recommendation |
|---|---|---|---|
| Sixth service | Retainer-Based Services | Developer Relations | **Decided:** Agentic Developer Relations, with copy drawn from agenticairelations.com (see 1.4). |
| Media section | "Press & Media" (logos linking out) | "Media" (podcast/video/article cards, no logos) | Name it **Press & Media**, keep the WordPress logo-card style, keep the ASP.NET type filters. |
| About | Long bio with newsletter, Agentic Relations, .NET Foundation | Mission/Vision/Goal tabs + shorter bio | Merge: tabs from ASP.NET, bio facts from WordPress. |
| Blog name | Blog & Insights | Blog | "Blog & Insights" in nav, `/blog/` route. |

### 1.3 Images in `/images`

| File | Size | Dimensions | Use |
|---|---|---|---|
| `Chris-Woodruff-01.png` | 39 MB | 4990 x 7485, transparent | Full-length portrait for the home hero |
| `Chris-Woodruff-head.png` | 5 MB | 2210 x 2210, transparent | About page, OpenGraph image, hero fallback |
| `Chris-Woodruff-head-1000.png` | 1 MB | 1000 x 1000, transparent | Avatar, author byline, favicon source |

Also available: `woodruffdev.web/wwwroot/images/woody-portrait.jpg` (9 MB, 5117 x 7675, photo credit David Chandler 2024).

Other assets already in `/docs`:

| File | Use |
|---|---|
| `docs/Christopher_Woodruff_Executive_Resume.pdf` (3 pages) | The "Download CV" target. A `prebuild` npm script copies it to `site/public/Christopher_Woodruff_Executive_Resume.pdf` so `/docs` stays the master and the download keeps its real file name. |
| `docs/Book Cover.png` (1800 x 2700, 2:3) | Temporary cover for every book in the Portfolio, stored as `site/src/content/portfolio/images/book-cover-placeholder.png`. Replace per book later by changing one `image:` line in each entry. |

### 1.4 agenticairelations.com (source for the Agentic Developer Relations service)

Reviewed on 2026-10-04. The site is a standalone body of work: What Is It, The Patterns (37 DevRel activity patterns reframed), New Roles, Measurement, Essays, For Companies, For DevRel Teams, About, newsletter ("The Agentic Developer Relations Brief", twice a month), RSS.

Material to lift into the service page and the home copy:

- Definition: "the discipline within Developer Relations responsible for ensuring that AI coding agents and autonomous AI systems can successfully integrate with, consume, and represent a platform accurately."
- The hook: "When a developer asks AI to integrate with your platform, does it work?"
- Three principles: the agent ecosystem is shaped by deliberate choices or by neglect; agent failures are silent and systemic; encoded judgment is the moat.
- Metrics: FAISR (First-Attempt Integration Success Rate), the Amdahl ceiling, recipe coverage, recipe freshness, competitive FAISR delta.
- The "four business arguments" and "investment tiers" from For Companies, which become the "Signals you need this" and "Engagement models" sections.
- The week-one exercise from For DevRel Teams (ten integration tasks, typical prompts, two AI tools, scored output) becomes the Micro-Consulting-sized entry offer.
- Credentials to add to About: coined the term Agentic Developer Relations; co-author of *Developer Relations Activity Patterns* (Apress, 2026, with Ted Neward, Scott McAllister, and David Neal); founder of EcoSynt.

Note the About page there lists `cwoodruff@live.com`; woodruff.dev keeps `chris@woodruff.dev`.

Plan for these: downscale the two large sources once (hero to 2400px tall, head to 1600px) and commit the downscaled copies under `site/src/assets/portraits/`. Astro's `<Image>`/`<Picture>` components then emit WebP/AVIF at the exact rendered widths at build time. The 39 MB original stays in `/images` as the master but is not referenced by the site, so it never enters the build.

Logos and badges still to fetch from WordPress uploads are listed in Appendix A.

---

## 2. New site outline

Routes are lower-case and trailing-slash so they match GitHub Pages directory hosting.

```
/                               Home
/services/                      Services overview (6 cards)
/services/fractional-architect/
/services/expert-witness/
/services/micro-consulting/
/services/project-based/
/services/agentic-developer-relations/
/services/advisory/
/portfolio/                     Filterable grid (All / Open Source / Writing)
/portfolio/<slug>/              Detail pages for internal items (book, course, workshops, podcasts)
/press-media/                   Press & Media logo cards (All / Podcasts / Videos / Articles)
/blog/                          Paginated listing, newest first
/blog/page/<n>/
/blog/<slug>/                   Post (flat; the YYYY/MM folders stay on disk only)
/blog/category/<category>/      Category listing
/blog/tag/<tag>/                Tag listing
/training/                      In-person training index ("coming soon" state until content is planned)
/training/<slug>/               Individual course / workshop page
/about/
/contact/
/rss.xml                        Blog feed
/sitemap-index.xml              From @astrojs/sitemap
/Christopher_Woodruff_Executive_Resume.pdf   Download CV
/404/
```

Navigation (desktop, matching the reference site's single-row header with a right-aligned CTA button):

```
[CW mark]  Home  Services ▾  Portfolio  Press & Media  Blog & Insights  Training  Network ▾  About      [Let's work →]
```

- Services dropdown: the six services.
- Network dropdown: Simplicity-First (external, simplicity-first.dev), Agentic Developer Relations (external, agenticairelations.com). The ASP.NET "coming soon" page is dropped because the real site now exists.
- "Let's work" goes to `/contact/`.
- Download CV moves from the header into the About page and the hero's secondary button, as the ASP.NET design already did. The header only has room for one CTA in the reference layout.
- Mobile: hamburger to a full-screen panel, same items flattened.

Home page sections, in order, mapped to the reference design's rhythm:

| # | Section | Source of copy | Reference-site equivalent |
|---|---|---|---|
| 1 | Hero: role line, headline with a typed rotating word, two buttons, social row, full-length portrait on the right, badges floating on the photo | ASP.NET `Index.cshtml` hero + WordPress hero paragraph | "Looking for help with your ___?" hero with portrait |
| 2 | The Problem / The Solution: two short columns | New copy, drawn from the Fractional Architect "Signals you need this" lists | "The Problem" / "The Solution" |
| 3 | "Hi there" intro: head photo, 2 paragraphs, link to About | WordPress About Me block | "Hi there! Allow me to introduce myself" |
| 4 | Logo strip: outlets I have appeared on (.NET Rocks, JetBrains, InfoWorld, TechBullion, The Azure Podcast, CDF) | Press & Media logos | Monogram logo strip |
| 5 | Numbers: 25+ years, 50+ talks, 2 books, 200+ articles, MVP since 2008 | WordPress About | "The numbers don't lie" count-up stats |
| 6 | What I do / How I do it: two columns with "Learn more" | ASP.NET "Why Work With Me" + "How I Work" condensed | "What I do? / How I do it?" |
| 7 | Services: six cards | ASP.NET services grid | (addition, kept from current site) |
| 8 | Portfolio: three featured cards + "See full portfolio" | ASP.NET featured portfolio | (addition) |
| 9 | Testimonials: Natalie Greenwood, Ted Neward, plus the third WordPress quote | ASP.NET + WordPress | "Some love from clients" |
| 10 | Blog: three latest posts with cover images | content collection | "Useful articles from the blog" |
| 11 | Companion sites: Simplicity-First and Agentic Developer Relations, two cards | ASP.NET block, extended | (addition) |
| 12 | Footer CTA: "Ready to untangle your system? Hire me." | ASP.NET CTA | "Ready to create your website copy? Hire me!" |
| 13 | Dark footer: brand, Services links, Navigate links, Network links, contact info, social, copyright | ASP.NET footer | Dark footer with Useful Links / Contact Info |

---

## 3. Design: based on kalogirourania.com

The reference site runs the WordPress "Lemmony" theme. The traits to reproduce:

**Typography**
- One typeface: Plus Jakarta Sans (self-host the woff2 files in `public/fonts/`, weights 400, 500, 600, 700, 800). No serif display face.
- Fluid type scale using `clamp()`. The reference's scale, rounded:

  | Token | Value |
  |---|---|
  | `--fs-gigantic` | `clamp(3.5rem, 2.9rem + 3vw, 6rem)` (hero h1) |
  | `--fs-huge` | `clamp(2.85rem, 2.4rem + 2vw, 4.5rem)` (section h2) |
  | `--fs-large` | `clamp(2.25rem, 2rem + 1vw, 3rem)` |
  | `--fs-medium` | `clamp(1.5rem, 1.4rem + 0.5vw, 1.875rem)` (h3, card titles) |
  | `--fs-small` | `clamp(1.125rem, 1.1rem + 0.15vw, 1.25rem)` (lead paragraph) |
  | `--fs-normal` | `1rem` (body) |
  | `--fs-tiny` | `clamp(0.875rem, 0.85rem + 0.15vw, 1rem)` (meta, labels) |

- Headings at weight 700/800 with tight letter-spacing (-0.02em) and line-height 1.1. Body at 400, line-height 1.7.

**Color**
- White background, near-black text, light-grey surfaces for alternating sections, one bright accent for highlights and the typed hero word, dark charcoal footer. Buttons are black pills with white text; the hover state inverts.

  | Token | Value | Role |
  |---|---|---|
  | `--c-bg` | `#FFFFFF` | page |
  | `--c-bg-alt` | `#F7F7F7` | alternating sections, cards |
  | `--c-bg-dark` | `#111111` | footer, footer CTA |
  | `--c-text` | `#1C1C1C` | body |
  | `--c-text-muted` | `#5C5C5C` | meta |
  | `--c-border` | `#E0E0E0` | hairlines |
  | `--c-accent` | `#c9a96e` | highlight marks, typed word underline, stat numbers, portrait backing block, chips |
  | `--c-accent-soft` | `rgba(201, 169, 110, 0.14)` | tinted backgrounds behind labels and the hero shape |
  | `--c-accent-ink` | `#8a6b36` | accent used as text on white (the gold itself is too light for small text to pass WCAG AA; this darker shade is 4.6:1) |
  | `--c-link` | `#1C1C1C` underlined, `--c-accent-ink` on hover | |

  **Decided:** warm gold `#c9a96e`, carried over from the ASP.NET design system. Buttons stay black like the reference; gold is used for fills, marks, and the typed hero word, never for body-size text on white.

**Layout and motion**
- Container 1200px, 24px gutters at phone width.
- Section padding on a fluid spacing scale (`--sp-60`: `clamp(3rem, 2.5rem + 2vw, 4rem)` and so on).
- Fixed header that becomes compact on scroll.
- Entrance animations: fade-in / fade-in-up / fade-in-left on scroll, implemented with an `IntersectionObserver` and CSS classes, honouring `prefers-reduced-motion`.
- Count-up numbers in the stats section and a typed rotating word in the hero (`typed.js` style, implemented in a tiny inline script to avoid a dependency).
- Rounded 16px corners on cards and images, soft 1px borders, no drop shadows.
- Portraits sit on a flat accent-coloured rounded block behind them (the reference places its portrait on a flat shape).
- Testimonials: three cards, round avatar, name, role, quote.
- Blog cards: cover image 16:9 on top, category chip, title, date.

**Light mode only.** The reference site is light only and the blog cover images were made for white backgrounds. Add a dark theme later if wanted; the token setup allows it.

---

## 4. Astro project structure

Astro 7.3 (current as of 2026-10-04), Node 24, npm. The site lives in `site/` so the ASP.NET project can stay until the cutover is done, then be deleted.

```
site/
├── astro.config.mjs
├── package.json
├── tsconfig.json
├── public/
│   ├── CNAME                      "woodruff.dev" (added at cutover, not before; see Phase 0 step 4)
│   ├── Christopher_Woodruff_Executive_Resume.pdf   copied from /docs by `npm run prebuild`
│   ├── favicon.svg
│   └── robots.txt
├── src/
│   ├── content.config.ts          collection definitions (section 5)
│   ├── content/
│   │   ├── blog/YYYY/MM/<slug>/index.md + images/
│   │   ├── media/<slug>.md        Press & Media items
│   │   ├── media/images/          outlet logos
│   │   ├── portfolio/<slug>.md
│   │   ├── portfolio/images/
│   │   ├── services/<slug>.md
│   │   ├── training/<slug>.md
│   │   └── testimonials/<slug>.md
│   ├── assets/
│   │   ├── fonts/                 Plus Jakarta Sans variable woff2 (bundled by Vite so the base path applies)
│   │   ├── portraits/             downscaled portrait PNGs
│   │   └── badges/                MVP, JBCC
│   ├── layouts/
│   │   ├── Base.astro             head, header, footer, scripts
│   │   ├── Page.astro             inner page with page-header
│   │   └── Post.astro             blog article
│   ├── components/
│   │   ├── Header.astro, Footer.astro, Nav.astro, MobileNav.astro
│   │   ├── Hero.astro, TypedWord.astro
│   │   ├── SectionHeader.astro
│   │   ├── ProblemSolution.astro, Intro.astro, LogoStrip.astro, Stats.astro
│   │   ├── WhatHow.astro, ServiceCard.astro, ServicesGrid.astro
│   │   ├── ProjectCard.astro, FilterTabs.astro
│   │   ├── MediaCard.astro
│   │   ├── BlogCard.astro, Pagination.astro
│   │   ├── TestimonialCard.astro
│   │   ├── CompanionSite.astro, CtaBand.astro
│   │   ├── ContactForm.astro
│   │   └── Seo.astro
│   ├── pages/
│   │   ├── index.astro
│   │   ├── about.astro, contact.astro, 404.astro
│   │   ├── services/index.astro, services/[slug].astro
│   │   ├── portfolio/index.astro, portfolio/[slug].astro
│   │   ├── press-media/index.astro
│   │   ├── blog/index.astro, blog/page/[page].astro
│   │   ├── blog/[slug].astro
│   │   ├── blog/category/[category].astro, blog/tag/[tag].astro
│   │   ├── training/index.astro, training/[slug].astro
│   │   └── rss.xml.ts
│   ├── styles/
│   │   ├── tokens.css, base.css, components.css, utilities.css
│   └── lib/
│       ├── posts.ts               sorting, excerpt, reading time, grouping
│       ├── site.ts                constants: name, email, phone, socials, nav, services
│       └── url.ts                 href(): prefixes internal links with the base path in preview builds
├── scripts/
│   ├── import-blog.mjs            used by the import workflow (section 6)
│   ├── import-media.mjs
│   └── migrate-aspnet-content.mjs one-off: BlogPosts/ and C# seed data into src/content
└── incoming/                      drop folders watched by the import workflow
    ├── blog/README.md
    └── media/README.md
```

Dependencies: `astro`, `@astrojs/sitemap`, `@astrojs/rss`, `sharp` (image service), `gray-matter` and `slugify` for the import scripts. No UI framework; every component is plain `.astro` with scoped CSS and small inline scripts.

`astro.config.mjs` essentials:

```js
import { defineConfig } from 'astro/config';
import sitemap from '@astrojs/sitemap';

const preview = process.env.PREVIEW === 'true'; // set in deploy.yml until cutover

export default defineConfig({
  site: preview ? 'https://cwoodruff.github.io' : 'https://woodruff.dev',
  base: preview ? '/woodruff-dev' : undefined,
  trailingSlash: 'always',
  integrations: [sitemap()],
  image: { service: { entrypoint: 'astro/assets/services/sharp' } },
  markdown: { shikiConfig: { theme: 'github-light' } },
});
```

Astro 7 notes that affect this build: the Rust compiler rejects unclosed or mis-nested HTML, so the WordPress-era HTML inside some posts must be valid; `compressHTML` defaults to JSX-style whitespace stripping, so inline elements need explicit spacing; the default Markdown processor is no longer remark/rehype, so if a remark plugin is wanted (reading time, for example) install `@astrojs/markdown-remark` and opt in.

---

## 5. Content collections

`src/content.config.ts` with the glob loader. Schemas:

**blog**

```ts
blog: defineCollection({
  loader: glob({ pattern: '**/index.md', base: './src/content/blog' }),
  schema: ({ image }) => z.object({
    title: z.string(),
    date: z.coerce.date(),
    updated: z.coerce.date().optional(),
    description: z.string().optional(),   // falls back to first paragraph
    categories: z.array(z.string()).default([]),
    tags: z.array(z.string()).default([]),
    coverImage: image().optional(),       // resolved relative to the post: "./images/foo.png"
    draft: z.boolean().default(false),
  }),
}),
```

Posts stay in `YYYY/MM/<slug>/` folders on disk (it keeps 217 folders browsable and matches the importer's output), but the public URL is flat: `/blog/<slug>/`. The loader's `generateId` takes the last path segment as the entry id, and the migration script asserts that all 217 slugs are unique before anything is written; a collision fails the build with both paths named. The migration script rewrites `coverImage: "foo.png"` to `coverImage: "./images/foo.png"` so the `image()` helper validates it, and leaves in-body `images/...` links alone because Astro resolves relative image paths inside Markdown on its own. `<!--more-->` markers are stripped at build time in `lib/posts.ts` and used as the excerpt boundary when present.

**media** (Press & Media)

```ts
media: defineCollection({
  loader: glob({ pattern: '*.md', base: './src/content/media' }),
  schema: ({ image }) => z.object({
    title: z.string(),
    url: z.string().url(),
    image: image(),                        // "./images/dotnet-rocks.png"
    outlet: z.string().optional(),
    type: z.enum(['podcast', 'video', 'article', 'talk']).default('article'),
    date: z.coerce.date().optional(),
    description: z.string().optional(),
    featured: z.boolean().default(false),  // shows in the home logo strip
  }),
}),
```

**portfolio**

```ts
portfolio: defineCollection({
  loader: glob({ pattern: '*.md', base: './src/content/portfolio' }),
  schema: ({ image }) => z.object({
    title: z.string(),
    category: z.enum(['open-source', 'book', 'course', 'workshop', 'podcast', 'article']),
    description: z.string(),
    image: image().optional(),
    url: z.string().url().optional(),      // external; when absent the Markdown body is the detail page
    tech: z.array(z.string()).default([]),
    year: z.number().optional(),
    featured: z.boolean().default(false),
    order: z.number().default(100),
  }),
}),
```

Seeded from `PortfolioService.cs` plus the two podcasts and the five `Pages/Projects/*.cshtml` bodies. Writing filter = book, course, workshop, article, podcast.

Books in the first pass, all pointing at `./images/book-cover-placeholder.png` (from `docs/Book Cover.png`) until real covers arrive: *ASP.NET Core Reimagined with htmx*; *Beyond Boundaries: Network Programming with C# 12 and .NET 8*; *Developer Relations Activity Patterns* (Apress, 2026, co-authored). Book cards render at a 2:3 aspect ratio so the placeholder and the eventual real covers share one layout.

**services**

```ts
services: defineCollection({
  loader: glob({ pattern: '*.md', base: './src/content/services' }),
  schema: z.object({
    title: z.string(),
    short: z.string(),                     // card blurb
    tagline: z.string(),                   // page-header subtitle
    order: z.number(),
    ctaHeading: z.string(),
    ctaText: z.string(),
  }),
}),
```

Bodies are five of the six `Pages/Services/*.cshtml` articles converted to Markdown. The sixth, `agentic-developer-relations.md`, is written fresh from agenticairelations.com using the same section pattern as the others:

1. **Service Overview**: the canonical definition and the hook question ("When a developer asks AI to integrate with your platform, does it work?"). Your platform now has two audiences, human developers and AI coding agents, and DevRel has no framework for the second one.
2. **Signals You Need This**: integration failures through AI tools that never show up in a dashboard; buyers evaluating the platform through an agent instead of a proof of concept; docs and SDKs written only for human navigation; DevRel knowledge leaving with the people who hold it (the four business arguments, reframed as symptoms).
3. **What's Included**: FAISR baseline across the major coding agents; an agent-readiness audit of docs, SDKs, error models, and schemas; a validated prompt-recipe library with coverage and freshness tracking; Agent Champion role design and hiring guidance; a measurement model (FAISR, Amdahl ceiling, recipe coverage, recipe freshness, competitive FAISR delta) the board can read; the human-facing DevRel track (DX audits, content, conference programs, community) carried over from the existing Developer Relations page.
4. **How It Works**: week-one diagnostic (ten common integration tasks, typical developer prompts, two AI tools, scored output) then a 90-day program, then handoff to an in-house Agent Champion.
5. **Engagement Models**: mapped to the site's investment tiers (part-time single practitioner, embedded program lead, multi-person team design) plus the existing DevRel fractional and project options.
6. **Why Work With Me**: coined the term; co-author of *Developer Relations Activity Patterns*; led DevRel at JetBrains and Rocket; founder of EcoSynt.
7. CTA plus a "Read the framework" link to agenticairelations.com.

The `[slug].astro` page adds the previous/next service links from `order`.

**training** (reserved, see section 7)

```ts
training: defineCollection({
  loader: glob({ pattern: '*.md', base: './src/content/training' }),
  schema: ({ image }) => z.object({
    title: z.string(),
    short: z.string(),
    format: z.enum(['workshop', 'course', 'bootcamp']),
    duration: z.string(),                  // "1 day", "3 x half-day"
    level: z.enum(['intermediate', 'advanced']),
    audience: z.string(),
    prerequisites: z.array(z.string()).default([]),
    modules: z.array(z.object({ title: z.string(), length: z.string().optional() })).default([]),
    image: image().optional(),
    accent: z.string().optional(),
    nextDates: z.array(z.object({ date: z.coerce.date(), city: z.string(), url: z.string().url().optional() })).default([]),
    draft: z.boolean().default(true),
    order: z.number().default(100),
  }),
}),
```

**testimonials**: `name`, `role`, `avatar` (optional image), body is the quote.

---

## 6. Content importers via GitHub Actions

Both importers share one workflow, `.github/workflows/import-content.yml`, which fires on pushes that touch `incoming/**`. After importing it commits the result and then runs the build and deploy in the same workflow run.

That last point matters: commits made with the default `GITHUB_TOKEN` do not trigger other `push` workflows, so a separate "deploy on push" workflow would never see the import commit. The deploy job is therefore defined once as a reusable workflow (`deploy.yml`, `on: workflow_call` plus `on: push` for ordinary commits) and the import workflow calls it after committing. No personal access token is needed.

### 6.1 Blog post importer

**What Woody does**

1. Write the post as Markdown with YAML front matter (title, date, categories, tags; all optional except title, which can also come from the first `# H1`).
2. Name the file and its featured image identically apart from the extension: `incoming/blog/leader-election-in-net.md` and `incoming/blog/leader-election-in-net.png`. Any other images the post uses go in the same folder and are referenced as `images/<name>` or just `<name>` in the Markdown.
3. Commit and push to `main` (or open a PR, see "PR mode" below).

**What the action does** (`scripts/import-blog.mjs`)

1. Find every `incoming/blog/*.md`. For each one:
   - Parse front matter with `gray-matter`. Fill defaults: `date` = today (UTC) if missing, `title` = first H1 or the file name if missing, `categories` = `["blog"]` if missing.
   - Derive `slug` from front matter `slug` if present, otherwise from the title with `slugify` (lower-case, ASCII, hyphens), matching the existing folder naming.
   - Compute `YYYY/MM` from `date`.
   - Create `site/src/content/blog/YYYY/MM/<slug>/images/`.
   - Move the same-basename `.png` (also accept `.jpg`, `.jpeg`, `.webp`) into `images/` and set `coverImage: ./images/<file>` in the front matter. Fail the job with a clear message if no image is found; a featured image is required by the blog card design.
   - Move any other image files referenced in the Markdown body from `incoming/blog/` into `images/` and rewrite the references to `images/<name>`.
   - Strip the H1 from the body if it duplicates the title.
   - Write `index.md` with normalised front matter.
   - Delete the processed files from `incoming/blog/`.
2. Run `astro check`-style validation by building the site (the build step fails on schema errors, so a bad front matter never reaches production).
3. Commit with `chore(blog): import <title>` as `github-actions[bot]`, push to `main`.
4. Call the deploy workflow.

Idempotent: if the target folder already exists, the script overwrites `index.md` and images (useful for corrections) and says so in the job summary.

### 6.2 Press & Media importer

**What Woody does**

Drop `incoming/media/<anything>.md` containing only front matter, plus the logo image it names:

```md
---
title: "C# Networking with Chris Woodruff"
url: https://podtail.com/en/podcast/-net-rocks/c-networking-with-chris-woodruff-2025-05-22/
image: dotnet-rocks.png
outlet: .NET Rocks!
type: podcast
date: 2025-05-22
description: "Carl and Richard talk with Chris about network programming in C#."
featured: true
---
```

Only `title`, `url`, and `image` are required. The image sits next to the `.md` file.

**What the action does** (`scripts/import-media.mjs`)

1. For each `incoming/media/*.md`: parse front matter, validate the three required fields, slugify the title into `<slug>.md`, move the image to `site/src/content/media/images/` (keeping the name, de-duplicating if the same logo is reused by several items), rewrite `image:` to `./images/<file>`, write the entry, delete the incoming files.
2. Commit `chore(media): add <title>` and deploy as above.

### 6.3 Workflow files

`.github/workflows/deploy.yml`

```yaml
name: Build and deploy
on:
  push:
    branches: [main]
    paths: ['site/**', 'docs/Christopher_Woodruff_Executive_Resume.pdf', '.github/workflows/deploy.yml']
  workflow_call:
  workflow_dispatch:

permissions:
  contents: read
  pages: write
  id-token: write

concurrency:
  group: pages
  cancel-in-progress: false

env:
  PREVIEW: 'true'   # flip to 'false' and add site/public/CNAME at cutover

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v7
      - uses: withastro/action@v6
        with:
          path: ./site
          node-version: 24
          package-manager: npm@latest
  deploy:
    needs: build
    runs-on: ubuntu-latest
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    steps:
      - id: deployment
        uses: actions/deploy-pages@v5
```

`.github/workflows/import-content.yml`

```yaml
name: Import content
on:
  push:
    branches: [main]
    paths: ['incoming/**']

permissions:
  contents: write
  pages: write
  id-token: write

jobs:
  import:
    runs-on: ubuntu-latest
    outputs:
      changed: ${{ steps.commit.outputs.changed }}
    steps:
      - uses: actions/checkout@v7
      - uses: actions/setup-node@v5
        with: { node-version: 24, cache: npm, cache-dependency-path: site/package-lock.json }
      - run: npm ci
        working-directory: site
      - run: node scripts/import-blog.mjs
      - run: node scripts/import-media.mjs
      - run: npm run build          # schema validation before anything is committed
        working-directory: site
      - id: commit
        run: |
          git config user.name "github-actions[bot]"
          git config user.email "41898282+github-actions[bot]@users.noreply.github.com"
          git add -A
          if git diff --cached --quiet; then echo "changed=false" >> "$GITHUB_OUTPUT"; exit 0; fi
          git commit -m "chore(content): import from incoming/"
          git push
          echo "changed=true" >> "$GITHUB_OUTPUT"
  deploy:
    needs: import
    if: needs.import.outputs.changed == 'true'
    uses: ./.github/workflows/deploy.yml
    permissions:
      contents: read
      pages: write
      id-token: write
```

**PR mode (optional, recommended once the site is live):** add `pull_request` paths `incoming/**` to the import workflow with a job that runs the scripts and `npm run build` but does not commit, so a PR shows whether the post will import cleanly before merge. The push-to-main job does the real import.

### 6.4 Contact form on a static host

The Razor Pages POST handler cannot run on GitHub Pages. **Decided: Web3Forms.**

- Plain HTML `<form action="https://api.web3forms.com/submit" method="POST">` with the same fields as today (Name, Email, Phone, Company, Subject select, Message) plus Training in the Subject list.
- Hidden fields: `access_key` (Web3Forms keys are designed to be public and are stored in `lib/site.ts`, not in a secret), `subject` built from the select, `from_name`, `redirect` to `https://woodruff.dev/contact/?sent=1`.
- Spam: Web3Forms' `botcheck` honeypot checkbox, hidden with CSS. hCaptcha can be switched on later from the Web3Forms dashboard without a code change.
- Setup: create the key at web3forms.com with `chris@woodruff.dev` as the destination, verify the address, paste the key into `lib/site.ts`.
- Progressive enhancement: a small inline script submits via `fetch` and shows the success state in place; without JavaScript the plain POST and redirect still work.

---

## 7. In-Person Training section (reserved)

Built in the first pass as structure only, content planned later:

- Collection and schema from section 5 (`training`).
- `/training/` index: page header, intro paragraph, cards per offering (title, format, duration, level, short), a "Bring this to your team" CTA band linking to `/contact/` with Subject preselected to "Training".
- `/training/<slug>/`: hero with accent colour, "Who it's for", "Prerequisites", "Agenda" (modules list), "Upcoming dates" (only when `nextDates` is non-empty), "Private delivery" CTA.
- **Decided:** the "Training" nav item ships at launch. While every entry is `draft: true` the index shows a "Workshops coming soon" state with a short paragraph and a "Tell me what your team needs" link to `/contact/`, so the link is never dead.
- `docs/COURSES.md` already holds three outlines (EF Core, C# Network Programming, htmx with Razor Pages) that can become the first three entries by moving the module lists into `modules:`.
- Contact form Subject select gains a "Training" option.

---

## 8. Portfolio and Network

**Portfolio** reproduces the ASP.NET page: page header, filter tabs (All / Open Source / Writing), card grid with cover image, category chip, title, description, tech chips, external-link indicator. Internal items render a detail page from their Markdown body (the five `Pages/Projects/*` bodies: htmx book, EF Core course, htmx workshop, Terraform workshop, plus The Woody Show and The Breakpoint Show). The GitHub item links out. Three `featured` items appear on the home page.

**Network** is a header dropdown, not a page: Simplicity-First (external, `simplicity-first.dev`) and Agentic Developer Relations (external, `agenticairelations.com`). The footer repeats both plus LinkedIn, GitHub, YouTube, Bluesky (`@woodruff.dev`), Mastodon (`mastodon.social/@cwoodruff`). The home page's companion-site block becomes a two-card row: Simplicity-First and Agentic Developer Relations, each with a one-paragraph description and a "Visit" button.

---

## 9. Migration steps

### Phase 0: scaffold (half a day)
1. `npm create astro@latest site` (empty template, TypeScript strict), add sitemap, rss, sharp.
2. Commit `astro.config.mjs`, `public/CNAME`, tokens, fonts, `Base.astro`, header and footer.
3. Add `deploy.yml`, enable GitHub Pages with source "GitHub Actions" in repo settings (`gh api repos/cwoodruff/woodruff-dev/pages` currently returns 404, so Pages is not yet enabled).
4. First deploy to `cwoodruff.github.io/woodruff-dev` for review. The deploy workflow sets `PREVIEW=true`, which switches `site` and `base` so every internal link works on the GitHub Pages URL. Every internal link goes through `href()` in `src/lib/url.ts` for that reason. `public/CNAME` must not exist until cutover: GitHub Pages would claim the custom domain on the first deploy and redirect the preview URL to it. At cutover, flip `PREVIEW` to `false` in `deploy.yml` and add `CNAME` in the same commit.

Phase 0 was completed on 2026-10-04 (branch `claude/astro-site-phase0`).

### Phase 1: content migration (one day)
1. `scripts/migrate-aspnet-content.mjs`:
   - Copy `woodruffdev.web/BlogPosts/**` to `site/src/content/blog/`, rewrite `coverImage` to `./images/...`, convert `.webp`/`.jpg` cover references verbatim, validate every file parses.
   - Emit portfolio, media, services, testimonials entries from the C# seed data and the Razor bodies (hand-check the Markdown conversion of the five migrated service pages). Write `agentic-developer-relations.md` by hand from section 5.
   - Copy `docs/Book Cover.png` to `src/content/portfolio/images/book-cover-placeholder.png`.
2. Fetch the Press & Media logos and the two badges from the WordPress uploads (Appendix A) into `src/content/media/images/` and `src/assets/badges/`.
3. Downscale the portraits into `src/assets/portraits/`.
4. Add the `prebuild` script that copies `docs/Christopher_Woodruff_Executive_Resume.pdf` into `site/public/`.
5. `npm run build` must pass with zero schema errors. Fix posts whose WordPress HTML the Astro 7 compiler rejects (unclosed tags). Expect a handful.

Phase 1 was completed on 2026-10-04 (branch `claude/astro-site-phase1`). The tracked `woodruffdev.web/BlogPosts` folder was moved with `git mv` rather than copied, so the 557 MB of post images did not enter the repository a second time. Testimonial from Ted Neward carries the ASP.NET wording and needs confirmation; the third WordPress testimonial was theme placeholder text and was dropped.

### Phase 2: pages and components (two to three days)
Home, Services (including the new Agentic Developer Relations page), Portfolio, Press & Media, Blog (index, pagination, post, category, tag), About, Contact, Training "coming soon", 404, RSS. Scroll animations, typed hero word, count-up stats, filter tabs, mobile nav. Lighthouse pass: performance and accessibility at 95+, every image with `alt`, visible focus states, reduced-motion respected.

Phase 2 was completed on 2026-10-04 (branch `claude/astro-site-phase2`): 328 pages, 1,769 optimized images, 28-second local build. One global CSS rule (`[hidden] { display: none !important }`) was needed because component display rules otherwise override the `hidden` attribute used by tabs and filters.

### Phase 3: importers (half a day)
`import-blog.mjs`, `import-media.mjs`, `import-content.yml`, `incoming/*/README.md` with the two front matter templates. Test with one post and one media item on a branch before merging.

Phase 3 was completed on 2026-10-04 (branch `claude/astro-site-phase3`). Both importers were exercised locally with fixtures: a post with an H1-derived title, a same-name cover, and an inline image; a media item with a new logo and one reusing an existing logo. The import workflow also runs on pull requests as a dry run.

### Phase 4: cutover
1. Export the WordPress permalink list (`wp post list --format=csv` or the sitemap) and confirm every post maps to `/blog/<slug>/`. The old URLs appear to be `/YYYY/MM/DD/slug/` or `/slug/`; GitHub Pages cannot do server redirects, so for the top-linked old URLs generate stub pages with `<meta http-equiv="refresh">` plus `<link rel="canonical">` from a `redirects.json` list. Because the new slug is the last segment of the old path in every case, the stub list can be generated mechanically from the WordPress export rather than by hand. Low-traffic URLs can be left to the 404 page, which will offer search-by-title.
2. RSS moves from `/feed/` to `/rss.xml`. Generate `/feed/index.html` as a refresh stub to the new feed for human visitors and announce the new URL once.
3. Add `public/CNAME`, point `woodruff.dev` A/AAAA records at GitHub Pages and `www` CNAME at `cwoodruff.github.io`, enable "Enforce HTTPS".
4. Keep WordPress reachable for a week at a temporary subdomain for reference, then decommission.
5. Delete `woodruffdev.web/`, `woodruffdev.sln`, and `.idea/` once the Astro site is the only source of truth. Update the root `README.md`.

---

## 10. Decisions (resolved 2026-10-04)

| # | Decision | Outcome |
|---|---|---|
| 1 | Accent colour | Warm gold `#c9a96e`, with `#8a6b36` for accent-as-text |
| 2 | Contact form | Web3Forms |
| 3 | Sixth service | Agentic Developer Relations, content from agenticairelations.com; replaces Retainer-Based Services |
| 4 | Blog permalinks | Flat `/blog/<slug>/` |
| 5 | Training nav item | Ships at launch in the "coming soon" state |
| 6 | CV download | `docs/Christopher_Woodruff_Executive_Resume.pdf`, copied into `public/` at build |
| 7 | Book covers | `docs/Book Cover.png` as the placeholder for every book until real covers are supplied |

Still open, low stakes: which outlet logos appear in the home logo strip (default: .NET Rocks!, JetBrains, InfoWorld, TechBullion, The Azure Podcast, CDF).

---

## Appendix A: assets to fetch from WordPress

Press & Media items (title, URL, logo file under `https://woodruff.dev/wp-content/uploads/`):

| Title | Type | URL | Logo |
|---|---|---|---|
| C# Networking with Chris Woodruff (.NET Rocks!, 2025-05-22) | podcast | podtail.com/en/podcast/-net-rocks/c-networking-with-chris-woodruff-2025-05-22/ | `2025/05/dotnet-rocks.png` |
| From Idea to Mic: Writing Winning Talk Proposals (The Angular Plus Show) | podcast | podtail.com/en/podcast/the-angular-show/s9e3-from-idea-to-mic-writing-winning-talk-proposa/ | `2025/04/angularplusshow.png` |
| Discussion on C# Network Programming (The Azure Podcast) | podcast | youtube.com/watch?v=K4DP21OlktM | `2025/03/TheAzure_3000.png` |
| Enhancing ASP.NET Core Razor Pages with HTMX (JetBrains) | video | youtube.com/watch?v=iHnAULyXGwM | `2025/03/jetbrains.png` |
| Rust-ifying Your C# Codebase (CDF) | video | youtube.com/watch?v=si_U3Umqtm8 | `2025/03/CDF.png` |
| How AI is transforming IDEs into intelligent development assistants (InfoWorld) | article | infoworld.com/article/3849532/... | `2025/04/infoworld_logo.jpeg` |
| Architecting Scalable Cloud Solutions for the Modern Enterprise (TechBullion) | article | techbullion.com/chris-woody-woodruff-architecting-scalable-cloud-solutions-for-the-modern-enterprise/ | `2025/08/TechBullion-Transparent-Logo.png` |
| Kill the Bloat (Simplicity-First) | article | simplicity-first.dev/kill-the-bloat/ | `2025/03/simplicityfirst.png` |
| Seizing Opportunities Through Simplicity | article | simplicity-first.dev/seizing-opportunities-through-simplicity/ | same |
| Unlocking Business Growth Through Simplicity | article | simplicity-first.dev/unlocking-business-growth-through-simplicity/ | same |
| Aligning the Simplicity-First Initiative with Green Software Principles | article | simplicity-first.dev/aligning-the-simplicity-first-initiative-with-green-software-principles/ | same |

The WordPress page lists the CDF talk twice (one with a timestamp); import it once.

Other assets:

- Microsoft MVP 2025 badge: `2025/08/2025-microsoft-most-valuable-professional-mvp-150x150.png` (ask for the full-size original)
- JetBrains JBCC badge: `2025/03/jbcc-badge-150x150.png` (same)
- Woody cartoon mark (current WordPress logo and favicon): `2025/08/WoodyBaldCartoon-transparent-150.png`
- Portrait photo used on the current site: `2026/02/Chandler_2025Feb17_0002.jpeg`
- Testimonial photos for Natalie Greenwood and Ted Neward (not in the repo; the ASP.NET README expected `testimonial-natalie.jpg` and `testimonial-ted.jpg`)
- Real book covers, later, to replace the placeholder: ASP.NET Core Reimagined with htmx, Beyond Boundaries, Developer Relations Activity Patterns

## Appendix B: front matter templates for `incoming/`

`incoming/blog/README.md`

```md
---
title: "Post title"
date: 2026-10-04
categories: ["patterns"]
tags: ["dotnet", "csharp"]
---

Body in Markdown. The featured image is <same-name>.png next to this file.
Other images: put them in this folder and reference them as images/<name>.png.
```

`incoming/media/README.md`

```md
---
title: "Episode or article title"
url: https://example.com/the-item
image: outlet-logo.png
outlet: Outlet name
type: podcast | video | article | talk
date: 2026-10-04
description: "One sentence."
featured: false
---
```
