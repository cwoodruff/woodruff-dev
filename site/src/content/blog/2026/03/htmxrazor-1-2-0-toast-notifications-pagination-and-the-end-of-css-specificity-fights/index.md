---
title: "htmxRazor 1.2.0: Toast Notifications, Pagination, and the End of CSS Specificity Fights"
date: 2026-03-07
description: "The first feature release after htmxRazor hit 1.1 is here, and it targets the three complaints I hear most from .NET developers building server-rendered apps with htmx: “I need toast notifications,”…"
categories:
  - "htmx"
tags:
  - "net"
  - "c"
  - "dotnet"
  - "htmxrazor"
  - "programming"
  - "ux"
  - "wed-components"
coverImage: "./images/htmxrazor-1-2-0-toast-notifications-pagination-and-the-end-of-css-specificity-fights.png"
---
The first feature release after htmxRazor hit 1.1 is here, and it targets the three complaints I hear most from .NET developers building server-rendered apps with htmx: “I need toast notifications,” “I need pagination that works with htmx from the start,” and “your CSS keeps fighting with mine.”

Version 1.2.0 addresses all three. Here is what shipped.

### Toast Notifications That Actually Work with htmx

Every htmx-powered app needs toast notifications. A user submits a form, the server processes it, and you need to tell them what happened. Until now, your options in the ASP.NET Core world were to wire up a JavaScript toast library by hand or build your own partial-view-plus-htmx-oob-swap plumbing.

htmxRazor 1.2.0 ships a complete toast notification system. Drop a `<rhx-toast-container>` on your layout, then trigger toasts from the server using the `HxToast()` or `HxToastOob()` extension methods. The component handles auto-dismiss timers, severity variants (success, warning, danger, info), stacking when multiple toasts fire at once, and `aria-live` announcements so screen readers pick up every notification automatically.

No JavaScript. No third-party library. One Tag Helper and a server-side method call.

### Pagination Built for htmx

Pagination is another pattern that shows up in nearly every production app, yet nobody had shipped a .NET Tag Helper that wires up htmx navigation correctly. The new `<rhx-pagination>` component gives you page buttons, ellipsis for large ranges, first/last/prev/next controls, and size variants. All page transitions happen through htmx, so you get partial page updates without full reloads.

If you have been hand-coding pagination partials on every project, this replaces all of that with a single component.

### CSS Cascade Layers: No More Specificity Wars

This is the change that will matter most to teams adopting htmxRazor in existing applications.

Every component library ships CSS that eventually collides with your own styles. You write a rule, the library’s rule wins because of higher specificity, and you start sprinkling `!important` everywhere. It is a familiar and miserable cycle.

Version 1.2.0 wraps all htmxRazor component CSS inside `@layer` declarations. Cascade layers let the browser resolve specificity in a predictable order: any CSS you write outside a layer will always beat CSS inside one. That means your application styles win by default, with zero specificity hacks needed.

This single change makes htmxRazor significantly easier to adopt in brownfield projects that already have their own stylesheets.

### Accessibility: ARIA Live Region Manager

The new `<rhx-live-region>` component solves a problem that most developers do not realize they have until an accessibility audit flags it. When htmx swaps content on the page, screen readers do not automatically announce the change. Users who rely on assistive technology can miss critical updates entirely.

The live region manager listens for htmx swaps and pushes announcements to screen readers with configurable politeness levels (`polite` or `assertive`) and atomic update control. If you care about building applications that work for all of your users, this component closes a real gap.

### View Transitions and hx-on:\* Support

Two smaller additions round out the release. The new `rhx-transition` and `rhx-transition-name` attributes let you wire up the View Transitions API for animated page transitions with no custom JavaScript. And the `hx-on:*` dictionary attribute on the base Tag Helper class brings full support for htmx 2.x event handler attributes across every component in the library.

### Upgrade Now

```bash
dotnet add package htmxRazor --version 1.2.0
```

Browse the full docs and live demos at [https://htmxRazor.com](https://htmxRazor.com), and check the source at [https://github.com/cwoodruff/htmxRazor](https://github.com/cwoodruff/htmxRazor).

htmxRazor is MIT licensed and accepting contributions. If toast notifications, proper pagination, or cascade layers solve a problem you have been working around, give 1.2.0 a try.
