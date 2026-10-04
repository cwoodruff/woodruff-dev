---
title: "Week 3: Wrap-Up: Data Modeling That Fights Back"
date: 2025-05-01
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "./images/Week-3-Wrap-Up-Data-Modeling-That-Fights-Back.png"
---

Three weeks into Rust, and if your brain isn’t at least a little bit melted, I applaud your resilience! This week was all about **data modeling**. But not just any modeling—the kind that actively _fights back_ when you try to make bad decisions.

If you're used to the world of C#, you probably lean on classes, POCOs, and the occasional enum to represent state. And hey, that works… until it doesn’t. Rust’s structs and enums bring some serious muscle to the table by making sure your data models are clear, correct, and (most importantly) safe by design.

Let’s take a moment to reflect on what we learned and why Rust’s approach makes data modeling feel less like a "best practices" hope-and-pray scenario and more like a language-enforced guarantee.

## Structs: Simple, Lean, and Mean

Rust structs are deceptively simple. They group your data together just like C# classes or structs, but with one key difference: they’re value types without any hidden behavior, inheritance chains, or surprise runtime overhead.

```rust
struct User {
    username: String,
    email: String,
    active: bool,
}

let user1 = User {
    username: String::from("woodydev"),
    email: String::from("woody@dev.com"),
    active: true,
};
```

Compare that to a typical C# class:

```csharp
public class User
{
    public string Username { get; set; }
    public string Email { get; set; }
    public bool Active { get; set; }
}

var user1 = new User
{
    Username = "woodydev",
    Email = "woody@dev.com",
    Active = true
};
```

Sure, they look similar—but Rust’s structs stay lean and honest. No hidden nullability, no inheritance headaches, and immutability by default unless you explicitly opt in.

## Enums: The Real MVP

If structs are the foundation, enums are the secret sauce. Rust’s enums aren’t just glorified integer labels. They’re fully powered, discriminated unions that can hold data.

```rust
enum Message {
    Quit,
    Move { x: i32, y: i32 },
    Write(String),
    ChangeColor(i32, i32, i32),
}

let msg = Message::Move { x: 10, y: 20 };
```

Compare this to how you'd represent something similar in C#:

```csharp
abstract class Message { }

class Quit : Message { }
class Move : Message { public int X { get; set; } public int Y { get; set; } }
class Write : Message { public string Text { get; set; } }
class ChangeColor : Message { public int R { get; set; } public int G { get; set; } public int B { get; set; } }
```

C# encourages you to utilize class hierarchies, inheritance, and pattern matching. Rust just gives you enums and `match`, and it works beautifully.

## Pattern Matching: Clarity Wins

With enums comes pattern matching. Rust’s `match` expression forces you to think through every possible case, no forgotten edge cases or accidental fall-throughs.

```rust
match msg {
    Message::Quit => println!("Quit"),
    Message::Move { x, y } => println!("Move to ({}, {})", x, y),
    Message::Write(text) => println!("Write message: {}", text),
    Message::ChangeColor(r, g, b) => println!("Change color to RGB({}, {}, {})", r, g, b),
}
```

Rust won’t compile if you forget to handle a variant. C# might warn you (if you’re lucky). Rust _enforces_ it.

## Why Rust Makes You Better (Even If It Hurts a Little)

The key lesson this week? Rust doesn’t just let you model state; it ensures you do it correctly. There’s no relying on convention or developer discipline to avoid bugs. Rust's type system works like that strict but loving coach who won't let you cut corners.

- No null surprises

- No accidental forgotten cases

- No ambiguous "magic strings" or fragile inheritance trees

Your models are explicit. Your intent is clear. And your compiler has your back.

## Wrapping Up Week 3

This week taught us that modeling state well isn’t about writing more code, it’s about writing better code. Rust’s structs and enums push you toward designs that are clear, correct, and maintainable from the start.

Next week, we level up again: modules, crates, and error handling strategies. Stay tuned!
