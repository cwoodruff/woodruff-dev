---
title: "Enterprise Patterns for ASP.NET Core Minimal API: Domain Model Pattern - When Your Core Rules Deserve Their Own Gravity"
date: 2025-12-01
categories: 
  - "patterns"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "patterns"
  - "programming"
coverImage: "./images/Domain-Model-Pattern-When-Your-Core-Rules-Deserve-Their-Own-Gravity.png"
---

Look at a typical enterprise ASP.NET Core application, and you often see the same pattern:

- Controllers validating requests, calculating totals, and applying discounts

- EF Core entities that are little more than property bags

- Stored procedures that quietly decide which orders are valid

If you need to know how orders work, you do not open a single file. You read controllers, queries, and database scripts until your eyes blur. The truth about the business lives everywhere and nowhere.

The Domain Model is the pattern that reverses this arrangement.

Instead of clever controllers and dumb entities, you move the rules into rich objects. Entities and value objects enforce invariants. The application layer orchestrates use cases by telling those objects what to do.

This post shows what that looks like in C#, and why putting rules next to data changes how your system behaves over time.

## What Domain Model Really Is

In Fowler’s terms, a Domain Model:

- Represents the business domain with rich objects

- Encapsulates rules and invariants inside those objects

- Treats the framework, database, and transport as details at the edges

In practical .NET terms:

- Your `Order` type knows what a valid order looks like

- Your `Customer` type knows whether it is eligible for a specific feature

- Controllers, message handlers, or background jobs call methods on those types

What it is not:

- It is not simply having classes called `Order` and `Customer` with auto properties

- It is not pushing every rule into a single God object

- It is not a diagram alone, while the code keeps all the rules in the controllers

The whole point is to make the rules you care about first-class citizens in your code.

## A Concrete Domain Model Slice

Here is a small, but real, `Order` aggregate with `OrderLine` in C#.

```csharp
public class Order
{
    private readonly List<OrderLine> _lines = new();

    private Order(Guid customerId)
    {
        Id = Guid.NewGuid();
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
        var order = new Order(customerId);

        foreach (var line in lines)
        {
            order.AddLine(line.ProductId, line.Quantity, line.UnitPrice);
        }

        if (!order._lines.Any())
        {
            throw new InvalidOperationException("Order must have at least one line.");
        }

        return order;
    }

    public void AddLine(Guid productId, int quantity, decimal unitPrice)
    {
        if (Status != OrderStatus.Draft)
        {
            throw new InvalidOperationException("Cannot change a non draft order.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (unitPrice <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice));
        }

        _lines.Add(new OrderLine(productId, quantity, unitPrice));
    }

    public void ApplyDiscount(decimal percent)
    {
        if (percent <= 0 || percent >= 50)
        {
            throw new ArgumentOutOfRangeException(nameof(percent));
        }

        foreach (var line in _lines)
        {
            line.ApplyDiscount(percent);
        }
    }

    public void Submit()
    {
        if (Status != OrderStatus.Draft)
        {
            throw new InvalidOperationException("Only draft orders can be submitted.");
        }

        if (!_lines.Any())
        {
            throw new InvalidOperationException("Cannot submit an empty order.");
        }

        Status = OrderStatus.Submitted;
    }
}

public class OrderLine
{
    public OrderLine(Guid productId, int quantity, decimal unitPrice)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(unitPrice);

        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid ProductId { get; }
    public int Quantity { get; }
    public decimal UnitPrice { get; private set; }
    public decimal Total => Quantity * UnitPrice;

    public void ApplyDiscount(decimal percent)
    {
        UnitPrice = UnitPrice * (1 - percent / 100m);
    }
}

public enum OrderStatus
{
    Draft = 0,
    Submitted = 1,
    Cancelled = 2
}

```

Notice what is happening here:

- Creation is controlled through `Order.Create`, not through `new Order()` scattered everywhere

- `Order` refuses to exist without at least one `OrderLine`

- `AddLine` and `ApplyDiscount` validate arguments and enforce state transitions

- `Submit` enforces that only draft orders are submitted and that empty orders are invalid

The rules live with the data. You no longer need to remember, in every controller, how discounts work or when an order may be modified.

## Before Domain Model: Controller As Decision Maker

Most enterprise apps start closer to this shape:

