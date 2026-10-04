// One-off migration from the ASP.NET Core solution (woodruffdev.web/) into
// Astro content collections. Safe to re-run: it overwrites generated files.
//
//   node scripts/migrate-aspnet-content.mjs
//
// 1. Blog posts. The tracked folder woodruffdev.web/BlogPosts was moved with
//    `git mv` to src/content/blog (same blobs, no repo growth). This step
//    normalises the posts in place:
//    - coverImage "<file>" is rewritten to "./images/<file>" so Astro's image() helper resolves it
//    - asserts every slug is unique (the public URL is /blog/<slug>/)
//    - asserts every coverImage exists on disk
// 2. Pages/Services/*.cshtml  ->  src/content/services/<slug>.md (HTML body -> Markdown)
//    DevRel.cshtml is skipped: agentic-developer-relations.md is hand-written.

import { existsSync, mkdirSync, readFileSync, readdirSync, statSync, writeFileSync } from 'node:fs';
import { basename, dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import TurndownService from 'turndown';

const here = dirname(fileURLToPath(import.meta.url));
const siteRoot = resolve(here, '..');
const repoRoot = resolve(siteRoot, '..');
const aspnet = resolve(repoRoot, 'woodruffdev.web');

// ---------------------------------------------------------------- blog
const BLOG_DIR = resolve(siteRoot, 'src/content/blog');

function walk(dir, out = []) {
  for (const name of readdirSync(dir)) {
    const p = join(dir, name);
    if (statSync(p).isDirectory()) walk(p, out);
    else if (name === 'index.md') out.push(p);
  }
  return out;
}

function normaliseBlog() {
  const files = walk(BLOG_DIR);
  const slugs = new Map();
  const problems = [];
  let rewritten = 0;

  for (const file of files) {
    const postDir = dirname(file);
    const slug = basename(postDir);
    const rel = postDir.slice(BLOG_DIR.length + 1); // YYYY/MM/slug

    if (slugs.has(slug)) problems.push(`duplicate slug "${slug}": ${slugs.get(slug)} and ${rel}`);
    slugs.set(slug, rel);

    const md = readFileSync(file, 'utf8');
    const fm = md.match(/^---\r?\n([\s\S]*?)\r?\n---/);
    if (!fm) {
      problems.push(`no front matter: ${rel}`);
      continue;
    }

    const cover = fm[1].match(/^coverImage:\s*"?([^"\r\n]+?)"?\s*$/m);
    if (!cover) continue;

    let name = cover[1].trim();
    if (name.startsWith('./images/')) name = name.slice('./images/'.length);
    if (!existsSync(join(postDir, 'images', name))) {
      problems.push(`missing cover image ${name}: ${rel}`);
      continue;
    }
    const line = `coverImage: "./images/${name}"`;
    if (cover[0] !== line) {
      writeFileSync(file, md.replace(cover[0], line));
      rewritten += 1;
    }
  }

  if (problems.length) {
    console.error('Blog problems:\n  ' + problems.join('\n  '));
    process.exit(1);
  }
  console.log(`blog: ${files.length} posts, ${slugs.size} unique slugs, ${rewritten} coverImage lines rewritten`);
}

// ---------------------------------------------------------------- services
const SERVICES = [
  {
    file: 'FractionalArchitect.cshtml',
    slug: 'fractional-architect',
    order: 1,
    navTitle: 'Fractional Architect',
    short:
      'Senior architecture leadership, embedded in your team 4 to 16 hours per week. Decisions with receipts, engineers who grow, and a 90-day technical roadmap you can share with the board.',
  },
  {
    file: 'ExpertWitness.cshtml',
    slug: 'expert-witness',
    order: 2,
    navTitle: 'Expert Witness',
    short:
      'Defensible technical analysis for litigation: contract, IP, negligence, and M&A disputes. Reports built for Daubert, testimony that holds up under cross-examination.',
  },
  {
    file: 'MicroConsulting.cshtml',
    slug: 'micro-consulting',
    order: 3,
    navTitle: 'Micro-Consulting',
    short:
      'A 90-minute deep-dive plus a written decision brief, for the single sharp question your team needs answered this week, not next quarter.',
  },
  {
    file: 'ProjectBased.cshtml',
    slug: 'project-based',
    order: 4,
    navTitle: 'Project-Based Contracts',
    short:
      'Scoped, fixed-fee engagements for architecture assessments, modernization plans, cloud migrations, and technical due diligence. Owned end-to-end, with a clean handoff.',
  },
  {
    file: 'Advisory.cshtml',
    slug: 'advisory',
    order: 6,
    navTitle: 'Advisory & Board Roles',
    short:
      'An independent technical voice for boards, founders, and PE operators: pressure-testing roadmaps, surfacing risk, and making trade-offs visible before the vote.',
  },
];

const td = new TurndownService({ headingStyle: 'atx', bulletListMarker: '-', codeBlockStyle: 'fenced', emDelimiter: '*' });

function yamlString(s) {
  return JSON.stringify(s);
}

function textOf(html) {
  return td.turndown(html).replace(/\s+/g, ' ').trim();
}

function migrateServices() {
  if (!existsSync(resolve(aspnet, 'Pages/Services'))) {
    console.log('services: ASP.NET source not present, skipping');
    return;
  }
  const dst = resolve(siteRoot, 'src/content/services');
  mkdirSync(dst, { recursive: true });

  for (const s of SERVICES) {
    const html = readFileSync(resolve(aspnet, 'Pages/Services', s.file), 'utf8').replace(/@@/g, '@');

    const title = textOf(html.match(/<h1>([\s\S]*?)<\/h1>/)[1]);
    const tagline = textOf(html.match(/<div class="page-header">[\s\S]*?<p>([\s\S]*?)<\/p>/)[1]);

    const bodyStart = html.indexOf('<div class="service-detail');
    const ctaStart = html.indexOf('<div class="service-cta">');
    const navStart = html.indexOf('<div class="service-nav">');
    if (bodyStart < 0 || ctaStart < 0 || navStart < 0) throw new Error(`unexpected layout in ${s.file}`);

    const bodyHtml = html.slice(html.indexOf('>', bodyStart) + 1, ctaStart);
    const ctaHtml = html.slice(ctaStart, navStart);
    const ctaHeading = textOf(ctaHtml.match(/<h3>([\s\S]*?)<\/h3>/)[1]);
    const ctaText = textOf(ctaHtml.match(/<p>([\s\S]*?)<\/p>/)[1]);
    const ctaButton = textOf(ctaHtml.match(/<a [^>]*class="btn[^"]*"[^>]*>([\s\S]*?)<\/a>/)[1]).replace(/\s*→\s*$/, '');

    // Turndown indents list items with three spaces after the marker; tidy that.
    const body = td
      .turndown(bodyHtml)
      .replace(/^-\s{3}/gm, '- ')
      .replace(/\n{3,}/g, '\n\n')
      .trim();

    const fm = [
      '---',
      `title: ${yamlString(title)}`,
      `navTitle: ${yamlString(s.navTitle)}`,
      `tagline: ${yamlString(tagline)}`,
      `short: ${yamlString(s.short)}`,
      `order: ${s.order}`,
      `ctaHeading: ${yamlString(ctaHeading)}`,
      `ctaText: ${yamlString(ctaText)}`,
      `ctaButton: ${yamlString(ctaButton)}`,
      '---',
      '',
    ].join('\n');

    writeFileSync(join(dst, `${s.slug}.md`), fm + body + '\n');
    console.log(`services: wrote ${s.slug}.md`);
  }
}

// ---------------------------------------------------------------- run
normaliseBlog();
migrateServices();
