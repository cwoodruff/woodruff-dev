---
title: "JSON Columns in SQL Server: Storing & Querying JSON with EF Core"
date: 2025-02-14
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
coverImage: "./images/JSON-Columns-in-SQL-Server-Storing-Querying-JSON-with-EF-Core.webp"
---

Ever wished you could store **semi-structured data** in your database without dealing with complex table relationships? **Good news!** SQL Server has **native JSON support**, and **Entity Framework Core makes working with JSON columns easier than ever**.

Whether you’re handling **dynamic configurations, logging data, or flexible user preferences**, **JSON columns let you mix structured and unstructured data in SQL Server**—without creating dozens of extra tables.

Let’s explore how to **store, query, and manipulate JSON data** in SQL Server using **Entity Framework Core (EF Core)**!

* * *

## **Why Store JSON in SQL Server?**

JSON columns are perfect when you need **flexibility** in your database structure without sacrificing **query performance**.

**Avoid Table Explosion** – No need for hundreds of tiny tables just to store key-value data.  
**Dynamic Data Storage** – Perfect for **storing user preferences, metadata, and logs**.  
**Efficient Queries** – SQL Server lets you **query JSON data using standard SQL functions**.  
**Better Than TEXT Columns** – Unlike plain text, JSON fields **can be indexed and queried efficiently**.

* * *

## **Step 1: Creating a Table with a JSON Column**

Let’s say we’re building an **e-commerce platform and** want to store **product attributes** (like size, color, and material) in a JSON column instead of separate columns.

### **1\. Define the Entity Model**

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string AttributesJson { get; set; } // Stores JSON data
}
```

Instead of **creating multiple columns** for attributes, we **store them as a JSON string**.

* * *

## **Step 2: Configuring JSON Columns in EF Core**

Now, configure EF Core **to use the JSON column**:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>()
        .Property(p => p.AttributesJson)
        .HasColumnType("nvarchar(max)"); // Store as JSON
}
```

**This ensures the JSON column is properly stored in SQL Server**.

* * *

## **Step 3: Inserting JSON Data into SQL Server**

Let’s **save a product** with JSON attributes:

```csharp
var product = new Product
{
    Name = "Running Shoes",
    Price = 99.99m,
    AttributesJson = JsonSerializer.Serialize(new
    {
        Size = "10",
        Color = "Red",
        Material = "Mesh"
    })
};

context.Products.Add(product);
await context.SaveChangesAsync();
```

**Why Serialize to JSON?**

- The **AttributesJson** column is a **string**, so we **serialize** the object before storing it.

- This makes it easy to **store dynamic product attributes** without modifying the database schema.

* * *

## **Step 4: Querying JSON Data in SQL Server**

Now that we’ve stored JSON, **how do we query it?**

### **1\. Querying JSON Using EF Core**

```csharp
var shoes = await context.Products
    .Where(p => p.AttributesJson.Contains("\"Color\":\"Red\""))
    .ToListAsync();
```

**This finds all products where `Color = Red` inside the JSON column!**

* * *

### **2\. Querying JSON Using SQL Server JSON Functions**

SQL Server has **native JSON functions**, so you can query JSON fields like structured data!

```sql
SELECT Name, Price, AttributesJson
FROM Products
WHERE JSON_VALUE(AttributesJson, '$.Color') = 'Red';
```

**SQL Server JSON Functions to Know:**

- **`JSON_VALUE(column, '$.key')`** – Extracts a single value.

- **`JSON_QUERY(column, '$.key')`** – Extracts an object/array.

- **`OPENJSON(column)`** – Converts JSON into **rows and columns**.

* * *

## **Step 5: Querying JSON Data in EF Core with SQL Functions**

We can **combine EF Core with SQL Server’s JSON functions** for more efficient queries:

```csharp
var products = await context.Products
    .Where(p => EF.Functions.JsonValue(p.AttributesJson, "$.Color") == "Red")
    .ToListAsync();
```

**Why is this better?**

- **Runs natively in SQL Server** (instead of filtering in memory).

- **More efficient for large datasets**.

* * *

## **Step 6: Updating JSON Data in SQL Server**

Need to **update a specific field inside JSON** without rewriting the whole string? SQL Server has your back!

```sql
UPDATE Products
SET AttributesJson = JSON_MODIFY(AttributesJson, '$.Size', '12')
WHERE Id = 1;
```

**No need to retrieve the entire JSON string first!**

* * *

## **Step 7: Indexing JSON Data for Faster Queries**

Since JSON is stored as text, **how do we make queries faster?**

### **Create a Computed Column for JSON Values**

```sql
ALTER TABLE Products
ADD Color AS JSON_VALUE(AttributesJson, '$.Color') PERSISTED;
```

Then, **add an index**:

```sql
CREATE INDEX IX_Products_Color ON Products(Color);
```

**Now, querying products by color is lightning fast!**

* * *

## **When Should You Use JSON Columns?**

- **For Storing Dynamic Data** – Great for user settings, metadata, and logs.

- **When Schema Changes Often** – Avoids frequent migrations for new fields.

- **When Data is Read-Heavy, Write-Light** – JSON works best for **frequent reads and occasional writes**.

* * *

## **When NOT to Use JSON Columns**

**Don’t use JSON for relational data!**

- If you need **JOINs, foreign keys, or referential integrity**, stick to **normal relational tables**.

- JSON is great for **flexible data** but **not a replacement for structured tables**.

* * *

## **Wrap-Up: JSON + SQL Server + EF Core = Flexible Data Storage**

SQL Server’s **JSON column support** lets you **store, query, and manipulate semi-structured data** without breaking relational integrity. Combined with **EF Core**, it’s a powerful way to handle **dynamic, evolving data** without constantly altering your database schema.

**Key Takeaways:**

- **Store JSON in SQL Server** for flexible data storage.

- **Query JSON efficiently** using SQL Server’s native JSON functions.

- **Combine EF Core with SQL functions** for optimized queries.

- **Use computed columns & indexes** for faster lookups.

Next time you need to store **dynamic, schema-free data**, consider **JSON columns**!

**How are you using JSON in your EF Core apps? Let’s chat in the comments!**
