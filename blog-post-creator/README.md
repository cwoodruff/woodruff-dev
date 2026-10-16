# Blog Post Creator

An Avalonia desktop app (macOS, Windows, Linux) that publishes a post to woodruff.dev through the documented drop-folder process in `incoming/blog/README.md`, and reports on every step until the page is live.

## Run

```bash
cd blog-post-creator
dotnet run --project src/BlogPostCreator
```

Requires the .NET 10 SDK, `git`, and the GitHub CLI signed in (`gh auth login`). The app finds the woodruff-dev repo by walking up from where it runs; use **Change repo** if it can't.

## What it does

Compose the post (or **Open Markdown file…** to load one, front matter and referenced images included), pick the featured image, then **Publish post**:

1. **Check repository & tools.** git and gh are present, gh is signed in, the branch is `main`, and nothing is already waiting in `incoming/blog/`.
2. **Sync with origin/main.** Fetches and fast-forwards. Stops if local `main` has unpushed commits, so they never ride along.
3. **Validate the post.** The importer's rules, checked locally: slug normalized and unique across years (same month = update in place), featured image is `.png/.jpg/.jpeg/.webp`, every image the body references is attached.
4. **Write to `incoming/blog/`.** `<slug>.md` with front matter, `<slug>.<ext>` featured image, and any extra images.
5. **Commit & push to main.** A pathspec commit of only those files. Anything else you have staged stays staged and is not pushed. If the push is rejected, the commit is undone and the files are removed.
6. **Start Import content.** Waits for the workflow run triggered by that commit.
7. **Import & build.** Follows the `import` job step by step. On failure, the failing step's log is shown in the app.
8. **Deploy to GitHub Pages.** Follows the `deploy / build` and `deploy / deploy` jobs.
9. **Verify it's live.** Requests `https://woodruff.dev/blog/<slug>/` until it returns 200. Skipped for drafts.
10. **Pull the imported post.** Fast-forwards local `main` to pick up the workflow's import commit.

**Check only** runs steps 1–3 and writes nothing. Publishing always asks for confirmation first. A desktop notification fires when a publish finishes (macOS and Linux). The form autosaves, so closing the app doesn't lose a draft.

## Look and feel

Colors, type, and components are taken from `site/src/styles/tokens.css` and `base.css`: Plus Jakarta Sans, the `#c9a96e` gold accent, pill buttons, uppercase labels with a gold rule, 16px-radius cards, and a dark activity panel. `Styles/Tokens.axaml` mirrors the CSS tokens, so update both together. The fonts are the static TTF builds of Plus Jakarta Sans (SIL OFL, `Assets/Fonts/OFL.txt`), because Avalonia can't load the site's woff2 files.

## Tests

```bash
dotnet test
```

These include an integration test that feeds the app's output to the site's real `site/scripts/import-blog.mjs` in a scratch directory. It needs `site/node_modules`, and returns early without asserting anything when that folder is missing.
