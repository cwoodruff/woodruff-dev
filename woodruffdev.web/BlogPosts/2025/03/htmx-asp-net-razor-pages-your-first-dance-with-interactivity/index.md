---
title: "htmx + ASP.NET Razor Pages: Your First Dance with Interactivity"
date: 2025-03-27
categories: 
  - "htmx"
tags: 
  - "asp-net"
  - "asp-net-core"
  - "htmx"
  - "web-development"
  - "webdev"
coverImage: "htmx-ASP.NET-Razor-Pages_-Your-First-Dance-with-Interactivity.png"
---

Hey there, fellow C# developers! Ready to sprinkle some htmx on your ASP.NET Razor Pages? You’re about to see how easy it is to get started with htmx and add dynamic features without turning your app into a JavaScript spaghetti mess. Let’s boogie!

## Installing and Configuring htmx in ASP.NET Core

Good news: Installing htmx is like adding sprinkles to a cake. You don’t have to mess around with NPM or crazy build pipelines. Just pop it into your layout page.

### Step 1: Create a New ASP.NET Core Razor Pages Project

First, make sure you have .NET 8 installed and create a new Razor Pages project:

```bash
dotnet new razor -n htmxDemo
```

### Step 2: Add htmx to Your Project

The beauty of htmx is that you can just drop it in as a script tag. So, in your `_Layout.cshtml` file, add this line inside the `<head>` tag:

```html
<script src="https://unpkg.com/htmx.org"></script>
```

Boom! You’re all set up. No npm install nonsense. Now, let’s put this puppy to work.

## Basic htmx Syntax and Adding Attributes

htmx works by using attributes that you slap onto your existing HTML. The main ones you’ll be using are:

- `hx-get` – Makes a GET request to your server.

- `hx-post` – Makes a POST request.

- `hx-trigger` – Decides when the request should happen (like clicking a button).

- `hx-target` – Where the returned HTML should be injected.

Easy, right? Now, let’s build something fun.

## Your First Example: A Simple GET Request with htmx

Let’s create a simple page that lets you request a greeting from the server.

### `Index.cshtml`

```html
@page
@model IndexModel

<!DOCTYPE html>
<html>
<head>
    <title>htmx Demo</title>
    <script src="https://unpkg.com/htmx.org"></script>
</head>
<body>
    <h1>Welcome to htmx + ASP.NET Razor Pages!</h1>

    <div id="greeting-area">
        <button hx-get="/Index?handler=GetGreeting" hx-target="#greeting-area">Get Greeting</button>
    </div>
</body>
</html>
```

### `Index.cshtml.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;

namespace htmxDemo.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGetGetGreeting()
    {
        var greetingHtml = "<p>Hello there! The time is " + DateTime.Now.ToString("HH:mm:ss") + "</p>";
        return Content(greetingHtml, "text/html");
    }
}
```

### What Just Happened?

- The `<button>` tag has two essential htmx attributes:
    - `hx-get="/Index?handler=GetGreeting"` – A GET request is sent to the server when you click the button.
    
    - `hx-target="#greeting-area"` – The response gets injected right into the `div` with `id="greeting-area"`.

- The C# handler `OnGetGetGreeting()` returns HTML content with a greeting message and the current time.

## Why This Is So Cool

- You didn’t have to write JavaScript.

- No need for client-side frameworks or complicated routing.

- Everything stays nice and simple, just the way you like it.

And that’s it! You’ve just built your first htmx-powered feature. Next up, we’ll take this to the next level by handling POST requests and building more interactive pages.

[If you want to read more about htmx and ASP.NET Razor Pages, I have an online book with deeper examples and information.](https://aspnet-htmx.com/)
