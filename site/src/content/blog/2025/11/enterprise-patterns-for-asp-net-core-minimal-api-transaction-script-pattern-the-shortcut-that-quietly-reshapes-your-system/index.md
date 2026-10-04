---
title: "Enterprise Patterns for ASP.NET Core Minimal API: Transaction Script Pattern - The Shortcut That Quietly Reshapes Your System"
date: 2025-11-29
categories: 
  - "patterns"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "patterns"
  - "programming"
coverImage: "./images/Transaction-Script-The-Shortcut-That-Quietly-Reshapes-Your-System.png"
---

Picture this. Product wants a minor discount tweak: if an order total is below 100, no discount. You open an endpoint, add a conditional, save and push. Ten minutes, job done.

Three months later, that simple rule exists in six different endpoints, each with its own tiny twist. Someone fixes a bug in two of them, forgets the others, and now nobody can answer a basic question: what is the real discount rule in this system?

That creeping mess has a name: Transaction Script.

Most teams start here. Some stay here forever and pay for it in every release. This post walks through what Transaction Script really is, how it looks in ASP.NET Core, where it shines, and how to recognize when it stops being a shortcut and becomes structural debt.

You will see C# examples of a pure Transaction Script, followed by a first refactoring step that opens the door to a richer design.

## **What Transaction Script Actually Is**

Transaction Script treats each use case as a single procedure that does everything:

- Reads the request

- Loads data

- Applies business rules

- Persists changes

- Shapes the response

There is no separate domain model with behavior, no dedicated service layer that orchestrates aggregates. There is one script per scenario.

The pattern is not automatically wrong. It is brutally simple, which makes it powerful in the proper context and dangerous when the domain keeps growing.

## **A Concrete Example: Apply Discount as a Transaction Script**

Here is a straight Transaction Script inside an ASP.NET Core minimal API endpoint.

```csharp
app.MapPost("/discounts/apply", async (ApplyDiscountDto dto, AppDbContext db) =>
{
    var customer = await db.Customers.FindAsync(dto.CustomerId);
    if (customer == null)
    {
        return Results.NotFound("Customer not found.");
    }

    var order = await db.Orders
        .Include(o => o.Lines)
        .FirstOrDefaultAsync(o => o.Id == dto.OrderId && o.CustomerId == dto.CustomerId);

    if (order == null)
    {
        return Results.NotFound("Order not found.");
    }

    var total = order.Lines.Sum(l => l.Quantity * l.UnitPrice);

    if (total < 100)
    {
        return Results.BadRequest("Order total too low for discount.");
    }

    order.ApplyDiscount(dto.DiscountPercent);
    await db.SaveChangesAsync();
    return Results.Ok(new { order.Id, order.TotalAmount });
});

public record ApplyDiscountDto(Guid CustomerId, Guid OrderId, decimal DiscountPercent);
```

Everything lives in one block:

- Data access through AppDbContext

- Business rule about the minimum total

- Behavior that applies a discount

- HTTP responses and status codes

That is pure Transaction Script: a single procedure handles the entire transaction.

## **When Transaction Script Makes Sense**

Used deliberately, Transaction Script fits real scenarios.

### **Small, Focused Endpoints**

If a feature is bounded and straightforward, a script like this can be ideal:

- One administrative endpoint that fixes a specific data issue

- A tiny internal API for a tool that may never grow beyond a handful of operations

- A migration utility that processes a file and writes the results once

In these cases, creating a rich domain model or elaborate service layer can be pure overhead.

### **Short-Lived Features And Experiments**

Sometimes you need a spike:

- Experiment with a new discount rule to see if customers respond

- Build an internal endpoint that may be replaced by a better system later

- Capture data for a temporary campaign

A Transaction Script lets you wire this up rapidly. If the feature dies quickly, you never pay a heavy design cost.

### **Teams Under Immediate Delivery Pressure**

There are moments when:

- The business is blocked until a particular rule exists

- You are in the middle of an incident and need a quick workaround

- The system is early, and the domain rules are still chaotic

In those conditions, a well-written script can keep the system moving while you learn what the real domain boundaries look like.

The critical word there is "while". At some point, the domain stabilizes. If the scripts remain the main design element, they become anchors.

## **The Real Cost: Repeating Yourself Into A Corner**

The example above does not look frightening. The trouble starts when requirements evolve.

Imagine:

- A new discount rule for VIP customers that uses a lower threshold

- Another use case that applies discounts during checkout, not only via this endpoint

- A batch job that reconciles discounts at the end of the day

Every time you add a rule in script form, you have a choice:

- Copy the logic into the new script

- Call the existing script from somewhere strange

- Extract part of it into a helper or static method

Most teams take the fastest path, which often means copy, tweak, repeat. After a few iterations you have:

- Slightly different minimum totals scattered across scripts

- Conditionals that no one dares to touch because they are not sure who depends on them

- Business logic that can only be understood by reading several endpoints line by line

At that stage, you are not doing "simple procedural code". You are maintaining a distributed domain model made of duplicated fragments.

### **Recognizing When Your Scripts Are Out Of Control**

You do not need a fancy metric. Look for these practical signals.

### **Same Rule, Many Places**

Search for the literal "Order total too low for discount" string. If you find variations of the same check in multiple endpoints or handlers, you already have a smell.

- Different thresholds per scenario, but no central decision point

