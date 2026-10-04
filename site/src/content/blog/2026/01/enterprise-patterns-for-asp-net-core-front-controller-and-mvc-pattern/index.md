---
title: "Enterprise Patterns for ASP.NET Core: Front Controller and MVC Pattern"
date: 2026-01-11
categories: 
  - "patterns"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "patterns"
  - "programming"
coverImage: "./images/Enterprise-Patterns-for-ASP.NET-Core-Front-Controller-and-MVC-Pattern.png"
---

If every controller in your system does its own authentication, logging, and error handling, you do not have an architecture. You have a crowd of small frameworks pretending to cooperate.

Front Controller and MVC are the patterns that push back against that drift.

- Front Controllers decide what _every_ HTTP request must pass through.

- MVC decides what belongs inside the web layer and what does not.

In ASP.NET Core, the pipeline and routing already give you a natural front controller. The real question is whether you treat that entry point as a disciplined gateway or as a dumping ground.

This post walks through:

- What Front Controller and MVC mean in practice for ASP.NET Core

- A compact custom Front Controller middleware and router

- Before and after Minimal API examples

- How does this pattern let you own cross-cutting concerns instead of scattering them

- When to lean into Front Controller and MVC, and when to keep things minimal

## Who Actually Owns Your HTTP Boundary

Picture a typical service after a few sprints:

- One team adds authentication checks inside controllers.

- Another team adds logging with `ILoggerFactory` directly in endpoints.

- A third team adds a custom filter for correlation IDs.

- Everyone wires error handling however they feel like that week.

No single place shows what happens to an HTTP request from the moment it arrives until the moment it leaves. If you want to add a feature flag, a rate limit, or a security policy, you end up chasing it across controllers, filters, and boilerplate copies.

Front Controller exists to collapse that mess into one place.

MVC exists to stop controllers from becoming a second domain layer.

## What Front Controller And MVC Actually Are

### Front Controller in practice

In Fowler’s terms, a Front Controller is:

- A single handler that receives all incoming requests

- A gateway that applies cross-cutting policies once

- A dispatcher that forwards to specific page controllers or handlers

In ASP.NET Core, you already have the makings of this:

- `Program.cs` where you build the middleware pipeline

- Routing that maps requests to endpoints

- A place to plug in logic that will intercept every request

The pattern is less about adding brand new mechanisms and more about using the existing ones with intent.

### MVC in practice

MVC splits responsibilities:

- The model holds domain data and behavior.

- Views presents data to the outside world: HTML, JSON, or something else.

- The controller translates HTTP requests into domain calls and selects views.

In ASP.NET Core:

- Models are domain entities, value objects, DTOs, and view models.

- Views are Razor views or serialized responses.

- Controllers or Minimal API handlers are the thin glue.

MVC goes wrong when controllers:

- Contain business rules

- Talk directly to the database

- Duplicate logic that belongs in the domain or application services

Front Controller and MVC together say:

- Put cross-cutting concerns at the gateway

- Keep controllers as simple adapters

- Keep business logic in application and domain services

## A Compact Front Controller Middleware

You can write a very direct Front Controller in ASP.NET Core using custom middleware and a simple router.

```csharp
public interface IEndpointHandler
{
    Task HandleAsync(HttpContext context);
}

public interface IEndpointRouter
{
    IEndpointHandler? Match(HttpRequest request);
}

public class FrontControllerMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context, IEndpointRouter router)
    {
        // Global cross cutting concerns can sit right here
        // Example: logging, correlation, feature flags

        var endpoint = router.Match(context.Request);
        if (endpoint == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Not found");
            return;
        }

        await endpoint.HandleAsync(context);
    }
}
```

A very simple router:

```csharp
public class SimpleEndpointRouter : IEndpointRouter
{
    private readonly IReadOnlyDictionary<string, IEndpointHandler> _handlers;

    public SimpleEndpointRouter(IEnumerable<IEndpointHandler> handlers)
    {
        // Basic example: key by path.
        _handlers = handlers.ToDictionary(
            h => h switch
            {
                GetOrderEndpoint => "/orders/get",
                PlaceOrderEndpoint => "/orders/place",
                _ => string.Empty
            },
            h => h);

        // In a real implementation, you would key by method and path pattern.
    }

    public IEndpointHandler? Match(HttpRequest request)
    {
        if (!_handlers.TryGetValue(request.Path, out var handler))
        {
            return null;
        }

        return handler;
    }
}
```

And one handler:

```csharp
public class GetOrderEndpoint(IOrderQueryService orders) : IEndpointHandler
{
    private readonly IOrderQueryService _orders = orders;

    public async Task HandleAsync(HttpContext context)
    {
        if (!Guid.TryParse(context.Request.Query["id"], out var orderId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Invalid id");
            return;
        }

        var order = await _orders.GetOrderDetailsAsync(orderId);

        if (order is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(order);
    }
}
```

This is deliberately minimal. It proves a point.

- One middleware sees every request.

- Route selection is centralized.

- Cross-cutting concerns sit right at the boundary.

In a real application, you probably rely on ASP.NET Core routing rather than a custom router. The pattern still applies. The pipeline is your Front Controller. The key is what you put there.

