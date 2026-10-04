---
title: "Enterprise Patterns for ASP.NET Core Minimal API: Data Mapper Pattern"
date: 2025-12-19
categories: 
  - "patterns"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "patterns"
  - "programming"
coverImage: "./images/Enterprise-Patterns-for-ASP.NET-Core-Minimal-API-Data-Mapper-Pattern.png"
---

If every interesting class in your system secretly knows a connection string, your domain is not a model. It is a thin layer of code on top of a data access layer.

You see it when:

- Domain classes inject `DbContext` directly

- Entities call `SaveChanges` on their own

- Simple rule tests require poking at a real database

At that point, changing a column name feels risky. Changing an important rule feels worse because it's tangled with SQL and infrastructure.

The Data Mapper pattern exists to cut that knot. It handles moving data between your object model and your data store. The mapper worries about tables. The domain worries about behavior.

This post walks through a clean C# implementation of the Data Mapper, shows a before-and-after Minimal API, and makes a clear case for when the pattern earns its complexity and when it does not.

## What Data Mapper Actually Does

In plain terms, a Data Mapper:

- Knows how to load domain objects from a data source

- Knows how to persist those objects back to the data source

- Keeps the domain model ignorant of any mapping details

So your `Customer` class focuses on credit rules, not on how to run `SELECT` and `UPDATE`.

As your rules and workflows grow beyond trivial, that split stops being academic. It becomes the difference between a model that can evolve and one that stays glued to the current schema.

## The Customer Domain Class: No SQL Allowed

Start with a domain object that understands customers, not tables.

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

    public void UpgradeCredit(decimal newLimit)
    {
        if (newLimit < CreditLimit)
        {
            throw new InvalidOperationException(
                "New limit must not be lower than current limit.");
        }

        CreditLimit = newLimit;
    }
}

```

Key traits:

- No `DbContext`

- No connection string

- No SQL constructs

The type knows how to validate and change its own state. It does not know how to reach the database.

## The Data Mapper Contract

Next, define a mapper interface that describes how to move `Customer` instances to and from storage.

```csharp
public interface ICustomerMapper
{
    Task<Customer?> FindAsync(int id, CancellationToken cancellationToken = default);
    Task SaveAsync(Customer customer, CancellationToken cancellationToken = default);
}

```

This interface belongs in your domain or application layer. It is an abstraction: the implementation might use raw ADO.NET, EF Core, Dapper, or something else entirely.

## A Concrete SQL Mapper

Now implement `ICustomerMapper` using plain ADO.NET. In a real project you may wrap EF Core instead, but the principle is identical.

```csharp
using System.Data;
using System.Data.SqlClient;

public class CustomerSqlMapper : ICustomerMapper
{
    private readonly string _connectionString;

    public CustomerSqlMapper(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Customer?> FindAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
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

        var customerId = reader.GetInt32(0);
        var email = reader.GetString(1);
        var creditLimit = reader.GetDecimal(2);

        return new Customer(customerId, email, creditLimit);
    }

    public async Task SaveAsync(
        Customer customer,
        CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        var cmd = new SqlCommand(
            @"UPDATE Customers
              SET Email = @Email, CreditLimit = @CreditLimit
              WHERE Id = @Id",
            conn);

        cmd.Parameters.AddWithValue("@Email", customer.Email);
        cmd.Parameters.AddWithValue("@CreditLimit", customer.CreditLimit);
        cmd.Parameters.AddWithValue("@Id", customer.Id);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}

```

Now the responsibilities are clear.

- `Customer` represents behavior and state

- `CustomerSqlMapper` knows how to translate that state into SQL operations

The domain model stays free to evolve. The mapper absorbs the pain of schema changes.

## Before: Minimal API That Talks Directly To SQL

Take a simple use case: raise a customer's credit limit.

Here is the version that ignores Data Mapper and lets the endpoint juggle everything.

```csharp
app.MapPost("/customers/{id:int}/upgrade-credit", async (
    int id,
    UpgradeCreditRequest dto,
    IConfiguration config,
    CancellationToken ct) =>
{
    if (dto.NewLimit <= 0)
    {
        return Results.BadRequest("New limit must be positive.");
    }

    var connectionString = config.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Problem("Missing connection string.");
    }

    await using var conn = new SqlConnection(connectionString);
    await conn.OpenAsync(ct);

    // Load
    var selectCmd = new SqlCommand(
        "SELECT Id, Email, CreditLimit FROM Customers WHERE Id = @Id",
        conn);

    selectCmd.Parameters.AddWithValue("@Id", id);

    await using var reader = await selectCmd.ExecuteReaderAsync(
        CommandBehavior.SingleRow,
        ct);

    if (!await reader.ReadAsync(ct))
    {
        return Results.NotFound();
    }

    var currentEmail = reader.GetString(1);
    var currentLimit = reader.GetDecimal(2);

    // Domain rule, expressed in the endpoint
    if (dto.NewLimit < currentLimit)
    {
        return Results.BadRequest("New limit must not be lower than current limit.");
    }

    // Save
    reader.Close();

    var updateCmd = new SqlCommand(
        "UPDATE Customers SET CreditLimit = @NewLimit WHERE Id = @Id",
        conn);

    updateCmd.Parameters.AddWithValue("@NewLimit", dto.NewLimit);
    updateCmd.Parameters.AddWithValue("@Id", id);

    await updateCmd.ExecuteNonQueryAsync(ct);

    return Results.Ok(new
    {
        Id = id,
        Email = currentEmail,
        CreditLimit = dto.NewLimit
    });
});

