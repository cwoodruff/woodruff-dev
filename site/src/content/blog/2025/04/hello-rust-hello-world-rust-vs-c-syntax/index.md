---
title: "Hello, Rust! Hello, World! Rust vs C# Syntax"
date: 2025-04-13
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "./images/Hello-World-Rust-vs-C-Syntax.png"
---

Let’s be honest—every new language journey begins the same way: with a humble "Hello, World!" It’s the developer's rite of passage. So today, on Day 3 of my 42-day Rust challenge, I’m writing the most iconic two-word phrase in programming in both C# and Rust… and then tearing it apart.

Because while the output is the same, what these two languages make you _say_ to get there reveals a lot about how they think.

### C# Says Hello (Politely, Verbosely)

Here’s what a typical C# “Hello, World!” looks like today:

```csharp
using System;

namespace HelloWorld;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}
```

If you're like me, this feels second nature. It’s ceremony. It's structure. It’s the .NET way. You’ve got a namespace, a class, a static method, and a whole lot of boilerplate for a single line of output.

We’ve come to accept this verbosity as normal. But when you compare it to Rust, you realize… maybe we’ve been doing too much.

### Rust Says Hello (Straight to the Point)

Here’s the Rust version:

```rust
fn main() {
    println!("Hello, World!");
}
```

Wait, that’s it? No `using` directives, no classes, no static modifiers. Just a function named `main` and a macro call.

Let’s unpack this simplicity:

- **No classes**: Rust isn’t object-oriented by default. You don’t need a class to hold your code.

- **`fn main()`**: Rust's entry point is just a function. No need for `static` or `void`.

- **`println!`**: That exclamation mark? It means you’re calling a _macro_, not a function. (More on that later)

### The Syntax Gap: What It Tells Us

Let’s highlight a few differences that immediately stood out:

| Concept | C# | Rust |
| --- | --- | --- |
| Entry Point | `static void Main(string[] args)` | `fn main()` |
| Print Output | `Console.WriteLine("text")` | `println!("text")` |
| Type Imports | `using System;` | None needed for `println!` |
| Semicolon Required? | Yes | Yes, usually (but careful—it matters more) |
| Object Orientation | Default | Optional / Trait-based |

What struck me right away is how _intentional_ Rust is. It doesn’t assume you want a class, or an object-oriented design. It gives you just what you need—and nothing more.

It’s like ordering a black coffee after years of Starbucks lattes with 3 pumps of ceremony.

### But... Where’s the Runtime?

C# runs on a managed runtime with JIT compilation and garbage collection. Rust, on the other hand, compiles down to machine code with zero runtime overhead. So when you build and run your Rust app, it's about as "close to the metal" as you can get—without writing C or sweating over memory allocation yourself.

That minimalism in the "Hello, World!" syntax? It's not just aesthetics—it’s a hint that Rust is designed to stay out of your way and let you control the details when you need to.

### Wrap-Up: A Simpler Start, A Different Philosophy

C# holds your hand—maybe too much. Rust gives you a nudge and says, “Go ahead, you’ve got this.”

Writing my first Rust app felt liberating. The syntax was lean, focused, and surprisingly friendly once I accepted that I didn’t need all the fluff.

Tomorrow, I’ll dig into variables—specifically Rust’s obsession with immutability. Spoiler: You have to _ask_ permission to change things.
