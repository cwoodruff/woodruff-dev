---
title: "Panic! vs Exceptions: Stop the World or Handle It?"
date: 2025-05-05
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "Panic-vs-Exceptions-Stop-the-World-or-Handle-It.png"
---

```rust
fn main() {
    panic!("Something went terribly wrong!");
}
```

Okay, we're on Day 25, and today we’re stepping into the world of failure again. But this time, it’s the catastrophic kind. We’re not talking about the "file didn’t open" kind of error. Nope, we’re talking about "game over, stop everything, hit the eject button" failure.

In .NET, you’re familiar with exceptions. In Rust, there’s something called **panic!**. But these two aren’t quite the same thing. Rust draws a hard line between **recoverable errors** and **unrecoverable failures**, and understanding that line is a big mindset shift for anyone coming from C#.

## C# Exceptions: One-Size-Fits-All Failure

In C#, exceptions handle just about everything that goes wrong:

```csharp
try
{
    var number = int.Parse("not a number");
}
catch (FormatException ex)
{
    Console.WriteLine($"Oops: {ex.Message}");
}
```

Whether it’s a minor issue or a catastrophic failure, C# throws an exception. You decide if and where to catch it. But because _everything_ is an exception, from a file not found to an out-of-memory error, it can be hard to tell whether you should handle it or let the app crash.

That’s where Rust takes a different approach.

## Rust's Two Types of Failure

In Rust, failure comes in two flavors:

1. **Recoverable Errors:** Things that might go wrong, but you can handle (e.g., file not found, bad input). This is where `Result<T,E>` shines.

3. **Unrecoverable Errors:** Things that should never happen (e.g., index out of bounds, logic bug, critical assumptions violated). This is where **panic!** steps in.

## Meet panic!

Panic in Rust is an intentional, immediate crash. It’s like Rust throwing its hands up and saying, "Nope, this is not okay, I’m out."

Example:

```rust
fn main() {
    let numbers = [1, 2, 3];
    println!("Number: {}", numbers[5]); // panic! here: index out of bounds
}
```

Or explicitly:

When panic happens, Rust **unwinds the stack** by default, cleaning up resources as it goes. You can also configure Rust to abort immediately if you prefer.

## But Wait! Isn’t That Like Throwing an Exception?

Yes… but with clearer intent.

In Rust:

- If it returns `Result<T,E>`, you’re expected to handle it.

- If it panics, it’s because the program hit a state that should _never_ happen.

Compare that to C#, where the difference between something like `IOException` and `OutOfMemoryException` can feel blurry unless you’re intentionally splitting catch blocks:

```csharp
try
{
    // risky work
}
catch (IOException ex)
{
    // recoverable
}
catch (OutOfMemoryException ex)
{
    // probably shouldn't even try to recover
    throw;
}
```

In Rust, that separation is built into the design.

## Should You Ever Catch a Panic?

Technically, yes, Rust does provide a way to catch panics with `std::panic::catch_unwind`. But the philosophy is: **don’t try to recover from panics unless you absolutely have to** (like in a test harness or maybe an embedded system).

In .NET terms, think of panics more like `Environment.FailFast()` than exceptions. They’re the "nuke it from orbit" option.

## Fault Domains and Failure Boundaries

C# developers often talk about **fault domains,** sections of your system that can fail without bringing down the whole app. You might catch exceptions at a service boundary and return a graceful error response.

Rust’s equivalent is pushing you to use `Result` at those boundaries. Panics are reserved for true logic errors, not for I/O failure or bad user input.

## Wrapping It Up

Rust’s philosophy is pretty clear:

- **Recoverable problems? Use** `**Result<T,E>**`**.**

- **Unrecoverable issues? Let it panic.**

By keeping these two paths separate, Rust helps you write code that’s safer, easier to reason about, and less likely to blow up in production from unexpected "oops, we forgot to catch that" moments.

Next up, we’ll look at crafting your own custom error types in Rust. Because when things _do_ go wrong, let’s at least make the error messages helpful!
