---
title: "Enterprise Patterns for ASP.NET Core Minimal API: Active Record Pattern"
date: 2025-12-15
categories: 
  - "patterns"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "patterns"
  - "programming"
coverImage: "./images/Enterprise-Patterns-for-ASP.NET-Core-Minimal-API-Active-Record-Pattern.png"
---

Sometimes, your domain is really just rows in a table.

You have a `Customers` table with `Id`, `Email`, and `CreditLimit`.  
You load a row, tweak a field, and write it back.  
That is the whole story.

In that situation, introducing a complete Domain Model, Data Mapper, and Repository stack can feel like ceremony for ceremony’s sake. The **Active Record** pattern exists for precisely this scenario: when your object and your row are basically the same thing.

In this post, we will:

- Define the Active Record pattern in practical .NET terms

- Build a complete `CustomerRecord` example using ADO.NET

- Show how it appears “by accident” in ASP.NET Core Minimal APIs

- Compare a “before” and “after” endpoint using Active Record

- Outline when the pattern is a good fit, and when to walk away from it

## What Active Record Really Is

In Fowler’s catalog, **Active Record** means:

- Each object instance wraps a row in a database table

- The class knows how to:
    - Load itself (or instances of itself) from the database
    
    - Save its changes back
    
    - Apply simple business operations

In C#, that usually looks like:

- A class with properties that mirror columns

- Static methods like `Find` or `FindByEmail` that query and return instances

- Instance methods like `Save`, `Delete`, and small domain operations

The key point: **persistence logic and business logic live together in the same class**.

## A Concrete Active Record: `CustomerRecord` in C#

Here is a fully fleshed out Active Record example using raw ADO.NET and SQL Server.

```csharp
using System.Data;
using System.Data.SqlClient;

public class CustomerRecord(string connectionString)
{
    // Columns
    public int Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public decimal CreditLimit { get; private set; }

    // Factory for a brand-new customer (not yet in DB)
    public static CustomerRecord CreateNew(
        string connectionString,
        string email,
        decimal creditLimit)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty.", nameof(email));
        }

        if (creditLimit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(creditLimit));
        }

        return new CustomerRecord(connectionString)
        {
            Email = email,
            CreditLimit = creditLimit
        };
    }

    // Load one customer by Id
    public static async Task<CustomerRecord?> FindAsync(
        int id,
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        var cmd = new SqlCommand(
            "SELECT Id, Email, CreditLimit FROM Customers WHERE Id = @Id",
            conn);

        cmd.Parameters.AddWithValue("@Id", id);

        await using var reader = await cmd.ExecuteReaderAsync(
            CommandBehavior.SingleRow,
            cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CustomerRecord(connectionString)
        {
            Id = reader.GetInt32(0),
            Email = reader.GetString(1),
            CreditLimit = reader.GetDecimal(2)
        };
    }

    // Insert or update, depending on whether Id is set
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        if (Id == 0)
        {
            var insertCmd = new SqlCommand(
                @"INSERT INTO Customers (Email, CreditLimit)
                  VALUES (@Email, @CreditLimit);
                  SELECT SCOPE_IDENTITY();",
                conn);

            insertCmd.Parameters.AddWithValue("@Email", Email);
            insertCmd.Parameters.AddWithValue("@CreditLimit", CreditLimit);

            var result = await insertCmd.ExecuteScalarAsync(cancellationToken);
            Id = Convert.ToInt32(result);
        }
        else
        {
            var updateCmd = new SqlCommand(
                @"UPDATE Customers
                  SET Email = @Email, CreditLimit = @CreditLimit
                  WHERE Id = @Id;",
                conn);

            updateCmd.Parameters.AddWithValue("@Email", Email);
            updateCmd.Parameters.AddWithValue("@CreditLimit", CreditLimit);
            updateCmd.Parameters.AddWithValue("@Id", Id);

            await updateCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        if (Id == 0)
        {
            throw new InvalidOperationException("Cannot delete a transient customer.");
        }

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        var cmd = new SqlCommand(
            "DELETE FROM Customers WHERE Id = @Id",
            conn);

        cmd.Parameters.AddWithValue("@Id", Id);

        await cmd.ExecuteNonQueryAsync(cancellationToken);

        Id = 0;
    }

    // Simple business behavior
    public void ChangeEmail(string newEmail)
    {
        if (string.IsNullOrWhiteSpace(newEmail))
        {
            throw new ArgumentException("Email cannot be empty.", nameof(newEmail));
        }

        Email = newEmail;
    }

    public void IncreaseCreditLimit(decimal delta)
    {
        if (delta <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(delta));
        }

        CreditLimit += delta;
    }
}
```

