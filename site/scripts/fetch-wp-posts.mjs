// Pulls blog posts that exist on the WordPress site but not in this repo, and
// drops them into incoming/blog/ in the shape the import-blog script expects
// (Markdown + featured image with the same base name + inline images).
//
//   node scripts/fetch-wp-posts.mjs            # fetch missing posts into incoming/blog
//   node scripts/fetch-wp-posts.mjs --dry-run  # list what would be fetched
//   node scripts/fetch-wp-posts.mjs --slug a-post-slug   # fetch one specific post
//
// Then run `node scripts/import-blog.mjs`. Useful until WordPress is retired.

import { existsSync, mkdirSync, readdirSync, statSync, writeFileSync } from 'node:fs';
import { basename, extname, join, resolve } from 'node:path';
import TurndownService from 'turndown';
import { incomingRoot, isoDay, siteRoot, slugify, yamlList, yamlValue } from './lib/import-utils.mjs';

const WP = 'https://woodruff.dev';
const UA = 'Mozilla/5.0 (compatible; woodruff-dev-sync/1.0; +https://github.com/cwoodruff/woodruff-dev)';
const INCOMING = resolve(incomingRoot, 'blog');
const BLOG = resolve(siteRoot, 'src/content/blog');

const args = process.argv.slice(2);
const dryRun = args.includes('--dry-run');
const onlySlug = args.includes('--slug') ? args[args.indexOf('--slug') + 1] : undefined;

function repoSlugs() {
  const set = new Set();
  if (!existsSync(BLOG)) return set;
  for (const y of readdirSync(BLOG)) {
    const yd = join(BLOG, y);
    if (!statSync(yd).isDirectory()) continue;
    for (const m of readdirSync(yd)) {
      const md = join(yd, m);
      if (!statSync(md).isDirectory()) continue;
      for (const s of readdirSync(md)) if (existsSync(join(md, s, 'index.md'))) set.add(s);
    }
  }
  return set;
}

async function getJson(url) {
  const res = await fetch(url, { headers: { 'User-Agent': UA, Accept: 'application/json' } });
  if (!res.ok) throw new Error(`${res.status} ${url}`);
  return { data: await res.json(), headers: res.headers };
}

async function allPosts() {
  const fields = 'id,slug,date,title,excerpt,content,_links';
  const out = [];
  let page = 1;
  let pages = 1;
  do {
    const { data, headers } = await getJson(`${WP}/wp-json/wp/v2/posts?per_page=100&page=${page}&_embed=wp:featuredmedia,wp:term&_fields=${fields},_embedded`);
    pages = Number(headers.get('x-wp-totalpages') ?? 1);
    out.push(...data);
    page += 1;
  } while (page <= pages);
  return out;
}

