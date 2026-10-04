---
title: "Enterprise Patterns for ASP.NET Core Minimal API: Service Layer Pattern - Making HTTP a Client, Not the Boss"
date: 2025-12-02
categories: 
  - "patterns"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "patterns"
  - "programming"
coverImage: "./images/Service-Layer-Making-HTTP-a-Client-Not-the-Boss.png"
---

Open a typical ASP.NET Core project, and you will often see the same shape:

- Controllers that validate input, construct entities and call several repositories

- Direct calls to external services (payments, credit, email) from controller actions

- Transactions managed in random places with `SaveChangesAsync` or manual transaction scopes

If you have ever tried to add a second client (a background worker, a message handler, or a gRPC API), you probably copied a large chunk of controller logic and hoped no one noticed.

The **Service Layer** pattern exists to stop that.

Instead of letting controllers improvise business workflows, you define **application services** that expose operations like `PlaceOrder`, `CancelOrder`, `ApproveRefund`. Every client calls those services. Controllers become thin, infrastructure becomes a detail, and the domain model stops depending on HTTP.

In this post, you will see:

- What the Service Layer Pattern really is in practical .NET terms

- A “before vs after” comparison of fat endpoints vs thin endpoints using a service layer

- A complete C# example: `IOrderApplicationService` and `OrderApplicationService`

- How the Service Layer Pattern sits between controllers, domain model and infrastructure

## What the Service Layer Pattern Actually Is

In Fowler’s description, the **Service Layer** Pattern:

- Defines a boundary for your application’s operations

- Encapsulates business workflows as methods on application services

- Coordinates domain objects, transactions, and external systems

- Exposes a consistent API to any client (web, messaging, scheduled jobs, etc.)

In a .NET application, that typically means:

- **Interfaces** like `IOrderApplicationService` in an application layer project

- **Implementation classes** that use domain models, repositories, credit services, and unit-of-work components

- **Controllers / minimal APIs** that depend on these interfaces, not on `DbContext` or domain objects directly

The key idea: **HTTP is just another client**. It does not own the business process.

## Before Service Layer: Controllers Doing Everything

Let’s start with a familiar “before” example: placing an order with a credit check in a Minimal API endpoint.

```csharp
app.MapPost("/orders/place", async (
    PlaceOrderRequest dto,
    AppDbContext db,
    ICreditGateway creditGateway,
    CancellationToken ct) =>
{
    // Validate request
    if (dto.Lines is null || dto.Lines.Count == 0)
    {
        return Results.BadRequest("Order must contain at least one line item.");
    }

    // Build domain entity directly in the endpoint
    var order = new OrderEntity
    {
        Id = Guid.NewGuid(),
        CustomerId = dto.CustomerId,
        Status = "Draft",
        CreatedAt = DateTime.UtcNow,
        Lines = dto.Lines.Select(l => new OrderLineEntity
        {
            ProductId = l.ProductId,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice
        }).ToList()
    };

    if (!order.Lines.Any())
    {
        return Results.BadRequest("Order must contain at least one line item.");
    }

    var totalAmount = order.Lines.Sum(l => l.Quantity * l.UnitPrice);

    // Call external credit service
    var approved = await creditGateway.ApproveAsync(
        dto.CustomerId,
        totalAmount,
        ct);

    if (!approved)
    {
        return Results.BadRequest("Credit check failed.");
    }

    // Save using DbContext directly
    await using var transaction = await db.Database.BeginTransactionAsync(ct);

    db.Orders.Add(order);
    await db.SaveChangesAsync(ct);

    await transaction.CommitAsync(ct);

    // Build HTTP response
    return Results.Created($"/orders/{order.Id}", new { order.Id, totalAmount });
});
```

This endpoint:

- Validates the request

- Builds an order object

- Calculates totals

- Calls the credit service

- Manages a transaction

- Persists with `DbContext`

- Returns HTTP responses

Now imagine you want:

- A background worker that places orders from a message queue

