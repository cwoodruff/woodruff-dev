---
date: 2026-01-18
category: Patterns
description: "Your domain model exists to protect your business rules. Your API exists to protect your clients. DTOs are the line in the sand."
---
# Enterprise Patterns for ASP.NET Core Minimal API: Data Transfer Object Pattern

*DTOs are the line in the sand between your domain and the outside world.*

Your domain model exists to protect your business rules. Your API exists to protect your clients. DTOs are the line in the sand.

## What Is a DTO?

A Data Transfer Object is a simple container that carries data between processes or layers. It has no behavior, no business logic, no dependencies. It exists solely to define the shape of data that crosses a boundary.

```csharp
public record AlbumDto(
    int Id,
    string Title,
    string ArtistName,
    decimal Price,
    int TrackCount);
```

That is it. No navigation properties, no computed fields, no ORM annotations. Just the data the caller needs.

## Why Not Just Return the Entity?

Returning your EF Core entity directly from an API endpoint is the fastest path to three problems:

1. **Over-posting** — clients can send properties you did not intend to be writable
2. **Over-fetching** — the response includes data the client does not need (or should not see)
3. **Coupling** — your API contract is now tied to your database schema. Change one, break the other.

DTOs decouple these layers. Your entity can evolve independently of your API contract.

## DTOs in Minimal APIs

```csharp
app.MapGet("/api/albums/{id}", async (int id, ChinookContext db) =>
{
    var album = await db.Albums
        .Include(a => a.Artist)
        .Include(a => a.Tracks)
        .FirstOrDefaultAsync(a => a.Id == id);

    if (album is null)
        return Results.NotFound();

    var dto = new AlbumDto(
        album.Id,
        album.Title,
        album.Artist.Name,
        album.Tracks.Sum(t => t.UnitPrice),
        album.Tracks.Count);

    return Results.Ok(dto);
});
```

The entity has navigation properties, ORM tracking, and possibly lazy-loading proxies. The DTO has exactly the fields the client needs, in exactly the shape the client expects.

## Input DTOs

The pattern works in both directions. For writes, define a separate DTO that represents what the client is allowed to send:

```csharp
public record CreateAlbumRequest(
    string Title,
    int ArtistId);

app.MapPost("/api/albums", async (CreateAlbumRequest request, ChinookContext db) =>
{
    var album = new Album
    {
        Title = request.Title,
        ArtistId = request.ArtistId
    };

    db.Albums.Add(album);
    await db.SaveChangesAsync();

    return Results.Created($"/api/albums/{album.Id}", album.Id);
});
```

The client cannot set the `Id`, the `Price`, or any other field you did not include in `CreateAlbumRequest`. Over-posting is structurally impossible.

## Mapping Strategies

For simple cases, manual mapping (as shown above) is clear and debuggable. For larger projects, consider:

- **Extension methods** — `album.ToDto()` keeps mapping logic discoverable
- **AutoMapper or Mapster** — useful at scale, but adds a layer of indirection

Start with manual mapping. Introduce a library only when the repetition becomes a maintenance burden.

## Key Takeaway

A DTO is not boilerplate. It is a contract. It defines what your system promises to the outside world, independent of how that system works internally. That independence is what makes your system evolvable.
