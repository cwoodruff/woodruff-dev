---
title: "Stop Letting Your Controllers Talk to SQL: Layered Architecture in ASP.NET Core"
date: 2025-11-28
categories: 
  - "patterns"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "patterns"
  - "programming"
coverImage: "Stop-Letting-Your-Controllers-Talk-to-SQL-Layered-Architecture-in-ASP.NET-Core.png"
---

Walk into almost any long-lived enterprise codebase, and you will find the same pattern:

- Controllers that know about routing, JSON, SQL, and domain rules

- Repositories that reach up into `HttpContext`

- Business rules scattered across UI, stored procedures, and helper classes

At that point, adding a new feature feels like surgery without a map. You poke at one place, something bleeds somewhere else, and nobody is sure why.

Layered architecture exists to stop that.

In this post, we will walk through a practical version of Fowler’s layered architecture in ASP.NET Core and C#. You will see:

- What each layer is allowed to know

- How to wire up a fundamental feature using three layers

- How does this structure make change cheaper and failure less chaotic

The example centers on a simple use case: creating an order.

## The core idea: three layers, three distinct responsibilities

Fowler’s baseline looks like this:

1. **Presentation layer**  
    Handles input and output. In web apps, this means HTTP, routing, model binding, and formatting responses.

3. **Domain layer**  
    Holds business rules, domain services, and aggregates. It talks about orders, customers, payments, not controllers or `DbContext`.

5. **Data source layer**  
    Owns persistence. It uses EF Core, raw SQL, caching, and talks to any external data store.

A simple rule captures the intent:

> If your controllers know SQL or your repositories know HTTP, you already lost separation.

The rest of this post shows what it looks like when you refuse to cross those lines.

## The scenario: placing an order in three slices

We will build a feature that lets a client create an order.

Requirements:

- Clients call `POST /orders` with customer and line items

- An order must contain at least one line item

- The system persists the order in a relational database

We will implement that with:

- A presentation layer endpoint

- A domain service and aggregate

- A data source layer using EF Core

## Presentation layer: thin HTTP endpoint

The presentation layer should only:

- Accept input

- Call the domain layer

- Shape the HTTP response

It should not:

- Reach into `DbContext`

- Perform business rules beyond basic request validation

- Use EF Core directly

Example with ASP.NET Core minimal APIs:

```csharp
// Program.cs

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderRepository, EfCoreOrderRepository>();

var app = builder.Build();

app.MapPost("/orders", async (CreateOrderDto dto, IOrderService orderService) =>
{
    // Basic request-level validation only
    if (dto.Lines is null || dto.Lines.Count == 0)
    {
        return Results.BadRequest("Order must contain at least one line item.");
    }

    var orderId = await orderService.CreateOrderAsync(dto.CustomerId, dto.Lines);
    return Results.Created($"/orders/{orderId}", new { Id = orderId });
});

app.Run();

public record CreateOrderDto(Guid CustomerId, List<OrderLineDto> Lines);

public record OrderLineDto(Guid ProductId, int Quantity);

```

Notice what the endpoint **does not** do:

- It does not call `AppDbContext`

- It does not calculate totals

- It does not decide how orders are stored

It simply coordinates HTTP and the domain service.

## Domain layer: business rules and language of the problem

The domain layer decides what an order is allowed to do.

It should:

- Enforce invariants

- Capture domain rules in one place

- Express intent through the language of the business

It should not:

- Know about HTTP

- Know about EF Core

- Reference ASP.NET Core packages

Domain service interface:

```csharp
public interface IOrderService
{
    Task<Guid> CreateOrderAsync(
        Guid customerId,
        IReadOnlyCollection<OrderLineDto> lines);
}

```

Domain service implementation:

```csharp
public class OrderService : IOrderService
{
    private readonly IOrderRepository _orders;

    public OrderService(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<Guid> CreateOrderAsync(
        Guid customerId,
        IReadOnlyCollection<OrderLineDto> lines)
    {
        if (!lines.Any())
        {
            throw new InvalidOperationException("Order must contain at least one line item.");
        }

        var order = Order.Create(
            customerId,
            lines.Select(l => new OrderLine(l.ProductId, l.Quantity)).ToList());

        await _orders.AddAsync(order);
        return order.Id;
    }
}

```

Domain model for `Order` and `OrderLine`:

```csharp
public class Order
{
    private readonly List<OrderLine> _lines = new();

    private Order(Guid customerId)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        Status = OrderStatus.Draft;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; }
    public Guid CustomerId { get; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; }
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
            throw new InvalidOperationException("Order must have at least one line item.");
        }

        return order;
    }

    public void AddLine(Guid productId, int quantity, decimal unitPrice)
    {
        if (Status != OrderStatus.Draft)
        {
            throw new InvalidOperationException("Cannot modify a non draft order.");
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

        // Save Order

        Status = OrderStatus.Submitted;
    }
}

public class OrderLine
{
    public OrderLine(Guid productId, int quantity, decimal unitPrice)
    {
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid ProductId { get; }
    public int Quantity { get; }
    public decimal UnitPrice { get; }
    public decimal Total => Quantity * UnitPrice;
}

public enum OrderStatus
{
    Draft = 0,
    Submitted = 1,
    Cancelled = 2
}

```

Key observation: the domain layer expresses business concepts directly.

- “Order must contain at least one line item” lives close to the `Order` aggregate

- Status transitions are enforced inside the entity

- Nothing here references controllers, HTTP, or EF Core

## Data source layer: persistence without domain leakage

The data source layer manages persistence.

It should:

- Map domain entities to database tables

- Provide repository interfaces and implementations

- Handle transactions and database concerns

It should not:

- Return HTTP-only models

- Depend on `HttpContext`

- Make business decisions

Repository interface:

```csharp
public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

```

EF Core implementation:

```csharp
public class EfCoreOrderRepository : IOrderRepository
{
    private readonly AppDbContext _dbContext;

    public EfCoreOrderRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .Include(o => o.Lines)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
    }
}

```

DbContext:

```csharp
public class AppDbContext : DbContext
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(builder =>
        {
            builder.HasKey(o => o.Id);
            builder.Property(o => o.CustomerId).IsRequired();
            builder.Property(o => o.Status).IsRequired();
            builder.Property(o => o.CreatedAt).IsRequired();

            builder.HasMany(typeof(OrderLine), "_lines")
                .WithOne()
                .HasForeignKey("OrderId")
                .IsRequired();
        });

        modelBuilder.Entity<OrderLine>(builder =>
        {
            builder.HasKey("Id");
            builder.Property<Guid>("OrderId");
            builder.Property(l => l.ProductId).IsRequired();
            builder.Property(l => l.Quantity).IsRequired();
            builder.Property(l => l.UnitPrice).IsRequired();
        });
    }
}

```

The repository understands EF Core and the database schema. The domain does not.

## How does this structure make change cheaper

The layered structure looks simple until you try to change things. That is where it earns its keep.

### 1\. Business rule changes

Suppose the business wants new rules:

- Orders below a specific total should be rejected

- Some customers have higher minimum totals

Where does that logic go?

- Not in the controller

- Not in the repository

It belongs in the domain layer, near the `Order` aggregate or in domain services.

You might extend `OrderService`:

```csharp
public async Task<Guid> CreateOrderAsync(
    Guid customerId,
    IReadOnlyCollection<OrderLineDto> lines)
{
    if (!lines.Any())
    {
        throw new InvalidOperationException("Order must contain at least one line item.");
    }

    var order = Order.Create(
        customerId,
        lines.Select(l => new OrderLine(l.ProductId, l.Quantity)).ToList());

    var minimum = await GetCustomerMinimumAsync(customerId);

    if (order.TotalAmount < minimum)
    {
        throw new InvalidOperationException(
            $"Order total must be at least {minimum} for this customer.");
    }

    await _orders.AddAsync(order);
    return order.Id;
}

private Task<decimal> GetCustomerMinimumAsync(Guid customerId)
{
    // Look up from a configuration service or customer settings
    return Task.FromResult(100m);
}

```

Presentation and data source layers stay unchanged.

### 2\. Switching transports: HTTP today, messaging tomorrow

Imagine you want to process orders from a message queue and via HTTP.

With a layered design, you write a message handler that calls the same `IOrderService`:

```csharp
public class OrderCreatedMessageHandler
{
    private readonly IOrderService _orderService;

    public OrderCreatedMessageHandler(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task HandleAsync(OrderCreatedMessage message)
    {
        var dtoLines = message.Lines
            .Select(l => new OrderLineDto(l.ProductId, l.Quantity))
            .ToList();

        await _orderService.CreateOrderAsync(message.CustomerId, dtoLines);
    }
}

public record OrderCreatedMessage(Guid CustomerId, List<OrderLineDto> Lines);

```

