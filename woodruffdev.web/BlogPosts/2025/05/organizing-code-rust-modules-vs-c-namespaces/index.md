---
title: "Organizing Code: Rust Modules vs C# Namespaces"
date: 2025-05-02
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "Organizing-Code-Rust-Modules-vs-C-Namespaces.png"
---

Welcome to Day 22! After a week of wrestling with data modeling, it’s time to talk about something near and dear to every developer’s heart: keeping your code organized (and your sanity intact).

If you're a C# developer, you've lived in the world of **namespaces**, `public` and `internal` access modifiers, and `.cs` files that can stack up faster than your coffee cups during crunch time. In Rust, the story is a little different, but delightfully simple once you get the hang of it.

## C# Namespaces: The Familiar Territory

In C#, you group your code into namespaces, like this:

```csharp
namespace MyApp.Services

public class EmailService
{
    public void SendEmail(string address, string message)
    {
        Console.WriteLine($"Sending '{message}' to {address}");
    }
}
```

C# gives you `public`, `internal`, `protected`, and `private` as tools to control access. Plus, namespaces can be arbitrarily deep, independent of file structure. Your project might have folders that _kind of_ line up with namespaces, but they don’t have to.

## Rust Modules: Organization That Matches Your Files

In Rust, code organization is driven by **modules**, which are explicitly tied to your folder and file structure. Here’s the core idea:

- `mod` declares a module.

- `pub` makes items public outside the module.

- Files and directories mirror the module structure.

Example:

```rust
// src/main.rs
mod services;

fn main() {
    services::send_email("woody@dev.com", "Hello from Rust!");
}
```

And in `src/services.rs`:

```rust
pub fn send_email(address: &str, message: &str) {
    println!("Sending '{}' to {}", message, address);
}
```

Notice how the module directly maps to the filename? Clean and predictable.

## Going Deeper: Nested Modules

Rust also lets you create submodules using folders and `mod.rs` files, like this:

```
src/
├── main.rs
└── services/
    ├── mod.rs
    └── email.rs
```

In `mod.rs`:

```rust
pub mod email;
```

In `email.rs`:

```rust
pub fn send_email(address: &str, message: &str) {
    println!("Sending '{}' to {}", message, address);
}
```

In `main.rs`:

```rust
mod services;

fn main() {
    services::email::send_email("woody@dev.com", "Hello again!");
}
```

## Access Control: pub vs. private by Default

In Rust, everything is **private by default,** unlike C#, where classes and methods are often public unless you say otherwise.

- `pub`: Makes things visible outside the module.

- No `pub`: Private to the module.

If you need to expose something to just sibling modules (like C#’s `internal`), Rust offers `pub(crate)` for crate-wide visibility.

```rust
pub(crate) fn internal_tool() {
    println!("Only visible within this crate!");
}
```

## C#'s Flexibility vs. Rust's Structure

C# lets you throw namespaces around however you want. Files don’t need to match namespaces. Rust, on the other hand, **encourages clarity** by enforcing file/module alignment. This can feel restrictive at first, but once you get into the flow, it makes code navigation and organization much easier.

## Why This Matters

In C#, you might find yourself hunting through files to match up namespaces and folders. Rust’s "your modules are your files" approach keeps things straightforward:

- Fewer surprises.

- Easier to onboard new developers.

- Access rules are enforced at compile-time, not just by convention.

## Wrapping It Up

Rust’s module system is like that friend who insists on labeling their spice jars and alphabetizing their bookshelf. At first, it feels a little rigid, but when you're looking for the paprika (or that one helper function), you’re _really_ glad they did it.

Tomorrow, we’ll dig into crates and dependencies. Where Rust’s package manager, Cargo, really shines. See you then!
