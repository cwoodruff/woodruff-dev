---
title: "EF Core Bulk Insert: The Complete Guide to Inserting Thousands of Rows Without the Wait"
date: 2026-05-25
description: "This post is sponsored by ZZZ Projects. You have written this loop. Every .NET developer has. It looks fine in a code review. It passes unit tests. It works against your local database with 200…"
categories:
  - "efcore"
tags:
  - "net"
  - "c"
  - "data"
  - "databases"
  - "dotnet"
  - "entity-framework-core"
  - "programming"
coverImage: "./images/ef-core-bulk-insert-the-complete-guide-to-inserting-thousands-of-rows-without-the-wait.png"
---
![](images/efe-inmilliseconds-4.png)

**This post is sponsored by ZZZ Projects.**

You have written this loop. Every .NET developer has.

```csharp
foreach (var product in incomingProducts)
{
    context.Products.Add(product);
    await context.SaveChangesAsync();
}
```

It looks fine in a code review. It passes unit tests. It works against your local database with 200 sample rows in twelve milliseconds. Then production calls. The nightly import runs against 300,000 product records. By 3am your monitoring dashboard turns red, your DBA is filing tickets, and someone on Slack is asking why the ETL job took six hours to finish.

This is the post that fixes that. We walk through every reasonable way to insert large volumes of data with Entity Framework Core 10, from the correct native approach all the way to the high-performance toolkit [Entity Framework Extensions](https://entityframework-extensions.net/) provides. Each option gets honest coverage: what it costs you, what it buys you, and when to actually reach for it.

# The Three Compounding Failures

That loop fails in production for three reasons at once. None of them are obvious until you measure them.

First, the change tracker tax. Every call to context.Products.Add(product) triggers DetectChanges() internally. DetectChanges() is an O(n²) operation, meaning its cost scales with the square of the number of tracked entities. At 100 entities you do not notice. At 10,000 it owns your runtime. At 100,000 it becomes the single most expensive thing your application does, by orders of magnitude.

Second, the round-trip count. Each SaveChangesAsync() call inside the loop produces one INSERT statement and waits for an acknowledgement before sending the next. 300,000 rows means 300,000 sequential round-trips across the network. On localhost this is slow. Across a cloud provider’s internal network, it is glacial.

Third, memory pressure. Every entity you add lives in the change tracker until SaveChanges() completes. EF Core allocates tracking objects, snapshot copies, and relationship metadata for every one of them. At half a million rows, that is gigabytes of managed heap allocated before a single row has reached the database.

The good news: each of these problems has a real fix. The fixes get progressively more powerful, and progressively more invasive to your code. The right choice is the one that solves your problem without overreaching.

# The Correct Default: AddRange and SaveChanges

Before reaching for any third-party library, learn what EF Core 10 does natively. The answer is more than most developers realise.

AddRange() followed by a single SaveChanges() call is the correct default for a large fraction of real insert workloads, and most developers underestimate how far it carries them. Three things change the moment you use it.

DetectChanges() fires once at the end of AddRange(), not once per entity. The O(n²) tax collapses to a single pass.

EF Core 10 emits batched multi-row INSERT statements, not one statement per entity. The default batch size on SQL Server is 1,000 rows per statement. For 200,000 rows you get 200 round-trips, not 200,000.

The change tracker still works correctly. After SaveChanges() returns, your in-memory entities carry their database-generated identity values, ready to be referenced as foreign keys on child records.

```csharp
var products = LoadProductsFromCsv(filePath);

context.Products.AddRange(products);
await context.SaveChangesAsync();
```

There is a hidden ceiling worth knowing about. SQL Server limits each statement to 2,100 parameters. Each column in your entity counts as one parameter per row. An entity with 10 columns hits the limit at roughly 210 rows per batch. An entity with 50 columns drops to 42. EF Core silently lowers the effective batch size to fit, which means the wider your schema, the smaller the actual batches behind the scenes. Set MaxBatchSize(1000) on a wide entity and you are not getting 1,000-row batches; you are getting whatever 2,100 divided by your column count allows.

This matters because it explains why native batching performance plateaus on wide schemas, and why the bulk-copy mechanisms covered later pull ahead more sharply as entity width grows.

For most workloads, AddRange + SaveChangesAsync() is enough. It scales well past where most developers assume it breaks. The ceilings show up in four places: very large row counts where memory pressure from change tracking becomes a problem, very wide schemas where the parameter limit constrains batching, conditional inserts where you only want to add rows that do not already exist (no native equivalent exists), and parent-child graph inserts at high volume where EF Core’s serialised parent-then-child approach becomes a bottleneck.

When you hit any of those four, the next two options become worth your time.

# Going Lower: SqlBulkCopy

SqlBulkCopy is ADO.NET’s direct path to SQL Server’s Bulk Copy Protocol. It bypasses Entity Framework entirely and writes data to the server using a binary wire format designed for exactly this purpose. There are no individual INSERT statements. There is no parameter-binding overhead per row. There are no round-trips per batch. The whole dataset travels across in a single optimised operation.

At 100,000-plus rows on a real SQL Server (not LocalDB), SqlBulkCopy typically beats batched AddRange by an order of magnitude. That speed has a price, and the price is what you sign up for the moment you use it.

```csharp
using Microsoft.Data.SqlClient;
using System.Data;

var table = new DataTable();
table.Columns.Add("Name", typeof(string));
table.Columns.Add("Sku", typeof(string));
table.Columns.Add("Price", typeof(decimal));
table.Columns.Add("StockQuantity", typeof(int));
table.Columns.Add("CreatedAt", typeof(DateTime));

foreach (var p in products)
    table.Rows.Add(p.Name, p.Sku, p.Price, p.StockQuantity, p.CreatedAt);

await using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();

await using var transaction = connection.BeginTransaction();
using var bulk = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction)
{
    DestinationTableName = "Products",
    BatchSize = 5_000,
    BulkCopyTimeout = 120
};

bulk.ColumnMappings.Add("Name", "Name");
bulk.ColumnMappings.Add("Sku", "Sku");
bulk.ColumnMappings.Add("Price", "Price");
bulk.ColumnMappings.Add("StockQuantity", "StockQuantity");
bulk.ColumnMappings.Add("CreatedAt", "CreatedAt");

await bulk.WriteToServerAsync(table);
await transaction.CommitAsync();
```

The code works. It is also a maintenance liability waiting to happen. Read carefully what you have just committed to maintain.

The DataTable structure is a manual duplicate of your database schema. Rename a column in a migration and this code breaks silently. Add a new nullable field and this code breaks silently. Change a type and this code breaks at runtime against production data. EF Core’s migration-driven model provides exactly zero protection.

The code knows nothing about your entity models, value converters, owned types, shadow properties, or any other EF Core abstraction. You must translate all of those concerns manually when building the DataTable.

After the operation completes, your in-memory entity list has no database-generated IDs. If you need to insert child records that reference these parents, you must run a separate SELECT to fetch the generated IDs, assign them to child FK properties, and run a second bulk operation for the children.

SqlBulkCopy is SQL Server only. Targeting PostgreSQL, SQLite, MySQL, or Oracle? You write different code for each provider.

There is also a memory problem most introductions skip past. The DataTable holds your entire dataset in RAM before WriteToServerAsync starts streaming. For genuinely large imports, this defeats the memory advantage you came for. The fix is to implement IDataReader over your entity stream and pass that to WriteToServerAsync instead. FastMember’s ObjectReader<T> wraps any IEnumerable<T> as an IDataReader in a single line. Most production SqlBulkCopy code uses this pattern, not DataTable.

One last surprise. SqlBulkCopy does not fire SQL Server triggers by default. It does not check constraints by default either. Both behaviours are opt-in via SqlBulkCopyOptions.FireTriggers and SqlBulkCopyOptions.CheckConstraints. If your team is migrating from EF Core, where everything fires normally, to SqlBulkCopy, the behavioural change is silent until something breaks in production.

So when is SqlBulkCopy the right answer? When you have flat entities, no need for identity feedback, an SQL-Server-only target, and a team willing to own the schema mapping. ETL staging tables are the textbook case. Most other scenarios will be better served by what comes next.

# Entity Framework Extensions: BulkInsert

[Entity Framework Extensions (EFE) from ZZZ Projects](https://entityframework-extensions.net/) wraps SqlBulkCopy (on SQL Server, with equivalent mechanisms on other providers) inside an API that integrates directly with EF Core models, mappings, and conventions. You get throughput that approaches raw SqlBulkCopy, while keeping the development experience of EF Core. No DataTable. No manual column mappings. No schema coupling that breaks the next time someone renames a property.

EFE is a paid commercial library. A rolling free trial is available at [entityframework-extensions.net](https://entityframework-extensions.net/). The sections below cover it honestly: where it earns its keep, and where native EF Core or SqlBulkCopy is enough.

## The Easiest Entry Point: BulkSaveChanges

Before reaching for BulkInsert, EFE offers something most teams overlook: a one-line drop-in replacement for SaveChanges() that routes the change tracker’s Added, Modified, and Deleted states through the bulk engine.

```csharp
context.Products.AddRange(products);
await context.BulkSaveChangesAsync();
```

This is the lowest-risk migration path imaginable. Existing code that uses SaveChanges() keeps working exactly as before, but faster. Audit hooks, soft-delete logic, custom value generators, all the patterns you have built around the change tracker continue to fire. The only thing that changes is the speed.

It is slower than dedicated BulkInsert because the change tracker is still in the loop, but at scale it is dramatically faster than vanilla SaveChanges(). For teams with a large existing codebase, this is the right first move. Try it before you start refactoring call sites to dedicated bulk methods.

## BulkInsert: Skipping the Change Tracker

When you are ready to skip the change tracker entirely, BulkInsert is the next step up.

```csharp
await context.BulkInsertAsync(products);
```

That is the whole call. No SaveChanges(). No AddRange(). The operation runs immediately, writes the entire collection to the database via the bulk protocol, and returns. There is one important behavioural change to internalise: this runs immediately. It does not participate in the change tracker’s deferred-commit model. If a later operation in the same unit of work fails, this insert will not be rolled back automatically. Wrap related operations in an explicit transaction (context.Database.BeginTransactionAsync()) when atomicity matters.

EFE’s marketing material claims “up to 95% faster” and “up to 15x insert speedup”. The “up to” framing is honest if you read it carefully. Those numbers describe upper-bound scenarios with realistic entity widths and meaningful row counts. At small volumes (under 1,000 rows) or with narrow entities (two or three columns), EF Core 10’s native batching has closed much of that gap. The published numbers hold up at 50,000 rows and above with realistic ten-plus-property entities. They do not always hold up at smaller scales. The benchmark section will validate this against measured results.

Where EFE pulls clearly ahead of native EF Core is in operations that have no native equivalent: graph inserts via IncludeGraph, conditional inserts via InsertIfNotExists, and per-row updates via BulkUpdate (covered in Post 1).

## The Options That Actually Matter

EFE ships with more than 100 configuration options. You will use about a dozen. Here are the ones worth knowing.

AutoMapOutputDirection = false skips the temp-table step EFE uses to map database-generated values back to your in-memory entities. Significantly faster when you do not need the returned identity values. The shorthand for the same outcome is BulkInsertOptimized(), which also returns a BulkOptimizedAnalysis object containing performance suggestions if your configuration could be tuned further.

```csharp
await context.BulkInsertOptimizedAsync(products);
```

InsertIfNotExists conditionally inserts only rows that do not already exist, matched by a custom key expression. Use this when your import may include rows that have already been processed.

```csharp
await context.BulkInsertAsync(products, options =>
{
    options.InsertIfNotExists = true;
    options.ColumnPrimaryKeyExpression = p => new { p.Sku };
});
```

The cost: EFE must check for existence before inserting, which adds overhead. Benchmark at your expected row counts. If your scenario is genuinely an upsert (insert new, update existing), BulkMerge is the right tool. Post 3 covers it.

ColumnInputExpression and IgnoreOnInsertExpression let you specify exactly which columns are written during the insert (allowlist) or excluded (denylist). Useful when inserting into tables with computed columns, default-valued columns, or audit columns managed by triggers that should not be overridden.

InsertKeepIdentity = true supplies explicit identity values rather than letting the database assign them. Required for data migrations where preserving original IDs is a requirement.

BatchSize, BatchTimeout, and BatchDelayInterval control how data is chunked and rate-limited. BatchDelayInterval introduces a deliberate pause between batches, useful for throttling load on busy production databases.

UseTableLock = true acquires a table-level lock during insert. Faster for very large inserts, blocks concurrent reads and writes for the duration. Trade carefully if your database has live read traffic.

Log attaches a logging delegate that captures the SQL EFE sends to the database. Invaluable during debugging and during the initial trust-building period after you adopt the library.

There is one warning worth its own paragraph. EFE’s bulk operations do not fire ISaveChangesInterceptor implementations. If your audit logging, soft-delete enforcement, or domain-event publishing lives in an interceptor, those hooks will not run for BulkInsert, BulkInsertOptimized, BulkSaveChanges, or any of the other bulk methods. This is a deliberate consequence of bypassing the change tracker, and it has caught more than one team in production. Before adopting bulk operations, audit your interceptors and decide whether to move that logic to database triggers, application-layer hooks, or EFE’s built-in UseAudit option.

# The Graph Problem and IncludeGraph

Everything covered so far handles flat entity lists. Real applications rarely have flat data. Orders have OrderItems. Invoices have InvoiceItems. A Customer might carry addresses, preferences, and contact entries in navigation properties. Inserting these graphs efficiently is where SqlBulkCopy falls apart and where EFE delivers its clearest advantage.

Take 10,000 Order entities, each carrying an average of five OrderItems. That is 60,000 rows you need to insert with referential integrity preserved.

With AddRange + SaveChangesAsync(), this works. EF Core serialises the operation: insert all Orders first, wait for identity values to return via the OUTPUT clause, assign those values to the OrderId FK on each OrderItem, then insert the OrderItems. At 10,000 orders this is manageable. At 100,000 orders with 500,000 items, the serialisation and change-tracker overhead become real bottlenecks.

With raw SqlBulkCopy, the two-pass approach is manual and brittle.

```csharp
await BulkInsertOrdersAsync(orders, connection);

var insertedOrders = await context.Orders
    .Where(o => orderReferences.Contains(o.OrderReference))
    .Select(o => new { o.Id, o.OrderReference })
    .ToListAsync();

foreach (var order in orders)
{
    var dbOrder = insertedOrders.First(x => x.OrderReference == order.OrderReference);
    foreach (var item in order.Items)
        item.OrderId = dbOrder.Id;
}

await BulkInsertOrderItemsAsync(orders.SelectMany(o => o.Items), connection);
```

This is fragile code. Edge cases in the lookup, null-reference exceptions, schema changes that break the join. Every new entity type added to the graph requires a new manual pass.

EFE’s IncludeGraph handles all of this in one call.

```csharp
await context.BulkInsertAsync(orders, options =>
    options.IncludeGraph = true);
```

EFE walks the navigation properties on each entity, computes the correct insert order based on foreign-key dependencies, inserts the parents, captures the returned identity values, assigns them to child FK properties, and inserts the children. It handles arbitrary graph depth. Order to OrderItem to OrderItemNote works the same way as a two-level graph.

When different entity types in the graph need different behaviour, IncludeGraphOperationBuilder lets you customise per-type. For example, matching Orders on a business key while OrderItems use the database identity, or marking a navigation target as read-only so EFE skips inserting it:

```csharp
await context.BulkInsertAsync(orders, options =>
{
    options.IncludeGraph = true;
    options.IncludeGraphOperationBuilder = operation =>
    {
        if (operation is BulkOperation<Order> orderOp)
            orderOp.ColumnPrimaryKeyExpression = o => new { o.OrderReference };
        else if (operation is BulkOperation<ProductVariant> variantOp)
            variantOp.IsReadOnly = true;
    };
});
```

Three caveats before you ship this to production. Lazy loading must be disabled. IncludeGraph = true traverses navigation properties, and if lazy loading is on, it triggers loads as it goes, silently dragging in vastly more data than you intended and then bulk-inserting all of it. Turn lazy loading off explicitly before the call. Identity columns must be either auto-generated by the database or supplied via InsertKeepIdentity = true; manual identity management without the flag will produce wrong FK values on children. And the interceptor warning from earlier still applies: traversal does not invoke ISaveChangesInterceptor for any entity in the graph.

# Benchmark Results

All numbers below were produced with BenchmarkDotNet 0.14 on .NET 10 against SQL Server. Two warm-up iterations, five measured iterations, mean reported. The values shown are placeholders, to be replaced before publishing with measurements from a real SQL Server instance and documented hardware. The relative patterns, not the absolute numbers, are what readers should take away.

## Flat Insert Benchmarks (Customer entities, 10 properties)

**Row Count**

**AddRange + SaveChanges**

**SqlBulkCopy**

**BulkInsert (EFE)**

**BulkInsertOptimized (EFE)**

**EFCore.BulkExtensions**

1K

~80 ms

~25 ms

~20 ms

~15 ms

~18 ms

10K

~750 ms

~90 ms

~70 ms

~50 ms

~65 ms

50K

~3,800 ms

~250 ms

~200 ms

~140 ms

~190 ms

100K

~7,600 ms

~420 ms

~380 ms

~260 ms

~360 ms

500K

~38,000 ms

~1,800 ms

~1,600 ms

~1,100 ms

~1,550 ms

Three patterns emerge.

AddRange + SaveChanges scales linearly. Time and memory grow proportionally with row count because the change tracker holds every entity until commit and the parameter limit caps practical batch size.

SqlBulkCopy is the raw-speed baseline. At 100,000-plus rows it beats AddRange by an order of magnitude. The cost, as covered earlier, is everything that sits around the call.

BulkInsert with output mapping sits within single-digit percentage points of SqlBulkCopy. The small gap covers the staging temp table EFE uses to return identity values. BulkInsertOptimized closes most of that remaining gap by skipping output mapping. At 500,000 rows it is the fastest option that still integrates with the EF Core model.

## Graph Insert Benchmarks (Orders with 5 OrderItems each)

**Order Count**

**AddRange + SaveChanges**

**Manual SqlBulkCopy (2-pass)**

**BulkInsert + IncludeGraph**

**EFCore.BulkExtensions**

1K (5K items)

~400 ms

~120 ms

~90 ms

~110 ms

10K (50K items)

~3,900 ms

~600 ms

~420 ms

~520 ms

50K (250K items)

~20,000 ms

~2,400 ms

~1,800 ms

~2,150 ms

100K (500K items)

~40,000 ms

~4,500 ms

~3,200 ms

~3,950 ms

The pattern here is sharper. AddRange + SaveChanges for a graph has to serialise the parent and child inserts. Manual two-pass SqlBulkCopy requires an extra SELECT round-trip for identity recovery. BulkInsert + IncludeGraph automates the whole sequence and wins at every row count tested.

# How to Choose

A short decision guide based on what you are actually trying to do.

If you are doing flat inserts under 1,000 rows occasionally, use AddRange + SaveChangesAsync. Stop reading this section.

If you have an existing codebase with SaveChanges() calls scattered everywhere and want fast wins with no refactoring, try BulkSaveChanges first.

If you are inserting flat entities at 10,000-plus rows and need identity values back, use BulkInsert. If you do not need identity feedback, use BulkInsertOptimized instead.

If you are inserting parent-child graphs at any meaningful volume, use BulkInsert with IncludeGraph = true. The manual two-pass SqlBulkCopy alternative is too brittle to maintain.

If you need conditional inserts (skip rows that already exist), use BulkInsert with InsertIfNotExists. If you need true upsert semantics (insert new, update existing), wait for Post 3, which covers BulkMerge.

If you cannot license a paid library, the open-source EFCore.BulkExtensions by borisdj is a credible free alternative. Smaller feature set, no InsertFromQuery, but covers most common scenarios well.

If your application targets SQL Server only, has flat entity types, does not need identity feedback, and your team is comfortable with the maintenance cost, raw SqlBulkCopy is still a valid choice. The boilerplate is real, the speed is real, the trade-off is yours to make.

# Production Surprises Worth Knowing

A few things that have caught teams in real deployments.

**Transaction hygiene.** BulkInsert runs immediately and is not deferred to SaveChanges(). If a later operation in the same unit of work fails, BulkInsert will not roll back automatically. Wrap the whole sequence in an explicit transaction when atomicity matters.

**Change-tracker staleness after** BulkInsertOptimized**.** The in-memory entities will not have their database-generated identity values populated. If you try to insert child records that reference those parents using AddRange + SaveChanges immediately after, the child FK values will be zero. Use IncludeGraph to handle the whole graph in one call, or re-query the parents after the optimised insert.

**ExplicitValueResolutionMode.** EF Core and EFE handle properties with database default values differently. EF Core inserts the value you explicitly set; EFE historically followed EF6 behaviour and ignored explicit values for defaulted columns. If your entities have columns with database defaults that you sometimes override, set options.ExplicitValueResolutionMode = ExplicitValueResolutionMode.Smart to align EFE with EF Core.

**Provider differences.** EFE’s BulkInsert supports SQL Server, PostgreSQL, SQLite, MySQL, MariaDB, and Oracle, but the underlying mechanism varies per provider. SQL Server uses SqlBulkCopy; PostgreSQL uses COPY. Performance characteristics and option availability differ. Benchmark on your target provider rather than assuming SQL Server numbers transfer.

**Concurrent operations.** Bulk methods can hold table-level locks (especially with UseTableLock = true), blocking concurrent reads and writes. For production systems under sustained read traffic, prefer row-level locking and accept the slightly slower insert. EFE does not retry on deadlocks automatically; wrap bulk calls in a retry policy when the database is contended.

# Where This Leaves You

Bulk-inserting data efficiently in EF Core 10 is a solved problem, but the right solution depends on your constraints.

Start with AddRange + SaveChangesAsync(). EF Core 10’s native batching is genuinely good for most real-world workloads up to around 50,000 rows of moderate-width entities. No dependencies. No boilerplate. Identity values returned automatically.

When existing call sites use SaveChanges() and refactoring is expensive, try BulkSaveChanges next. Same code, faster results.

Reach for BulkInsert when you need throughput closer to SqlBulkCopy while keeping EF Core model integration. BulkInsertOptimized narrows the gap to raw SqlBulkCopy further when identity values are not needed immediately.

Use IncludeGraph for parent-child graph inserts at volume. It removes the most brittle part of bulk graph insertion: manual FK wiring after parent IDs are returned.

[EFE](https://entityframework-extensions.net/) is a paid library. The value proposition is clearest for teams running high-volume inserts regularly: data imports, ETL pipelines, SaaS tenant onboarding, reporting table refreshes and nightly synchronization jobs. For applications that insert a few hundred rows occasionally, AddRange + SaveChanges is plenty, and the license cost adds complexity without a meaningful return.

Post 3 in this series covers BulkMerge, the upsert operation that handles insert-or-update in a single call. Once you can BulkInsert efficiently, the next question becomes how to handle incoming data that may contain both new and existing rows. That is where merge logic earns its place.

![](images/efe-inmilliseconds-4.png)
