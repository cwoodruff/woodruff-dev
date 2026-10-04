---
title: "Option&lt;T>: Where Null Is Not an Option"
date: 2025-04-29
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "./images/OptionT-Where-Null-Is-Not-an-Option.png"
---

Ah, nulls, the "billion-dollar mistake" that haunts just about every C# developer. How many times have you chased down a `NullReferenceException`, muttering under your breath, "But how could this even be null?" Well, guess what? In Rust, nulls are not a thing. At least, not in the wild-and-dangerous sense we're used to in .NET.

Instead, Rust gives us `Option<T>`. And let me tell you, once you get a taste of it, you'll wonder why we ever let `null` run loose in the first place.

## What Is Option?

`Option<T>` is Rust’s way of representing an optional value. A value can either be `Some(T)`—meaning there's a value or `None`, meaning there isn't.

Here’s the basic idea:

```rust
fn main() {
    let some_number = Some(5);
    let no_number: Option<i32> = None;

    if let Some(n) = some_number {
        println!("Number is: {}", n);
    } else {
        println!("No number found");
    }
}
```

No rogue nulls. No "oops, I forgot to check". It’s all baked into the type system, and the compiler makes sure you handle both cases.

## Nullable in C#: Close, But Not Quite

C# has `Nullable<T>`, which handles value types like `int?`, but reference types have always had a bit of a Wild West relationship with null. C# 8+ introduced nullable reference types with warnings and annotations, but it’s still on you to check and handle it properly.

Example in C#:

```csharp
int? someNumber = 5;

if (someNumber.HasValue)
{
    Console.WriteLine($"Number is: {someNumber.Value}");
}
else
{
    Console.WriteLine("No number found");
}
```

But with reference types:

```csharp
string? name = null;

if (name != null)
{
    Console.WriteLine(name);
}
else
{
    Console.WriteLine("No name");
}
```

The difference? In Rust, you can't even _forget_ to check. The compiler will smack your hand if you try.

## Pattern Matching FTW

Rust makes handling `Option<T>` not only safe but also elegant with pattern matching:

```rust
fn print_number(num: Option<i32>) {
    match num {
        Some(n) => println!("The number is: {}", n),
        None => println!("No number provided"),
    }
}

fn main() {
    print_number(Some(42));
    print_number(None);
}
```

Compare that with how much boilerplate we often have to write in C# just to feel "safe."

## Chaining Options: The `unwrap_or` Magic

Sometimes you want to provide a default if `None` shows up. Rust makes this painless:

```rust
fn main() {
    let number = Some(5);
    let value = number.unwrap_or(0); // returns 5 because Some(5)

    let no_number: Option<i32> = None;
    let default_value = no_number.unwrap_or(0); // returns 0 because None

    println!("Value: {} | Default: {}", value, default_value);
}
```

This beats the pants off of repetitive null checks any day.

## Why This Matters

Rust’s approach forces you to handle the "maybe" case upfront, making your intentions clear. No surprises at runtime. No, wondering if a function might hand you a null in disguise. And because it’s enforced at compile-time, you sleep better at night.

In C#, nullable reference types are a nice band-aid, and `Nullable<T>` helps with value types, but neither offers the rock-solid guarantee that Rust’s `Option<T>` brings to the table.

## Wrapping Up

Rust’s `Option<T>` turns a common headache into a non-issue. It’s not about being fancy—it’s about making "do you have a value or not?" explicit, safe, and easy to handle.

Next time, we’ll look at `Result<T,E>`—because sometimes things don’t just disappear, they fail. And Rust wants to help you deal with that gracefully. See you there!