- Edge conditions handled in some scripts, forgotten in others

That is a sign that the domain concept "discount eligibility" deserves its own abstraction.

### **Scripts That Do Everything**

If an endpoint:

- Validates input

- Loads multiple aggregates

- Calculates totals

- Applies discounts

- Logs audits

- Sends notifications

- Saves changes

You no longer have a transaction script... You have a transaction novel. Changing anything inside that block risks side effects everywhere.

### **Testing Feels Painful**

If writing tests for your discount behavior:

- Requires spinning up a full test server

- Requires hitting HTTP routes for every variant

- Requires seeding a complex database state repeatedly

Then the rules are too entangled with infrastructure. Transaction Scripts have swallowed your business logic.

## **First Refactor: Keep The Script, Extract The Rule**

You do not need to jump straight from scripts to a full Domain Model. The first step can be modest: pull core rules into a domain abstraction, let the script delegate.

Introduce a small domain service that knows how discounts work.

```csharp
public interface IDiscountPolicy
{
    DiscountDecision Evaluate(decimal orderTotal, decimal requestedPercent);
}

public record DiscountDecision(bool IsAllowed, string? Reason);

public class MinimumTotalDiscountPolicy : IDiscountPolicy
{
    private readonly decimal _minimumTotal;

    public MinimumTotalDiscountPolicy(decimal minimumTotal)
    {
        _minimumTotal = minimumTotal;
    }

    public DiscountDecision Evaluate(decimal orderTotal, decimal requestedPercent)
    {
        if (orderTotal < _minimumTotal)
        {
            return new DiscountDecision(false, "Order total too low for discount.");
        }

        if (requestedPercent <= 0 || requestedPercent >= 50)
        {
            return new DiscountDecision(false, "Discount percent out of allowed range.");
        }

        return new DiscountDecision(true, null);
    }
}
```

Now change the script to use this policy.

```csharp
app.MapPost("/discounts/apply", async (
    ApplyDiscountDto dto,
    AppDbContext db,
    IDiscountPolicy discountPolicy) =>
{
    var customer = await db.Customers.FindAsync(dto.CustomerId);
    if (customer == null)
    {
        return Results.NotFound("Customer not found.");
    }

    var order = await db.Orders
        .Include(o => o.Lines)
        .FirstOrDefaultAsync(o => o.Id == dto.OrderId && o.CustomerId == dto.CustomerId);

    if (order == null)
    {
        return Results.NotFound("Order not found.");
    }

    var total = order.Lines.Sum(l => l.Quantity * l.UnitPrice);
    var decision = discountPolicy.Evaluate(total, dto.DiscountPercent);

    if (!decision.IsAllowed)
    {
        return Results.BadRequest(decision.Reason);
    }

    order.ApplyDiscount(dto.DiscountPercent);
    await db.SaveChangesAsync();
    return Results.Ok(new { order.Id, order.TotalAmount });
});
```

You still have a Transaction Script. It still coordinates the transaction. Something important has changed.

- The rule about whether a discount is allowed now lives in one place

- Other scripts or background jobs can reuse IDiscountPolicy

- You can unit test the policy with plain C# tests without a test server or database

This is the hinge where your system can move toward a Domain Model or a more structured Service Layer.

## **When Transaction Script Should Give Way To Other Patterns**

Once a rule becomes central to the product, you want more than a simple procedure.

### **Domain Model For Rich, Interconnected Rules**

If discounts:

- Depend on customer status, product category, time of day, and campaign rules

- Interact with loyalty points, tax calculations, and fulfillment

- Require sophisticated validation and simulation

then a Domain Model gives you a place to express those relationships. Aggregates, value objects, and domain services can capture those rules in a way that scripts never will.

In that world, discount logic might live inside an Order aggregate or a dedicated DiscountEngine, tested directly and used from multiple entry points.

### **Table Module For Table Focused Operations**

If most of the logic operates on a single table or view, such as OrderSummary or InvoiceRow, and the operations are set-oriented, a Table Module can be enough.

A DiscountTableModule class that encapsulates queries and updates for discount fields can centralize behavior without inventing a full object model.

The critical point is not the pattern's name. It is whether the rules live in a singular, testable place, rather than in scattered fragments.

## **Practical Guidelines For .NET Teams**

If you build ASP.NET Core systems, you can treat Transaction Script as a controlled tool rather than a reflexive default.

- Use scripts for isolated use cases with simple rules and short life spans

- Watch for duplication: once a rule appears in more than one script, extract it into a domain component

- Keep orchestration and infrastructure in the script, push domain decisions into services or entities

- Add tests around the extracted domain pieces before you touch the scripts again

The goal is not to outlaw Transaction Script. The goal is to avoid waking up one day in a codebase where every important rule lives inside an untested endpoint.

## **A Challenge For Your Current Codebase**

Pick one feature today:

1. Find an endpoint or handler that reads from the database, applies a rule, and writes back in a single method.

3. Identify a single rule inside that method that business people care about.

5. Extract that rule into a domain service or helper with its own unit tests.

7. Leave the rest of the script intact, let the endpoint call the new abstraction.

Run the tests. Show that nothing changed in behavior. Then ask yourself: if this one small extraction already made the rule clearer, what happens when you repeat that move across the parts of the system that hurt the most?

That is how Transaction Script turns from a permanent architecture into a stepping stone.