public sealed class UpgradeCreditRequest
{
    public decimal NewLimit { get; set; }
}

```

This endpoint knows:

- Validation rules

- Where the `Customers` table lives

- How to construct and update a customer row

It works. It also makes every rule that relates to credit limits harder to reuse. Any change touches both HTTP code and SQL.

## After: Minimal API That Uses Data Mapper And Domain Model

Now use `Customer` and `ICustomerMapper` to reshape the same endpoint.

First, register the mapper in `Program.cs`:

```csharp
builder.Services.AddScoped<ICustomerMapper>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Missing connection string.");
    return new CustomerSqlMapper(connectionString);
});

```

Then define the endpoint:

```csharp
app.MapPost("/customers/{id:int}/upgrade-credit", async (
    int id,
    UpgradeCreditRequest dto,
    ICustomerMapper mapper,
    CancellationToken ct) =>
{
    if (dto.NewLimit <= 0)
    {
        return Results.BadRequest("New limit must be positive.");
    }

    var customer = await mapper.FindAsync(id, ct);
    if (customer is null)
    {
        return Results.NotFound();
    }

    try
    {
        customer.UpgradeCredit(dto.NewLimit);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }

    await mapper.SaveAsync(customer, ct);

    return Results.Ok(new
    {
        customer.Id,
        customer.Email,
        customer.CreditLimit
    });
});

```

The endpoint now:

- Accepts input and returns output

- Delegates rules to `Customer.UpgradeCredit`

- Delegates persistence to `ICustomerMapper`

HTTP becomes transport, not orchestration. SQL becomes infrastructure, not scattered inline strings.

The credit rule itself sits in one place:

```csharp
public void UpgradeCredit(decimal newLimit)
{
    if (newLimit < CreditLimit)
    {
        throw new InvalidOperationException(
            "New limit must not be lower than current limit.");
    }

    CreditLimit = newLimit;
}

