---
title: "Interactive Forms Made Easy: htmx Meets ASP.NET Razor Pages"
date: 2025-03-29
categories: 
  - "htmx"
tags: 
  - "asp-net"
  - "asp-net-core"
  - "htmx"
  - "web-development"
  - "webdev"
coverImage: "Interactive-Forms-Made-Easy-htmx-Meets-ASP.NET-Razor-Pages.png"
---

Welcome back to the htmx party! Today, we’re talking about a topic every web developer has wrestled with at some point: forms. They’re essential, but making them play nicely with your backend can sometimes feel like wrestling a bear. Enter HTMX. It’s here to simplify your form handling and make the experience feel smooth and modern.

## Enhancing Form Submissions with htmx

Normally, handling forms in ASP.NET Razor Pages means doing a full-page reload when you hit that submit button. Not terrible, but certainly not snappy. With htmx, you can make your forms interactive without all the overhead of a traditional JavaScript-heavy solution.

### Setting Up Your Project

Before we dive into the code, make sure your project is set up with htmx. This means adding the following line to your `_Layout.cshtml` file:

```bash
<script src="https://unpkg.com/htmx.org"></script>
```

Done? Cool. Let’s get to it.

## Building a Search Feature with hx-post and hx-include

Let’s imagine you’re building a simple search feature where you can type in a query and get the results instantly without refreshing the page. Here’s how to make it happen.

### Search Page (Index.cshtml)

```html
@page
@model IndexModel

<!DOCTYPE html>
<html>
<head>
    <title>htmx Search Example</title>
    <script src="https://unpkg.com/htmx.org"></script>
</head>
<body>
    <h1>Search for Products</h1>

    <form id="search-form" hx-post="/Index?handler=Search" hx-target="#search-results">
        <input type="text" name="query" placeholder="Search...">
        <button type="submit">Search</button>
    </form>

    <div id="search-results"></div>
</body>
</html>
```

### Backend Handler (Index.cshtml.cs)

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;

namespace YourNamespace.Pages;

public class IndexModel : PageModel
{
    private static readonly List<string> Products = new()
    {
        "Laptop", "Smartphone", "Keyboard", "Mouse", "Monitor", "Headphones"
    };

    public IActionResult OnPostSearch(string query)
    {
        var results = Products
            .Where(p => p.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(p => $"<div>{p}</div>")
            .ToList();

        var responseHtml = results.Any() ? string.Join("", results) : "<div>No results found.</div>";
        return Content(responseHtml, "text/html");
    }
}
```

### What’s Going On Here?

- The form is using `hx-post` to send the request to the server-side handler. No page reloads here.

- `hx-target` is telling htmx to inject the results directly into the `#search-results` div.

- You can still handle your request in pure C# on the server side. No JavaScript drama involved.

## Handling Validation and Responses

Let’s say you want to provide some basic validation. Instead of refreshing the page or writing a ton of JavaScript, htmx makes this painless.

### Adding Basic Validation

Update your `Index.cshtml` file like this:

```html
<form id="search-form" hx-post="/Index?handler=Search" hx-target="#search-results">
    <input type="text" name="query" placeholder="Search..." required>
    <button type="submit">Search</button>
</form>

<div id="search-results"></div>
```

Now, the browser will handle the basic `required` validation. But let’s say you want your server to validate the query too. Here’s how you can do that:

### Updated Handler (Index.cshtml.cs)

```csharp
public IActionResult OnPostSearch(string query)
{
    if (string.IsNullOrWhiteSpace(query))
    {
        return Content("<div>Please enter a valid search query.</div>", "text/html");
    }

    var results = Products
        .Where(p => p.Contains(query, StringComparison.OrdinalIgnoreCase))
        .Select(p => $"<div>{p}</div>")
        .ToList();

    var responseHtml = results.Any() ? string.Join("", results) : "<div>No results found.</div>";
    return Content(responseHtml, "text/html");
}
```

## What Did We Learn?

- You can make your forms interactive without sacrificing server-side logic.

- `hx-post` allows you to submit data without refreshing the page.

- `hx-target` controls where the response goes.

- Validation can be handled both client-side and server-side.

In just a few lines of code, you’ve created a responsive, search-driven interface that feels modern and polished. And the best part? You didn’t have to wrestle with JavaScript at all.

Stay tuned for the next post where we’ll dive into building full CRUD interfaces with htmx and ASP.NET Razor Pages. It’s about to get even better!
