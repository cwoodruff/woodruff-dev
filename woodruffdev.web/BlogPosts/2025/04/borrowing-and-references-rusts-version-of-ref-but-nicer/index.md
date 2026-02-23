---
title: "Borrowing and References: Rust’s Version of ref (But Nicer)"
date: 2025-04-20
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "Borrowing-and-References-Rusts-Version-of-ref-But-Nicer.png"
---

If you've been writing C# for a while, you’ve likely crossed paths with `ref`, `in`, and `out` parameters. They allow you to pass variables by reference, enabling a method to read or modify the original value.

Useful? Definitely.

Safe? Uh... sometimes.

In Rust, there's a similar concept called **borrowing**. It uses `&` and `&mut`, and it feels a lot like passing `ref` or `in` in C#, but with one significant difference:

> The **compiler** enforces safety rules that make data races and invalid access _impossible_.

Today, we’re diving into borrowing, references, and how Rust holds your hand (and your memory) without letting you write foot-gun code.

### C# Refresher: Passing by Reference

In C#, you might pass by reference like this:

```csharp
void Double(ref int x) {
    x *= 2;
}

int value = 10;
Double(ref value);
Console.WriteLine(value); // 20
```

This works, but you’re on the honor system. You could accidentally mutate something you didn’t mean to. There’s nothing stopping you from sharing that `ref` with another thread or keeping it alive too long.

Rust doesn’t leave this to chance.

### Rust’s Version: Borrowing with `&`

Let’s start with borrowing in read-only mode:

```rust
fn main() {
    let name = String::from("Alice");
    greet(&name);
    println!("{}", name); // Still valid!
}

fn greet(person: &String) {
    println!("Hello, {}!", person);
}
```

Here, `&name` means “borrow `name` temporarily.” The `greet` function doesn’t take ownership; it just gets a reference to the data.

Rust tracks this at compile time. When `greet` is done, the borrow ends, and `name` is still fully usable.

### Want to Mutate? Use `&mut`

Just like C#'s `ref`, Rust lets you _mutably_ borrow a value using `&mut` but it’s even stricter.

```rust
fn main() {
    let mut count = 5;
    double(&mut count);
    println!("Doubled: {}", count);
}

fn double(num: &mut i32) {
    *num *= 2;
}
```

Notes:

- You need to declare the original variable as `mut`.

- You borrow it mutably with `&mut`.

- Inside `double`, you _dereference_ it with `*num`.

And the best part? Rust won’t let you have more than one mutable reference at a time. You either get:

- Many immutable references  
    **or**

- One mutable reference

Not both.

This avoids the classic threading issues and memory races that C# devs have to tiptoe around with locks or `volatile`.

### The Borrow Checker: Annoying but Trustworthy

Here’s where Rust gets strict. Try doing this:

```rust
let mut count = 10;
let r1 = &mut count;
let r2 = &mut count; // ERROR!
```

Rust won’t allow it. Why? Because you’re trying to create two mutable references to the same data at the same time.

Even if `r1` and `r2` exist in separate lines, Rust sees the _overlapping lifetimes_ and panics _at compile time_. No runtime surprises. No corrupted memory. No segfaults.

It feels annoying at first, but it’s the kind of frustration that saves you from hours of debugging later.

### Lifetime of a Reference? Compiler's Got It Covered

You don’t have to manually manage memory like in C++. Rust figures out the **lifetime** of each reference for you in most cases. It ensures that your borrowed data never outlives the thing it points to.

This means:

```rust
fn main() {
    let r;
    {
        let x = 5;
        r = &x; // ERROR: x doesn’t live long enough
    }
    println!("{}", r);
}
```

This won't compile because `x` gets dropped when the inner scope ends. Rust refuses to let you create a dangling reference. C# would happily let you hold a reference to a deallocated object (until the GC saves you… or doesn’t).

### TL;DR: Borrowing Is Like `ref`, But With Rules

Here’s the cheat sheet:

| Concept | C# | Rust |
| --- | --- | --- |
| Pass by ref | `ref`, `in`, `out` | `&`, `&mut` |
| Dereference | Implicit (`x`) | Explicit (`*x`) |
| Safety | Runtime GC, trust system | Compile-time rules |
| Thread-safe? | Not guaranteed | Guaranteed |

* * *

### Final Thoughts

Borrowing in Rust might feel heavy-handed at first, but it’s actually freeing.

It frees you from GC pauses. It frees you from race conditions. It frees you from wondering, _"Can I still use this?"_

And it all happens **before** your code even runs.

Tomorrow we tackle the ultimate tough-love teacher: **the Borrow Checker** itself. You’ll probably hate it a little, but eventually, you’ll realize it’s the mentor you never knew you needed.
