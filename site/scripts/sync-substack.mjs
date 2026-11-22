// Refreshes src/data/substack-posts.json, the snapshot the essays collection
// falls back to when Substack blocks or times out the build-time fetch.
//
//   npm run sync:essays

import { writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { siteRoot } from './lib/import-utils.mjs';

const API = 'https://simplicityfirstphilosophy.substack.com/api/v1/posts?limit=50';
const UA = 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0 Safari/537.36';
const OUT = resolve(siteRoot, 'src/data/substack-posts.json');
const FIELDS = ['slug', 'title', 'subtitle', 'post_date', 'canonical_url', 'wordcount', 'type'];

const res = await fetch(API, { headers: { 'User-Agent': UA, Accept: 'application/json' } });
if (!res.ok) throw new Error(`Substack returned HTTP ${res.status}`);
const body = await res.json();
const posts = Array.isArray(body) ? body : body.posts;
if (!Array.isArray(posts)) throw new Error('Unexpected response shape from Substack');

const snapshot = posts.map((p) => Object.fromEntries(FIELDS.map((k) => [k, p[k] ?? null])));
writeFileSync(OUT, `${JSON.stringify(snapshot, null, 1)}\n`);
console.log(`Wrote ${snapshot.length} posts to ${OUT}`);
if (posts.length >= 50) console.warn('Substack returned a full page of 50 posts; older essays may be missing.');
