---
title: "Enterprise Patterns for ASP.NET Core Minimal API: Data Transfer Object Pattern"
date: 2026-01-19
categories: 
  - "patterns"
coverImage: "Enterprise-Patterns-for-ASP.NET-Core-Minimal-API-Data-Transfer-Object-Pattern.png"
---

Your domain model exists to protect your business rules.  
Your API exists to protect your clients.

When you expose EF Core entities directly from your ASP.NET Core endpoints, you throw both of those protections away.

The Data Transfer Object Pattern (DTO) is the line in the sand.

DTOs carry data across boundaries. They flatten and optimize your internal objects for remote calls. They let your domain change without renegotiating contracts with every consumer.

In this post, you will see:

- What DTOs really are in .NET

- How they differ from domain and persistence models

- Before and after Minimal API examples

- Practical mapping patterns in C#

- When DTOs are essential and when they are just ceremony

## Your Domain Is Not An API Contract

A very familiar anti-pattern looks like this.

```csharp
app.MapGet("/orders/{id:guid}", async (
    Guid id,
    AppDbContext db,
    CancellationToken ct) =>
{
    var order = await db.Orders
        .Include(o => o.Lines)
        .SingleOrDefaultAsync(o => o.Id == id, ct);

    if (order is null)
    {
        return Results.NotFound();
    }

    // Directly returning an EF Core entity
    return Results.Ok(order);
});
```

It feels efficient:

- No extra classes

- No mapping code

- One entity in, one entity out

It also quietly locks your API to your internal model:

- Renaming a property on `Order` changes your public JSON

- Splitting `Order` into `Order` plus `OrderHeader` breaks clients

- Adding a navigation property leaks internal relationships into the contract

You did not design a contract. You simply let your persistence model walk out the door.

DTOs exist to fix that.

## What A DTO Actually Is

A Data Transfer Object is boring on purpose.

- It carries data across process boundaries like HTTP, gRPC, or queues

- It contains no domain behavior and no persistence logic

- It is shaped around consumer needs and contract stability

In .NET, DTOs are simple classes or records. Here is a typical order DTO.

```csharp
public record OrderDto(
    Guid Id,
    Guid CustomerId,
    decimal TotalAmount,
    string Status,
    IReadOnlyCollection<OrderLineDto> Lines);

public record OrderLineDto(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice);

```

DTOs are not your domain model, and they are not your EF entities. They are the shape you promise to the outside world.

## Domain Model vs Persistence Model vs DTO

It helps to keep three different models in your head.

### Domain model

This is your business.

```csharp
public enum OrderStatus
{
    Draft,
    Confirmed,
    Shipped,
    Cancelled
}

public sealed class Order
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

    public static Order Create(Guid customerId, IEnumerable<(Guid ProductId, int Quantity, decimal UnitPrice)> lines)
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

        order.Status = OrderStatus.Confirmed;
        return order;
    }

    public void AddLine(Guid productId, int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (Status != OrderStatus.Draft && Status != OrderStatus.Confirmed)
        {
            throw new InvalidOperationException("Cannot add lines to a non editable order.");
        }

        _lines.Add(new OrderLine(productId, quantity, unitPrice));
    }
}

public sealed class OrderLine
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

This model enforces invariants. It cares about business rules, not JSON.

### Persistence model

Sometimes your EF entities are the domain. Sometimes, they mainly reflect database constraints.

```csharp
public sealed class OrderEntity
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = default!;
    public decimal TotalAmount { get; set; }

    public List<OrderLineEntity> Lines { get; set; } = new();
}

public sealed class OrderLineEntity
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

```

These types are aware of the database schema and relationships. They do not have to be identical to your domain model.

### DTO model

This is the external contract.

```csharp
public record OrderSummaryDto(
    Guid Id,
    decimal TotalAmount,
    string Status);

public record OrderDetailsDto(
    Guid Id,
    Guid CustomerId,
    decimal TotalAmount,
    string Status,
    IReadOnlyCollection<OrderLineDto> Lines);

```

The DTO model exists so that the domain and persistence can change while the contract stays stable.

## Mapping Between Domain And DTOs

You need mapping code. That is the price of decoupling.

### Domain to DTO

```csharp
public static class OrderDtoMapping
{
    extension(Order order)
    {
        public OrderSummaryDto ToSummaryDto() =>
            new(order.Id, order.TotalAmount, order.Status.ToString());

        public OrderDetailsDto ToDetailsDto() =>
            new(
                order.Id,
                order.CustomerId,
                order.TotalAmount,
                order.Status.ToString(),
                order.Lines
                    .Select(l => new OrderLineDto(l.ProductId, l.Quantity, l.UnitPrice))
                    .ToList());
    }
}
```

### DTO to domain command

For writes, convert inbound DTOs into command objects.

```csharp
public sealed record CreateOrderRequest(
    Guid CustomerId,
    IReadOnlyCollection<CreateOrderLineRequest> Lines);

public sealed record CreateOrderLineRequest(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice);

