---
title: "Enterprise Patterns for ASP.NET Core Minimal API: Repository Pattern"
date: 2025-12-29
categories: 
  - "patterns"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "patterns"
  - "programming"
coverImage: "./images/Enterprise-Patterns-for-ASP.NET-Core-Minimal-API-Repository-Pattern.png"
---

If `DbContext` shows up in every corner of your codebase, you do not have a domain model. You have a thin layer of LINQ wrapped in HTTP.

You see it when:

- Minimal API endpoints inject `AppDbContext` and write queries inline

- Domain services take `DbContext` instead of domain interfaces

- Every feature invents its own way to load the same `Order` or `Customer`

A tiny change in schema or query behavior then turns into a scavenger hunt across controllers, services, and helpers.

The Repository pattern exists to stop that.

A repository gives you a **collection-like gateway** for an aggregate. Application code asks the repository for `Order` or `Customer` in domain terms. The repository hides SQL, ORM setup, and query shapes. It becomes the one place where you tune how aggregates are loaded and persisted.

In this post, you will see:

- What the Repository pattern is in practical .NET terms

- A complete `IOrderRepository` and `EfCoreOrderRepository` example

- Before and after Minimal API snippets that move from `DbContext` to Repository

- When the pattern pays off and when it is just noise

## What the Repository Pattern Actually Is

A repository represents a **collection of aggregate roots**.

Instead of sprinkling queries everywhere, you give your domain a small interface like:

```csharp
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> FindActiveForCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task RemoveAsync(Order order, CancellationToken cancellationToken = default);
}

```

Repository responsibilities:

- Hide queries and persistence details

- Expose methods that work in domain terms, not in SQL terms

- Provide a single, consistent access point for a given aggregate

Fowler places Repository in the object-relational patterns as a way to further isolate domain logic from data access. In practice, that means **domain code never sees `DbContext` or SQL at all**. It talks to repositories.

## The Order Aggregate: A Quick Domain Model

To make this concrete, start with a small `Order` aggregate.

```csharp
public enum OrderStatus
{
    Draft,
    Active,
    Cancelled,
    Completed
}

public class Order
{
    private readonly List<OrderLine> _lines = new();

    private Order(Guid id, Guid customerId)
    {
        Id = id;
        CustomerId = customerId;
        Status = OrderStatus.Draft;
    }

    public Guid Id { get; }
    public Guid CustomerId { get; }
    public OrderStatus Status { get; private set; }
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();
    public decimal TotalAmount => _lines.Sum(l => l.Total);

    public static Order Create(Guid customerId, IEnumerable<OrderLine> lines)
    {
        var order = new Order(Guid.NewGuid(), customerId);

        foreach (var line in lines)
        {
            order.AddLine(line.ProductId, line.Quantity, line.UnitPrice);
        }

        if (!order._lines.Any())
        {
            throw new InvalidOperationException("Order must have at least one line.");
        }

        order.Status = OrderStatus.Active;

        return order;
    }

    public void AddLine(Guid productId, int quantity, decimal unitPrice)
    {
        if (Status != OrderStatus.Draft && Status != OrderStatus.Active)
        {
            throw new InvalidOperationException("Can only change draft or active orders.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        _lines.Add(new OrderLine(productId, quantity, unitPrice));
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Completed)
        {
            throw new InvalidOperationException("Completed orders cannot be cancelled.");
        }

        Status = OrderStatus.Cancelled;
    }
}

public class OrderLine
{
    public OrderLine(Guid productId, int quantity, decimal unitPrice)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid ProductId { get; }
    public int Quantity { get; }
    public decimal UnitPrice { get; private set; }
    public decimal Total => Quantity * UnitPrice;
}

```

Note what is _not_ there:

- No `DbContext`

- No mapping attributes

- No SQL knowledge

`Order` cares about behavior. Persistence will live somewhere else.

## Before: Minimal API Endpoints With `DbContext` Everywhere

Here is a typical Minimal API endpoint that cancels an order directly using `AppDbContext`.

```csharp
app.MapPost("/orders/{id:guid}/cancel", async (
    Guid id,
    AppDbContext db,
    CancellationToken ct) =>
{
    var orderEntity = await db.Orders
        .Include(o => o.Lines)
        .SingleOrDefaultAsync(o => o.Id == id, ct);

    if (orderEntity is null)
    {
        return Results.NotFound();
    }

    if (orderEntity.Status == OrderStatus.Completed)
    {
        return Results.BadRequest("Completed orders cannot be cancelled.");
    }

    orderEntity.Status = OrderStatus.Cancelled;

    await db.SaveChangesAsync(ct);

    return Results.Ok(new { orderEntity.Id, orderEntity.Status });
});

```

