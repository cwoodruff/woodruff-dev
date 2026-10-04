# Cutover runbook: WordPress → Astro on GitHub Pages

Follow in order. Each step says what to check before moving on. Total active time is under an hour; DNS and the TLS certificate add waiting.

## 0. Before the day

- [ ] PRs #4, #5, #6, #7, and the Phase 4 PR are merged to `main` and the preview at https://cwoodruff.github.io/woodruff-dev/ has been reviewed.
- [ ] Web3Forms: create an access key at https://web3forms.com with `chris@woodruff.dev` as the destination, confirm the verification email, and replace `REPLACE_WITH_WEB3FORMS_ACCESS_KEY` in `site/src/lib/site.ts`. Send one test message from the preview site.
- [ ] Confirm the Ted Neward testimonial wording (`site/src/content/testimonials/ted-neward.md`) or remove the file.
- [ ] Optional: real book covers into `site/src/content/portfolio/images/` and update the three `image:` lines.
- [ ] Pull anything published on WordPress since the last sync:
  ```bash
  cd site
  node scripts/fetch-wp-posts.mjs --dry-run   # lists posts missing from the repo
  node scripts/fetch-wp-posts.mjs             # writes them to incoming/blog/
  node scripts/import-blog.mjs                # moves them into src/content/blog/
  git add -A incoming site/src/content && git commit -m "content: sync from WordPress" && git push
  ```
  (Or just push the `incoming/` files and let the Action do the import.)
- [ ] Lower the TTL on the current `woodruff.dev` DNS records to 300 seconds a day ahead, so the switch propagates quickly.

## 1. Merge the cutover PR

The cutover PR (branch `claude/astro-site-cutover`) does four things in one commit:

1. Adds `site/public/CNAME` containing `woodruff.dev`.
2. Sets `PREVIEW: 'false'` in `.github/workflows/deploy.yml`, so the build uses `site: https://woodruff.dev` and no base path.
3. Deletes the retired ASP.NET solution (`woodruffdev.web/`, `woodruffdev.sln`).
4. Rewrites the root `README.md` for the Astro site.

Merging it triggers a deploy. When the run finishes, GitHub Pages picks up the custom domain from `CNAME`; the Pages settings page will show `woodruff.dev` with a DNS check that fails until step 2 is done. The github.io URL starts redirecting to woodruff.dev immediately, which still points at WordPress, so nothing public changes yet.

Check: `gh run list --workflow=deploy.yml --limit 1` shows success.

## 2. DNS

At the registrar / DNS host for `woodruff.dev`:

| Type | Name | Value |
|---|---|---|
| A | `@` | `185.199.108.153` |
| A | `@` | `185.199.109.153` |
| A | `@` | `185.199.110.153` |
| A | `@` | `185.199.111.153` |
| AAAA | `@` | `2606:50c0:8000::153` |
| AAAA | `@` | `2606:50c0:8001::153` |
| AAAA | `@` | `2606:50c0:8002::153` |
| AAAA | `@` | `2606:50c0:8003::153` |
| CNAME | `www` | `cwoodruff.github.io` |

Remove the old A/AAAA/CNAME records that point at the WordPress host. Leave MX and TXT records alone.

Check: `dig +short woodruff.dev A` returns the four GitHub addresses.

## 3. HTTPS

In the repository: Settings → Pages. Once the DNS check passes (minutes to an hour), tick **Enforce HTTPS**. GitHub provisions the certificate; this can take up to an hour. Until then the site serves over HTTP.

Check:

```bash
curl -sI https://woodruff.dev/ | head -3                       # 200
curl -sI https://www.woodruff.dev/ | grep -i location          # 301 to https://woodruff.dev/
curl -sI https://woodruff.dev/the-n1-query-problem-in-ef-core/ | head -1   # 200 (meta-refresh stub)
curl -s https://woodruff.dev/rss.xml | head -c 200             # RSS XML
curl -sI https://woodruff.dev/feed/ | head -1                  # 200 stub that refreshes to /rss.xml
```

## 4. After the switch

- [ ] Update anything that consumes the WordPress feed: the GitHub profile README automation (it read `https://woodruff.dev/feed/`; point it at `https://woodruff.dev/rss.xml`), newsletter tools, Feedly, etc.
- [ ] Search Console: submit `https://woodruff.dev/sitemap-index.xml`.
- [ ] Spot-check a handful of old inbound links from LinkedIn and the podcast show notes; each should land on `/blog/<slug>/` via the stub.
- [ ] Keep the WordPress install reachable at a temporary hostname for a week in case a page was missed, then cancel the hosting.
- [ ] Delete the `images/` masters from the working tree if they are no longer needed locally (they were never committed).

## Rollback

Point the DNS A/AAAA records back at the WordPress host. Nothing on GitHub needs to change; the custom domain can stay configured.

## URL map

| WordPress | Astro |
|---|---|
| `/<post-slug>/` | `/blog/<post-slug>/` (stub generated for every post) |
| `/category/<cat>/` | `/blog/category/<cat>/` |
| `/feed/` | `/rss.xml` |
| `/fractional-architect/` | `/services/fractional-architect/` |
| `/expert-witness/` | `/services/expert-witness/` |
| `/micro-consulting/` | `/services/micro-consulting/` |
| `/project-based-contracts/` | `/services/project-based/` |
| `/retainer-based-services/` | `/services/agentic-developer-relations/` |
| `/advisory-board-roles/` | `/services/advisory/` |
| `/about/`, `/contact/`, `/press-media/` | unchanged |
