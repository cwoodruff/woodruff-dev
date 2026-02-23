---
title: "Week 4: Reflecting on Errors and Structure"
date: 2025-05-08
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "Reflecting-on-Errors-and-Structure.png"
---

Day 28 marks the end of Week 4 and it is time to pause for a quick reflection. We have covered a lot of ground this week from organizing Rust code with modules and crates to handling errors with grace instead of chaos. Coming from a C# background this week might have felt both familiar and refreshingly different.

## Rust Keeps You Honest

If there is one theme that runs through Rust’s approach to error handling and code organization it is this: Rust keeps you honest. It makes sure you declare your intentions clearly and handle failure up front rather than hoping everything goes smoothly and catching surprises at runtime.

In C# we are used to the flexibility of exceptions. You can throw any object that inherits from `Exception` and catch it wherever you want or not at all. This leads to custom exception hierarchies with classes like `UserNotFoundException` or `InvalidOrderException`. That works but it often leaves error handling as an afterthought. Exceptions bubble up through layers and unless you are deliberate about your catch blocks you might miss important failure points.

Rust flips this story. Errors are part of the function signature. They are right there in your face:

```rust
fn read_file(path: &str) -> Result<String, io::Error> {
    let mut file = File::open(path)?;
    let mut contents = String::new();
    file.read_to_string(&mut contents)?;
    Ok(contents)
}
```

This is not optional. If something can fail the function signature tells you and the compiler makes sure you handle it.

## Structure Matters

When it comes to organizing code C# gives you namespaces and assemblies. You can sprinkle classes across folders and rely on conventions to keep things sane. Rust demands more structure with its module system. Your folder layout directly impacts how you structure your code.

```rust
// src/main.rs
mod services;

fn main() {
    services::process();
}
```

```rust
// src/services.rs
pub fn process() {
    println!("Processing service logic");
}
```

You cannot just toss things anywhere and hope the compiler figures it out. Modules and visibility rules make your structure explicit.

In C# you might declare something `internal` or `public`. In Rust everything is private by default and you use `pub` to opt into visibility. It nudges you toward deliberate API boundaries.

## Composability Over Inheritance

C# leans on object-oriented patterns like inheritance and interfaces to share behavior. Rust takes a more composable approach. Instead of base classes you use traits and generics to define behavior. Instead of class hierarchies you use enums and pattern matching to handle different states.

```rust
enum OrderStatus {
    Pending,
    Shipped,
    Delivered,
    Cancelled,
}

fn print_status(status: OrderStatus) {
    match status {
        OrderStatus::Pending => println!("Order is pending"),
        OrderStatus::Shipped => println!("Order has shipped"),
        OrderStatus::Delivered => println!("Order delivered"),
        OrderStatus::Cancelled => println!("Order cancelled"),
    }
}
```

This makes state handling explicit and the compiler helps ensure you do not miss a case. No base classes no inheritance chains just clear data models and behavior.

## Rust Makes You Slow Down In a Good Way

One of the takeaways from this week is that Rust asks you to slow down and be intentional. You cannot skip error handling. You cannot hide behind inheritance. You have to design your data flow and error paths clearly.

And while that might feel like extra work up front it pays off when you revisit your code months later. The structure is not just for the compiler it is there to help you and your team keep things maintainable and understandable.

## Wrapping Up Week 4

This week highlighted one of Rust’s biggest strengths. It nudges you toward making better choices without forcing you into heavy patterns. It lets you design small focused pieces that compose well together.

Next week we will get into traits generics lifetimes and closures. Buckle up because we are about to take the flexibility of Rust’s type system for a spin.