public sealed record PlaceOrderCommand(
    Guid CustomerId,
    IReadOnlyCollection<PlaceOrderLine> Lines);

public sealed record PlaceOrderLine(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice);

public static class CreateOrderCommandMapping
{
    public static PlaceOrderCommand ToCommand(this CreateOrderRequest request) =>
        new(
            request.CustomerId,
            request.Lines.Select(l =>
                new PlaceOrderLine(l.ProductId, l.Quantity, l.UnitPrice)).ToList());
}

```

The application service now talks in domain terms, not HTTP terms.

## Before vs After: Exposing Entities vs Using DTOs

### Before: returning EF Core entities directly

```csharp
app.MapGet("/orders/{id:guid}", async (
    Guid id,
    AppDbContext db,
    CancellationToken ct) =>
{
    var entity = await db.Orders
        .Include(o => o.Lines)
        .SingleOrDefaultAsync(o => o.Id == id, ct);

    if (entity is null)
    {
        return Results.NotFound();
    }

    // Public JSON equals internal EF Core model
    return Results.Ok(entity);
});

```

Tightly coupled behavior:

- JSON structure equals table structure

- Any schema change becomes an API change

- You often send more data than the client needs

### After: clean DTO response via application service

First, an application service that exposes a query method.

```csharp
public interface IOrderQueryService
{
    Task<OrderDetailsDto?> GetDetailsAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class OrderQueryService(AppDbContext dbContext) : IOrderQueryService
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<OrderDetailsDto?> GetDetailsAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Orders
            .Include(o => o.Lines)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        // Map EF entity to domain, or map directly to DTO if you treat entity as domain
        var order = new OrderSnapshot(
            entity.Id,
            entity.CustomerId,
            Enum.Parse<OrderStatus>(entity.Status),
            entity.Lines.Select(l =>
                new OrderLineSnapshot(l.ProductId, l.Quantity, l.UnitPrice)).ToList());

        return order.ToDetailsDto();
    }
}

// A simple read-only domain snapshot
public sealed record OrderLineSnapshot(Guid ProductId, int Quantity, decimal UnitPrice);

public sealed class OrderSnapshot(
    Guid id,
    Guid customerId,
    OrderStatus status,
    IReadOnlyCollection<OrderLineSnapshot> lines)
{
    public Guid Id { get; } = id;
    public Guid CustomerId { get; } = customerId;
    public OrderStatus Status { get; } = status;
    public IReadOnlyCollection<OrderLineSnapshot> Lines => lines;
    public decimal TotalAmount => lines.Sum(l => l.Quantity * l.UnitPrice);
}

public static class OrderSnapshotDtoMapping
{
    public static OrderDetailsDto ToDetailsDto(this OrderSnapshot order) =>
        new(
            order.Id,
            order.CustomerId,
            order.TotalAmount,
            order.Status.ToString(),
            order.Lines
                .Select(l => new OrderLineDto(l.ProductId, l.Quantity, l.UnitPrice))
                .ToList());
}