```csharp
app.MapPost("/orders", async (CreateOrderDto dto, AppDbContext db) =>
{
    if (dto.Lines is null || dto.Lines.Count == 0)
    {
        return Results.BadRequest("Order must have at least one line.");
    }

    var orderEntity = new OrderEntity
    {
        Id = Guid.NewGuid(),
        CustomerId = dto.CustomerId,
        Status = "Draft",
        CreatedAt = DateTime.UtcNow
    };

    foreach (var lineDto in dto.Lines)
    {
        if (lineDto.Quantity <= 0)
        {
            return Results.BadRequest("Quantity must be positive.");
        }

        orderEntity.Lines.Add(new OrderLineEntity
        {
            ProductId = lineDto.ProductId,
            Quantity = lineDto.Quantity,
            UnitPrice = lineDto.UnitPrice
        });
    }

    db.Orders.Add(orderEntity);
    await db.SaveChangesAsync();

    return Results.Created($"/orders/{orderEntity.Id}", new { orderEntity.Id });
});

```

And later, somewhere else, a discount endpoint:

```csharp
app.MapPost("/orders/{id:guid}/discounts", async (
    Guid id,
    ApplyDiscountDto dto,
    AppDbContext db) =>
{
    var order = await db.Orders
        .Include(o => o.Lines)
        .SingleOrDefaultAsync(o => o.Id == id);

    if (order == null)
    {
        return Results.NotFound();
    }

    if (order.Status != "Draft")
    {
        return Results.BadRequest("Cannot change a non draft order.");
    }

    if (!order.Lines.Any())
    {
        return Results.BadRequest("Cannot discount an empty order.");
    }

    if (dto.Percent <= 0 || dto.Percent >= 50)
    {
        return Results.BadRequest("Discount percent out of range.");
    }

    foreach (var line in order.Lines)
    {
        line.UnitPrice = line.UnitPrice * (1 - dto.Percent / 100m);
    }

    await db.SaveChangesAsync();

    return Results.Ok(new { order.Id });
});

```

The same rules are repeated in different forms:

- Draft status checks

- Non-empty order checks

- Discount percent range checks

- Positive quantity rules

The code works until a new rule arrives and someone updates one endpoint but misses the others.

## After Domain Model: Controller As Orchestrator

Now see how the controller changes when you let the domain model handle behavior.

Assume you already use the `Order` aggregate from earlier and have a repository.

```csharp
public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

```

### Creating an Order

```csharp
app.MapPost("/orders", async (
    CreateOrderDto dto,
    IOrderRepository orders,
    CancellationToken ct) =>
{
    var lines = dto.Lines.Select(l =>
        new OrderLine(l.ProductId, l.Quantity, l.UnitPrice));

    Order order;

    try
    {
        order = Order.Create(dto.CustomerId, lines);
    }
    catch (Exception ex) when (ex is ArgumentOutOfRangeException || ex is InvalidOperationException)
    {
        return Results.BadRequest(ex.Message);
    }

    await orders.AddAsync(order, ct);

    return Results.Created($"/orders/{order.Id}", new { order.Id });
});

public record CreateOrderDto(
    Guid CustomerId,
    List<CreateOrderLineDto> Lines);

public record CreateOrderLineDto(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice);

```

The endpoint now:

- Translates input DTOs into domain `OrderLine` objects

- Delegates invariant to `Order.Create` and `OrderLine` constructors

- Catches domain exceptions and maps them to HTTP responses

The logic that defines a valid order lives inside `Order`, not inside the endpoint.

### Applying a Discount

```csharp
app.MapPost("/orders/{id:guid}/discounts", async (
    Guid id,
    ApplyDiscountDto dto,
    IOrderRepository orders,
    CancellationToken ct) =>
{
    var order = await orders.GetByIdAsync(id, ct);

    if (order == null)
    {
        return Results.NotFound();
    }

    try
    {
        order.ApplyDiscount(dto.Percent);
    }
    catch (ArgumentOutOfRangeException ex)
    {
        return Results.BadRequest(ex.Message);
    }

    await orders.AddAsync(order, ct); // or SaveChanges via Unit of Work

    return Results.Ok(new { order.Id, order.TotalAmount });
});

public record ApplyDiscountDto(decimal Percent);

```

The discount rule is expressed once, in the domain model:

```csharp
public void ApplyDiscount(decimal percent)
{
    if (percent <= 0 || percent >= 50)
    {
        throw new ArgumentOutOfRangeException(nameof(percent));
    }

    foreach (var line in _lines)
    {
        line.ApplyDiscount(percent);
    }
}

```

Controllers have one job:

- Load the aggregate

- Tell it what to do

- Persist the result

- Translate domain errors to responses

That is the essence of Domain Model in a web app.

## Why Putting Rules Next To Data Matters

