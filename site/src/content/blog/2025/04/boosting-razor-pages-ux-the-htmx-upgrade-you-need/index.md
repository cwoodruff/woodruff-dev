---
title: "Boosting Razor Pages UX: The htmx Upgrade You Need"
date: 2025-04-05
categories: 
  - "htmx"
tags: 
  - "asp-net"
  - "asp-net-core"
  - "htmx"
  - "web-development"
  - "webdev"
coverImage: "./images/Boosting-Razor-Pages-UX-The-htmx-Upgrade-You-Need.png"
---

Hey there, fellow .NET developer! Today, we’re talking about how you can take your ASP.NET Razor Pages apps from "meh" to "whoa!" with htmx. We’re diving into seamless navigation, enhancing user interactions, and even doing a little case study to show you how this all comes together. Ready? Let’s go.

## The Problem With Traditional Razor Pages

Don’t get me wrong. Razor Pages are fantastic. They let you build dynamic web apps with clean separation of concerns. But when it comes to adding interactivity, you often find yourself writing JavaScript to handle basic stuff. And if you’re not careful, that JavaScript snowballs into a giant mess.

## Enter htmx: Your UI Sidekick

htmx makes it incredibly easy to enhance user experience by letting your server do the heavy lifting. You don’t need a ton of client-side JavaScript. Just some clever HTML attributes and your server-side C# logic.

### How htmx Enhances UX

- **Seamless Navigation:** Load new content without full-page refreshes.

- **Dynamic Interactions:** Update parts of a page without touching JavaScript.

- **Progressive Enhancement:** Everything still works even if JavaScript is disabled.

## Seamless Navigation With htmx

Let’s say you have a blog with multiple articles. Normally, clicking on an article would trigger a full-page reload. But what if we could just load the content in place?

### Example: Loading Articles Without Refresh

#### Index.cshtml

```html
@page
@model IndexModel

<!DOCTYPE html>
<html>
<head>
    <title>Blog Articles</title>
    <script src="https://unpkg.com/htmx.org"></script>
</head>
<body>
    <h1>Blog Articles</h1>

    <div id="article-list">
        @foreach (var article in Model.Articles)
        {
            <div>
                <a href="#" hx-get="/Index?handler=LoadArticle?id=@article.Id" hx-target="#content">@article.Title</a>
            </div>
        }
    </div>

    <div id="content">
        <p>Select an article to read.</p>
    </div>
</body>
</html>
```

#### Index.cshtml.cs

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace YourNamespace.Pages;

public class IndexModel : PageModel
{
    public List<Article> Articles { get; set; } = new()
    {
        new Article { Id = 1, Title = "Understanding HTMX" },
        new Article { Id = 2, Title = "Razor Pages Best Practices" }
    };

    public IActionResult OnGetLoadArticle(int id)
    {
        var content = id switch
        {
            1 => "<h2>Understanding HTMX</h2><p>HTMX makes HTML fun again!</p>",
            2 => "<h2>Razor Pages Best Practices</h2><p>Keep your views clean and your logic lean.</p>",
            _ => "<p>Article not found.</p>"
        };

        return Content(content, "text/html");
    }

    public class Article
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }
}
```

### What’s Happening Here

- We’re using `hx-get` to load article content from the server.

- `hx-target="#content"` specifies where the content should be rendered.

- No page reloads. No JavaScript. Just pure Razor Pages magic.

## Enhancing User Interactions

Now, let’s take a simple form submission and make it feel snappier with htmx.

### Example: Adding a Comment Without Reloading

#### Comments.cshtml

```html
@page
@model CommentsModel

<!DOCTYPE html>
<html>
<head>
    <title>Comments</title>
    <script src="https://unpkg.com/htmx.org"></script>
</head>
<body>
    <h1>Comments</h1>

    <div id="comments-section">
        @foreach (var comment in Model.Comments)
        {
            <p>@comment</p>
        }
    </div>

    <form hx-post="/Comments?handler=AddComment" hx-target="#comments-section">
        <input type="text" name="comment" placeholder="Add a comment..." required>
        <button type="submit">Post Comment</button>
    </form>
</body>
</html>
```

#### Comments.cshtml.cs

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace YourNamespace.Pages;

public class CommentsModel : PageModel
{
    public static List<string> Comments = new() { "Great post!", "Very informative." };

    public void OnGet()
    {
    }

    public IActionResult OnPostAddComment(string comment)
    {
        if (!string.IsNullOrWhiteSpace(comment))
        {
            Comments.Add(comment);
            var newCommentHtml = $"<p>{comment}</p>";
            return Content(newCommentHtml, "text/html");
        }

        return BadRequest("Comment cannot be empty.");
    }
}
```

### Why This Works

- The form uses `hx-post` to send data to the server and immediately inject the response.

- No need for AJAX setup, JavaScript event listeners, or weird client-side state handling.

## Case Study: Improving an Existing App with htmx

Imagine you have a Razor Pages app that displays a product catalog. Normally, clicking a product link would take you to a new page. With htmx, you can make this feel much more interactive.

### Before htmx

- Clicking a product triggers a full-page reload.

- Navigation feels clunky and slow.

### After htmx

- Clicking a product loads the details dynamically via `hx-get`.

- The user stays on the same page, with the content seamlessly replaced.

## Why htmx Improves UX

- It reduces page reloads, making navigation feel instantaneous.

- It allows you to load only the parts of the page that change.

- It plays nicely with server-rendered HTML, so you don’t have to throw away everything you know.

htmx is a fantastic way to upgrade the UX of your ASP.NET Razor Pages apps without going down the JavaScript framework rabbit hole. Give it a try and watch your apps become faster, simpler, and way more enjoyable to use.
