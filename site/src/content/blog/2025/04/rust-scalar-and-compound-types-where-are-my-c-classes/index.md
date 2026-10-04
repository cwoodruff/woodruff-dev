---
title: "Rust Scalar and Compound Types: Where Are My C# Classes?"
date: 2025-04-16
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "./images/Rust-Scalar-and-Compound-Types-Where-Are-My-C-Classes.png"
---

When I first started exploring Rust, one of my instincts was to reach for a class. You know the drill. Need to model some data? Write a class, slap a few properties on it, and maybe add a constructor or two.

But Rust had other plans.

On Day 6, I dug into Rust’s scalar and compound types and quickly realized that Rust doesn’t _want_ you to start with classes. Instead, it hands you a small set of powerful primitives and says, “Let’s build something lean.”

Let’s take a look at how that plays out, especially through the eyes of a C# developer.

### Scalars: The Simple Stuff

In C#, we have our familiar scalar types:

```csharp
int x = 42;
float pi = 3.14f;
char c = 'A';
bool isReady = true;
```

Rust matches us almost one-for-one but requires more explicitness upfront.

```rust
let x: i32 = 42;
let pi: f32 = 3.14;
let c: char = 'A';
let is_ready: bool = true;
```

Rust supports:

- Signed/unsigned integers (`i8` to `i128`, `u8` to `u128`)

- Floating points (`f32`, `f64`)

- `char` (which is a Unicode scalar, not a byte!)

- `bool`

Rust can also infer types most of the time:

```rust
let count = 10; // i32 by default
let ratio = 0.5; // f64 by default
```

But if you're coming from C#'s flexible `var`, Rust’s inference feels stricter; you'll find yourself annotating types more often than not, especially in more complex scenarios.

### Tuples: Lightweight Data Bundles

Here’s a quick reality check: no `Tuple<T1, T2>` or deconstruction syntax required here. Rust tuples are first-class citizens:

```rust
let person = ("Chris", 42);
```

Want to access the values?

```rust
let name = person.0;
let age = person.1;
```

Or destructure them (which feels very modern C#):

```rust
let (name, age) = person;
```

There’s no class, no struct, just a tuple carrying multiple values with different types. Quick, dirty, and highly useful when you don’t need a full data model.

Compare that to C#:

```csharp
var person = ("Chris", 42);
var name = person.Item1;
var age = person.Item2;
```

C#'s support for value tuples is decent. In Rust, it’s built right in and super ergonomic.

### Arrays and Slices: Familiar with a Twist

C# arrays are flexible and garbage-collected:

```csharp
int[] scores = new int[] { 10, 20, 30 };
```

Rust’s arrays are fixed-size by default:

```rust
let scores: [i32; 3] = [10, 20, 30];
```

The `[i32; 3]` type means “an array of three `i32`s.” You can also initialize them with repetition:

```rust
let zeros = [0; 5]; // same as [0, 0, 0, 0, 0]
```

To get the slice (similar to `Span<T>` in .NET), you can reference it like this:

```rust
let part = &scores[1..3]; // gets [20, 30]
```

And yes, Rust will panic if you index out of bounds. But it does this at runtime with a very detailed error, which honestly still feels nicer than IndexOutOfRangeExceptions in C#.

### Wait, No Classes Yet?

Nope. Not even structs at this point (that’s tomorrow). Rust really wants you to get comfortable with simple, efficient types before moving into more abstract modeling.

And you know what? It works.

C# makes it easy to jump to classes for everything. But Rust pushes you to start small—model your data with tuples or arrays, and only reach for structs when the pattern demands it. It’s a different mindset, and I kind of love it.

### Final Thoughts: Minimal Types, Maximum Power

Rust’s scalar and compound types feel simple but there’s a surprising amount of power in that simplicity. You’re not juggling `object` or `dynamic`. There’s no `null`. You just get clean, lean, explicit values.

Tomorrow, we graduate to _structs_ and talk about how Rust models data without the ceremony of C# classes.

Spoiler: No `public`, no `get; set;`, no inheritance, and I’m not even mad.
