---
title: "Functions in Rust: Familiar Yet Different"
date: 2025-04-15
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "./images/Functions-in-Rust-Familiar-Yet-Different.png"
---

Today’s Rust lesson hit a familiar note but with a twist. Writing functions in Rust feels _almost_ like writing them in C#. Until, of course, the compiler reminds you that this language doesn’t always play by the same rules.

On Day 5 of my Rust-for-C# developers journey, let’s break down function definitions, type inference, return values, and where Rust and C# shake hands... and where they give each other a suspicious side-eye.

### The C# Way: You Know It By Heart

Let’s take a basic C# method:

```csharp
int Add(int a, int b)
{
    return a + b;
}
```

Simple, clean, and typed from top to bottom. It’s the kind of method you’ve probably written a thousand times.

Now let’s take a look at the Rust equivalent.

### The Rust Way: Concise, but Watch the Semicolon

```rust
fn add(a: i32, b: i32) -> i32 {
    a + b
}
```

Looks familiar, right? But let’s pause.

That return value isn’t using the `return` keyword. Instead, Rust has this _expression-style_ return: the last line of your function becomes the return value **as long as you don’t end it with a semicolon**.

Now try this:

```rust
fn add(a: i32, b: i32) -> i32 {
    a + b;
}
```

Boom. The compiler complains. Why? Because adding that semicolon turns the expression into a statement—and statements return `()` (the unit type), not your expected value.

So yeah… **semicolons are not just decoration in Rust. They have meaning.**

If you prefer being explicit, you _can_ use `return`:

```rust
fn add(a: i32, b: i32) -> i32 {
    return a + b;
}
```

That works too, but the idiomatic way in Rust is to lean into expression returns when possible.

### Type Inference: The Good and the Explicit

C# lets us do things like:

```csharp
var result = Add(2, 3);
```

And Rust? It loves type inference _inside_ functions but **not at function boundaries**.

Inside the function:

```rust
fn greet(name: &str) {
    let message = format!("Hello, {}!", name);
    println!("{}", message);
}
```

Totally inferred. But when you _define_ a function, Rust wants you to be explicit with parameter and return types. No `var`\-style magic here.

So this won’t fly:

```rust
fn multiply(a, b) {
    a * b
}
```

Nope. Rust needs to know what `a` and `b` are, or it won’t compile.

### Multiple Returns? Think Tuples

If you’re used to C# tuples or out parameters, Rust does tuples too:

```rust
fn split_number(n: i32) -> (i32, i32) {
    (n / 2, n % 2)
}
```

You call it like:

```rust
let (quotient, remainder) = split_number(9);
```

Destructuring built-in? Yes, please.

### Functions as First-Class Citizens

Rust also lets you pass functions around just like delegates in C#:

```rust
fn apply_twice(f: fn(i32) -> i32, x: i32) -> i32 {
    f(f(x))
}

fn square(n: i32) -> i32 {
    n * n
}

fn main() {
    let result = apply_twice(square, 2);
    println!("Result: {}", result); // 16
}
```

Yep, functions are values. Just like C# delegates and lambdas—but with less ceremony.

### Final Thoughts: Familiar, but with Rules That Bite

Writing functions in Rust gave me déjà vu, but in a good way. But the small differences matter. Rust functions are predictable, expressive, and often less verbose than C# once you get past the quirks:

- Semicolons are powerful.

- Return types matter.

- You’ll love tuples.

- And the compiler? It wants you to be _very_ clear.

Tomorrow we explore Rust’s basic data types, including tuples and arrays. The title might just be: _“Where Are My Classes?”_