A few problems:

- Endpoint knows which includes it needs

- Endpoint knows about status rules

- Endpoint is responsible for translating domain rules into property changes

- If the cancellation rule changes, you touch this endpoint and any other place that cancels orders

Now imagine you have:

- Another endpoint that cancels from an admin UI

- A background worker that cancels expired orders

Each one might be implementing its own variation of “how cancellation works.”

## After: Minimal API Endpoints Using `IOrderRepository`

Now introduce a repository and let the domain enforce its own rules.

First, the repository interface:

```csharp
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> FindActiveForCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task RemoveAsync(Order order, CancellationToken cancellationToken = default);
}

```

Then the endpoint was refactored to use it:

```csharp
app.MapPost("/orders/{id:guid}/cancel", async (
    Guid id,
    IOrderRepository orders,
    CancellationToken ct) =>
{
    var order = await orders.GetByIdAsync(id, ct);
    if (order is null)
    {
        return Results.NotFound();
    }

    try
    {
        order.Cancel();
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }

    await orders.AddAsync(order, ct); // in many setups, tracked aggregates are updated automatically
    return Results.Ok(new { order.Id, order.Status });
});

```

Now:

- The endpoint does not know how to query an order

- The endpoint does not encode the cancellation rule

- The endpoint calls `order.Cancel()`, and the domain decides whether that is allowed

Cancellation logic lives in one place: `Order.Cancel`.

You can reuse it from:

- Other endpoints

- A background worker

- A scheduled job

without duplicating rule logic.

## Implementing `IOrderRepository` With EF Core

You need a concrete implementation in infrastructure. There are two common styles:

- Domain entities are also EF Core entities

- Domain entities are separate from EF Core entities, and you map between them

To keep the example focused, assume `Order` and `OrderLine` are EF Core entities as shown.

```csharp
public class AppDbContext : DbContext
{
    public DbSet<Order> Orders => Set<Order>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var order = modelBuilder.Entity<Order>();
        order.HasKey(o => o.Id);
        order.Property(o => o.Status)
             .HasConversion<string>();

        var line = modelBuilder.Entity<OrderLine>();
        line.HasKey(l => new { l.ProductId, l.Quantity, l.UnitPrice });

        order.HasMany(typeof(OrderLine), "_lines");
    }
}

```

Repository implementation:

```csharp
public class EfCoreOrderRepository : IOrderRepository
{
    private readonly AppDbContext _dbContext;

    public EfCoreOrderRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .Include(o => o.Lines)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> FindActiveForCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .Include(o => o.Lines)
            .Where(o => o.CustomerId == customerId && o.Status == OrderStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        // This covers both new and tracked aggregates depending on how you attach them
        _dbContext.Orders.Update(order);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Order order, CancellationToken cancellationToken = default)
    {
        _dbContext.Orders.Remove(order);
        return Task.CompletedTask;
    }
}

```

Wire it up in `Program.cs`:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped<IOrderRepository, EfCoreOrderRepository>();

```

From this point forward, everything outside the infrastructure talks only to `IOrderRepository`.

## Before vs After: Querying Orders For A Customer

One more example: listing a customer's active orders.

### Before: Endpoint Owns The Query

```csharp
app.MapGet("/customers/{customerId:guid}/orders/active", async (
    Guid customerId,
    AppDbContext db,
    CancellationToken ct) =>
{
    var orders = await db.Orders
        .Include(o => o.Lines)
        .Where(o => o.CustomerId == customerId && o.Status == OrderStatus.Active)
        .Select(o => new
        {
            o.Id,
            o.Status,
            Total = o.Lines.Sum(l => l.Total)
        })
        .ToListAsync(ct);

    return Results.Ok(orders);
});

```

The endpoint knows:

- How to filter by status

- That lines should be included

- That total should be computed by summing line totals

Any other feature that needs “active orders for customer” will reinvent this.

### After: Endpoint Delegates To `FindActiveForCustomerAsync`

```csharp
app.MapGet("/customers/{customerId:guid}/orders/active", async (
    Guid customerId,
    IOrderRepository orders,
    CancellationToken ct) =>
{
    var activeOrders = await orders.FindActiveForCustomerAsync(customerId, ct);

    var response = activeOrders.Select(o => new
    {
        o.Id,
        o.Status,
        Total = o.TotalAmount
    });

    return Results.Ok(response);
});

