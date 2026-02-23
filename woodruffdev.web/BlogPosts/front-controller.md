---
date: 2026-01-10
category: Patterns
description: "If every controller does its own authentication, logging, and error handling, you don't have an architecture. You have a crowd of small frameworks pretending to cooperate."
---
# Enterprise Patterns for ASP.NET Core: Front Controller and MVC Pattern

*A single entry point that brings order to request handling.*

If every controller does its own authentication, logging, and error handling, you don't have an architecture. You have a crowd of small frameworks pretending to cooperate.

## What Is the Front Controller Pattern?

The Front Controller pattern routes all requests through a single handler that performs common processing before dispatching to specific handlers. In ASP.NET Core, this is the middleware pipeline itself.

```
Request → [Middleware Pipeline] → [Routing] → [Controller/Page Handler]
              ↓
         Authentication
         Authorization
         Logging
         Error Handling
         CORS
```

Every request passes through the same pipeline. Cross-cutting concerns are handled once, in one place. Individual handlers focus only on their specific logic.

## ASP.NET Core as a Front Controller

You are already using this pattern if you use ASP.NET Core. The `Program.cs` pipeline is the front controller:

```csharp
var app = builder.Build();

app.UseExceptionHandler("/Error");      // Global error handling
app.UseHsts();                          // Security headers
app.UseHttpsRedirection();              // Force HTTPS
app.UseStaticFiles();                   // Serve static content
app.UseRouting();                       // Determine endpoint
app.UseAuthentication();                // Identify the user
app.UseAuthorization();                 // Check permissions
app.MapControllers();                   // Dispatch to handler
```

Each `Use*` call adds a middleware component. The order matters. Authentication must come before authorization. Routing must come before endpoint dispatch. This ordering is the front controller's primary responsibility.

## Custom Middleware

When you need cross-cutting behavior that does not fit existing middleware, write your own:

```csharp
public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        await _next(context);
        sw.Stop();
        _logger.LogInformation("Request {Method} {Path} completed in {Elapsed}ms",
            context.Request.Method, context.Request.Path, sw.ElapsedMilliseconds);
    }
}
```

Register it once. It applies to every request. No decorator attributes scattered across controllers. No base class inheritance chains.

## MVC: The Dispatch Layer

While the front controller handles cross-cutting concerns, MVC handles dispatch. Each request is routed to a specific controller and action based on the URL pattern:

- **Model** — the data and business logic
- **View** — the HTML or JSON response
- **Controller** — the coordinator that connects model to view

In ASP.NET Core Minimal APIs, the controller layer is implicit. The route handler is the controller:

```csharp
app.MapGet("/api/albums", async (ChinookContext db) =>
    await db.Albums.Select(a => new { a.Id, a.Title }).ToListAsync());
```

## When This Breaks Down

The front controller assumes all requests need similar processing. If you have wildly different request types (WebSocket connections, file uploads, streaming responses), you may need to branch the pipeline or use endpoint-specific middleware.

## Key Takeaway

The Front Controller pattern is not something you add to ASP.NET Core. It is ASP.NET Core. Understanding this pattern helps you use the framework the way it was designed: centralize cross-cutting concerns in middleware, and let individual handlers focus on their specific job.