Shifting behavior into domain objects does more than make code “cleaner”. It changes several properties of your system.

### One Place To Ask “What Is The Rule”

If a product owner asks:

> What exactly are the conditions for applying a discount?

You can answer by opening `Order.ApplyDiscount` and related collaborators. There is no tour of controllers, repositories, and stored procedures.

### Transport Independence

Imagine you want a background service that runs a nightly promotion:

- It reads eligible orders from the database

- It applies a discount to each

- It sends confirmation emails

With a Domain Model, this worker calls the same `ApplyDiscount` method that your HTTP endpoint uses. If you switch to messaging or add a gRPC API, they all reuse the same behavior.

### Stronger, Cheaper Tests

You can write unit tests directly against `Order`:

```csharp
[Fact]
public void ApplyDiscount_Throws_WhenPercentOutOfRange()
{
    var order = Order.Create(
        Guid.NewGuid(),
        new[] { new OrderLine(Guid.NewGuid(), 1, 100m) });

    Assert.Throws<ArgumentOutOfRangeException>(() => order.ApplyDiscount(0));
    Assert.Throws<ArgumentOutOfRangeException>(() => order.ApplyDiscount(60));
}

[Fact]
public void Submit_SetsStatusToSubmitted_WhenDraftAndHasLines()
{
    var order = Order.Create(
        Guid.NewGuid(),
        new[] { new OrderLine(Guid.NewGuid(), 1, 100m) });

    order.Submit();

    Assert.Equal(OrderStatus.Submitted, order.Status);
}

```

No test server, no HTTP, no database. You can exhaustively test the behavior that matters while keeping integration tests focused on wiring.

## Integrating Domain Model With Application And Infrastructure

Domain Model does not live alone. It cooperates with:

- An application layer that coordinates use cases

- An infrastructure layer that persists, aggregates, and talks to external systems

A typical setup in .NET:

- `MyApp.Domain`
    - Entities, value objects, domain services and interfaces for repositories

- `MyApp.Application`
    - Application services that orchestrate commands and queries

- `MyApp.Infrastructure`
    - EF Core mappings, repository implementations, unit of work

- `MyApp.Web`
    - Controllers or minimal APIs that call application services

Example application service using `Order`:

```csharp
public interface IOrderApplicationService
{
    Task<Guid> CreateOrderAsync(CreateOrderCommand command, CancellationToken ct = default);
}

public class OrderApplicationService : IOrderApplicationService
{
    private readonly IOrderRepository _orders;

    public OrderApplicationService(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<Guid> CreateOrderAsync(CreateOrderCommand command, CancellationToken ct = default)
    {
        var lines = command.Lines.Select(l =>
            new OrderLine(l.ProductId, l.Quantity, l.UnitPrice));

        var order = Order.Create(command.CustomerId, lines);

        await _orders.AddAsync(order, ct);

        return order.Id;
    }
}

public record CreateOrderCommand(
    Guid CustomerId,
    IReadOnlyCollection<CreateOrderLineCommand> Lines);

public record CreateOrderLineCommand(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice);

```

Controllers or endpoints call `IOrderApplicationService`, not `Order` directly. That keeps HTTP details and use case orchestration together, while the domain model stays focused on rules.

## Signs You Are Pretending To Have A Domain Model

Many teams say, “We are doing DDD,” while their code tells a different story. Look for these patterns.

- Entities with only auto properties and no behavior

- Controllers or handlers performing status transitions and complex validations

- Stored procedures implementing key rules, such as discount criteria or eligibility

- Domain types that depend directly on `DbContext` or `HttpContext`

If any of those describe your system, you have building blocks for a domain model, not an actual model.

## First Steps Toward A Real Domain Model

You do not need a significant rewrite. Start small.

1. **Pick one important concept**  
    Order, Subscription, Invoice, or any aggregate that matters to the business.

3. **Move a single rule into that entity**  
    For example, “order must have at least one line” or “cannot modify submitted orders”.

5. **Expose behavior, not just state**  
    Add methods like `AddLine`, `ApplyDiscount`, `Submit`, instead of letting the outside world mutate collections directly.

7. **Write tests against the entity**  
    Prove that the rules hold even when no controller or database is involved.

9. **Refactor controllers to call the domain model**  
    Remove duplicated checks, catch domain exceptions, map them to HTTP responses.

Repeat that in the parts of the system that hurt the most. Over time, the gravity of the domain model grows, and the framework falls into its proper role as plumbing.

If your core rules are worth money, they are worth a real home in your code. Treat them as the main asset, not as an afterthought squeezed into controllers and stored procedures.
