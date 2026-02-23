---
date: 2026-01-05
category: Patterns
description: "If a single endpoint pulls half your database just to render a small card on a mobile screen, your problem is not the database."
---
# Enterprise Patterns for ASP.NET Core Minimal API: Lazy Load Pattern

*Load data when you need it, not before.*

If a single endpoint pulls half your database just to render a small card on a mobile screen, your problem is not the database.

## What Is Lazy Loading?

Lazy loading defers the retrieval of related data until it is actually accessed. Instead of loading an entity and all its relationships upfront, you load the entity first and fetch related data on demand.

In Entity Framework Core, lazy loading works through navigation properties that are transparently loaded when you access them:

```csharp
var album = await db.Albums.FindAsync(id);

// Tracks are NOT loaded yet
Console.WriteLine(album.Title); // No database call

// Tracks ARE loaded now (database call happens here)
foreach (var track in album.Tracks)
{
    Console.WriteLine(track.Name);
}
```

## Enabling Lazy Loading in EF Core

EF Core supports lazy loading through proxies or through the `ILazyLoader` service:

```csharp
// Option 1: Proxies (requires Microsoft.EntityFrameworkCore.Proxies)
builder.Services.AddDbContext<ChinookContext>(options =>
    options
        .UseSqlServer(connectionString)
        .UseLazyLoadingProxies());

// Navigation properties must be virtual
public class Album
{
    public int Id { get; set; }
    public string Title { get; set; }
    public virtual ICollection<Track> Tracks { get; set; }
    public virtual Artist Artist { get; set; }
}
```

## The N+1 Problem

Lazy loading has a well-known trap. Consider listing albums with their artist names:

```csharp
var albums = await db.Albums.ToListAsync(); // 1 query

foreach (var album in albums)
{
    Console.WriteLine(album.Artist.Name); // N queries (one per album)
}
```

This generates N+1 database queries. For 100 albums, that is 101 queries. The fix is eager loading for known access patterns:

```csharp
var albums = await db.Albums
    .Include(a => a.Artist)     // JOIN in a single query
    .ToListAsync();             // 1 query total
```

## When to Use Lazy Loading

- **Exploratory UIs** — when you do not know which related data the user will drill into
- **Optional relationships** — when related data is rarely needed
- **Prototyping** — when you want quick results and will optimize later

## When to Avoid It

- **API endpoints** — you know exactly what data you need. Use projection or eager loading.
- **Batch operations** — N+1 queries will destroy performance
- **Serialization** — serializing an entity graph with lazy-loading proxies can trigger unexpected queries

## The Minimal API Approach

In Minimal APIs, prefer explicit loading over lazy loading. Be deliberate about what you fetch:

```csharp
app.MapGet("/api/albums/{id}", async (int id, ChinookContext db) =>
{
    var album = await db.Albums
        .Where(a => a.Id == id)
        .Select(a => new
        {
            a.Id,
            a.Title,
            ArtistName = a.Artist.Name,
            TrackCount = a.Tracks.Count
        })
        .FirstOrDefaultAsync();

    return album is not null ? Results.Ok(album) : Results.NotFound();
});
```

This generates a single query that fetches exactly the fields you need. No lazy loading. No N+1. No surprises.

## Key Takeaway

Lazy loading is a convenience, not a strategy. Use it when exploration is the goal. Use eager loading or projection when performance is the goal. In APIs, always prefer explicit data loading—your database will thank you.