```

Then the Minimal API endpoint becomes:

```csharp
app.MapGet("/orders/{id:guid}", async (
    Guid id,
    IOrderQueryService orders,
    CancellationToken ct) =>
{
    var dto = await orders.GetDetailsAsync(id, ct);
    if (dto is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(dto);
});

```

Same endpoint, different attitude:

- Contract is explicit

- Internals can be refactored

- Storage strategy is invisible to clients

## DTOs For Writes: Keeping Input Honest

The same problem appears in the other direction when you bind requests directly onto entities.

### Before: binding request body to EF Core entity

```csharp
app.MapPost("/orders", async (
    AppDbContext db,
    OrderEntity request,
    CancellationToken ct) =>
{
    // Request body is bound directly to EF entity
    db.Orders.Add(request);
    await db.SaveChangesAsync(ct);

    return Results.Created($"/orders/{request.Id}", new { request.Id });
});

```

Risks:

- Client can set fields that should be server controlled

- Client can accidentally or intentionally break invariants

- EF entity ends up full of fields that exist only for API reasons

### After: inbound DTO plus application service

Define a clean request DTO.

```
public sealed record CreateOrderRequest(
    Guid CustomerId,
    IReadOnlyCollection<CreateOrderLineRequest> Lines);

public sealed record CreateOrderLineRequest(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice);

```

Command and service:

```csharp
public sealed record PlaceOrderCommand(
    Guid CustomerId,
    IReadOnlyCollection<PlaceOrderLine> Lines);

public sealed record PlaceOrderLine(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice);

public interface IOrderApplicationService
{
    Task<Guid> PlaceOrderAsync(PlaceOrderCommand command, CancellationToken cancellationToken);
}

public sealed class OrderApplicationService(IOrderRepository orders, IUnitOfWork unitOfWork) : IOrderApplicationService
{
    private readonly IOrderRepository _orders = orders;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Guid> PlaceOrderAsync(
        PlaceOrderCommand command,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginAsync(cancellationToken);

        try
        {
            var lines = command.Lines
                .Select(l => (l.ProductId, l.Quantity, l.UnitPrice))
                .ToList();

            var order = Order.Create(command.CustomerId, lines);

            await _orders.AddAsync(order, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return order.Id;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

Mapping from request to command:

```csharp
public static class CreateOrderRequestMapping
{
    public static PlaceOrderCommand ToCommand(this CreateOrderRequest request) =>
        new(
            request.CustomerId,
            request.Lines.Select(l =>
                new PlaceOrderLine(l.ProductId, l.Quantity, l.UnitPrice)).ToList());
}

```

Minimal API endpoint now:

```csharp
app.MapPost("/orders", async (
    CreateOrderRequest request,
    IOrderApplicationService service,
    CancellationToken ct) =>
{
    if (request.Lines == null || !request.Lines.Any())
    {
        return Results.BadRequest("Order must contain at least one line.");
    }

    var command = request.ToCommand();
    var orderId = await service.PlaceOrderAsync(command, ct);

    return Results.Created($"/orders/{orderId}", new { Id = orderId });
});

```

The boundary is explicit:

- HTTP requests talk in DTOs

- The application service talks in commands and domain types

- Persistence and invariants live behind the DTOs

## When To Use DTOs

DTOs are not optional when any of these are true.

### Public APIs and external clients

- Third parties integrate with your service

- Mobile apps consume your endpoints

- You cannot redeploy clients at will

You need a contract that can outlive your current entity design.

### Cross-service messaging

- Commands and events on queues

- Integration between services or bounded contexts

DTOs become message contracts:

- Versioned

- Documented

- Independent of internal model changes

### Security and data minimization

You want to avoid:

- Leaking internal foreign keys

- Exposing audit data or internal flags

- Accepting fields from clients that they should never control

DTOs let you design exactly what travels across the boundary.

### Performance and payload design

Clients may need:

- A light summary for list pages

- A heavy detail view for a specific screen

- A specialized projection for reporting

Different DTOs for each scenario keep payloads focused and efficient.

## When DTOs Are Just Noise

DTOs are not sacred. Sometimes they are an unnecessary ceremony.

### Small, internal tools

- An internal admin API used by one team

- Short-lived services

- Low risk if the shape changes

You may accept using entities directly while the system is experimental, as long as you know this is a temporary trade.

### Simple CRUD that will not evolve much

- Tables that map almost exactly to the external data representation

- Very stable models with low likelihood of change

In that case, DTOs that simply copy every property can be redundant.

### DTOs that mirror entities blindly

If your DTOs are literally:

```csharp
public class OrderDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public List<OrderLine> Lines { get; set; } = new();
}

```

you are not gaining much. The pattern is not about duplicating classes. It is about designing contracts.

* * *

## DTOs, Commands, Queries, And CQRS

DTOs fit naturally into a CQRS style.

- Input DTOs represent HTTP request payloads

- Commands represent intent in domain language

- Query DTOs represent read models

A common setup:

- `CreateOrderRequest` (input DTO) maps to `PlaceOrderCommand`

- Command handler uses domain entities and repositories

- Domain events may produce `OrderCreatedEventDto` for messaging

- Read endpoints return `OrderSummaryDto` or `OrderDetailsDto` built from read models

Each step has a clear purpose.

## Practical Guidelines For DTO Design

A few rules that keep DTO usage sane.

1. Keep DTOs flat and focused
    - Flatten nested data where appropriate
    
    - Avoid reflecting deep object graphs unless the client really needs them

3. Separate input and output DTOs
    - Do not reuse the same DTO for create, update, and read
    
    - Avoid exposing fields that clients should not control

5. Make mapping explicit
    - Use extension methods or dedicated mapping classes
    
    - Avoid magic reflection-based mapping without tests

7. Validate at the DTO layer
    - Use data annotations or FluentValidation on DTOs
    
    - Keep entity invariants inside the domain, not in controllers

## Introducing DTOs Into An Existing ASP.NET Core App

If your current app leaks entities everywhere, you can still fix it incrementally.

1. Find one endpoint that exposes entities
    - Ideally, one that external clients rely on

3. Design DTOs for that endpoint
    - Based on what consumers actually use
    
    - Strip out internal fields and relationships

5. Add mapping
    - Domain to DTO for responses
    
    - DTO to command or domain input for requests

7. Swap the endpoint to use DTOs
    - Keep the behavior the same
    
    - Keep tests green

9. Repeat for other external endpoints
    - Prioritize the ones that are public and hard to change

Bit by bit, your EF entities stop being your API.

## Closing Thought

Every endpoint in your ASP.NET Core application answers one question:

Are you talking to your own code, or are you talking to every client and system that depends on you?

DTOs are the pattern that admits that difference.

Behind the DTO boundary, you can refactor aggressively.  
In front of it, you owe your callers stability.

If you return entities directly today, pick one critical endpoint and move it to DTOs. After that exercise, you will have a much harder time pretending that your persistence model and your contract are the same thing.
