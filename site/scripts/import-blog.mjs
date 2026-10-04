// Imports blog posts dropped into incoming/blog/ into src/content/blog/YYYY/MM/<slug>/.
//
//   node scripts/import-blog.mjs            # from site/
//
// Input: incoming/blog/<name>.md with optional YAML front matter, plus the
// featured image <name>.png (or .jpg/.jpeg/.webp) next to it. Any other image
// the post references by file name (or images/<file>) is moved along with it.
//
// Output: src/content/blog/YYYY/MM/<slug>/index.md and images/, front matter
// normalised to the blog collection schema. Processed files are deleted from
// incoming/. Re-importing the same slug overwrites the existing post.

import { existsSync, mkdirSync, readFileSync, readdirSync, renameSync, statSync, unlinkSync, writeFileSync } from 'node:fs';
import { basename, dirname, extname, join, resolve } from 'node:path';
import matter from 'gray-matter';
import {
  fail,
  findSibling,
  incomingRoot,
  isoDay,
  listFiles,
  localImageRefs,
  siteRoot,
  slugify,
  summary,
  toDate,
  yamlList,
  yamlValue,
} from './lib/import-utils.mjs';

const INCOMING = resolve(incomingRoot, 'blog');
const DEST = resolve(siteRoot, 'src/content/blog');

function existingPostDirs() {
  const map = new Map(); // slug -> absolute dir
  if (!existsSync(DEST)) return map;
  for (const year of readdirSync(DEST)) {
    const yDir = join(DEST, year);
    if (!statSync(yDir).isDirectory()) continue;
    for (const month of readdirSync(yDir)) {
      const mDir = join(yDir, month);
      if (!statSync(mDir).isDirectory()) continue;
      for (const slug of readdirSync(mDir)) {
        if (existsSync(join(mDir, slug, 'index.md'))) map.set(slug, join(mDir, slug));
      }
    }
  }
  return map;
}

function titleFromBody(body) {
  const m = body.match(/^#\s+(.+?)\s*$/m);
  return m?.[1];
}

function titleFromFilename(name) {
  return name
    .replace(/[-_]+/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
    .replace(/\b\w/g, (c) => c.toUpperCase());
}

const files = listFiles(INCOMING, ['md']).filter((f) => f.toLowerCase() !== 'readme.md');
if (files.length === 0) {
  console.log('import-blog: nothing in incoming/blog/');
  process.exit(0);
}

const existing = existingPostDirs();
const report = ['## Blog import', ''];

for (const file of files) {
  const base = basename(file, '.md');
  const raw = readFileSync(join(INCOMING, file), 'utf8');
  const { data: fm, content } = matter(raw);
  let body = content.replace(/^\s+/, '');

  const title = fm.title ?? titleFromBody(body) ?? titleFromFilename(base);
  if (fm.title === undefined) {
    // Drop the H1 when it supplied the title; the layout renders the title.
    body = body.replace(/^#\s+.+?\s*\n+/, '');
  }
  const date = toDate(fm.date) ?? new Date();
  const slug = fm.slug ? slugify(fm.slug) : slugify(title);
  if (!slug) fail(`${file}: could not derive a slug from the title "${title}"`);

  const cover = fm.coverImage ? basename(String(fm.coverImage)) : findSibling(INCOMING, base);
  if (!cover || !existsSync(join(INCOMING, cover))) {
    fail(`${file}: featured image not found. Expected ${base}.png (or .jpg/.jpeg/.webp) next to the Markdown file.`);
  }

  const year = String(date.getUTCFullYear());
  const month = String(date.getUTCMonth() + 1).padStart(2, '0');
  const postDir = join(DEST, year, month, slug);
  const prior = existing.get(slug);
  if (prior && resolve(prior) !== resolve(postDir)) {
    fail(`${file}: slug "${slug}" already exists at ${prior.slice(DEST.length + 1)}. Set a different "slug:" in the front matter.`);
  }
  const imagesDir = join(postDir, 'images');
  mkdirSync(imagesDir, { recursive: true });

  // Move the featured image.
  renameSync(join(INCOMING, cover), join(imagesDir, cover));

  // Move other referenced images and rewrite their paths.
  const moved = [cover];
  for (const ref of localImageRefs(body)) {
    const name = basename(ref);
    const src = join(INCOMING, name);
    if (!existsSync(src)) continue;
    renameSync(src, join(imagesDir, name));
    moved.push(name);
    body = body.split(ref).join(`images/${name}`);
  }

  const categories = Array.isArray(fm.categories) ? fm.categories : fm.categories ? [fm.categories] : ['blog'];
  const tags = Array.isArray(fm.tags) ? fm.tags : fm.tags ? [fm.tags] : [];

  const lines = ['---', `title: ${yamlValue(title)}`, `date: ${isoDay(date)}`];
  if (fm.updated) lines.push(`updated: ${isoDay(toDate(fm.updated) ?? new Date())}`);
  if (fm.description) lines.push(`description: ${yamlValue(fm.description)}`);
  lines.push('categories:', yamlList(categories.map(String)));
  if (tags.length) lines.push('tags:', yamlList(tags.map(String)));
  lines.push(`coverImage: ${yamlValue(`./images/${cover}`)}`);
  if (fm.draft === true) lines.push('draft: true');
  lines.push('---', '');

  writeFileSync(join(postDir, 'index.md'), lines.join('\n') + body.trimEnd() + '\n');
  unlinkSync(join(INCOMING, file));
  existing.set(slug, postDir);

  report.push(`- ${prior ? 'Updated' : 'Imported'} **${title}** → \`src/content/blog/${year}/${month}/${slug}/\` (${moved.length} image${moved.length === 1 ? '' : 's'})`);
}

// Anything left behind that is not the README is a mistake worth flagging.
const leftovers = listFiles(INCOMING).filter((f) => f.toLowerCase() !== 'readme.md');
if (leftovers.length) {
  report.push('', `⚠️ Left in incoming/blog/ (no matching post referenced them): ${leftovers.map((f) => `\`${f}\``).join(', ')}`);
}

summary(report);
