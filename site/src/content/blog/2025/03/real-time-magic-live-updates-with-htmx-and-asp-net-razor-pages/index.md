---
title: "Real-Time Magic: Live Updates with htmx and ASP.NET Razor Pages"
date: 2025-03-31
categories: 
  - "htmx"
tags: 
  - "asp-net"
  - "asp-net-core"
  - "htmx"
  - "web-development"
  - "webdev"
coverImage: "./images/Real-Time-Magic-Live-Updates-with-htmx-and-ASP.NET-Razor-Pages.png"
---

Welcome back, code adventurer. Today, we’re diving into how htmx can give your ASP.NET Razor Pages some real-time flavor. Whether you want to build a chat app, a live stock ticker, or a dashboard that updates itself without manual refreshing, htmx has you covered.

## Why Real-Time Matters

Traditional web apps usually rely on page reloads to fetch fresh data. But what if your users want that sweet, smooth experience where updates just appear magically on their screen? That’s where htmx comes into play.

You might be thinking, "Isn’t this what SignalR is for?" And yes, SignalR is awesome for certain scenarios. But if you want something lightweight, simple, and doesn’t require websockets, htmx is your new best friend.

## How htmx Makes Real-Time Easy

htmx allows you to poll the server at intervals and update parts of your page when new data is available. And the best part? It’s dead simple to set up.

### Example: Live Stock Price Updates

Let’s say you want to build a stock ticker that shows live price updates without having to hit that refresh button.

### Index.cshtml

```html
@page
@model IndexModel

<!DOCTYPE html>
<html>
<head>
    <title>Live Stock Prices</title>
    <script src="https://unpkg.com/htmx.org"></script>
</head>
<body>
    <h1>Live Stock Prices</h1>

    <div id="stock-ticker" hx-get="/Index?handler=GetStockPrices" hx-trigger="every 2s" hx-target="#stock-ticker">
        Loading stock prices...
    </div>
</body>
</html>
```

### Index.cshtml.cs

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;

namespace YourNamespace.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGetGetStockPrices()
    {
        var random = new Random();
        var stocks = new List<string>
        {
            $"<p>Apple: ${random.Next(150, 200)}.00</p>",
            $"<p>Microsoft: ${random.Next(250, 300)}.00</p>",
            $"<p>Google: ${random.Next(1000, 1500)}.00</p>"
        };

        var responseHtml = string.Join("", stocks);
        return Content(responseHtml, "text/html");
    }
}
```

## What’s Happening Here

- **hx-get:** Makes a GET request to `/Index?handler=GetStockPrices` to fetch the latest data.

- **hx-trigger="every 2s":** Tells htmx to poll the server every 2 seconds.

- **hx-target:** Specifies where the response should be injected.

No page reloads, no WebSockets, and no need for client-side frameworks. It just works.

## Making Things Even Cooler

What if you only want to update the stock prices when they change? htmx has a handy attribute for that: **hx-swap="outerHTML"**. You can combine this with conditional server-side rendering to make sure you’re only sending fresh content when something actually changes.

## Example: Selective Updates

Update your `Index.cshtml` like this:

```html
<div id="stock-ticker" hx-get="/Index?handler=GetStockPrices" hx-trigger="every 2s" hx-target="#stock-ticker" hx-swap="outerHTML">
    Loading stock prices...
</div>
```

Now, the content will only be replaced if the server sends back something different.

## When To Use This Pattern

- Live Dashboards

- Notifications

- Status Monitoring

- News Feeds

## When Not To Use This Pattern

- High-frequency data updates (Use SignalR instead).

- Scenarios where maintaining a persistent connection is required.

htmx gives you a low-effort way to provide a smooth real-time experience without the hassle of WebSockets or heavy client-side frameworks. Next time, we’ll explore more advanced real-time patterns to make your apps feel even more alive.