No duplication of rules. No extra data access logic. You add another presentation layer entry point.

### 3\. Swapping EF Core for another persistence mechanism

If you ever need to switch persistence, the blast radius is clear.

- Domain layer remains the same

- Presentation layer remains the same

- Only `IOrderRepository` implementations and `AppDbContext` related types change

You can introduce another repository, for example `DapperOrderRepository`, and swap registrations in the DI container.

## How teams quietly destroy their layers

Codebases rarely lose layering due to a single catastrophic decision. They lose it through a stream of small, “temporary” shortcuts.

### Shortcut 1: “Just this one query in the controller”

A developer needs a special report. They already have access to `AppDbContext` in the controller, so they write:

```csharp
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public ReportsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("reports/orders")]
    public async Task<IActionResult> GetOrdersReport()
    {
        var data = await _dbContext.Orders
            .Include(o => o.Lines)
            .Where(o => o.CreatedAt >= DateTime.UtcNow.AddDays(-7))
            .ToListAsync();

        // Transform into view model
        return Ok(data);
    }
}

```

It works. It ships. It also teaches the team that controllers are fair game for data access. After a few months, there is no clear separation at all.

Better approach: move data access to a query service or repository and call that from the controller.

### Shortcut 2: Repositories returning view models

Another developer builds a dashboard. They want to avoid extra mapping, so they let the repository spit out DTOs used by the UI.

```csharp
public interface IOrderRepository
{
    Task<IReadOnlyCollection<OrderSummaryDto>> GetRecentSummariesAsync();
}

```

That feels efficient. It also tangles the data source layer with a specific presentation need.

Six months later, a background worker wants a different representation, and the repository keeps growing special cases.

Better approach: keep repositories returning domain objects or well-defined query models that are not tied directly to controllers.

### Shortcut 3: Domain services reading `HttpContext`

Sometimes, domain services need user information or tenant context. The easy path is to inject `IHttpContextAccessor` directly.

```csharp
public class DiscountService
{
    private readonly IHttpContextAccessor _accessor;

    public DiscountService(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public decimal GetDiscount()
    {
        var user = _accessor.HttpContext?.User;
        // ...
    }
}

```

This locks the domain to ASP.NET Core. Reusing the domain in background services or tests becomes painful.

Better approach: define an abstraction such as `ICurrentUser` or `ICurrentTenant` in the domain layer, and implement it in the presentation or infrastructure layer using `HttpContext`.

## Enforcing layers with project structure

Code style and good intentions are not enough. The solution's physical structure should reinforce the boundaries.

A common layout:

- `MyApp.Domain`
    - Entities, value objects, domain services, domain interfaces

- `MyApp.Application` (optional, if you separate application from pure domain)
    - Use cases, application services, DTOs

- `MyApp.Infrastructure`
    - EF Core mappings, repositories, integration with external services

- `MyApp.Web`
    - ASP.NET Core host, controllers, endpoints, filters

Dependency rules:

- `MyApp.Web` references `MyApp.Domain` and `MyApp.Application`

- `MyApp.Infrastructure` references `MyApp.Domain` and `MyApp.Application`

- `MyApp.Domain` does not reference any other project

- `MyApp.Application` references `MyApp.Domain` only

You can even add build checks or analyzers to prevent forbidden references.

## Raising the stakes: what you lose without layers

If this sounds abstract, consider the cost of ignoring it.

- You lose the ability to isolate a single change. Every change becomes a search through controllers, EF queries, and domain objects.

- You lose optionality. Moving to messaging, splitting services, or swapping databases becomes nearly impossible without a rewrite.

- You lose trust. Diagrams and architecture documents claim there are layers. The code says otherwise. Eventually, the team stops listening to both.

Layered architecture will not save you from every problem. It does something more modest and more powerful:

It gives you clear seams to cut, adapt, and evolve.

## A simple challenge for your current system

Pick one feature in your application. Then:

1. Identify the presentation, domain, and data source pieces.

3. Count how many times each one crosses its boundary.

5. Move a single business rule out of a controller into a domain service or aggregate.

7. Move a single data access concern from a controller to a repository.

You do not fix an entire enterprise codebase in one refactor. You fix it one layer at a time, one leak at a time.

Once your controllers stop talking to SQL and your repositories stop knowing HTTP, everything else gets easier.
