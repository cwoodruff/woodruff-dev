// Imports Press & Media items dropped into incoming/media/ into src/content/media/.
//
//   node scripts/import-media.mjs            # from site/
//
// Input: incoming/media/<name>.md with front matter (title, url, image required;
// outlet, type, date, description, featured, order optional) and the logo image
// it names next to it. Output: src/content/media/<slug>.md plus the logo in
// src/content/media/images/. Processed files are deleted from incoming/.

import { existsSync, mkdirSync, readFileSync, renameSync, unlinkSync, writeFileSync } from 'node:fs';
import { basename, extname, join, resolve } from 'node:path';
import matter from 'gray-matter';
import { fail, filesEqual, incomingRoot, isoDay, listFiles, siteRoot, slugify, summary, toDate, yamlValue } from './lib/import-utils.mjs';

const INCOMING = resolve(incomingRoot, 'media');
const DEST = resolve(siteRoot, 'src/content/media');
const IMAGES = join(DEST, 'images');
const TYPES = ['podcast', 'video', 'article', 'talk'];

const files = listFiles(INCOMING, ['md']).filter((f) => f.toLowerCase() !== 'readme.md');
if (files.length === 0) {
  console.log('import-media: nothing in incoming/media/');
  process.exit(0);
}

mkdirSync(IMAGES, { recursive: true });
const report = ['## Press & Media import', ''];

for (const file of files) {
  const raw = readFileSync(join(INCOMING, file), 'utf8');
  const { data: fm, content } = matter(raw);

  for (const key of ['title', 'url', 'image']) {
    if (!fm[key]) fail(`${file}: front matter is missing required "${key}"`);
  }
  try {
    new URL(String(fm.url));
  } catch {
    fail(`${file}: "url" is not a valid absolute URL: ${fm.url}`);
  }
  const type = fm.type ? String(fm.type).toLowerCase() : 'article';
  if (!TYPES.includes(type)) fail(`${file}: "type" must be one of ${TYPES.join(', ')}`);

  const imageName = basename(String(fm.image));
  const imageSrc = join(INCOMING, imageName);
  if (!existsSync(imageSrc)) fail(`${file}: image "${imageName}" not found next to the Markdown file`);

  // Reuse an identical logo already in the collection; otherwise de-duplicate the name.
  let target = join(IMAGES, imageName);
  if (existsSync(target) && !filesEqual(imageSrc, target)) {
    const ext = extname(imageName);
    const stem = imageName.slice(0, -ext.length);
    let n = 2;
    while (existsSync(join(IMAGES, `${stem}-${n}${ext}`)) && !filesEqual(imageSrc, join(IMAGES, `${stem}-${n}${ext}`))) n += 1;
    target = join(IMAGES, `${stem}-${n}${ext}`);
  }
  if (filesEqual(imageSrc, target)) unlinkSync(imageSrc);
  else renameSync(imageSrc, target);

  const slug = fm.slug ? slugify(fm.slug) : slugify(fm.title);
  if (!slug) fail(`${file}: could not derive a slug from the title`);
  const dest = join(DEST, `${slug}.md`);
  const existed = existsSync(dest);

  const lines = ['---', `title: ${yamlValue(fm.title)}`, `url: ${yamlValue(fm.url)}`, `image: ${yamlValue(`./images/${basename(target)}`)}`];
  if (fm.outlet) lines.push(`outlet: ${yamlValue(fm.outlet)}`);
  lines.push(`type: ${type}`);
  const date = toDate(fm.date);
  if (date) lines.push(`date: ${isoDay(date)}`);
  if (fm.description) lines.push(`description: ${yamlValue(fm.description)}`);
  if (fm.featured === true) lines.push('featured: true');
  if (typeof fm.order === 'number') lines.push(`order: ${fm.order}`);
  lines.push('---', '');

  writeFileSync(dest, lines.join('\n') + content.trim() + (content.trim() ? '\n' : ''));
  unlinkSync(join(INCOMING, file));
  report.push(`- ${existed ? 'Updated' : 'Imported'} **${fm.title}** → \`src/content/media/${slug}.md\` (logo: \`${basename(target)}\`)`);
}

const leftovers = listFiles(INCOMING).filter((f) => f.toLowerCase() !== 'readme.md');
if (leftovers.length) {
  report.push('', `⚠️ Left in incoming/media/ (no item referenced them): ${leftovers.map((f) => `\`${f}\``).join(', ')}`);
}

summary(report);