## ASP.NET Core As A Front Controller

You do not need to reinvent routing to apply this pattern. Use what the framework already gives you, but treat it as a strict boundary.

`Program.cs` already acts as the front door.

```csharp
var builder = WebApplication.CreateBuilder(args);

// Registration
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options => { /* options */ });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Your application services, UoW, repositories, etc
builder.Services.AddScoped<IOrderApplicationService, OrderApplicationService>();
builder.Services.AddScoped<IOrderRepository, EfCoreOrderRepository>();

var app = builder.Build();

// Global front controller pipeline
app.UseMiddleware<RequestCorrelationMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();       // MVC
app.MapMinimalApis();       // extension that registers your minimal endpoints

app.Run();

```

Here, the pipeline is the Front Controller:

- Every request receives a correlation id.

- Every unhandled exception is handled in a single place.

- Authentication and authorization run centrally.

Controllers and endpoints do not need to worry about these mechanics.

## Before: Controllers That Improvise Infrastructure

Consider an ASP.NET Core API that skipped discipline at the boundary.

```csharp
[ApiController]
[Route("api/orders")]
public class OrdersController(AppDbContext db, ILogger<OrdersController> logger) : ControllerBase
{
    private readonly AppDbContext _db = db;

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(PlaceOrderRequest request)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            logger.LogWarning("Unauthenticated order attempt");
            return Unauthorized();
        }

        try
        {
            using var transaction = await _db.Database.BeginTransactionAsync();

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = request.CustomerId,
                CreatedAtUtc = DateTime.UtcNow
            };

            foreach (var line in request.Lines)
            {
                order.Lines.Add(new OrderLine
                {
                    ProductId = line.ProductId,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice
                });
            }

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            logger.LogInformation("Order {OrderId} placed by {UserId}", order.Id, User.Identity?.Name);

            await transaction.CommitAsync();

            return CreatedAtAction(nameof(GetById), new { id = order.Id }, new { order.Id });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error placing order");
            return StatusCode(StatusCodes.Status500InternalServerError, "Unexpected error");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        // similar pattern: local try catch, local logging, direct DbContext, etc
        // omitted for brevity
        throw new NotImplementedException();
    }
}
```

Problems:

- Authentication check is inside the action.

- Transaction management is inside the action.

- Logging and error handling are inside the action.

- DbContext is used directly instead of application services.

If you need to change the logging format, transaction strategy, or authentication rules, you need to take many actions.

This controller does not sit behind a clear Front Controller. It _is_ the front controller for its own small kingdom.

## After: Pipeline As Front Controller, Controllers As Glue

Refactor so that cross-cutting concerns live in the pipeline and application logic lives in services. Controllers become thin coordinators.

### Global correlation and exception middleware

Correlation ID middleware:

```csharp
public class RequestCorrelationMiddleware(
    RequestDelegate next,
    ILogger<RequestCorrelationMiddleware> logger)
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers[CorrelationIdHeader] = correlationId!;
        }

        context.Response.Headers[CorrelationIdHeader] = correlationId!;

        using (logger.BeginScope(new Dictionary<string, object?>
               {
                   ["CorrelationId"] = correlationId.ToString()
               }))
        {
            await next(context);
        }
    }
}
```

Global exception middleware:

```csharp
public class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Path}", context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var problem = new
            {
                title = "Unexpected error",
                status = StatusCodes.Status500InternalServerError,
                correlationId = context.Response.Headers["X-Correlation-Id"].ToString()
            };

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
```

Update `Program.cs` to enforce this entry point:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddAuthentication("Cookies").AddCookie();
builder.Services.AddAuthorization();

builder.Services.AddScoped<IOrderApplicationService, OrderApplicationService>();

var app = builder.Build();

app.UseMiddleware<RequestCorrelationMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

Now rewrite the controller to be as thin as possible.

```csharp
[ApiController]
[Route("api/orders")]
public class OrdersController(IOrderApplicationService orders) : ControllerBase
{
    private readonly IOrderApplicationService _orders = orders;

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(PlaceOrderRequest request, CancellationToken ct)
    {
        // Authentication is handled globally
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return Unauthorized();
        }

        var customerId = GetCustomerIdFromUser(User); // simple mapping

        var orderId = await _orders.PlaceOrderAsync(customerId, request, ct);

        return CreatedAtAction(nameof(GetById), new { id = orderId }, new { Id = orderId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var order = await _orders.GetDetailsAsync(id, ct);
        if (order is null)
        {
            return NotFound();
        }

        return Ok(order);
    }

    private Guid GetCustomerIdFromUser(ClaimsPrincipal user)
    {
        var idClaim = user.FindFirst("sub") ?? user.FindFirst(ClaimTypes.NameIdentifier);
        if (idClaim is null)
        {
            throw new InvalidOperationException("Missing customer id claim.");
        }

        return Guid.Parse(idClaim.Value);
    }
}
```

The heavy lifting has moved into `OrderApplicationService`.

