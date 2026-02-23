---
title: "Why Rust? A C# Developer’s Journey Begins"
date: 2025-04-11
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "Why-Rust-A-C-Developers-Journey-Begins.png"
---

_The daily list of topics for the series is here: [From C# to Rust: A 42-Day Developer Challenge](https://woodruff.dev/from-c-to-rust-a-42-day-developer-challenge/)_

Today marks Day 1 of my 42-day challenge to learn Rust—with a twist. I’m not approaching this as a blank-slate beginner. I’m bringing along years of C# experience, mental muscle memory from countless LINQ expressions, async/await workflows, and enough `IEnumerable<T>`s to make your head spin.

But now? I’m learning a language that doesn’t have a garbage collector, doesn’t throw exceptions the same way, and thinks `null` is a bad idea.

Why?

### The Rusty Itch I Needed to Scratch

Here’s the thing—I've grown very comfortable in the .NET ecosystem. Too comfortable. Writing code in C# often feels like working in a luxury car: smooth, feature-rich, and with great support systems like LINQ, Jetbrains Rider, and the ever-present garbage collector doing its thing behind the scenes.

But lately, I’ve been craving something that makes me think differently, something that forces me out of autopilot. I wanted a systems language that feels modern, safe, and powerful but without the memory horror stories of C++.

That’s where Rust comes in.

Rust doesn’t hold your hand but doesn’t let you shoot yourself in the foot without asking, “Are you _really_ sure about that?”

### First Impressions: C# Brain Meets Rust Compiler

Let me say that my C# brain and the Rust compiler are not best friends yet.

For instance, in C#, I might casually throw around a variable like this:

```csharp
var name = "Rustacean";
Console.WriteLine(name);
```

In Rust?

```rust
fn main() {
    let name = "Rustacean";
    println!("{}", name);
}
```

Looks similar, right? But the moment I try to _change_ that value, Rust slams the brakes.

```rust
fn main() {
    let name = "Rustacean";
    name = "Ferris"; // Compiler says nope!
}

```

If I want mutability, I have to _explicitly_ ask for it:

```rust
fn main() {
    let mut name = "Rustacean";
    name = "Ferris";
    println!("{}", name);
}

```

The Rust compiler is basically a strict teacher who wants me to write safer code by default. It’s like going from a chill substitute teacher (C#) to a no-nonsense professor with a red pen (Rust).

### Why This Journey Matters

I’m not here to switch camps. I love C#—it’s elegant, powerful, and great for web apps, APIs, and enterprise workloads. But if there's one thing I've learned over the years, it's that **learning a new language sharpens your skills in all the others.**

Rust’s approach to memory management, error handling, and data modeling forces you to think differently. And that "different" is what excites me. Even if I never ship a production app in Rust, the ideas I pick up will make my C# code better.

Also, let’s be real—how many developers can say they’ve wrestled with the borrow checker and lived to tell the tale?

### What to Expect

For the next 42 days, I’ll be documenting my Rust journey daily. Each post will draw connections between Rust and C#, highlight the “aha!” moments, and probably include a few “why won’t this compile?!” meltdowns.

If you’re a .NET dev curious about Rust or want to follow along for the entertainment, welcome aboard.

Tomorrow: installing Rust and comparing `cargo new` to `dotnet new`. Spoiler: One of them feels like it came from 2025.
