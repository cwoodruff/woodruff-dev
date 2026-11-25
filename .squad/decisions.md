# Squad Decisions

## Active Decisions

### 2026-11-25T19-34-17: APPROVE: GA4 (G-ZGNC3D2CKX) in Base.astro, production-only
**By:** simon
**What:** APPROVE: GA4 (G-ZGNC3D2CKX) in Base.astro, production-only
**References:** site/src/layouts/Base.astro
**Why:** Verdict: APPROVE. Verified via npm run build: all 358 Base.astro pages (home, 404, blog index, posts, about) carry exactly one gtag.js loader and one gtag('config') call inside <head> with G-ZGNC3D2CKX; ID is defined once and passed via is:inline define:vars, which wraps the script in an IIFE. gtag stays global because it is only called inside that scope. Nothing GA-related is bundled into _astro. The old G-346496664 is replaced and appears nowhere in dist. Guarded by import.meta.env.PROD, so astro dev emits nothing. No ClientRouter/view transitions, so full page loads give correct page_views. No CSP meta. The sample gtag snippet in the REST code-on-demand post renders escaped and is inert. Pages without tracking: /feed/ stub and 269 WordPress meta-refresh redirect stubs. These are 0-second redirects to tracked pages, so the gap does not matter. Advisory only: the gtag function lives inside the IIFE, so later page code cannot call window.gtag for events. Events are out of scope today. The .gitignore and CLAUDE.md diffs come from the Squad install and were already present before this change.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
