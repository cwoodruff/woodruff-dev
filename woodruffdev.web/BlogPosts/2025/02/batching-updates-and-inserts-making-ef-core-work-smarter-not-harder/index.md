---
title: "Batching Updates and Inserts: Making EF Core Work Smarter, Not Harder"
date: 2025-02-01
categories: 
  - "efcore"
tags: 
  - "net"
  - "c"
  - "data"
  - "databases"
  - "entity-framework-core"
  - "programming"
coverImage: "Batching-Updates-and-Inserts-Making-EF-Core-Work-Smarter-Not-Harder.png"
---

Let’s face it: making multiple database calls is like running back and forth between the kitchen and dining room to serve dinner one plate at a time. It’s inefficient, exhausting, and makes your app look like it’s stuck in the Stone Age. But with **batching** in EF Core, you can wave goodbye to this inefficiency. It's the magic trick that lets you update or insert multiple records in a single trip to the database, making you and your app more productive and efficient.

Batching is all about doing more with less. It minimizes round trips to the database, saves resources, and makes your app faster. Understanding and implementing batching in EF Core is like gaining a superpower- it makes your app work smarter, not harder, and gives you a sense of control and confidence in your development process.

* * *

## **What is Batching?**

Batching combines multiple updates or inserts into a single database operation. Instead of executing a separate `INSERT` or `UPDATE` for every record, EF Core groups them into batches and sends them in one go. It’s like placing one big order at a restaurant instead of going back to the counter for every dish.

Here’s a quick comparison:

### Without Batching

```csharp
foreach (var user in users)
{
    context.Users.Add(user);
    await context.SaveChangesAsync();
}
```

Each `SaveChangesAsync` call sends a separate `INSERT` to the database. Painful.

### With Batching

```csharp
context.Users.AddRange(users);
await context.SaveChangesAsync();
```

One call to `SaveChangesAsync`, one batch of `INSERT`s. Much better!

* * *

## **Why Should You Care?**

Batching in EF Core isn’t just cool—it’s necessary if you care about performance. Here’s why:

1. **Fewer Round Trips**  
    Every database call adds latency. Batching reduces the number of trips, making your app faster.

3. **Lower Resource Usage**  
    Each call to the database consumes resources on both the app and database server. Batching minimizes this overhead.

5. **Better Scalability**  
    When your app grows, batching helps handle larger workloads without crushing your database.

* * *

## **How to Batch Like a Pro**

#### 1\. **Batching Inserts**

Let’s say you’re adding multiple users to the database. Instead of adding them one by one:

```csharp
var newUsers = new List<User>
{
    new User { Name = "Alice", Email = "alice@example.com" },
    new User { Name = "Bob", Email = "bob@example.com" }
};

context.Users.AddRange(newUsers);
await context.SaveChangesAsync();
```

EF Core batches these `INSERT`s into a single database call.

* * *

#### 2\. **Batching Updates**

Updating multiple records? No problem. Use a `foreach` loop and call `SaveChangesAsync` once:

```csharp
var users = await context.Users.Where(u => u.IsActive).ToListAsync();

foreach (var user in users)
{
    user.LastActive = DateTime.UtcNow;
}

await context.SaveChangesAsync();
```

EF Core combines all the `UPDATE`s into a single batch.

* * *

#### 3\. **Handling Large Batches**

EF Core batches operations automatically by default, but there’s a limit to how many commands can fit in a batch. If you’re working with massive datasets, consider processing them in chunks:

```csharp
const int batchSize = 100;

for (int i = 0; i < users.Count; i += batchSize)
{
    var batch = users.Skip(i).Take(batchSize).ToList();
    context.Users.AddRange(batch);
    await context.SaveChangesAsync();
}
```

This ensures you don’t overwhelm the database with a single gigantic batch.

* * *

## **Tips for Efficient Batching**

### **Use AddRange and UpdateRange**

Methods like `AddRange` and `UpdateRange` are your best friends for batching. Use them whenever you’re working with collections.

### **Leverage Transactions**

Combine batching with transactions to ensure all your operations succeed or fail together:  

```csharp
using var transaction = await context.Database.BeginTransactionAsync();

context.Users.AddRange(users);
await context.SaveChangesAsync();

await transaction.CommitAsync();
```

### **Profile Your Queries**

Use tools like SQL Profiler or EF Core’s logging to see how batches are sent to the database. Optimize as needed.

* * *

## **When NOT to Batch**

Batching is fantastic, but it’s not always the right choice. Here are a few cases where batching might not work well:

- **When Operations Depend on Each Other**  
    If one operation relies on the result of another, batching won’t work.

- **Small Datasets**  
    For a handful of records, batching might not offer noticeable benefits.

- **Complex Relationships**  
    If your entities have complex relationships, batching could lead to unexpected results. Always test!

* * *

## **Wrap-Up: Batch It Up!**

Batching updates and inserts in EF Core is a simple yet powerful way to improve performance. By reducing database round trips and leveraging efficient operations, you can make your app faster, lighter, and ready to scale.

So, next time you add or update multiple records, ask yourself: “Am I batching this?” If not, it’s time to level up your EF Core game. Your app—and your database—will thank you.