You can see all three aspects in one place:

- **Data:** `Id`, `Email`, `CreditLimit`

- **Persistence:** `FindAsync`, `SaveAsync`, `DeleteAsync`

- **Behavior:** `ChangeEmail`, `IncreaseCreditLimit`

That is pure Active Record.

## “Before” Minimal API: Controller Doing Everything Manually

Now embed this idea into an ASP.NET Core Minimal API. Let’s start with a _non-_Active Record style endpoint, where the endpoint coordinates everything manually.

```csharp
app.MapPost("/customers/{id:int}/change-email", async (
    int id,
    ChangeEmailRequest dto,
    IConfiguration config,
    CancellationToken ct) =>
{
    var connectionString = config.GetConnectionString("DefaultConnection");
    if (connectionString is null)
    {
        return Results.Problem("Connection string missing.");
    }

    if (string.IsNullOrWhiteSpace(dto.Email))
    {
        return Results.BadRequest("Email cannot be empty.");
    }

    await using var conn = new SqlConnection(connectionString);
    await conn.OpenAsync(ct);

    // Load
    var selectCmd = new SqlCommand(
        "SELECT Id, Email, CreditLimit FROM Customers WHERE Id = @Id",
        conn);

    selectCmd.Parameters.AddWithValue("@Id", id);

    await using var reader = await selectCmd.ExecuteReaderAsync(ct);
    if (!await reader.ReadAsync(ct))
    {
        return Results.NotFound();
    }

    var currentEmail = reader.GetString(1);
    var creditLimit = reader.GetDecimal(2);

    // Business rule: here, in the endpoint
    if (currentEmail == dto.Email)
    {
        return Results.BadRequest("Email is unchanged.");
    }

    // Save
    var updateCmd = new SqlCommand(
        "UPDATE Customers SET Email = @Email WHERE Id = @Id",
        conn);

    updateCmd.Parameters.AddWithValue("@Email", dto.Email);
    updateCmd.Parameters.AddWithValue("@Id", id);

    await updateCmd.ExecuteNonQueryAsync(ct);

    return Results.Ok(new { Id = id, Email = dto.Email, CreditLimit = creditLimit });
});

public sealed class ChangeEmailRequest
{
    public string Email { get; set; } = string.Empty;
}
```

This works, but:

- Persistence is tightly coupled to the endpoint

- Business rule checks live in the endpoint

- Reusing this logic elsewhere means copy and paste

## “After” Minimal API: Endpoint Using Active Record

Now see the same use case implemented through `CustomerRecord`.

```csharp
app.MapPost("/customers/{id:int}/change-email", async (
    int id,
    ChangeEmailRequest dto,
    IConfiguration config,
    CancellationToken ct) =>
{
    var connectionString = config.GetConnectionString("DefaultConnection");
    if (connectionString is null)
    {
        return Results.Problem("Connection string missing.");
    }

    var customer = await CustomerRecord.FindAsync(id, connectionString, ct);
    if (customer is null)
    {
        return Results.NotFound();
    }

    try
    {
        customer.ChangeEmail(dto.Email);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }

    await customer.SaveAsync(ct);

    return Results.Ok(new
    {
        customer.Id,
        customer.Email,
        customer.CreditLimit
    });
});

```

What changed:

- The endpoint no longer knows SQL