```

Now the rules about what “active orders” mean, and how to load them efficiently, live in the repository. The endpoint just shapes the response.

If you later decide:

- Active should exclude certain edge cases

- Orders should always be loaded with a new related entity

- Performance requires changing `Include` strategy

you change it in one place.

## Repository vs Data Mapper vs EF Core

You may ask: if EF Core is already a mapper, why add repositories?

Think in layers:

- EF Core is a **Data Mapper** implementation. It maps objects to tables.

- A Repository is a **domain-facing facade** over that mapper. It hides EF specifics and speaks domain language.

Comparisons:

- Direct EF Core in the domain or endpoints
    - Quick to start
    
    - Strong coupling to ORM and database schema
    
    - Queries scattered everywhere

- Data Mapper only
    - Explicit mapping code between domain objects and records
    
    - Still no consistent collection-like abstraction

- Repository on top
    - Aggregates are retrieved and persisted through a single gateway
    
    - Domain and application services work in terms of `Order` and `Customer`, not `DbSet<Order>`

You can use EF Core directly in repository implementations. The repository makes sure the rest of your code does not care.

## When To Use The Repository Pattern

A repository is not a rule. It is a tradeoff. It pays off in specific situations.

### 1\. Central Aggregates Used Everywhere

If an aggregate like `Order` appears in:

- Checkout flows

- Customer dashboards

- Admin tools

- Background processes

then you want one definitive place that controls:

- How it is loaded

- Which relationships are included

- How archived or soft-deleted data is filtered

That is what a repository is for.

### 2\. You Want Consistent Access Rules

You may have cross-cutting rules such as:

- All queries should filter out soft-deleted records

- All active orders must eagerly load lines to avoid N+1 issues

Repositories let you express these once. Without them, you rely on every caller remembering to apply the same filters and includes.

### 3\. Multiple Stores Or Read Models

If there is any chance you will:

- Add a read-optimized store

- Introduce caching for certain aggregates

- Split reads and writes across different data sources

a repository interface gives you the seam you need. Implement it differently for different scenarios; the rest of the system continues to call the same methods.

### 4\. Rich Domain Model With Real Behavior

If you invested in domain objects that encapsulate rules, you want those objects to be loaded and persisted in a disciplined way. Repositories are the natural companion to a Domain Model and Service Layer.

## When Not To Use The Repository Pattern

Sometimes a repository is just noise.

### 1\. Tiny CRUD Applications

If your app:

- Has a handful of endpoints

- Mirrors the database schema directly

- Contains almost no business logic

then a full Repository layer might slow you down more than it helps. A thin service over `DbContext` or even direct endpoint queries can be acceptable.

### 2\. One-Off Tools And Scripts

For:

- Migration utilities

- Maintenance scripts

- Quick internal data tools

introducing repositories and interfaces often adds ceremony without long-term benefit. These tools are not your core domain.

### 3\. Heavy Reporting And Projections

Complex read queries that:

- Join across multiple bounded contexts

- Aggregate data for dashboards and analytics

may be better served by dedicated query handlers that return projections rather than aggregates. Forcing them through an aggregate repository can make designs confusing.

### 4\. Generic `IRepository<T>` Everywhere

The most common anti-pattern:

```csharp
public interface IRepository<T>
{
    Task<T?> GetByIdAsync(Guid id);
    Task AddAsync(T entity);
    Task RemoveAsync(T entity);
}

```

This encourages:

- Anemic domain models with generic operations only

- No domain-specific methods like `FindActiveForCustomerAsync`

- A false sense of abstraction without meaningful domain language

If you go with repositories, make them **specific to aggregates** and name methods in domain terms.

## Introducing Repositories Into An Existing App

You do not have to refactor everything.

A pragmatic path:

1. Pick one high-value aggregate, such as `Order`.

3. Design a repository interface that expresses what callers actually need, in domain terms.

5. Implement it once using your existing data access code.

7. Update a few key endpoints or services to depend on the repository instead of `DbContext`.

9. Observe how much duplication and cognitive load disappears around that aggregate.

If it feels cleaner and safer, continue with other aggregates. If it feels like ceremony for your situation, stop there.

## Closing Thoughts

A repository is not a magical pattern. It is a disciplined way to say:

- “All access to this aggregate goes through this one door.”

In ASP.NET Core Minimal APIs, that single decision shifts your structure:

- Endpoints become thinner, focused on HTTP concerns

- Domain objects hold rules and behavior

- Repositories become the single source of truth for how aggregates are loaded and persisted

The next time you reach for `DbContext` inside an endpoint or domain service, ask yourself a simple question:

_Should this code really know how orders are stored, or should it just ask for an `Order` and let a repository handle the rest?_
