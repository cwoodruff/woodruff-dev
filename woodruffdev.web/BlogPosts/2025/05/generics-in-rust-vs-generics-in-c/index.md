---
title: "Generics in Rust vs Generics in C#"
date: 2025-05-11
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "Generics-in-Rust-vs-Generics-in-C.png"
---

Welcome to Day 31. Today, we're diving into generics. If you are coming from C# this is probably familiar ground. You know and love List<T>, Dictionary<TKey, TValue>, and all the type-safe goodness that generics bring to your code.

Rust has generics too but with a twist. The syntax might look similar but Rust’s approach gives you some extra control and a few new things to think about.

## Generic Basics: The Same but Different

Let us start with the basic syntax comparison. In C# you might write something like:

```csharp
public class Box<T>
{
    public T Value { get; set; }

    public Box(T value)
    {
        Value = value;
    }
}
```

In Rust the equivalent looks like this:

```rust
pub struct Box<T> {
    value: T,
}

impl<T> Box<T> {
    pub fn new(value: T) -> Self {
        Box { value }
    }
}
```

So far so good. Both languages let you define types and functions that work with any type.

## Adding Constraints: Traits vs where T : Interface

In C# you often constrain generics like this:

```csharp
public void Process<T>(T item) where T : IDisposable
{
    item.Dispose();
}
```

In Rust you use trait bounds:

```rust
pub fn process<T: std::fmt::Display>(item: T) {
    println!("{}", item);
}
```

Rust even lets you write the constraint separately with a `where` clause just like C#:

```rust
pub fn process<T>(item: T)
where
    T: std::fmt::Display,
{
    println!("{}", item);
}
```

The trait system in Rust is roughly equivalent to interfaces in C#. They both define shared behavior that can be enforced through generics.

## Variance and Ownership

One of the subtle differences between Rust and C# generics is variance. In C# you can mark interfaces and delegates with `out` and `in` to control covariance and contravariance.

```csharp
public interface IProducer<out T>
{
    T Produce();
}
```

Rust handles variance through its ownership model and lifetime system. There is no `out` or `in` keyword. Instead variance is inferred based on how types are used and the borrow checker ensures safety.

This makes variance feel a bit more automatic but it also means you sometimes need to think about lifetimes and references when using generics in Rust.

## Type Inference: Helpful in Both

Both C# and Rust provide type inference to make using generics easier. In C# you might rely on `var` to avoid specifying the generic type explicitly.

```csharp
var box = new Box<int>(42);
```

In Rust the compiler often infers types for you as well:

```rust
let box = Box::new(42);
```

If Rust cannot infer the type it will tell you and you can specify it explicitly:

```rust
let box: Box<i32> = Box::new(42);
```

## Compile Time Guarantees

One of Rust’s big strengths is that generic constraints are checked at compile time with no runtime overhead. Because Rust uses monomorphization it generates a concrete version of the code for each type you use. This is similar to how C++ templates work.

In C# generics use a type-erased model for reference types and JIT specialization for value types. This makes C# generics flexible but sometimes slower at runtime for certain scenarios.

## Wrapping It Up

Generics in Rust feel familiar if you come from C#. The syntax lines up well and the core ideas are the same. The differences come down to how constraints are expressed with traits instead of interfaces and how Rust leans on the borrow checker and lifetimes to manage safety.

Tomorrow we will step into lifetimes and see how Rust makes sure your references never outlive their welcome. See you then!