- A gRPC method that performs the same operation

- A scheduled job that replays failed orders

You either duplicate this workflow or you create awkward “helper” classes that are really an informal service layer with no clear interface.

## After Service Layer: Controllers as Thin Clients

Now we introduce a **Service Layer**.

### Step 1: Define the application contract

```csharp
public sealed class PlaceOrderRequest
{
    public Guid CustomerId { get; set; }
    public List<PlaceOrderLineRequest> Lines { get; set; } = new();
}

public sealed class PlaceOrderLineRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public interface IOrderApplicationService
{
    Task<Guid> PlaceOrderAsync(
        PlaceOrderRequest request,
        CancellationToken cancellationToken = default);
}

```

This interface lives in an **Application** project. It knows nothing about HTTP or EF Core.

### Step 2: Implement the service, orchestrating the domain and infrastructure

Assume we have:

- A domain model `Order` and `OrderLine`

- An `IOrderRepository` for persistence

- An `ICustomerCreditService` abstraction for credit checks

- An `IUnitOfWork` abstraction for transaction boundaries

```csharp
public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
}

public interface ICustomerCreditService
{
    Task<bool> ApproveAsync(
        Guid customerId,
        decimal amount,
        CancellationToken cancellationToken = default);
}

public interface IUnitOfWork
{
    Task BeginAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

```

Service implementation:

```csharp
public class OrderApplicationService : IOrderApplicationService
{
    private readonly IOrderRepository _orders;
    private readonly ICustomerCreditService _creditService;
    private readonly IUnitOfWork _unitOfWork;

    public OrderApplicationService(
        IOrderRepository orders,
        ICustomerCreditService creditService,
        IUnitOfWork unitOfWork)
    {
        _orders = orders;
        _creditService = creditService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> PlaceOrderAsync(
        PlaceOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginAsync(cancellationToken);

        try
        {
            // Construct the domain aggregate
            var lines = request.Lines.Select(l =>
                new OrderLine(l.ProductId, l.Quantity, l.UnitPrice));

            var order = Order.Create(request.CustomerId, lines);

            // External credit check
            var approved = await _creditService.ApproveAsync(
                request.CustomerId,
                order.TotalAmount,
                cancellationToken);

            if (!approved)
            {
                throw new InvalidOperationException("Credit check failed.");
            }

            // Persist via repository
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

Here, **all the orchestration lives in one place**:

- Domain aggregate creation

- Credit checks

- Transaction boundaries

- Persistence

And none of it knows about HTTP.

### Step 3: Thin Minimal API endpoint

Now the endpoint shrinks dramatically.

```csharp
app.MapPost("/orders/place", async (
    PlaceOrderRequest dto,
    IOrderApplicationService service,
    CancellationToken ct) =>
{
    try
    {
        var id = await service.PlaceOrderAsync(dto, ct);
        return Results.Created($"/orders/{id}", new { Id = id });
    }
    catch (InvalidOperationException ex) when (ex.Message == "Credit check failed.")
    {
        return Results.BadRequest(ex.Message);
    }
});

```

The endpoint:

- Accepts `PlaceOrderRequest` from the request body

- Calls a single method on `IOrderApplicationService`

- Translates known domain/application exceptions to HTTP status codes

Everything else is the application’s job, not HTTP’s job.

## Before vs After: An MVC Controller Example

If you prefer MVC controllers, the difference is similar.

### Before: fat controller action

```csharp
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICreditGateway _creditGateway;

    public OrdersController(AppDbContext db, ICreditGateway creditGateway)
    {
        _db = db;
        _creditGateway = creditGateway;
    }

    [HttpPost("place")]
    public async Task<IActionResult> Place([FromBody] PlaceOrderRequest dto, CancellationToken ct)
    {
        if (dto.Lines is null || dto.Lines.Count == 0)
        {
            return BadRequest("Order must contain at least one line item.");
        }

        var order = new OrderEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = dto.CustomerId,
            Status = "Draft",
            CreatedAt = DateTime.UtcNow,
            Lines = dto.Lines.Select(l => new OrderLineEntity
            {
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice
            }).ToList()
        };

        var total = order.Lines.Sum(l => l.Quantity * l.UnitPrice);

        var approved = await _creditGateway.ApproveAsync(dto.CustomerId, total, ct);

        if (!approved)
        {
            return BadRequest("Credit check failed.");
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, new { order.Id });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var order = await _db.Orders.FindAsync(new object[] { id }, ct);
        if (order == null) return NotFound();
        return Ok(order);
    }
}

