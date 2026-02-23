---
title: "Working with Files and the Filesystem in Rust"
date: 2025-05-18
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "Working-with-Files-and-the-Filesystem.png"
---

Welcome to Day 38. Today, we're getting our hands dirty with file I/O. Reading and writing files is one of those tasks every app must perform at some point. If you have written C# code using `System.IO` this is going to feel familiar but with a Rust twist.

## Reading Files in C#: The Classic Approach

In C# reading a file might look like this:

```csharp
string content = File.ReadAllText("config.txt");
Console.WriteLine(content);
```

Simple and effective. The `System.IO` namespace gives you helpers like `ReadAllText`, `WriteAllText`, and streams when you need more control.

## Reading Files in Rust: std::fs::read\_to\_string

Rust has similar helpers in `std::fs`. Here is how you read a file into a string:

```rust
use std::fs;
use std::io;

fn main() -> io::Result<()> {
    let content = fs::read_to_string("config.txt")?;
    println!("{}", content);
    Ok(())
}
```

The `?` operator automatically handles the `Result` returned by `read_to_string`. If the file is missing or there is an error, the function returns early with the error.

Compare that to C#, where you need a `try/catch` block to handle the exception.

## Writing Files

In C#:

```csharp
File.WriteAllText("output.txt", "Hello World");
```

In Rust:

```rust
use std::fs;
use std::io;

fn main() -> io::Result<()> {
    fs::write("output.txt", "Hello World")?;
    println!("File written successfully");
    Ok(())
}
```

Again the `?` operator keeps error handling clean and clear.

## Appending to a File

Rust gives you `OpenOptions` for more control, just like C# uses `FileStream` and its options.

In C#:

```csharp
using (var writer = File.AppendText("log.txt"))
{
    writer.WriteLine("Log entry");
}
```

In Rust:

```rust
use std::fs::OpenOptions;
use std::io::Write;
use std::io;

fn main() -> io::Result<()> {
    let mut file = OpenOptions::new()
        .append(true)
        .create(true)
        .open("log.txt")?;

    writeln!(file, "Log entry")?;
    Ok(())
}
```

Same idea with clear options for appending or creating the file if it does not exist.

## Checking if a File Exists

In C#:

```csharp
if (File.Exists("data.txt"))
{
    Console.WriteLine("File exists");
}
```

In Rust:

```rust
use std::path::Path;

fn main() {
    if Path::new("data.txt").exists() {
        println!("File exists");
    } else {
        println!("File not found");
    }
}
```

Rust uses `Path` to work with file paths and check for existence.

## Creating Directories

C# example:

```csharp
Directory.CreateDirectory("logs");
```

Rust:

```rust
use std::fs;
use std::io;

fn main() -> io::Result<()> {
    fs::create_dir_all("logs")?;
    println!("Directory created");
    Ok(())
}
```

The `create_dir_all` function makes sure the entire directory path exists just like `Directory.CreateDirectory`.

## Why Rust’s Approach Works Well

- Compile-time safety with `Result` handling

- Clear options for file modes and behaviors

- Flexible path handling with `Path` and `PathBuf`

- Easy to plug into Rust’s iterator and error-handling systems

## Wrapping It Up

Working with the filesystem in Rust feels familiar if you come from C#, but with a focus on safety and explicit error handling. Instead of relying on exceptions, Rust uses the type system to ensure that you handle every case up front.

Next time, we will move into writing tests for our Rust projects to keep our code solid and trustworthy. See you there!