- The endpoint no longer cares how `CustomerRecord` persists itself

- Email validation rules live in `ChangeEmail` instead of in the endpoint

This is the “happy path” for Active Record: the endpoint’s job is just HTTP input/output; the record’s job is data plus simple business behavior.

## Where Active Record Shows Up Without Being Named

Even if you never write raw ADO.NET:

- EF Core entities that contain methods that call `SaveChanges`

- Entities that reach up to `DbContext` or service locators

- Static helper methods on your entity types that perform queries

All of these indicate an **Active Record vibe**.

Example of accidental Active Record with EF Core:

```csharp
public class Customer
{
    private readonly AppDbContext _dbContext;

    public Customer(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public int Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public decimal CreditLimit { get; private set; }

    public async Task SaveAsync(CancellationToken ct = default)
    {
        if (Id == 0)
        {
            _dbContext.Customers.Add(this);
        }

        await _dbContext.SaveChangesAsync(ct);
    }
}

```

The entity now:

- Knows about `AppDbContext`

- Decides how to save itself

That is Active Record, just hidden behind EF Core.

## When to Use Active Record

Active Record is not a mistake by default. It shines in specific contexts.

### 1\. Simple, CRUD-heavy domains

If your system is mostly:

- Creates, reads, updates, and deletes records

- Has minimal rules beyond field-level validation

then Active Record can deliver:

- Fast development

- Low conceptual overhead

- Straightforward mapping between the database and code

Examples:

- Internal admin tools

- Simple contact or configuration systems

- Back-office utilities that mirror tables directly

### 2\. Small applications and utilities

For:

- Command-line migration tools

- One-off synchronization apps

- Tiny websites with a handful of screens

Active Record lets you focus on the task instead of on architecture.

### 3\. Early prototypes and spikes

When you are:

- Exploring a problem space

- Validating whether a product idea has legs

Active Record gives you a straight line from concept to working software. If the idea dies, you are not stuck with a bunch of unused abstractions.

### 4\. Edge subsystems

In a larger system, some parts are inherently table-shaped:

- Reporting tables

- Lookup tables and reference data

- Audit logs

Using Active Record at the edges (while using a richer model in the core) can be a pragmatic mix.

A useful rule:

> If the main question is “how do I get and save this row,” Active Record is probably fine.

## When **Not** to Use Active Record

The pattern breaks down once the domain stops being “just rows".

### 1\. Rich, evolving business rules

If you have:

- Cross-aggregate invariants (orders, customers, and products must stay consistent together)

- Complex workflows (sagas, long-running processes)

- Frequent rule changes that must remain coherent

Active Record becomes a liability because:

- Every record class now mixes behavior with persistence

- Rules often involve multiple records, but there is no clear place to express those interactions

In those cases, a Domain Model, along with repositories or data mappers, is a better center of gravity.

### 2\. Persistence concerns infect every rule

When your “business methods” are full of:

- Connection strings

- Queries

- Transaction management

you lose the ability to reason about rules without thinking about the database. You also make testing drastically harder.

### 3\. Testing behavior feels heavy

If testing “change email” requires:

- Setting up a real database

- Managing connections

- Cleaning up test data

then your design makes business rules more expensive to validate than they need to be.

With a separate domain model and a data mapper, you can test rules in memory and leave persistence to separate tests.

### 4\. Long-lived, collaborative systems

In systems that:

- Live for years

- Have teams joining and leaving

- See rules accumulate over time

a clean separation between domain logic and persistence buys flexibility. Active Record tends to tangle those concerns together, making deep refactoring more painful.

A useful rule:

> If the main question is “how should this rule behave over time,” Active Record is not the right center.

## Comparing Active Record with Domain Model + Repository

Let us recast our `CustomerRecord` behavior into a simple Domain Model approach.

### Active Record version

```csharp
// Already shown above
customer.ChangeEmail(newEmail);
await customer.SaveAsync(ct);

```

### Domain Model plus Repository version

Domain entity:

