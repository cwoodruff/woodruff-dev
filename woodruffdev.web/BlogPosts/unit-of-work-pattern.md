---
date: 2025-12-20
category: Patterns
description: "If a single business operation calls SaveChangesAsync three times, you do not have a transaction. You have a sequence of partial commits."
---
# Enterprise Patterns for ASP.NET Core Minimal API: Unit of Work Pattern

*One transaction, one commit, zero partial states.*

If a single business operation calls SaveChangesAsync three times, you do not have a transaction. You have a sequence of partial commits.

## What Is Unit of Work?

The Unit of Work pattern tracks all changes made during a business transaction and commits them as a single atomic operation. Either everything succeeds or nothing does.

In Entity Framework Core, `DbContext` is already a Unit of Work. It tracks every entity you add, modify, or delete, and `SaveChangesAsync()` commits them all in a single database transaction.

```csharp
// All changes tracked by the same DbContext
db.Orders.Add(order);
db.Inventory.Update(inventoryItem);
db.AuditLog.Add(auditEntry);

// One call, one transaction
await db.SaveChangesAsync();
```

## The Problem Without Unit of Work

Without this pattern, you end up with code like this:

```csharp
await orderRepository.SaveAsync(order);         // Committed
await inventoryRepository.SaveAsync(item);       // Committed
await auditRepository.SaveAsync(auditEntry);     // Fails!
// Order exists, inventory updated, but no audit record
```

If the third save fails, you have an inconsistent state. The order was placed, inventory was decremented, but there is no audit trail. Rolling back manually is error-prone and often forgotten.

## Unit of Work With EF Core

The simplest approach: let the `DbContext` scope define the Unit of Work boundary.

```csharp
app.MapPost("/api/orders", async (CreateOrderRequest request, AppDbContext db) =>
{
    var order = new Order
    {
        CustomerId = request.CustomerId,
        Items = request.Items.Select(i => new OrderItem
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity
        }).ToList()
    };

    foreach (var item in order.Items)
    {
        var product = await db.Products.FindAsync(item.ProductId);
        if (product is null || product.Stock < item.Quantity)
            return Results.BadRequest("Insufficient stock");
        product.Stock -= item.Quantity;
    }

    db.Orders.Add(order);
    await db.SaveChangesAsync(); // All changes committed atomically

    return Results.Created($"/api/orders/{order.Id}", order.Id);
});
```

One `SaveChangesAsync()`. One transaction. If any part fails, the database rolls back everything.

## Explicit Unit of Work Interface

For larger applications, you might want an explicit interface:

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public class AppDbContext : DbContext, IUnitOfWork
{
    // DbContext already implements SaveChangesAsync
}
```

This lets you inject `IUnitOfWork` into services that need to commit changes without depending on the full `DbContext`.

## Scope and Lifetime

In ASP.NET Core, `DbContext` is registered as scoped by default—one instance per HTTP request. This means each request is a natural Unit of Work boundary. All repository operations within a request share the same `DbContext` and the same transaction.

## Key Takeaway

The Unit of Work pattern ensures that a set of related changes either all succeed or all fail. In EF Core, you get this for free through `DbContext`. The discipline is calling `SaveChangesAsync()` once, at the end of the operation, not scattered throughout your code.
