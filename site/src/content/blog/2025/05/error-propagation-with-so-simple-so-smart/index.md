---
title: "Error Propagation with ?: So Simple, So Smart"
date: 2025-05-04
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "./images/Error-Propagation-with-So-Simple-So-Smart.png"
---

Day 24... today we’re digging into one of my favorite "why doesn’t every language have this?" features in Rust: the `?` operator.

If you've spent any time in C#, you're no stranger to the good old `try/catch` flow. You wrap some code in a `try`, you catch the exception, and you hope you didn’t forget to check for null or some unexpected state.

Rust takes a different approach with `Result<T,E>` and the magic of the `?` operator. It keeps your error handling clean, readable, and safe without the overhead (or drama) of exceptions.

## The Usual Suspects: C# try/catch

Let’s start with what you know:

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

It works. But it’s noisy. And if you’re calling multiple methods that could throw, your `try/catch` blocks can quickly become a tangle of indentation and exception juggling.

## Rust’s Way: Result + ? = Clean

In Rust, instead of exceptions, functions return `Result<T,E>`. And instead of wrapping every call in a match or if statement, you can use the `?` operator to automatically return early if there’s an error.

Here’s a function that reads a file and propagates errors up the call stack:

```rust
use std::fs::File;
use std::io::{self, Read};

fn read_file_contents(path: &str) -> Result<String, io::Error> {
    let mut file = File::open(path)?; // if this fails, the error is returned immediately
    let mut contents = String::new();
    file.read_to_string(&mut contents)?; // same here
    Ok(contents)
}

fn main() {
    match read_file_contents("config.txt") {
        Ok(data) => println!("File contents: {}", data),
        Err(e) => println!("Failed to read file: {}", e),
    }
}
```

No try, no catch, no drama. The `?` says: "If this operation fails, bail out and return the error. Otherwise, keep going."

## Why This Is So Smart

The brilliance of the `?` operator is in its simplicity:

- **It reduces boilerplate.** No need for repetitive match statements.

- **It’s explicit.** You can see exactly where errors might occur.

- **It’s enforced by the type system.** Rust won’t let you forget to handle an error.

Compare that with chaining methods in C#, where you either:

- Hope nothing throws, or

- Wrap everything in `try/catch`, or

- Check every return value manually if you’re avoiding exceptions.

## Even Cleaner: Using `thiserror` and Custom Errors

Rust makes it easy to define your own error types and still use `?`. Example:

```rust
use thiserror::Error;

#[derive(Error, Debug)]
pub enum MyError {
    #[error("IO error: {0}")]
    Io(#[from] io::Error),
    #[error("Invalid input!")]
    InvalidInput,
}

fn read_file(path: &str) -> Result<String, MyError> {
    let mut file = File::open(path)?; // auto-converts io::Error into MyError::Io
    let mut contents = String::new();
    file.read_to_string(&mut contents)?;
    Ok(contents)
}
```

This keeps your error types meaningful without sacrificing the ease of `?`.

## Wrapping It Up

Error handling is one of those areas where Rust's philosophy really shines: catch problems early, handle them clearly, and avoid surprise runtime crashes.

The `?` operator makes error propagation _so_ simple that once you get used to it, going back to nested `try/catch` blocks feels like using a fax machine.

Tomorrow, we’ll talk about panic and how Rust handles those truly unrecoverable situations. See you then!