```

### After: controller delegates to the Service Layer

```csharp
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderApplicationService _service;

    public OrdersController(IOrderApplicationService service)
    {
        _service = service;
    }

    [HttpPost("place")]
    public async Task<IActionResult> Place([FromBody] PlaceOrderRequest dto, CancellationToken ct)
    {
        try
        {
            var id = await _service.PlaceOrderAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Credit check failed.")
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        // this might still use a query service or repository
        // the key here is that the "Place order" workflow no longer lives here
        return Ok(new { Id = id });
    }
}

```

The controller is now:

- Easier to read

- Easier to test (mock `IOrderApplicationService`)

- Less likely to diverge from other clients that call the same use case

## Where the Service Layer Pattern Fits in a Typical .NET Architecture

A simple project layout that uses the Service Layer Pattern might look like this:

- `MyApp.Domain`
    - Entities and value objects (`Order`, `OrderLine`, `Customer`, etc.)
    
    - Domain services
    
    - Repository interfaces (`IOrderRepository`, `ICustomerRepository`)

- `MyApp.Application`
    - Application services (`IOrderApplicationService`, `OrderApplicationService`)
    
    - DTOs and commands/queries (`PlaceOrderRequest`, `CancelOrderRequest`)

- `MyApp.Infrastructure`
    - EF Core `DbContext`
    
    - Repository implementations
    
    - Unit of Work implementation
    
    - External service adapters (`CustomerCreditService`)

- `MyApp.Web`
    - Minimal API or MVC controllers
    
    - DI configuration

Dependency rules:

- `Web` → `Application` → `Domain`

- `Infrastructure` → `Application`, `Domain`

- `Domain` has no dependency on `Web` or `Infrastructure`

The Service Layer lives in the **Application** project and becomes the main API that the outside world uses.

## Smells That You Need a Service Layer

You probably need a Service Layer if:

- Controllers or endpoints are long and hard to test

- Multiple controllers or clients implement similar workflows differently

- Transaction handling and credit checks are scattered across your codebase

- Adding a new client (e.g., message consumer) feels like copying controller code

In short, if **business processes lack a single home**, you are a good candidate for a Service Layer.

## Introducing a Service Layer Pattern into an Existing App

You do not have to rewrite your system to adopt this pattern. Start small.

1. **Pick a single important use case**  
    For example: place an order, cancel an order, approve a refund.

3. **Extract orchestration into a service**  
    Move:
    - Creation and modification of domain objects
    
    - External service calls
    
    - Transaction handling

5. **Change controllers to call the service**  
    Keep validation and HTTP concerns in controllers; move business coordination out.

7. **Add a second client**  
    Create a background worker, message handler, or integration endpoint that calls the same service.  
    You will see reuse and consistency immediately.

9. **Repeat for other use cases**  
    As you migrate workflows to the Service Layer, controllers shrink and the application model becomes explicit.

## Closing Thoughts

The Service Layer pattern is not about ceremony. It is about **choosing one place where each business operation lives**, then letting every client call that place.

When you make HTTP a client instead of the boss:

- Controllers stay simple and focused on transport concerns

- Domain rules and workflows have a clear home

- Adding new delivery channels stops being a duplication exercise

Find one fat endpoint in your ASP.NET Core application. Extract its workflow into a service. Let the controller become a thin client. That single move often changes how you think about the entire system.
