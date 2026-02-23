---
title: "Rust Iterators and Functional Combinators"
date: 2025-05-14
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "Iterators-and-Functional-Combinators.png"
---

Welcome to Day 34, and today we are taking a stroll through one of the most satisfying parts of Rust. If you're a C# developer who loves LINQ, this will feel like home, with just enough Rust flavor to keep it interesting.

## LINQ in C#: The Gold Standard

In C#, you probably know and love LINQ. You can chain together operations like `Select`, `Where`, and `ToList` to transform and filter collections elegantly.

```csharp
var numbers = new List<int> { 1, 2, 3, 4, 5 };
var evens = numbers.Where(n => n % 2 == 0).Select(n => n * n).ToList();
```

LINQ enables you to work with IEnumerable and chain operators without needing to write manual loops.

## Rust’s Iterator Pattern: Same Idea, Different Wrapping

Rust has iterators and it offers similar functional combinators like `map`, `filter`, and `collect`. Here is the same example in Rust:

```rust
let numbers = vec![1, 2, 3, 4, 5];
let evens: Vec<i32> = numbers
    .iter()
    .filter(|n| *n % 2 == 0)
    .map(|n| n * n)
    .collect();

println!("{:?}", evens); // [4, 16]
```

The vibe is the same. Chain methods transform the collection. The difference is how Rust enforces ownership and borrowing while keeping things fast and safe.

## Iterators in Rust: Lazy Evaluation

Just like LINQ Rust iterators are lazy. That means the operations like `map` and `filter` do not actually run until you call something like `collect` or `for_each`.

```rust
let numbers = vec![1, 2, 3, 4, 5];
let iter = numbers.iter().map(|n| n * n); // Nothing happens yet

for n in iter {
    println!("{}", n); // Now the squares are calculated
}
```

## Collect: Turning Iterators into Collections

In LINQ you call `ToList` or `ToArray` to materialize the results. Rust uses `collect` which can create different types of collections depending on what you ask for:

```rust
let squares: Vec<i32> = (1..=5).map(|n| n * n).collect();
println!("{:?}", squares); // [1, 4, 9, 16, 25]
```

The type annotation on `squares` tells Rust what to build from the iterator.

## More Fun with Iterators

Here are some other handy methods:

- `sum`: Adds up the values

- `count`: Counts the elements

- `any` and `all`: Check conditions across the elements

Example:

```rust
let numbers = vec![1, 2, 3, 4, 5];
let total: i32 = numbers.iter().sum();
println!("Total: {}", total); // Total: 15

let has_even = numbers.iter().any(|n| *n % 2 == 0);
println!("Contains even number: {}", has_even); // true
```

## Why Rust’s Iterators Are Awesome

- Lazy by default

- Zero-cost abstractions with no runtime overhead

- Ownership and borrowing baked into the design

- Powerful chaining without allocating unless you choose to collect

Rust iterators give you the functional expressiveness of LINQ while staying true to Rust’s philosophy of safety and performance.

## Wrapping It Up

If you enjoy LINQ you will love Rust’s iterators. They let you work with data in a clean readable way without giving up control over performance. Tomorrow we will reflect on the week and talk about the power and challenges of traits and lifetimes. See you then!
