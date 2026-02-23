---
title: "Going Modular: Using htmx with Partial Views in Razor Pages"
date: 2025-04-02
categories: 
  - "htmx"
tags: 
  - "asp-net"
  - "asp-net-core"
  - "htmx"
  - "web-development"
  - "webdev"
coverImage: "Going-Modular-Using-htmx-with-Partial-Views-in-Razor-Pages.png"
---

Hey there, fellow coder. Today, we’re diving into the wonderful world of partial views and how you can make them dance beautifully with htmx. If you’ve ever wanted to break down your Razor Pages into reusable, modular components, you’re going to love this. Let’s get rolling.

## Why Partial Views Matter

Breaking your UI into smaller, reusable components is a great way to keep your code clean and organized. Razor Pages offers the `Partial()` method to render sections of HTML, but what if you want to **dynamically load or update these partials without refreshing the entire page**? That’s where htmx comes to the rescue.

## Setting Up the Project

We’re going to build a simple dashboard where you can update different parts of the UI independently using partial views and htmx.

Start by creating a new ASP.NET Razor Pages project:

```bash
dotnet new razor -n htmxPartialDemo
```

Add htmx to your `_Layout.cshtml`:

```html
<script src="https://unpkg.com/htmx.org"></script>
```

## Creating the Partial View

Let’s say we have a simple user dashboard with a statistics panel that gets updated periodically.

### \_StatsPartial.cshtml

```html
@model int

<div>
    <h3>User Statistics</h3>
    <p>Total Users: @Model</p>
    <button hx-get="/Dashboard/Stats" hx-target="#stats-partial">Refresh Stats</button>
</div>
```

## Adding the Backend Logic

Now, we’ll build the page model to serve this partial view and update it upon request.

### Dashboard.cshtml.cs

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;

namespace htmxPartialDemo.Pages;

public class DashboardModel : PageModel
{
    private static readonly Random Random = new();

    public int TotalUsers { get; private set; }

    public void OnGet()
    {
        TotalUsers = Random.Next(1000, 5000);
    }

    public IActionResult OnGetStats()
    {
        var updatedCount = Random.Next(1000, 5000);
        return Partial("_StatsPartial", updatedCount);
    }
}
```

## Building the Main Page

Here’s the main page where the partial view is included and updated dynamically.

### Dashboard.cshtml

```html
@page
@model htmxPartialDemo.Pages.DashboardModel

<!DOCTYPE html>
<html>
<head>
    <title>htmx + Partial Views</title>
    <script src="https://unpkg.com/htmx.org"></script>
</head>
<body>
    <h1>Dashboard</h1>

    <div id="stats-partial">
        @await Html.PartialAsync("_StatsPartial", Model.TotalUsers)
    </div>

    <div>
        <p>Last refreshed at: @DateTime.Now.ToString("HH:mm:ss")</p>
    </div>
</body>
</html>
```

## What’s Happening Here

- The `_StatsPartial.cshtml` file is a simple partial view that renders the user count.

- The `OnGetStats()` method in `Dashboard.cshtml.cs` serves the partial view asynchronously.

- The button in the partial view triggers a GET request to the server using `hx-get` and replaces the content in the `#stats-partial` div using `hx-target`.

## Why This Approach Rocks

- **Reusability:** You can use partial views all over your application and fetch them dynamically as needed.

- **Performance:** Only the needed section of the page is updated, saving bandwidth and making your app feel snappy.

- **Simplicity:** You’re still using Razor Pages, just enhanced with a touch of htmx magic.

## Expanding the Concept

This technique works perfectly for:

- Dashboards with various panels updated independently.

- Multi-step forms where each step is a partial view.

- Modals that load their content on demand.

Next time, we’ll dive even deeper into building more advanced applications using htmx and Razor Pages. Stay tuned.
