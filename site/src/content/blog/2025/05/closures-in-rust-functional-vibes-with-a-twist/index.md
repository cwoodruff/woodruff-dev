---
title: "Closures in Rust: Functional Vibes with a Twist"
date: 2025-05-13
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "./images/Closures-in-Rust-Functional-Vibes-with-a-Twist.png"
---

Welcome to Day 33, and today we are jumping into closures. If you have been writing C# for a while, you are no stranger to lambdas, delegates, and maybe even expression trees. Rust has closures, too, and they bring a nice functional flavor to the language with some unique Rust twists.

## Lambdas and Delegates in C#: The Familiar Territory

In C#, you have probably written something like this a thousand times:

```csharp
Func<int, int> square = x => x * x;
Console.WriteLine(square(5)); // 25
```

Or passed a lambda into LINQ:

```csharp
var evens = numbers.Where(n => n % 2 == 0);
```

C# lets you define small anonymous functions with lambdas and use them wherever a delegate or expression tree is expected.

## The Rust Way: Closures with Capture

In Rust, closures look very similar at first glance:

```rust
let square = |x: i32| x * x;
println!("{}", square(5)); // 25
```

You use the `| |` syntax to define parameters; the body comes after the arrow or directly if it is an expression.

But here is the twist. Rust closures can automatically capture variables from their environment. This makes them feel more like C# lambdas with closures rather than just plain delegates.

Example:

```rust
let factor = 2;
let multiply = |x: i32| x * factor;
println!("{}", multiply(5)); // 10
```

The closure grabs `factor` from the surrounding scope without needing you to pass it explicitly.

## Closures and Traits: Fn, FnMut, FnOnce

Traits power Rust’s closures. Three main traits describe what kind of access the closure needs to its environment:

- `Fn`: Takes arguments by reference, does not mutate captured variables

- `FnMut`: Can mutate captured variables

- `FnOnce`: Consumes captured variables and can only be called once

You do not have to specify these most of the time. The compiler figures it out based on what your closure does. But you can add these traits explicitly when needed, especially in generic functions.

Example of a closure that mutates captured state:

```rust
let mut count = 0;
let mut increment = || {
    count += 1;
    println!("Count: {}", count);
};

increment();
increment();
```

This closure implements `FnMut` because it modifies `count`.

## Comparing to Expression Trees

In C#, expression trees are often used when you need to compile or analyze the structure of a lambda rather than execute it:

```csharp
Expression<Func<int, int>> expr = x => x * x;
```

Rust does not have a direct equivalent of expression trees in the same way. Instead, Rust favors higher-order functions and generics to handle most functional use cases. If you want to build something like an expression tree, you would define your own enums and interpret them manually.

## Returning Closures from Functions

In Rust, closures have anonymous types, so if you want to return a closure from a function, you usually have to use `impl Fn` or `Box<dyn Fn>`.

Example:

```rust
fn make_multiplier(factor: i32) -> impl Fn(i32) -> i32 {
    move |x| x * factor
}

let multiply_by_3 = make_multiplier(3);
println!("{}", multiply_by_3(10)); // 30
```

Notice the use of `move`. This tells Rust to move ownership of captured variables into the closure.

## Why Closures Feel Familiar Yet Different

- You get the same flexibility as C# lambdas

- Rust’s capture behavior feels closer to C# closures

- You explicitly choose when a closure consumes its environment

- Type inference handles most of the heavy lifting for closure types

Closures in Rust combine the convenience of functional programming with Rust’s usual safety guarantees. They let you write flexible, composable code without giving up control over memory and lifetimes.

## Wrapping It Up

Closures are one of those tools that feel right at home if you have written C# lambdas, but they bring a little extra flavor when you add Rust’s traits and ownership model into the mix.

Tomorrow, we will look at iterators and functional combinators, such as map and filter. See you then!
