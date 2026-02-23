---
date: 2026-01-15
category: htmx
description: "If your default move for 'modern UX' is a SPA, you are paying a tax you do not need. This workshop is a different bet."
---
# Stop Building SPAs for Every Screen: htmx + ASP.NET Core Razor Pages Workshop

*A workshop that challenges the SPA-by-default mindset.*

If your default move for "modern UX" is a SPA, you are paying a tax you do not need. This workshop is a different bet.

## The Problem With SPA-by-Default

Single Page Applications solved a real problem in 2013: server-rendered pages felt slow, and users expected desktop-like responsiveness. But the solution came with a cost that has compounded every year since:

- **Build complexity** — bundlers, transpilers, module systems, tree-shaking
- **State management** — Redux, Zustand, Recoil, Jotai, or whatever ships next month
- **API duplication** — every server-side model gets a client-side twin
- **Hydration costs** — ship JavaScript to recreate what the server already rendered

For dashboards, admin panels, content sites, and internal tools, this is an enormous tax for a marginal UX improvement.

## What htmx Offers

htmx lets you build dynamic, interactive pages by returning HTML from the server instead of JSON. No client-side framework. No build step. No state synchronization.

```html
<button hx-get="/api/notifications"
        hx-target="#notification-list"
        hx-swap="innerHTML">
    Refresh
</button>

<div id="notification-list">
    <!-- Server returns HTML fragments that replace this content -->
</div>
```

The server renders the HTML. htmx swaps it into the page. The browser does what browsers are good at: rendering HTML.

## Workshop Format

This is a hands-on, full-day workshop built around ASP.NET Core Razor Pages and htmx:

### Module 1: Foundations
- Setting up htmx with Razor Pages
- `hx-get`, `hx-post`, `hx-target`, `hx-swap`
- Returning partial views from page handlers

### Module 2: Interactive Patterns
- Search-as-you-type with `hx-trigger="keyup changed delay:300ms"`
- Infinite scroll with `hx-trigger="revealed"`
- Inline editing with `hx-swap="outerHTML"`

### Module 3: Forms and Validation
- Server-side validation with htmx form submission
- Progressive enhancement — forms work without JavaScript
- File uploads with progress indicators

### Module 4: Real-World Application
- Building a complete CRUD interface
- Combining htmx with ASP.NET Core anti-forgery tokens
- Performance comparison: SPA vs. htmx approach

## Who Should Attend

- Backend developers tired of maintaining a separate frontend
- Teams building internal tools and admin panels
- Anyone curious about the "HTML over the wire" approach

## Key Takeaway

htmx is not anti-JavaScript. It is anti-unnecessary-JavaScript. For a large class of web applications, the server can do the rendering, the browser can do the display, and you can skip the entire client-side framework layer in between.
