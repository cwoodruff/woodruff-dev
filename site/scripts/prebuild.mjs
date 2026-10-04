// Runs automatically before `npm run build` (npm "prebuild" lifecycle).
// Copies build-time inputs that live outside the site folder into public/.
import { copyFileSync, existsSync, mkdirSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(here, '..', '..');
const publicDir = resolve(here, '..', 'public');

const copies = [
  {
    from: resolve(repoRoot, 'docs', 'Christopher_Woodruff_Executive_Resume.pdf'),
    to: resolve(publicDir, 'Christopher_Woodruff_Executive_Resume.pdf'),
    required: false,
  },
];

mkdirSync(publicDir, { recursive: true });

for (const { from, to, required } of copies) {
  if (!existsSync(from)) {
    const msg = `prebuild: missing ${from}`;
    if (required) throw new Error(msg);
    console.warn(`${msg} (skipped)`);
    continue;
  }
  copyFileSync(from, to);
  console.log(`prebuild: copied ${from} -> ${to}`);
}