```

Any other client can reuse that rule without duplicating it.

## How Data Mapper Relates To Active Record And Repository

Data Mapper looks similar to Active Record at first glance. Both deal with storing objects. The critical difference lies in who owns that responsibility.

- Active Record: the domain object loads and saves itself

- Data Mapper: a separate component handles loading and saving

Repositories often sit on top of Data Mappers:

- Repository exposes operations in domain terms, such as `GetById` or `FindByEmail`

- Repository uses Data Mappers internally to talk to the data store

In other words, Data Mapper is the plumbing. Repository is the collection-like facade above it.

## When To Use Data Mapper

Data Mapper is not free. It introduces an abstraction layer. The key is to make sure that layer pays for itself.

Situations where it usually does.

### Complex or Growing Domain Models

If your domain has:

- Rich behavior that goes beyond simple field checks

- Important invariants that cross multiple entities

- Rules that change frequently

you need domain classes that can change shape without breaking persistence every time. Data Mapper decouples those concerns.

You can refactor `Customer` extensively and only adjust mapping code in one place.

### Systems With Shifting Persistence Requirements

Consider scenarios like:

- Migrating from one relational database to another

- Introducing a read model or cache alongside the primary database

- Moving from SQL to a combination of SQL and document stores

Without Data Mapper, your domain usually references concrete persistence APIs. That makes change painful.

With Data Mapper, you can introduce alternative implementations behind the same interface, then switch them through configuration.

### Testability And Design Clarity

If you want to test `Customer.UpgradeCredit` without spinning up a database, Data Mapper makes that trivial. Mock or fake the mapper. Test domain objects directly.

Unit tests on domain types become small and fast. Integration tests focus on verifying that mappers talk to the database correctly.

### Multiple Data Sources Behind One Domain Model

Sometimes the same domain object is backed by:

- A relational database in production

- An in-memory store during testing

- A cache for read-heavy paths

Implement `ICustomerMapper` several ways, then choose the appropriate implementation at runtime. The domain code does not know or care.

A useful filter: if you expect to evolve both your rules and your persistence over time, Data Mapper rarely feels optional.

## When Data Mapper Is Overkill

There are places where Data Mapper adds complexity without a real payoff.

### Thin, CRUD Only Systems

If the entire application:

- Creates, reads, updates, and deletes rows

- Contains almost no business rules

a heavy mapping layer may slow you down for little gain. In that world, Active Record or direct use of EF Core in controllers can be sufficient, as long as you acknowledge the tradeoff.

### Short-lived Tools And Scripts

For:

- One-time migration tools

- Simple ETL processes

- Internal utilities with limited scope

you can often accept tighter coupling between logic and persistence. Long-term maintainability matters less.

### Purely Data Centric Components

Reporting subsystems and analytics pipelines often speak in terms of tables and queries by design. If there is no meaningful object model, Data Mapper does not help much. The data is the model.

The danger appears when these areas grow into real domains without anyone noticing, still wrapped in table centric thinking.

## Data Mapper In An EF Core World

Modern ORMs such as EF Core already perform object relational mapping. That can tempt you to think the pattern is “built in” and therefore not worth discussing.

Two clarifications help.

First, EF Core acts as a sophisticated Data Mapper:

- It maps classes to tables or views

- It tracks state and generates insert and update statements

Second, persistence ignorance still matters:

- Keep your domain types free of direct `DbContext` references

- Prefer fluent configurations in an infrastructure project rather than scattering attributes across domain classes

- Avoid putting queries and `SaveChanges` logic directly on domain types

You can treat EF Core as one implementation detail behind an abstraction such as `ICustomerRepository` or `ICustomerMapper`, leaving the rest of the system free to pretend a database does not exist.

## Integrating Data Mapper Into An ASP.NET Core Solution

A typical structure might look like this.

- `MyApp.Domain`
    - Domain entities like `Customer`, `Order`, and domain services
    
    - Interfaces for `ICustomerMapper`, `IOrderMapper` or repositories

- `MyApp.Infrastructure`
    - Implementations such as `CustomerSqlMapper`, EF Core DbContexts, configurations
    
    - Transaction and unit of work implementations

- `MyApp.Application`
    - Application services or use case handlers that coordinate domain objects and mappers

- `MyApp.Web`
    - Minimal API endpoints or MVC controllers that call into application services

Dependencies flow inward:

- Web depends on Application and Domain

- Application depends on Domain and the abstractions for mapping

- Infrastructure depends on Domain and Application, and provides concrete implementations that DI wires together

Domain types never reference infrastructure. That is the entire point.

## Migrating From Active Record To Data Mapper

Many teams start with Active Record or controller centric logic because it is quick. The question is how to move once complexity crosses a line.

A practical path.

1. Identify a class that both encapsulates rules and knows how to hit the database.

3. Extract a pure domain class without any persistence code. Move business methods there.

5. Create a mapper or repository that:
    - Uses the old persistence code
    
    - Constructs and saves the new domain object

7. Update endpoints and services to use the new domain type and mapper instead of the old mixed class.

9. Retire or thin out the original Active Record object as other code migrates.

You do not need a full rewrite. You only need to choose one rule at a time and give it a cleaner home.

## Closing Thought

Data Mapper is not glamorous. It is plumbing. That is precisely why it matters.

If your domain model is the part of the system you care about most, the system that carries your rules and your value, it deserves freedom from table details. Every time you let SQL drift into those classes, you trade that freedom for a bit of short term convenience.

Treat mapping as infrastructure. Let your domain forget the database exists.
