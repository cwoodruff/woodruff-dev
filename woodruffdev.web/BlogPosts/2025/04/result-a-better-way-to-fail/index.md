---
title: "Result&lt;T,E>: A Better Way to Fail"
date: 2025-04-30
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "ResultT-E-A-Better-Way-to-Fail.png"
---

Welcome to Day 20! Today we’re talking about failure, but in the best possible way. Because, let's be honest, things go wrong. Files go missing. Network calls timeout. Data isn’t always what we expect. And if you've been living in the .NET world like I have, your first instinct might be to reach for the trusty `try-catch` block.

Rust, though? Rust says, "Let’s not wait until runtime to deal with failure. Let’s handle it right now, at compile time." And the tool that it gives us to do that is `Result<T,E>`.

## Exceptions in .NET: The Traditional Way

In C#, you’re probably used to something like this:

```csharp
try
{
    var data = File.ReadAllText("config.txt");
    Console.WriteLine(data);
}
catch (IOException ex)
{
    Console.WriteLine($"Failed to read file: {ex.Message}");
}
```

Exceptions bubble up at runtime, and unless you're really diligent about catching them, your app might crash unexpectedly. Plus, there's always that overhead of exception handling, especially if you’re dealing with lots of small errors.

## Enter `Result<T,E>`: Rust’s Smarter Approach

Rust flips the script. Instead of letting errors ambush you at runtime, it makes success or failure part of the type system itself:

```rust
use std::fs::File;
use std::io::{self, Read};

fn read_file_contents(path: &str) -> Result<String, io::Error> {
    let mut file = File::open(path)?;
    let mut contents = String::new();
    file.read_to_string(&mut contents)?;
    Ok(contents)
}

fn main() {
    match read_file_contents("config.txt") {
        Ok(data) => println!("File contents: {}", data),
        Err(e) => println!("Failed to read file: {}", e),
    }
}
```

No exceptions. No surprises. `Result<T,E>` makes it explicit: either you get `Ok(T)` or you get `Err(E)`. And the compiler makes sure you handle both.

## What Makes `Result<T,E>` Better?

- **No runtime surprises**: You can't accidentally forget to handle an error case.

- **Lightweight error handling**: No stack unwinding or performance penalty like exceptions.

- **Safer concurrency**: Errors stay contained, making async and concurrent code easier to manage.

- **Composable**: Use operators like `?` to propagate errors easily without boilerplate.

Compare that with how tedious some error handling can be in .NET:

```csharp
try
{
    var result = await SomeApiCallAsync();
    if (result == null)
    {
        throw new InvalidOperationException("API returned null!");
    }
    Console.WriteLine(result);
}
catch (Exception ex)
{
    Console.WriteLine($"Something went wrong: {ex.Message}");
}
```

In Rust, you could handle this more gracefully with:

```rust
fn do_something() -> Result<String, String> {
    // Some logic that might fail
    Err(String::from("API returned an error!"))
}

fn main() {
    match do_something() {
        Ok(result) => println!("Success: {}", result),
        Err(e) => println!("Error: {}", e),
    }
}
```

## Chaining with `?`: Cleaner Code, Fewer Tears

Rust's `?` operator is the ultimate helper when working with `Result`. Instead of writing nested `match` statements, you can propagate errors up the call stack effortlessly:

```rust
fn read_username_from_file() -> Result<String, io::Error> {
    let mut file = File::open("username.txt")?;
    let mut username = String::new();
    file.read_to_string(&mut username)?;
    Ok(username)
}
```

If any of the operations fail, `?` automatically returns the error from the function. Clean and efficient.

## Wrapping It Up

In .NET, exceptions are often the blunt instrument of error handling. Rust’s `Result<T,E>` offers a scalpel—a precise, clear, and safe way to deal with failures.

By making failure part of the type system, Rust encourages you to think about the "what ifs" upfront, not as an afterthought. And honestly? That mindset shift is a game-changer.

Next up, we’re taking a look at panic and how Rust handles those "stop the world" situations versus recoverable errors like `Result`. Stick around!
