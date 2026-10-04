// Shared helpers for the incoming/ content importers.
import { appendFileSync, existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
export const siteRoot = resolve(here, '..', '..');
export const repoRoot = resolve(siteRoot, '..');
export const incomingRoot = resolve(repoRoot, 'incoming');

export const IMAGE_EXTS = ['png', 'jpg', 'jpeg', 'webp', 'gif', 'svg'];

export function slugify(text) {
  return String(text)
    .normalize('NFKD')
    .replace(/[̀-ͯ]/g, '')
    .replace(/[’'"]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 120);
}

export function toDate(value) {
  if (value instanceof Date && !Number.isNaN(value.valueOf())) return value;
  if (typeof value === 'string' || typeof value === 'number') {
    const d = new Date(value);
    if (!Number.isNaN(d.valueOf())) return d;
  }
  return undefined;
}

export function isoDay(d) {
  return d.toISOString().slice(0, 10);
}

/** YAML-safe scalar: strings are JSON-quoted (valid YAML double-quoted strings). */
export function yamlValue(v) {
  if (v instanceof Date) return isoDay(v);
  if (typeof v === 'boolean' || typeof v === 'number') return String(v);
  return JSON.stringify(String(v));
}

export function yamlList(items) {
  return items.map((i) => `  - ${yamlValue(i)}`).join('\n');
}

export function listFiles(dir, exts) {
  if (!existsSync(dir)) return [];
  return readdirSync(dir)
    .filter((f) => !f.startsWith('.') && statSync(join(dir, f)).isFile())
    .filter((f) => !exts || exts.includes(f.split('.').pop().toLowerCase()))
    .sort();
}

/** Find <basename>.<any image ext> in dir. */
export function findSibling(dir, basename) {
  for (const ext of IMAGE_EXTS) {
    for (const candidate of [`${basename}.${ext}`, `${basename}.${ext.toUpperCase()}`]) {
      if (existsSync(join(dir, candidate))) return candidate;
    }
  }
  return undefined;
}

/** Image references in Markdown/HTML that are plain file names or images/<name>. */
export function localImageRefs(body) {
  const refs = new Set();
  const md = /!\[[^\]]*\]\(\s*<?([^)\s>]+)>?(?:\s+"[^"]*")?\s*\)/g;
  const html = /<img[^>]+src=["']([^"']+)["']/gi;
  for (const re of [md, html]) {
    let m;
    while ((m = re.exec(body))) {
      const p = m[1];
      if (/^(https?:)?\/\//.test(p) || p.startsWith('/') || p.startsWith('data:')) continue;
      refs.add(p);
    }
  }
  return [...refs];
}

export function summary(lines) {
  const text = lines.join('\n') + '\n';
  console.log(text);
  if (process.env.GITHUB_STEP_SUMMARY) appendFileSync(process.env.GITHUB_STEP_SUMMARY, text);
}

export function fail(message) {
  console.error(`\n✖ ${message}\n`);
  if (process.env.GITHUB_STEP_SUMMARY) appendFileSync(process.env.GITHUB_STEP_SUMMARY, `\n❌ ${message}\n`);
  process.exit(1);
}

export function filesEqual(a, b) {
  if (!existsSync(a) || !existsSync(b)) return false;
  const x = readFileSync(a);
  const y = readFileSync(b);
  return x.length === y.length && x.equals(y);
}
