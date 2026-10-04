# Drop folder: Press & Media

Put a Markdown file with front matter plus the logo image it names here, then commit and push to `main`:

```
incoming/media/dotnet-rocks-networking.md
incoming/media/dotnet-rocks.png
```

The `Import content` GitHub Action creates `site/src/content/media/<slug>.md`, moves the logo into `site/src/content/media/images/`, commits, and deploys. Processed files are removed from this folder. This README is ignored.

## Front matter

`title`, `url`, and `image` are required. The body is optional and ignored by the cards.

```md
---
title: "C# Networking with Chris Woodruff"
url: https://podtail.com/en/podcast/-net-rocks/c-networking-with-chris-woodruff-2025-05-22/
image: dotnet-rocks.png          # file next to this one; reused if an identical logo already exists
outlet: ".NET Rocks!"
type: podcast                    # podcast | video | article | talk (default: article)
date: 2025-05-22                 # optional; newest first on the page
description: "Carl and Richard talk with Chris about network programming in C#."
featured: true                   # shows the logo in the home page strip
order: 5                         # optional tie-breaker when dates are missing
slug: dotnet-rocks-csharp-networking   # optional; defaults to a slug of the title
---
```

Re-importing with the same slug updates the existing item.
