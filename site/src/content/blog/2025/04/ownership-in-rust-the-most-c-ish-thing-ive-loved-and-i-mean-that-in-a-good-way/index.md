---
title: "Ownership in Rust: The Most C++-ish Thing I’ve Loved (and I Mean That in a Good Way)"
date: 2025-04-18
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "./images/Ownership-Borrowing-and-the-Compilers-Wisdom.png"
---

Let’s get one thing out of the way: as a C# developer, I’ve never had to think too hard about memory. The garbage collector (GC) is always there, lurking in the background, sweeping up after my code, like a very polite, very invisible butler.

But Rust? Rust doesn’t _do_ garbage collection. There’s no `GC.Collect()`, no memory profiler needed to chase leaks from forgotten `Dispose()` calls. Instead, Rust gives you something bold, powerful, and… at first, kind of intimidating:

**Ownership.**

Today’s post kicks off Week 2 of my Rust adventure, and it’s all about how this one idea changes everything.

### Memory Management in .NET: The Easy Life

In C#, memory is managed for you. You create an object:

```csharp
var user = new User("Alice");
```

And you just use it. When the runtime decides the object isn’t needed anymore, it gets cleaned up—eventually. Maybe after a few GC generations. Maybe never (hi, memory leaks). But the point is: _you don’t manage it directly._

### Rust’s Ownership Model: No GC, No Problem

In Rust, you don’t have a GC. Instead, you have rules. The compiler ensures that you follow them.

Here’s the core idea:

> Every value in Rust has a single owner. When the owner goes out of scope, the value is dropped (freed).

Let’s break it down with an example:

```rust
fn main() {
    let name = String::from("Alice");
    say_hello(name);
    println!("{}", name); // uh-oh
}

fn say_hello(person: String) {
    println!("Hello, {}!", person);
}
```

This code **won’t compile**.

Why? Because when you pass `name` into `say_hello`, ownership of the `String` moves to that function. After the call, you no longer “own” it, so trying to use `name` again is a big no-no.

The compiler won’t let you shoot yourself in the foot. That’s ownership in action.

### But Wait—Why Is This Better?

At first, it feels restrictive. But once you get it, you realize: **this is compile-time memory safety without a garbage collector**.

No dangling pointers. No use-after-free. No double-frees. No finalizers. No memory pressure spikes during a GC sweep.

It’s like having C++’s raw performance and control… without the classic C++ landmines.

### Want to Keep Using That Variable? Borrow It!

Rust gives you an elegant way to _temporarily_ use a value without transferring ownership: borrowing.

```rust
fn main() {
    let name = String::from("Alice");
    greet(&name); // pass a reference
    println!("{}", name); // still works
}

fn greet(person: &String) {
    println!("Hi, {}!", person);
}
```

The `&` means “borrow, don’t take.” So `greet` can use the `String`, but it doesn’t own it. The original `main` function still holds the ownership, so we’re allowed to use `name` afterward.

C# has something kind of like this with `ref`, `in`, and `out` parameters, but in Rust, **borrowing is the foundation of how data flows**.

### What About Heap vs Stack?

In .NET, you might wonder whether something is a value type (stack) or reference type (heap). In Rust, you get more control, but also clearer semantics:

- Values like `i32`, `bool`, and `char` are stored on the stack.

- Things like `String` (heap-allocated) are managed by ownership.

There’s no need to remember what’s a “reference type.” Instead, you think in terms of **who owns the data**, and where it lives becomes a natural consequence of that.

### So, Is This Slower?

Nope. In fact, Rust’s zero-cost abstractions make this model **faster**, because there's no GC overhead. Memory gets released the moment it’s no longer needed, and it’s all done at compile time.

It’s like being back in C++ land—but safer, which is wild.

### Final Thoughts: Rules That Make You Better

Ownership in Rust makes you think harder about how data moves through your app. It’s annoying at first (especially when the compiler tells you “value moved here” fifty times), but then… something clicks.

You start seeing data flow more clearly. You stop writing sloppy, leaky code. You trust the compiler like never before.

Tomorrow, we’ll go deeper into what happens when values move, and why your variable sometimes just… disappears.

Spoiler: it's not magic. It's Rust.
