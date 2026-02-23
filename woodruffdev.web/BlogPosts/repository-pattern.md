---
date: 2025-12-10
category: Patterns
description: "If DbContext shows up in every corner of your codebase, you do not have a domain model. You have a thin layer of LINQ wrapped in HTTP."
---
# Enterprise Patterns for ASP.NET Core Minimal API: Repository Pattern

*A clean boundary between your domain logic and your data access.*

If DbContext shows up in every corner of your codebase, you do not have a domain model. You have a thin layer of LINQ wrapped in HTTP.

## What Is the Repository Pattern?

The Repository pattern provides a collection-like interface for accessing domain objects, hiding the details of data access behind a well-defined API. Consumers work with domain objects; the repository handles persistence.

```csharp
public interface IAlbumRepository
{
    Task<Album?> GetByIdAsync(int id);
    Task<IReadOnlyList<Album>> GetAllAsync();
    Task<IReadOnlyList<Album>> GetByArtistAsync(int artistId);
    Task AddAsync(Album album);
    Task UpdateAsync(Album album);
    Task DeleteAsync(int id);
}
```

The consumer of this interface does not know whether the data comes from SQL Server, PostgreSQL, an in-memory cache, or a flat file. That is the point.

## Implementation With EF Core

```csharp
public class AlbumRepository : IAlbumRepository
{
    private readonly ChinookContext _db;

    public AlbumRepository(ChinookContext db) => _db = db;

    public async Task<Album?> GetByIdAsync(int id) =>
        await _db.Albums
            .Include(a => a.Artist)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<IReadOnlyList<Album>> GetAllAsync() =>
        await _db.Albums
            .Include(a => a.Artist)
            .OrderBy(a => a.Title)
            .ToListAsync();

    public async Task<IReadOnlyList<Album>> GetByArtistAsync(int artistId) =>
        await _db.Albums
            .Where(a => a.ArtistId == artistId)
            .OrderBy(a => a.Title)
            .ToListAsync();

    public async Task AddAsync(Album album)
    {
        _db.Albums.Add(album);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Album album)
    {
        _db.Albums.Update(album);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var album = await _db.Albums.FindAsync(id);
        if (album is not null)
        {
            _db.Albums.Remove(album);
            await _db.SaveChangesAsync();
        }
    }
}
```

## Using Repositories in Minimal APIs

```csharp
app.MapGet("/api/albums", async (IAlbumRepository repo) =>
    Results.Ok(await repo.GetAllAsync()));

app.MapGet("/api/albums/{id}", async (int id, IAlbumRepository repo) =>
    await repo.GetByIdAsync(id) is { } album
        ? Results.Ok(album)
        : Results.NotFound());
```

The endpoint handler has no knowledge of EF Core, no `DbContext`, no LINQ. It speaks the language of the domain.

## The Debate: Repository Over EF Core?

EF Core's `DbContext` is already a repository and unit of work. Adding another repository layer on top can feel redundant. Here is when it is worth it:

**Use repositories when:**
- You want to test business logic without a database
- You have complex query logic that deserves encapsulation
- You might switch data access technologies
- You want to enforce consistent data access patterns across the team

**Skip repositories when:**
- The application is small and CRUD-heavy
- You are the only developer
- EF Core is deeply embedded and unlikely to change

## Generic Repository: Proceed With Caution

A generic `IRepository<T>` can reduce boilerplate, but it often leaks abstraction:

```csharp
// This looks clean...
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task AddAsync(T entity);
}

// ...but what about GetByArtistAsync?
// You end up adding IQueryable<T> or expression parameters,
// which defeats the purpose of the abstraction.
```

Prefer specific repositories that speak the domain language over generic ones that speak the database language.

## Key Takeaway

The Repository pattern draws a line between "what data do I need" and "how do I get it." In small applications, EF Core's `DbContext` can serve as both. In larger systems, explicit repositories keep your domain clean and your data access testable.