```csharp
public interface IOrderApplicationService
{
    Task<Guid> PlaceOrderAsync(Guid customerId, PlaceOrderRequest request, CancellationToken cancellationToken);
    Task<OrderDetailsDto?> GetDetailsAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class PlaceOrderRequest
{
    public List<OrderLineDto> Lines { get; set; } = new();
}

public sealed class OrderLineDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public sealed class OrderDetailsDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class OrderApplicationService(IOrderRepository orders, IUnitOfWork unitOfWork) : IOrderApplicationService
{
    private readonly IOrderRepository _orders = orders;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Guid> PlaceOrderAsync(
        Guid customerId,
        PlaceOrderRequest request,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginAsync(cancellationToken);

        try
        {
            var lines = request.Lines
                .Select(l => new OrderLine(l.ProductId, l.Quantity, l.UnitPrice))
                .ToList();

            var order = Order.Create(customerId, lines);

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

    public async Task<OrderDetailsDto?> GetDetailsAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return null;
        }

        return new OrderDetailsDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString()
        };
    }
}
```

The HTTP boundary is now:

- Central and predictable in the pipeline.

- Thin and focused in controllers.

- Backed by a service layer that owns use case orchestration.

That is the Front Controller and MVC at work, using built-in tools rather than inventing new ones.

## Minimal APIs: Front Controller Without Controllers

Minimal APIs do not remove the Front Controller pattern. They only move it closer to the pipeline.

A typical minimal setup:

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseMiddleware<RequestCorrelationMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/orders", async (
    PlaceOrderRequest request,
    IOrderApplicationService orders,
    ClaimsPrincipal user,
    CancellationToken ct) =>
{
    if (!user.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    var customerId = GetCustomerIdFromUser(user);
    var id = await orders.PlaceOrderAsync(customerId, request, ct);
    return Results.Created($"/orders/{id}", new { Id = id });
});

app.Run();
```

The pipeline still acts as the Front Controller. Your choice is how disciplined you stay:

- You can keep auth, logging, and error handling in middleware.

- You can drop all of that straight into every endpoint handler.

The pattern works or fails based on that decision, not on whether you use controllers or Minimal APIs.

## When Front Controller and MVC Are Worth It

You gain leverage from this pattern when:

### Multiple teams add endpoints over time

If many developers are contributing to one service:

- You want one place to enforce security policies.

- You want consistent logs and metrics.

- You want a single knob for rate limiting and feature flags.

A Front Controller style pipeline gives you that control. MVC conventions keep controllers from becoming grab bags.

### Cross-cutting concerns are non-negotiable

If your system must:

- Log every request with correlation IDs.

- Perform audit logging on sensitive routes.

- Wrap responses in a uniform envelope.

You need a consistent HTTP entry point. You cannot rely on each controller remembering to play along.

### You already have other enterprise patterns

If you use:

- Domain Model

- Repositories

- Service Layer

- Unit of Work

then there is no good reason to let controllers improvise infrastructure logic. Front Controller and MVC let you complete the picture.

## When Not To Overbuild

Front Controller and MVC are tools. You do not need the full structure everywhere.

Scenarios where a heavy pattern is overkill:

### Tiny internal APIs and experiments

If you have:

- A small internal tool

- A prototype or throwaway service

- A single developer touching the code

then a few Minimal APIs with light middleware can be enough. Just keep an eye on growth. Once it becomes core, you can harden the boundary.

### Serverless function per endpoint models

If each endpoint is deployed and scaled independently:

- The function runtime already acts as a focused entry point.

- Trying to layer a complex Front Controller abstraction on top may only add complexity.

### Very simple CRUD sites

If the entire application is:

- A straightforward CRUD over a handful of tables

- No serious cross-cutting rules or policies

you can often rely on stock ASP.NET Core conventions with a slim pipeline and modest controllers.

In other words, treat the pattern as a response to complexity, not as a checklist item.

## Practical Steps In A Real Codebase

If you suspect your HTTP boundary is out of control, a simple process looks like this:

1. Map the actual path of a request
    - List every middleware and filter that can run.
    
    - Read a couple of controllers end-to-end.
    
    - Note where auth, logging, error handling, and response shaping happen.

3. Decide what belongs at the front door
    - Authentication and authorization.
    
    - Correlation and logging.
    
    - Global exception handling.
    
    - Rate limiting, feature flags, audit hooks.

5. Move that logic to the pipeline
    - Create focused middleware for each concern.
    
    - Remove the duplicated code from controllers.
    
    - Keep controllers or Minimal APIs focused on orchestrating use cases.

7. Establish controller and endpoint conventions
    - No direct `DbContext` usage.
    
    - No local transaction handling.
    
    - Minimal branching.
    
    - Delegate to application services and repositories.

You now have a real Front Controller and a sane MVC boundary, even if you never mention the pattern names in the code.

## Closing Thought

Whatever controls your HTTP boundary controls your power.

If you let every controller and endpoint do its own thing, you trade that power for short-term convenience. You also commit to a slow, expensive cleanup later.

Treat the ASP.NET Core pipeline as your Front Controller on purpose. Treat controllers and Minimal APIs as thin adapters on purpose.

Once you do that, the rest of your architecture finally has the chance to behave like a system instead of a collection of unrelated tricks.
