---
title: "Stop Building SPAs for Every Screen: htmx + ASP.NET Core Razor Pages Workshop (Open)"
date: 2026-01-14
categories: 
  - "htmx"
tags: 
  - "net"
  - "asp-net-core"
  - "c"
  - "dotnet"
  - "htmx"
  - "programming"
coverImage: "./images/htmx-ASP.NET-Core-Razor-Pages-Workshop.png"
---

If your default move for “modern UX” is a SPA, you are paying a tax you do not need. You pay for it in build pipelines, duplicated validation rules, fragile client state, and a front end that turns routine CRUD into an engineering project.

This workshop is a different bet. We keep the server in charge, keep HTML as the interface, and still ship interactions that feel sharp. Razor Pages stays at the center. htmx becomes the small layer that turns clicks and form posts into targeted fragment updates.

I am opening the full workshop materials to everyone. You can clone the repository, run the starter app, and work through the labs at your own pace.

You can find the workshop instructions here. [https://htmx-workshop.com/](https://htmx-workshop.com/)

The GitHub repo for the workshop is here. [https://github.com/cwoodruff/htmx-razor-workshop](https://github.com/cwoodruff/htmx-razor-workshop)

## What You Will Learn

### Hypermedia, in Practical Terms

Links and forms are state transitions. The browser requests the next UI state. The server responds with HTML that represents that state. You inspect it, you swap it, you move on.

### The htmx Mental Model

You will learn one loop and reuse it across every lab.

- Pick a fragment boundary

- Wrap it with a stable id

- Trigger a request

- Return a fragment

- Swap it into place

### Hands-On Labs that Map to Real Work

You will build a small Razor Pages app and evolve it through patterns you can reuse on Monday.

- Fragment-first composition with partials and stable targets

- Partial updates with `hx-get` and `hx-post`

- Server-side validation with immediate feedback and clean invalid flows

- Details panel or modal, confirm delete, paging and filtering with history support

- Variable sub-collections, dependent dropdowns, and long-running work with polling

- Out-of-band updates for messages and status

## Who Should Join

You already know C# and Razor Pages. You want modern interactions without a client-side app taking over your architecture. You want to debug with DevTools and server logs, not with a maze of client state and side effects.

## Why this Matters

Many teams keep adding JavaScript stacks to solve problems created by leaving the web platform behind. If you can deliver the same experience with server-rendered fragments, you reduce surface area and regain control.

## How to Participate

Clone the repo, run the app, and follow the lab documents in order. Each lab is designed as a sequence of small, testable changes. You can apply the patterns page by page in a real product without a rewrite.

## Help Me Improve It

If you find a bug, a confusing step, or a missing explanation, please share what you saw and how to reproduce it. The strongest way is to open an issue in the GitHub repository with:

- What you expected

- What happened instead

- Steps to reproduce

- Your environment details (OS, .NET SDK version, IDE)

- Screenshots or snippets when useful

If you prefer, send the details directly, and I will capture them as an issue.

If your current stack claims a SPA is mandatory for every screen, run this workshop. Then decide which complexity you still want to carry.
