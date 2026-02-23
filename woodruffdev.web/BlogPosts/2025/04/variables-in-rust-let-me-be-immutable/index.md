---
title: "Variables in Rust: Let Me Be Immutable"
date: 2025-04-14
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "Variables-in-Rust-Let-Me-Be-Immutable.png"
---

So today, I ran head-first into a Rust design decision that made me pause and go, “Wait… really?”

In C#, I can declare a variable with `var` and change it later. No fuss. In Rust? Not so fast. Your variables are frozen solid if you don’t explicitly ask for mutability. Welcome to Day 4: the world of `let`, `mut`, and what happens when your muscle memory meets a strict compiler.

### The C# Way: Flexible by Default

In C#, we’re used to this kind of setup:

```csharp
var name = "Chris";
name = "Woody"; // totally fine
```

Unless we go out of our way to make something `readonly`, we expect variables to be mutable. It’s the default. We use `readonly` for class fields when we want to protect them. But local variables? We change those all the time.

### The Rust Way: Immutable Until Proven Mutable

In Rust, the story flips.

```rust
fn main() {
    let name = "Chris";
    name = "Woody"; // ERROR: cannot assign twice to immutable variable
}
```

The compiler stops you cold. Why? Because `let` creates an _immutable_ binding by default.

If you want to change a value later, you need to opt the variable from the start into mutability:

```rust
fn main() {
    let mut name = "Chris";
    name = "Woody"; // now it's totally fine
    println!("{}", name);
}
```

Adding `mut` tells Rust, “Yes, I know this might change—and I’m okay with that.”

### Why Does Rust Care So Much?

Rust’s whole design centers around _safety and predictability_. By defaulting to immutability, the compiler helps prevent accidental state changes that can introduce bugs—especially in concurrent scenarios.

It’s like using `readonly` in C#, but it's baked into every line unless you say otherwise.

### You Don’t Own the Variable, You Bind It

One thing that tripped me up was this little mental shift: Rust doesn’t really talk about “declaring variables.” It talks about _binding values to names_.

```rust
let x = 5;
```

You’re binding the value `5` to the name `x`. It’s not just a semantic detail—this matters big-time when you get into move semantics, ownership, and borrowing (spoiler alert for next week).

### Shadowing: The Plot Twist

Oh—and get this: you can “re-bind” a variable to a new value (even with a different type) using the same name:

```rust
fn main() {
    let score = "100";
    let score = score.parse::<i32>().unwrap();
    println!("Parsed score: {}", score);
}
```

That’s not a mistake—it’s called _shadowing_, and it’s allowed (and encouraged) in Rust when you want to transform or reinterpret values without mutating state.

### Wrap-Up: Mutability as a Conscious Choice

I’m starting to appreciate how Rust makes me _think_ before changing things. It’s like the language is saying, “Are you really sure this needs to change?” And honestly, sometimes the answer is no.

C# is like a whiteboard—easy to scribble on and erase. Rust is more like a stone tablet—you can write on it, but you’d better mean it unless you explicitly chip away at it with `mut`.

Tomorrow, we dive into functions. Can you guess whether return values need semicolons or not? I couldn’t either.
