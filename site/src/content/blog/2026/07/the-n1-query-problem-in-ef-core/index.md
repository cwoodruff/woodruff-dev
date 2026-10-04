---
title: "The N+1 Query Problem in EF Core: Detection, Diagnosis, and Permanent Fixes"
date: 2026-07-07
description: "An order summary page renders in 40 milliseconds against your development database. The same page takes eleven seconds in production. You open the profiler, and the controller method looks clean. You…"
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
coverImage: "./images/the-n1-query-problem-in-ef-core.png"
---
[![EF Extensions header banner with logo, listing Bulk Insert, Bulk Update, Bulk Merge and the tagline 'Save thousands of entities — in milliseconds'.](images/efe-inmilliseconds.png)](https://entityframework-extensions.net/)

An order summary page renders in 40 milliseconds against your development database. The same page takes eleven seconds in production. You open the profiler, and the controller method looks clean. You reread the LINQ, and it reads correctly. You check the indexes, and every column you need is covered. The code passed review. The tests are green. And the page still crawls.

The cause is almost always the same defect, and it hides where nobody looks. Here is the shape of it.

```csharp
var orders = await context.Orders
    .Where(o => o.OrderDate >= startOfMonth)
    .ToListAsync();
 
foreach (var order in orders)
{
    // Every read of order.Customer fires its own SELECT
    Console.WriteLine($"{order.OrderNumber}: {order.Customer.Name}");
}
```

One query loads the orders. Then every trip through the loop touches order.Customer, and every touch fires its own SELECT against the database. Load 200 orders, and you have not run one query. You have run 201.

Do the arithmetic that your development database hides from you. Each of those little queries is fast, say three milliseconds round-trip. Two hundred of them in sequence is 600 milliseconds of pure waiting, and that is before the database does a single millisecond of real work. Scale the page to 5,000 orders, and the same code issues 5,001 queries. The controller that felt instant on your laptop now holds a connection open for the better part of a minute.

This defect has a name: the N+1 query problem. One query to load the parent rows, then N more to load a related value for each one. It survives code review because every line is idiomatic. It survives testing because test databases hold hundreds of rows, where production holds millions. And it survives profiling right up until a customer complains, because the ORM hides the flood of SQL behind property access that looks like plain C#.

This post covers the whole problem: what N+1 actually is, including three shapes that most articles skip, how to catch it on purpose rather than by accident, and the full ladder of permanent fixes in Entity Framework Core 10. [Entity Framework Extensions](https://entityframework-extensions.net/) from [ZZZ Projects](https://zzzprojects.com/) gets honest treatment along the way. The classic read-side N+1 is a problem native EF Core solves completely, and this post says so plainly rather than reaching for a paid library to fix something the free tools already handle.

# **The Four Shapes of N+1**

Ask most developers to define N+1, and they will describe lazy loading. That answer is correct and incomplete. The same query storm arrives through four different doors, and three of them are immune to the fix people reach for first. Name all four now, so the rest of the post can point at them precisely.

**Shape one is lazy loading in a loop.** Navigation property access inside iteration, each access firing a silent query. This is the classic, and it needs lazy loading proxies or an injected ILazyLoader to happen at all.

**Shape two is the explicit query loop.** No proxies, no magic. A developer writes context.Customers.FirstOrDefault(c => c.Code == row.Code) inside a foreach and runs it once per incoming record. Import and reconciliation code is full of this shape. It ignores every lazy-loading fix in the book because there is no lazy loading involved. It needs set-based thinking instead.

**Shape three is the serializer storm.** Return tracked entities straight from an API action with lazy loading on, and the JSON serializer walks every navigation property while building the response, firing queries after your action method has already returned. The storm comes from a layer nobody thinks to profile.

**Shape four is the write-side N+1.** Call SaveChanges once per entity inside a loop, and you have the same defect pointed at inserts and updates instead of reads. Earlier posts in this series cover it in full, so this post names it as family and moves on.

The thread joining all four is one sentence: per-item round trips where one set-based operation would have done the job. Hold that sentence. Every fix below is a variation on it.

# **Detection: Making the Invisible Visible**

Here is the rule that separates engineers from guessers: never diagnose N+1 by feel. Count the queries. A page either issues a bounded number of SQL statements or it does not, and you can measure which in under a minute. Here is the toolkit, ordered from zero setup to production grade.

Start with the cheapest look you have. Point EF Core’s logger at the console and read what it emits.

```csharp
optionsBuilder
    .UseSqlServer(connectionString)
    .LogTo(Console.WriteLine, LogLevel.Information)
    .EnableSensitiveDataLogging(); // development only
```

Run the page once and watch the output. If you see the same SELECT against Customers repeated forty times with only the parameter changing, you have found your storm. One change to note in EF Core 10: constant values in logged SQL are redacted by default, so turn on EnableSensitiveDataLogging in development if you want to see the actual parameter values rather than a redacted marker.

Reading logs by eye works for a quick check. It does not scale to a test suite, and it will not prevent the defect from recurring. For that, count queries in code with an interceptor.

```csharp
public sealed class QueryCountInterceptor : DbCommandInterceptor
{
    private int _count;
    public int Count => _count;
    public void Reset() => Interlocked.Exchange(ref _count, 0);
 
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Interlocked.Increment(ref _count);
        return base.ReaderExecuting(command, eventData, result);
    }
 
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _count);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }
}
```

Wire that counter into an integration test with a budget, and N+1 stops being a production surprise.

```csharp
[Fact]
public async Task Order_summary_stays_within_query_budget()
{
    _queryCounter.Reset();
 
    await _client.GetAsync("/orders/summary");
 
    Assert.True(_queryCounter.Count <= 10,
        $"Order summary issued {_queryCounter.Count} queries; budget is 10.");
}
```

That single test is the highest-value habit in this entire post. A page that used to silently balloon to 200 queries now breaks the build the moment someone reintroduces the loop. You have turned an invisible runtime defect into a loud compile-time one.

Want the runtime itself to refuse the defect? Turn the lazy loading event into an exception.

```csharp
optionsBuilder.ConfigureWarnings(warnings =>
    warnings.Throw(
        CoreEventId.NavigationLazyLoading,
        CoreEventId.DetachedLazyLoadingWarning));
```

Now any lazy load in an environment that bans it throws on the spot during development, instead of degrading quietly in production. The same mechanism catches the multiple-collection warning from the split-query section.

When a storm shows up in a database-side capture, and you need to trace it back to the exact line of C# that caused it, tag your queries.

```csharp
var orders = await context.Orders
    .TagWith("OrderSummary: monthly customer roll-up")
    .Where(o => o.OrderDate >= startOfMonth)
    .ToListAsync();
```

The tag rides along as an SQL comment, so the offending call site is one search away when you are staring at a query captured from SQL Server Extended Events or the query store. Cheap, permanent, and almost nobody uses it.

# **Fix Tier 1: Eager Loading with Include**

You have found the storm. Now kill it. The first tool on the ladder handles shape one directly. Tell EF Core which related data the code path needs, and it fetches everything in one query instead of N.

```csharp
var orders = await context.Orders
    .Where(o => o.OrderDate >= startOfMonth)
    .Include(o => o.Customer)
    .ToListAsync();
```

One SELECT with a JOIN replaces the 201 queries from the opening. The customer name is already loaded by the time the loop runs, so no property access fires anything.

Need only part of a collection? Filtered Include loads the subset.

```csharp
var orders = await context.Orders
    .Include(o => o.Items.Where(i => i.IsActive))
    .ToListAsync();
```

For a relationship that nearly every query needs, you can push the Include into the model so you never forget it.

```csharp
modelBuilder.Entity<Order>()
    .Navigation(o => o.Customer)
    .AutoInclude();
 
// Opt out on a specific query
var orders = await context.Orders
    .IgnoreAutoIncludes()
    .ToListAsync();
```

That convenience has a cost, and honesty demands stating it. Auto-includes hide loading behavior at the call site, so a query that looks cheap in the LINQ can drag three related tables you forgot were configured. When you need the bare entity, IgnoreAutoIncludes opts out per query.

Include has a ceiling of its own. It loads whole entities. When your page needs three columns from the order and one from the customer, Include hauls every column of both across the wire and materializes full tracked objects you will never mutate. That waste is the opening for the strongest fix, two tiers down. First, a trap hiding inside the fix you just applied.

# **Fix Tier 2: Cartesian Explosion and Split Queries**

Include solves N+1. Applied carelessly, it creates a worse problem in its place.

Include two sibling collections on the same query and watch what the JOIN does to your row count.

```csharp
var orders = await context.Orders
    .Include(o => o.Items)
    .Include(o => o.Payments)
    .ToListAsync();
```

Each order with ten items and five payments no longer returns as one row. It returns as fifty, the cross product of both collections, with every order column duplicated across all fifty. Load a thousand such orders, and you have pulled fifty thousand rows across the wire to represent fifteen thousand. You traded a round-trip problem for a data-volume problem, and on wide parent rows, the second problem bites harder than the first.

EF Core sees this coming and warns you about it by name, the multiple-collection-include warning. Route that warning through the Throw configuration from the detection section and EF Core refuses to run the cartesian query at all until you make a decision.

The decision is one method call. AsSplitQuery breaks the single JOIN into one query per collection.

```csharp
var orders = await context.Orders
    .Include(o => o.Items)
    .Include(o => o.Payments)
    .AsSplitQuery()
    .ToListAsync();
```

No row multiplication. Each collection comes back in its own clean result set, and EF Core stitches them together in memory. The trade is real and worth stating: split queries mean multiple round-trip requests, and without a wrapping transaction, they offer no guarantee that all three queries saw the same snapshot of the database. Single queries give you one consistent read at the risk of a cartesian blowup. Neither choice is universally correct, which is exactly why EF Core makes you choose.

Set the default once if your app leans one way.

```csharp
optionsBuilder.UseSqlServer(connectionString,
    o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
```

Then override per query wherever the default is wrong, in either direction, with AsSplitQuery or AsSingleQuery.

# **Fix Tier 3: Projections, the Strongest Fix**

Include and split queries to manage the cost of loading entities. Projection removes the entity from the equation entirely, and with it the whole category of defect.

Select exactly the columns the page needs into a shape that is not an entity.

```csharp
var summary = await context.Orders
    .Where(o => o.OrderDate >= startOfMonth)
    .Select(o => new OrderSummaryDto
    {
        OrderNumber  = o.OrderNumber,
        CustomerName = o.Customer.Name,
        Total        = o.Total
    })
    .ToListAsync();
```

Read what that query does not do. It does not load Customer as an entity. It does not track anything. It does not leave a single navigation property lying around for a loop or a serializer to trip over. EF Core composes one SQL statement that returns four columns, and the result carries no proxies at all. There is no storm to fix here, because there is nothing in the result for a loop or a serializer to touch.

Push aggregates into the database while you are there.

```csharp
var summary = await context.Orders
    .Where(o => o.OrderDate >= startOfMonth)
    .Select(o => new OrderSummaryDto
    {
        OrderNumber   = o.OrderNumber,
        CustomerName  = o.Customer.Name,
        LineItemCount = o.Items.Count,
        Total         = o.Items.Sum(i => i.Quantity * i.UnitPrice)
    })
    .ToListAsync();
```

That count and that sum run as SQL on the server. The line items never travel to your process. Compare that to the Include approach, which would have loaded every line item into memory just so you could call Count on the list.

Projections nest, so a parent-with-children DTO composes cleanly.

```csharp
var orders = await context.Orders
    .Select(o => new OrderDto
    {
        OrderNumber = o.OrderNumber,
        Items = o.Items.Select(i => new ItemDto
        {
            Sku      = i.Sku,
            Quantity = i.Quantity
        }).ToList()
    })
    .ToListAsync();
```

One more benefit rides along for free. A projection to a non-entity type is never tracked, so the change-tracker cost that an earlier post in this series measured in detail simply does not apply here. No snapshots, no identity resolution, no tracked graph eating heap.

There is a narrow middle ground worth knowing. When you already hold one entity in memory and need one relationship loaded conditionally, explicit loading fits.

```csharp
var order = await context.Orders.FirstAsync(o => o.Id == id);
 
if (order.RequiresAudit)
{
    await context.Entry(order)
        .Collection(o => o.Items)
        .LoadAsync();
}
```

Use it for exactly that case. Put explicit loading inside a loop, and you have rebuilt N+1 by hand with more ceremony. The tool is fine. The loop is the crime.

# **Lazy Loading Itself: Keep It or Kill It**

Every fix so far treats symptoms of lazy loading. Step back and ask the harder question: should lazy loading be on at all?

It reaches your app one of two ways. The proxies package plus virtual navigations.

```csharp
optionsBuilder
    .UseLazyLoadingProxies()
    .UseSqlServer(connectionString);
```

Or an injected ILazyLoader for proxy-free lazy loading. Both make navigation property access fire silent queries, and both turn shape one and shape three from possible into likely.

The argument for switching it off in the service and API code is direct. Lazy loading defers a database call until some code reads a property. In a request-scoped world, that whenever can land after your action returns, inside the serializer, on a connection you assumed was done. Shape three is not a quirk. It is the predictable result of combining lazy loading with returning entities from endpoints. The structural fix is the projection tier you just read: return DTOs instead of entities, and the serializer has nothing to walk.

Lazy loading is not always wrong. A long-lived desktop client with a context that stays open, or an exploratory script where you are poking at data by hand, both benefit from loading relationships on demand. Judge it by the lifetime of your context, not by fashion.

If you decide to kill it, here is the checklist. Drop the proxies package or remove the opt-in call. Set context.ChangeTracker.LazyLoadingEnabled to false for any scope where you want it off, but cannot remove the package yet. Then turn on the throw-on-lazy-load event from the detection section, so anything you missed announces itself loudly rather than lurking.

# **The Lookup-List N+1 and the Parameter Ceiling**

Shape two earns its own section, because the obvious fix runs headfirst into a change EF Core 10 shipped that this series has already documented.

Picture import code. Five thousand records arrive from an external system, and for each one, you check whether it already exists.

```csharp
foreach (var row in importedRows)
{
    var existing = context.Customers
        .FirstOrDefault(c => c.Code == row.Code);
    // ... one round trip per row, 5,000 times
}
```

Five thousand records, five thousand round trips. The same N+1 defect, wearing the clothes of a validation loop. The set-based rewrite is obvious and correct: pull the keys, ask the database once.

```csharp
var codes = importedRows.Select(r => r.Code).ToList();
 
var existing = await context.Customers
    .Where(c => codes.Contains(c.Code))
    .ToListAsync();
```

One query. N+1 collapses to 1. And then you deploy it, and it throws in production against the very data volume it was meant to handle.

Here is why. EF Core 10 changed how it translates Contains over a collection. EF Core 8 and 9 packed the list into a single JSON parameter and unpacked it server-side with OPENJSON. EF Core 10 moved the default back toward emitting one scalar parameter per element, with a setting to control the strategy. On SQL Server, that runs straight into the hard ceiling of 2,100 parameters per statement. Your Contains query with 500 keys sailed through testing. The same query with 5,000 keys does not run slowly in production. It does not run at all. It throws.

The native ways around it are real, and each carries a bill. Chunk the key list into batches under the ceiling and issue one query per chunk, which works and adds round-trip proportional to the number of chunks.

```csharp
var results = new List<Customer>();
foreach (var chunk in codes.Chunk(2000))
{
    var found = await context.Customers
        .Where(c => chunk.Contains(c.Code))
        .ToListAsync();
    results.AddRange(found);
}
```

Switch the parameterized-collection mode back to the JSON translation per query or globally, which restores the single-parameter behavior along with the query-plan trade-offs that motivated the EF Core 10 change in the first place. Or hand-write a temp-table join, which performs well and hands you back exactly the boilerplate an ORM was supposed to remove.

Composite keys make it worse. Match incoming rows on a pair like Email plus PhoneNumber, and Contains has no clean translation at all. The usual escape, gluing the fields into one concatenated string key, throws away every index on those columns and turns a seek into a scan. This is the honest edge of what native EF Core does gracefully, and the natural handoff to the next section.

# **Where Entity Framework Extensions Fits, Honestly**

Say the uncomfortable part first, because the editorial mandate for this series demands it: Entity Framework Extensions is not the fix for the classic lazy loading N+1. Include, split queries, and projections solve shapes one and three completely, natively, and for free. If you came here hoping a paid library would rescue you from a loop that Include already handles, the honest answer is that you do not need one. EFE earns its place at two specific points on this map, both of them past the edge of what native EF Core does cleanly.

## **Large-List Lookups: WhereBulkContains and BulkRead**

The parameter ceiling from the previous section is where the first one lands. WhereBulkContains filters a query against an in-memory list of any size, and it does not build a giant IN clause. It bulk-loads your list into a temporary table and joins against it.

```csharp
var existing = context.Customers
    .WhereBulkContains(importedRows)
    .ToList();
```

Any size means any size. Five thousand keys, fifty thousand, more. The temp-table join does not care about the 2,100-parameter limit, because there are no per-element parameters to count. The method is deferred and returns IQueryable, so it composes with Where and Include like any other LINQ operator.

Need the results immediately instead of a composable query? BulkRead is the same mechanism with a ToList baked in.

```csharp
var existing = context.Customers.BulkRead(codes);
```

The composite-key case that defeated native Contains is a first-class citizen here. Pass an anonymous type and match on as many columns as you like.

```csharp
var existing = context.Customers
    .BulkRead(importedRows, x => new { x.Email, x.PhoneNumber });
```

There is even an overload for the relationship version of the storm, filtering related entities inside an Include while returning all the root rows.

```csharp
var orders = context.Orders
    .Include(o => o.Items)
    .WhereBulkContains(o => o.Items, productIds, i => i.ProductId)
    .ToList();
```

And the exclusion case, which native code usually writes as a giant NOT IN, translates as a clean NOT EXISTS.

```csharp
var missing = context.Customers
    .WhereBulkNotContains(importedRows)
    .ToList();
```

## **List-Side Filtering: WhereBulkContainsFilterList**

Flip the question around. Instead of which database rows match my list, import code constantly asks which of my list items already exist in the database. Written naively, that is one existence check per item, N+1 all over again. WhereBulkContainsFilterList answers it in one round trip and hands back the matching items from your in-memory list rather than from the table.

```csharp
// Items from the import that already exist in the database
var toUpdate = context.Customers
    .WhereBulkContainsFilterList(importedRows);
 
// Items from the import that are new
var toInsert = context.Customers
    .WhereBulkNotContainsFilterList(importedRows);
```

Its sibling, WhereBulkNotContainsFilterList, returns the items that are missing. Together, they let import code split an incoming batch into update-these and insert-those with two queries total, the read-side companion to the import pipeline covered earlier in the series.

## **Honest Caveats for the EFE Read Methods**

None of this is free of edges, and pretending otherwise would break the trust this series runs on.

Provider support is the first limit. Per the official documentation, WhereBulkContains and its siblings support SQL Server and PostgreSQL only. On SQLite, MySQL, or Oracle, you are back to the native chunking approach from the previous section.

Inheritance is the second. The documentation states that the TPH, TPT, and TPC mapping strategies are not supported by these methods. If your lookup targets an entity in an inheritance hierarchy, test before you commit, because this is exactly the kind of edge a mid-sized codebase hits late and at the worst time.

These methods also do not chain onto EF Core’s native ExecuteUpdate or ExecuteDelete. EFE’s own UpdateFromQuery and DeleteFromQuery fill that role and compose with WhereBulkContains directly, covered earlier in this series.

And the temp-table machinery has fixed overhead per call. For a list of a few hundred items, plain Contains is simpler and probably faster, because the setup cost of building and populating a temp table is real. The crossover point where WhereBulkContains pulls ahead is a number to measure on your schema, not a slogan to repeat. The benchmark section quantifies it rather than asserting it.

One disclosure, stated plainly: Entity Framework Extensions is a paid commercial library with a rolling free trial at entityframework-extensions.net. This post is part of a sponsored series for ZZZ Projects, and the coverage above is written to read the same whether or not a check changed hands, because a recommendation you cannot trust is worth nothing to you and nothing to them.

## **The Write-Side N+1, Referenced Not Repeated**

SaveChanges-per-entity loops, per-row update loops, and hand-rolled upsert loops are all write-side members of the same N+1 family. BulkSaveChanges, BulkInsert, BulkUpdate, and BulkMerge are their fixes, and earlier posts in this series cover them in depth. Naming them here keeps the family complete without turning this read-side post into a rerun.

# **Benchmark Results**

Numbers first, opinions second. Every measurement below comes from BenchmarkDotNet 0.15.4 on .NET 10 against a standalone SQL Server instance, never LocalDB. Database-backed runs use the Monitoring strategy with a single invocation per iteration and a fresh DbContext for each one, so no cached change-tracker state carries between runs and every lazy load truly reaches the database. The data is generated once from a fixed seed, so every run sees identical rows.

## **Table 1: Rendering a Parent List with Child Data**

**Parents**

**Lazy loading loop**

**Include (single query)**

**Include + AsSplitQuery**

**Projection (Select)**

200

381.252 ms

17.003 ms

15.455 ms

6.920 ms

1K

1,717.090 ms

52.188 ms

39.218 ms

12.034 ms

5K

7,348.441 ms

265.661 ms

155.730 ms

37.699 ms

The query-count column on the lazy-loading row is the whole story. The milliseconds matter, but the count is what production feels: one query becomes hundreds while not a single line of the loop looks wrong.

## **Table 2: Lookup of an In-Memory Key List**

**Keys**

**Query-per-item loop**

**Contains (default mode)**

**Contains (chunked)**

**WhereBulkContains (EFE)**

500

109.506 ms

4.303 ms

5.752 ms

19.725 ms

2K

687.329 ms

48.164 ms

32.506 ms

17.503 ms

5K

2,048.023 ms

throws (parameter ceiling)

81.672 ms

25.349 ms

50K

18,852.338 ms

throws (parameter ceiling)

815.603 ms

88.957 ms

The two throwing cells are the finding, not a hole in the data. A Contains rewrite that sailed through testing at 500 keys fails outright at 5,000, and a query that fails outright is the exact thing that turns a routine change into a production incident.

## **Table 3: Existence Partitioning of an Import List**

**Items**

**Existence check per item**

**Two Contains queries (chunked)**

**WhereBulkContainsFilterList (EFE)**

1K

468.71 ms

13.03 ms

59.62 ms

10K

4,016.02 ms

167.61 ms

109.78 ms

100K

37,364.20 ms

1,691.01 ms

4,892.86 ms

Every row reflects a mechanism rather than a coincidence. The per-item loop scales straight up with item count, since the work is one round trip per element and nothing amortizes that. Projection moves the least data on the parent-list table: four columns instead of whole entities, and no tracking. Split query pulls ahead of the single query as the sibling collections grow, sidestepping the cartesian product that the single JOIN produces. Plain Contains holds its own at small list sizes, then hits the wall at the parameter ceiling while WhereBulkContains stays flat, its temp-table join carrying no per-element parameter to count. Read the tables for the shape first and the absolute numbers second, because the shape is what repeats on your hardware.

# **Decision Guide**

**Scenario**

**Recommended Approach**

**Why**

Loop reads navigation properties on loaded entities

Include / ThenInclude, or a projection when only some columns are needed

Shape one; solved natively, no library required

Multiple sibling collection Includes, large collections

AsSplitQuery

Avoids cartesian row multiplication; accept the extra round trips

Page or endpoint needs a subset of columns

Select into a DTO

Strongest fix; removes lazy loading and tracking cost together

API returns entities and queries fire during serialization

Return DTOs via projection; switch lazy loading off in API code

Shape three; a structural fix beats configuration

Per-item database lookup inside a loop over an in-memory list

Contains for small lists; WhereBulkContains or BulkRead (EFE) for large lists or composite keys

Shape two; the parameter ceiling and composite keys are the dividing line

Which of my list items already exist in the database

WhereBulkContainsFilterList / WhereBulkNotContainsFilterList (EFE)

One round trip replaces N existence checks

SaveChanges called once per entity in a loop

Move SaveChanges outside the loop; BulkSaveChanges or bulk methods at volume

Shape four; covered in earlier posts

Non SQL Server or PostgreSQL provider with a large lookup list

Chunked Contains or parameterized-collection mode tuning

The EFE read methods do not support the provider, per official docs

# **Production Notes**

Query budgets belong in CI. The interceptor counter from the detection section, wired into integration tests with a per-endpoint limit, is the one habit that keeps N+1 from creeping back after you have cleaned it up. If you adopt a single idea from this post, adopt that one.

Global query filters, including the named filters new in EF Core 10, compose with every fix here and quietly add predicates to your generated SQL. When a query shape surprises you, check the filters before you suspect the fix.

AsNoTracking still earns its place on any read-only path that materializes real entities. It does nothing for projections, which are untracked by definition, so do not sprinkle it on a Select and imagine it helped.

Sometimes the honest fix for a per-item lookup is neither a join nor a bulk read. Reference data that changes once a week and gets queried once per row wants a cache, and a small IMemoryCache beats every query shape when the data barely moves.

Last, the crossover discipline. The fixed overhead of the EFE temp-table methods means the point where they overtake native Contains belongs in your team’s written guidance once measured. Reaching for WhereBulkContains on a fifty-item list is cargo cult. Reaching for it on a fifty-thousand-item list is engineering. Know which one you are doing.

# **Where This Leaves You**

N+1 is one defect with four faces. Lazy loading loops and serializer storms fall to Include and projections. Query loops fall to set-based reads. Save loops fall to bulk writes. Detection is a discipline you can automate: count the queries, put a budget on them in tests, and make lazy loads throw. Do that, and the storm you cannot see becomes a storm your build will not let you ship.

On the library question, the summary is the same one the post opened with. Native Entity Framework Core 10 owns the read-side fixes for the classic shapes, and you should reach for Include, split queries, and projections there without hesitation or a purchase order. [Entity Framework Extensions](https://entityframework-extensions.net/) earns its license at the edges the native tools leave rough: lookup lists past the parameter ceiling, composite-key matching, one-query existence partitioning, and the write-side bulk operations the rest of this series covers. Hit those edges often, and the library pays for itself in a week. Never leave the classic shapes, and you will not need them for this problem. A sponsored post that tells you otherwise would not be worth your time.

Fix the placement of SaveChanges first, always. Count your queries second. Everything else is choosing the right tool once you can finally see the problem.

***Sponsored content in partnership with ZZZ Projects.***

[![EF Extensions header banner with logo, listing Bulk Insert, Bulk Update, Bulk Merge and the tagline 'Save thousands of entities — in milliseconds'.](images/efe-inmilliseconds.png)](https://entityframework-extensions.net/)
