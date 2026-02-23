---
title: "Slices and Strings with Rust: Goodbye C# StringBuilder?"
date: 2025-04-22
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "Slices-and-Strings-Goodbye-C-StringBuilder.png"
---

When you’ve spent years writing C#, you get really comfortable with `string` being immutable, `Span<T>` being your performance trick, and `StringBuilder` being your go-to hammer when a `for` loop starts building text.

And then you start learning Rust.

Suddenly, `String` isn’t immutable. `&str` looks suspiciously like a `Span<char>` in disguise. And you realize… wait, do I even need a `StringBuilder` anymore?

Today on Day 12, I dove into **slices and strings in Rust**, and let me tell you, it’s a whole new world, but a surprisingly elegant one.

### Strings in C#: Immutable, Safe, and Everywhere

You probably know this dance by heart:

```csharp
string name = "Alice";
string upper = name.ToUpper();
Console.WriteLine(name); // still "Alice"
```

Strings in .NET are immutable reference types backed by UTF-16. If you want to modify one, you either:

- Reassign the variable, or

- Reach for a `StringBuilder` if performance matters

You also get tools like `Span<char>` and `Memory<T>` for performance-critical code when slicing or manipulating buffers.

### Strings in Rust: A Bit More to Unpack

Rust splits strings into **two distinct types**:

- `String` – a growable, heap-allocated UTF-8 string

- `&str` – a string slice, referencing a part of a `String` (or a string literal)

```rust
fn main() {
    let name = String::from("Alice");
    let greeting = format!("Hello, {}!", name);
    println!("{}", greeting);
}
```

You can modify `String` directly:

```rust
let mut msg = String::from("Hello");
msg.push_str(", world");
println!("{}", msg); // Hello, world
```

Yep, no `StringBuilder` needed. Just mutate the `String`.

### `&str`: The Slice-y Sidekick

The `&str` type is like a read-only view into a `String`. Think of it like a `Span<char>` in C#, but with guaranteed safety and lifetimes checked at compile time.

```rust
fn greet(name: &str) {
    println!("Hi, {}!", name);
}

fn main() {
    let user = String::from("Alice");
    greet(&user); // passing a &str
}
```

You can even slice strings with range syntax:

```rust
let text = String::from("Rustacean");
let slice = &text[0..4]; // "Rust"
println!("{}", slice);
```

But here's the catch: Rust strings are UTF-8 encoded. So slicing is **byte-based**, not char-based. Slicing in the middle of a multibyte character? Compiler panic.

### Comparing with C#: Span<T> Vibes

Rust's `&str` gives you the safety and flexibility of a C# `ReadOnlySpan<char>`, but with much tighter compiler enforcement.

And if you want something like a `Span<u8>` in Rust, just use a `&[u8]` slice:

```rust
let bytes = b"hello"; // byte string literal
```

There’s no need to pin memory, worry about unsafe access, or juggle multiple string types. Rust's system is consistent, even if it’s more verbose at times.

### UTF-8 vs UTF-16: Why It Matters

C# strings are UTF-16. That means most common characters (including emojis) take 2 bytes, but some use surrogate pairs. Slicing blindly is usually okay, but not always safe.

Rust strings are **UTF-8**, which is smaller for ASCII and more web-native. But it also means:

```rust
let smile = "😊";
println!("{}", &smile[0..1]); // ERROR!
```

Why? Because you're trying to cut a byte in the middle of a 4-byte emoji. Rust protects you from doing that by accident. Thanks, borrow checker (again).

### Do You Ever Need StringBuilder in Rust?

Not really.

With `String`, you can:

- Append with `push_str()` or `push()`

- Format with `format!()` (like `string.Format()` or C# interpolated strings)

- Replace substrings, trim, split... everything you expect

If you need high-performance streaming or chunked writes, you'd probably reach for `std::fmt::Write` or buffered I/O, but for most devs, `String` just does the job.

### Final Thoughts: It’s Simpler Than It Looks

At first, having `String` and `&str` felt like one type too many. But now? I get it.

Rust separates string _ownership_ from string _views_. And once you embrace slices, you realize they’re everywhere: strings, arrays, buffers. It’s one concept to learn that applies to multiple types.

Tomorrow, we examine **shadowing**, specifically re-declaring variables with the same name intentionally. It may sound unusual, but it’s actually quite elegant. Stay tuned.
