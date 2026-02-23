---
title: "Traits in Rust: Interfaces That Do More"
date: 2025-05-09
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "Traits-in-Rust-Interfaces-That-Do-More.png"
---

Welcome to Day 29 where we are stepping into one of Rust’s most powerful features. If you are a C# developer think of traits as interfaces but with some serious upgrades. Traits in Rust define shared behavior just like interfaces do in C#. But Rust’s approach feels more flexible and composable.

## Interfaces in C#: The Starting Point

In C# you are used to defining interfaces like this:

```csharp
public interface ILogger
{
    void Log(string message);
}

public class ConsoleLogger : ILogger
{
    public void Log(string message)
    {
        Console.WriteLine(message);
    }
}
```

The interface defines the contract. Any class that implements the interface agrees to provide those methods. This is solid and works well but it can get cumbersome when you start layering on inheritance or trying to compose behavior across unrelated types.

## Enter Traits in Rust

Rust’s version of interfaces is called a trait. Here is the same idea in Rust:

```rust
pub trait Logger {
    fn log(&self, message: &str);
}

pub struct ConsoleLogger;

impl Logger for ConsoleLogger {
    fn log(&self, message: &str) {
        println!("{}", message);
    }
}
```

The syntax feels similar but here is where Rust starts to shine. Traits can be implemented for structs enums or any type. No inheritance chain required.

## Traits and Generics: Power Couple

Traits in Rust work beautifully with generics. You can use trait bounds to ensure that a generic type implements certain behavior.

```rust
fn process<T: Logger>(logger: &T) {
    logger.log("Processing order");
}

fn main() {
    let logger = ConsoleLogger;
    process(&logger);
}
```

This is like adding `where T : ILogger` in C# but with tighter integration into the type system.

```csharp
public void Process<T>(T logger) where T : ILogger
{
    logger.Log("Processing order");
}
```

The difference is that in Rust the trait system is deeply tied to the compiler's ability to optimize and enforce correctness.

## Default Implementations

Another neat feature of traits is that you can provide default implementations for methods. Interfaces in C# gained something similar with default interface methods but Rust has had this baked in from the start.

```rust
pub trait Logger {
    fn log(&self, message: &str) {
        println!("[Default Logger] {}", message);
    }
}
```

This allows you to define common behavior while still letting specific types override it when needed.

## Blanket Implementations

Rust lets you implement a trait for any type that meets certain conditions. This is called a blanket implementation.

```rust
impl<T: Display> Logger for T {
    fn log(&self, message: &str) {
        println!("{}: {}", self, message);
    }
}
```

You cannot really do this in C# without reflection or heavy use of base classes. Rust makes it part of the language.

## Why Traits Feel More Flexible

- Traits are not tied to inheritance chains

- They work with enums structs and more

- They compose well with generics and type bounds

- The compiler enforces trait usage at compile time with zero runtime cost

This approach pushes you toward composition instead of inheritance. It keeps your code modular and lets behavior be shared without forcing a strict object hierarchy.

## Wrapping It Up

Traits give Rust a way to share behavior that feels familiar to C# developers but with a lot more flexibility. They pair perfectly with generics and encourage you to think about behavior and capability instead of class hierarchies.

Tomorrow we will explore trait objects and how Rust handles dynamic dispatch. See you there!
