---
title: "Reflections on Week 1: Rust's Minimalism Hits Different"
date: 2025-04-17
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "Reflections-on-Week-1-Rusts-Minimalism-Hits-Different.png"
---

Seven days into learning Rust, and I feel like I’ve been through a developer bootcamp with a compiler that doubles as a personal trainer. It doesn’t let you slack, but it _does_ make you better.

This week, I went from installing the toolchain to wrestling with immutable variables and puzzling over semicolons in return statements. As a C# developer, I expected syntax differences, but I didn’t expect the philosophical shifts.

Let’s recap what stood out, what stung, and why I’m still excited to keep going.

### Hello, World—Now Please Explain Yourself

Starting with “Hello, World!” was comforting. It felt familiar until I realized that in Rust, functions don’t need a class, and printing uses a macro. Like this:

```rust
fn main() {
    println!("Hello, World!");
}
```

No `class Program`, no `static void Main`, and no `using System;`. Clean. Minimal. Straight to the point.

It was the first sign that Rust’s default posture is: **Do less. Mean more.**

### dotnet new vs cargo new

Setting up my first Rust project with `cargo new` was suspiciously smooth. Almost _too_ smooth.

Compared to the .NET CLI:

```bash
dotnet new console -n HelloWorld
```

Rust’s version felt leaner and more focused:

```bash
cargo new hello_world
```

And the fact that it created a Git repo for me automatically? Bonus points. `cargo` is one of the nicest CLIs I’ve ever used and that’s coming from someone who genuinely enjoys `dotnet`.

### Let Me Be Immutable

In C#, `var` is friendly. It assumes you might want to change things. Rust says: _Prove it._

```rust
let name = "Chris";    // Immutable
let mut name = "Woody"; // Mutable, but only if you ask
```

Having to add explicitly mut made me stop and think: Do I need this to change? That pause, though annoying at first, actually led to cleaner code.

Compare that to how easily we sprinkle state changes throughout our C# apps without thinking twice.

### Functions: Watch That Semicolon

I didn’t expect to be tripped up by a semicolon, but here we are:

```rust
fn add(a: i32, b: i32) -> i32 {
    a + b
}
```

In Rust, that works. But if you add a semicolon?

```rust
fn add(a: i32, b: i32) -> i32 {
    a + b;
}
```

Now the function returns `()` (unit), not the sum of the numbers. It’s wild how one little `;` changes everything. But it’s a good reminder that **Rust is an expression-first language**, not a statement-first one like C#.

### Types Without the Bloat

Rust handed me scalars, tuples, and arrays and basically said, “See what you can do without reaching for a class.”

```rust
let person = ("Chris", 45);
let scores = [10, 20, 30];
```

I found myself modeling real things without writing a single `struct`. That restraint? Kinda refreshing. Like being told you can’t use PowerPoint and having to explain your idea with a whiteboard instead. You get creative.

### What Clicked

- **The tooling** is fantastic. `cargo` makes the .NET CLI look a bit dated.

- **The compiler** is strict but helpful. It’s not just yelling, it’s coaching.

- **The language** is expressive without being verbose.

I started to feel at home once I embraced the idea that **Rust wants me to think before I write**. It’s not trying to be easy it’s trying to help me write better code.

### What Tripped Me Up

- Semicolon sensitivity in return values.

- Forgetting to add `mut` when I just wanted to change a value “real quick.”

- The fact that there are no classes yet. I kept reaching for one like muscle memory.

### Week 1 in a Sentence?

> "It’s like learning to cook with only five ingredients and realizing that’s all you needed in the first place."

Next week we dive into **ownership, borrowing, and the borrow checker**, aka “The Compiler's Tough-Love Phase.” Time to rewire my .NET brain a little more.
