# Simon — History

## Core Context

- **Role:** Test Engineer
- **Joined:** 2026-11-25

## Learnings

<!-- Append new learnings below -->

- 2026-11-25: Reviewed GA4 in Base.astro (APPROVE). Learning: Astro `define:vars` turns an inline script into an IIFE, so `function gtag` is no longer global. That is fine for config-only GA, but adding event tracking later needs `window.gtag = gtag`. Verify GA coverage by scanning dist for pages that have `<html` but no tag; on this site those are only redirect stubs.