```csharp
public class Customer
{
    public Customer(int id, string email, decimal creditLimit)
    {
        Id = id;
        ChangeEmail(email);
        CreditLimit = creditLimit;
    }

    public int Id { get; }
    public string Email { get; private set; } = string.Empty;
    public decimal CreditLimit { get; private set; }

    public void ChangeEmail(string newEmail)
    {
        if (string.IsNullOrWhiteSpace(newEmail))
        {
            throw new ArgumentException("Email cannot be empty.", nameof(newEmail));
        }

        Email = newEmail;
    }

    public void IncreaseCreditLimit(decimal delta)
    {
        if (delta <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(delta));
        }

        CreditLimit += delta;
    }
}

```

Repository:

```csharp
public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default);
    Task SaveAsync(Customer customer, CancellationToken ct = default);
}

```

Repository implementation (using ADO.NET):

```csharp
public class CustomerRepository : ICustomerRepository
{
    private readonly string _connectionString;

    public CustomerRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing connection string.");
    }

    public async Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var cmd = new SqlCommand(
            "SELECT Id, Email, CreditLimit FROM Customers WHERE Id = @Id",
            conn);

        cmd.Parameters.AddWithValue("@Id", id);

        await using var reader = await cmd.ExecuteReaderAsync(
            CommandBehavior.SingleRow,
            ct);

        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        return new Customer(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetDecimal(2));
    }

    public async Task SaveAsync(Customer customer, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        if (customer.Id == 0)
        {
            var insertCmd = new SqlCommand(
                @"INSERT INTO Customers (Email, CreditLimit)
                  VALUES (@Email, @CreditLimit);",
                conn);

            insertCmd.Parameters.AddWithValue("@Email", customer.Email);
            insertCmd.Parameters.AddWithValue("@CreditLimit", customer.CreditLimit);

            await insertCmd.ExecuteNonQueryAsync(ct);
        }
        else
        {
            var updateCmd = new SqlCommand(
                @"UPDATE Customers
                  SET Email = @Email, CreditLimit = @CreditLimit
                  WHERE Id = @Id;",
                conn);

            updateCmd.Parameters.AddWithValue("@Email", customer.Email);
            updateCmd.Parameters.AddWithValue("@CreditLimit", customer.CreditLimit);
            updateCmd.Parameters.AddWithValue("@Id", customer.Id);

            await updateCmd.ExecuteNonQueryAsync(ct);
        }
    }
}

```

Minimal API endpoint:

```csharp
app.MapPost("/customers/{id:int}/change-email", async (
    int id,
    ChangeEmailRequest dto,
    ICustomerRepository customers,
    CancellationToken ct) =>
{
    var customer = await customers.GetByIdAsync(id, ct);
    if (customer is null)
    {
        return Results.NotFound();
    }

    try
    {
        customer.ChangeEmail(dto.Email);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }

    await customers.SaveAsync(customer, ct);

    return Results.Ok(new { customer.Id, customer.Email, customer.CreditLimit });
});

```

Here:

- The **domain entity** knows the rules

- The **repository** knows persistence

- The endpoint coordinates, but neither “owns” both at once

In more complex domains, that separation scales better than Active Record.

## A Practical Way Forward

You do not need to swear off Active Record or embrace it everywhere. Treat it as one tool in your kit.

- Use Active Record where:
    - The domain is simple
    
    - The lifetime is short
    
    - The code is close to the data

- Use Domain Model + Repository where:
    - Rules are central
    
    - Behavior crosses entities
    
    - The system must evolve for years

A useful exercise for your current codebase:

1. Find a class that both loads/saves itself and contains non-trivial rules.

3. Ask: Is this really just a table row, or is it a key domain concept?

5. If it is just a row, lean into Active Record and keep it small and honest.

7. If it is a key concept, consider splitting behavior from persistence before the complexity grows.

That deliberate choice, more than the pattern name, is what keeps enterprise systems from quietly turning into unstructured scripts that happen to be written in C#.