function decode(html) {
  return html
    .replace(/&#(\d+);/g, (_, n) => String.fromCodePoint(Number(n)))
    .replace(/&#x([0-9a-f]+);/gi, (_, n) => String.fromCodePoint(parseInt(n, 16)))
    .replace(/&amp;/g, '&')
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&quot;/g, '"')
    .replace(/&#8217;|&rsquo;/g, '’')
    .replace(/&#8216;|&lsquo;/g, '‘')
    .replace(/&#8220;|&ldquo;/g, '“')
    .replace(/&#8221;|&rdquo;/g, '”')
    .replace(/&nbsp;/g, ' ')
    .replace(/&hellip;/g, '…')
    .replace(/&ndash;/g, '–')
    .replace(/&mdash;/g, '—');
}

function stripTags(html) {
  return decode(html.replace(/<[^>]+>/g, ' ')).replace(/\s+/g, ' ').trim();
}

const td = new TurndownService({ headingStyle: 'atx', bulletListMarker: '-', codeBlockStyle: 'fenced', emDelimiter: '*', hr: '---' });
// Keep <figure> captions, drop WordPress wrappers.
td.addRule('figure', {
  filter: 'figure',
  replacement: (content, node) => {
    const img = node.querySelector('img');
    const cap = node.querySelector('figcaption');
    const link = node.querySelector('a[href]');
    if (!img) return content;
    const alt = (img.getAttribute('alt') ?? '').replace(/[\[\]]/g, '');
    const src = img.getAttribute('src') ?? '';
    const md = `![${alt}](${src})`;
    const linked = link && !link.getAttribute('href')?.match(/\.(png|jpe?g|webp|gif)$/i) ? `[${md}](${link.getAttribute('href')})` : md;
    return `\n\n${linked}${cap ? `\n*${cap.textContent.trim()}*` : ''}\n\n`;
  },
});
// Code blocks. Two shapes on woodruff.dev:
//   <pre class="wp-block-code"><code class="language-x">…</code></pre>   (core block)
//   <pre class="EnlighterJSRAW" data-enlighter-language="csharp">…</pre>  (EnlighterJS plugin)
const LANG_ALIASES = { generic: '', raw: '', cs: 'csharp', 'c#': 'csharp', shell: 'bash', dotnet: 'csharp', xml: 'xml', msbuild: 'xml' };
td.addRule('wpcode', {
  filter: (node) => node.nodeName === 'PRE',
  replacement: (_content, node) => {
    const code = node.querySelector('code') ?? node;
    const cls = `${code.getAttribute('class') ?? ''} ${node.getAttribute('class') ?? ''}`;
    let lang = node.getAttribute('data-enlighter-language') ?? cls.match(/(?:language|lang)-([\w#+-]+)/)?.[1] ?? '';
    lang = LANG_ALIASES[lang.toLowerCase()] ?? lang.toLowerCase();
    const text = code.textContent.replace(/^\n+|\n+$/g, '');
    return `\n\n\`\`\`${lang}\n${text}\n\`\`\`\n\n`;
  },
});

async function download(url, dest) {
  const res = await fetch(url, { headers: { 'User-Agent': UA } });
  if (!res.ok) throw new Error(`${res.status} ${url}`);
  writeFileSync(dest, Buffer.from(await res.arrayBuffer()));
}

function uniqueName(dir, name, used) {
  let candidate = name;
  let n = 2;
  while (used.has(candidate) || existsSync(join(dir, candidate))) {
    const ext = extname(name);
    candidate = `${name.slice(0, -ext.length)}-${n}${ext}`;
    n += 1;
  }
  used.add(candidate);
  return candidate;
}

const have = repoSlugs();
const posts = await allPosts();
const missing = posts.filter((p) => (onlySlug ? p.slug === onlySlug : !have.has(p.slug)));
console.log(`WordPress: ${posts.length} posts. Repo: ${have.size}. To fetch: ${missing.length}.`);
if (dryRun) {
  for (const p of missing) console.log(`- ${p.date.slice(0, 10)}  ${p.slug}`);
  process.exit(0);
}

mkdirSync(INCOMING, { recursive: true });
const used = new Set();

for (const p of missing) {
  const slug = p.slug;
  const title = decode(p.title.rendered);
  const date = new Date(p.date);
  const terms = p._embedded?.['wp:term'] ?? [];
  const categories = (terms[0] ?? []).map((t) => t.slug).filter((s) => s !== 'uncategorized');
  const tags = (terms[1] ?? []).map((t) => t.slug);
  const featured = p._embedded?.['wp:featuredmedia']?.[0]?.source_url;
  let description = stripTags(p.excerpt?.rendered ?? '').replace(/\s*\[…\]\s*$|\s*…\s*$/, '');
  if (description.length > 200) {
    const cut = description.slice(0, 200);
    description = cut.slice(0, cut.lastIndexOf(' ')).replace(/[,;:]$/, '') + '…';
  }

  let md = td.turndown(p.content.rendered).replace(/\n{3,}/g, '\n\n').trim();

  // Inline images hosted on WordPress: download next to the post and reference by file name.
  const refs = [...md.matchAll(/!\[[^\]]*\]\((https?:\/\/(?:www\.)?woodruff\.dev\/wp-content\/uploads\/[^)\s]+)\)/g)].map((m) => m[1]);
  for (const url of new Set(refs)) {
    const name = uniqueName(INCOMING, basename(new URL(url).pathname), used);
    await download(url, join(INCOMING, name));
    md = md.split(url).join(name);
  }

  // Featured image with the same base name as the Markdown file.
  const base = slugify(slug);
  if (featured) {
    const ext = extname(new URL(featured).pathname) || '.png';
    await download(featured, join(INCOMING, `${base}${ext}`));
  } else {
    console.warn(`  ! ${slug}: no featured image on WordPress; add ${base}.png by hand before importing`);
  }

  const fm = ['---', `title: ${yamlValue(title)}`, `date: ${isoDay(date)}`, `slug: ${yamlValue(slug)}`];
  if (description) fm.push(`description: ${yamlValue(description)}`);
  fm.push('categories:', yamlList(categories.length ? categories : ['blog']));
  if (tags.length) fm.push('tags:', yamlList(tags));
  fm.push('---', '');

  writeFileSync(join(INCOMING, `${base}.md`), fm.join('\n') + md + '\n');
  console.log(`+ ${isoDay(date)}  ${slug}${featured ? '' : '  (no featured image)'}`);
}

console.log(`\nWrote ${missing.length} post(s) to incoming/blog/. Next: node scripts/import-blog.mjs`);
