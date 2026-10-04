---
title: "Explicit Includes: The Art of Fetching Just Enough Data in EF Core"
date: 2025-01-29
categories: 
  - "efcore"
tags: 
  - "net"
  - "c"
  - "data"
  - "databases"
  - "entity-framework-core"
  - "programming"
coverImage: "./images/Explicit-Includes-The-Art-of-Fetching-Just-Enough-Data-in-EF-Core.png"
---

If you’ve ever gone grocery shopping while hungry, you know how easy it is to end up with more than you need. The same thing happens in EF Core when you’re too eager with your Include statements—you fetch all the data, even the stuff you don’t need, and suddenly, your app feels sluggish. That’s where **Explicit Includes** comes in, allowing you to fetch only what you need when needed, making your development process more efficient. Think of it as grocery shopping with a list but for your code.

* * *

## **What Are Explicit Includes?**

When you fetch an entity in EF Core, its related data doesn’t magically appear unless you tell EF Core to include it. Explicit Includes give you precise control over which related data gets loaded, helping you:

- Avoid over-fetching.

- Keep your queries lean and mean.

- Improve app performance by loading related data only when it’s actually needed.

In other words, Explicit Includes are the Marie Kondo of data fetching—they spark joy by keeping your queries clean and intentional.

* * *

## **How to Use Explicit Includes**

Here’s the scenario: You’re building a blogging app. Each blog has a list of posts, and each post has comments. Fetching everything in one go can quickly turn into a monster query.

### Basic Fetch Without Includes

```csharp
var blogs = await context.Blogs.ToListAsync();
```

This fetches blogs but leaves the related posts and comments behind. If you try to access them, you’ll get a sad, empty collection.

### Include to the Rescue

```csharp
string titleFilter = "Entity Framework Core"; // Example filter condition

var blogs = await context.Blogs
    .Include(b => b.Posts.Where(p => p.Title.Contains(titleFilter)))
    .ThenInclude(p => p.Comments)
    .ToListAsync();
```

Now you’ve got blogs, their posts, and even the comments—all in one query. But wait... is this too much data for your use case? 🤔

* * *

## **When to Use Explicit Includes**

Here’s where Explicit Includes shine: when you need related data, but not all at once. Instead of loading everything up front, you can fetch related data as needed.

### Example: Fetching Related Data on Demand

Let’s say you fetch your blogs without their posts initially:

```csharp
var blogs = await context.Blogs.ToListAsync();
```

Then, for a specific blog, you explicitly load its posts later:

```csharp
foreach (var blog in blogs)
{
    await context.Entry(blog)
        .Collection(b => b.Posts)
        .LoadAsync();
}
```

You’re fetching posts only when necessary, keeping your initial query lightweight. It’s like ordering dessert only after you’ve decided you’re not full from dinner.

* * *

## **Why Explicit Includes Matter**

Here’s why you’ll love Explicit Includes:

1. **Avoid Over-fetching**  
    Fetch only the needed data, reducing memory usage and speeding up queries.

3. **Fine-Grained Control**  
    Decide precisely when and how related data gets loaded rather than fetching it all upfront.

5. **Better Performance for Large Datasets**  
    Loading everything in one query can overwhelm your database server. Explicit Includes let you break things down into smaller, more manageable chunks.

* * *

## **A Real-World Scenario**

Imagine a dashboard showing a list of blogs. Each blog displays the number of posts but not the posts themselves. Fetching posts in this case is unnecessary.

### The Wrong Way: Over-fetching

```csharp
var blogs = await context.Blogs
    .Include(b => b.Posts)
    .ToListAsync();
```

You’ve just fetched all the posts for every blog, even though you only needed a count. Oops.

### The Right Way: Explicit Loading

```csharp
var blogs = await context.Blogs.ToListAsync();

foreach (var blog in blogs)
{
    blog.PostCount = await context.Entry(blog)
        .Collection(b => b.Posts)
        .Query()
        .CountAsync();
}
```

This approach gives you the count without fetching all the posts, saving memory and database round trips.

* * *

## **When to Avoid Explicit Includes**

While Explicit Includes are fantastic, they’re not always the right choice. Here’s when to stick with regular `Include` statements:

- **Small Datasets:** If you’re working with a small amount of data, loading everything upfront might be simpler and faster.

- **One-Off Queries:** If you only need the related data once, a single query with `Include` might be more efficient.

* * *

## **Tips for Mastering Explicit Includes**

- **Combine with AsNoTracking:** If you’re not modifying the data, use `.AsNoTracking()` to avoid unnecessary tracking overhead.

- **Profile Your Queries:** Use tools like SQL Server Profiler or EF Core’s logging to see exactly what SQL is being executed.

- **Don’t Nest Too Deeply:** Fetching deeply nested relationships explicitly can get tricky. Consider restructuring your query or simplifying your data model.

* * *

## **Wrap-Up: Fetch Smart, Not Hard**

Explicit Includes give you the power to fetch data intentionally, avoiding the pitfalls of over-fetching and bloated queries. By loading related data only when you actually need it, you’ll keep your EF Core app running smoothly and efficiently.

So, next time you build a query, remember: You don’t need to fetch the whole grocery store when all you want is a loaf of bread. Fetch smart, and let your app (and your database) thank you.
