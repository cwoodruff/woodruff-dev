---
title: "Custom Errors: From Display to thiserror"
date: 2025-05-06
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "./images/Custom-Errors-From-Display-to-thiserror.png"
---

Look at this! Day 26, and today we’re talking about one of my favorite things to _not_ ignore: error messages. Because when things go sideways (and they will), your future self and your users will appreciate error handling that’s clear, structured, and helpful.

If you’re coming from C#, you’re used to building custom exception classes when the built-in ones just don’t cut it:

```csharp
public class UserNotFoundException : Exception
{
    public UserNotFoundException(string username)
        : base($"User '{username}' was not found.")
    {
    }
}
```

In Rust, we do something similar, but instead of throwing exceptions, we define **custom error types** that work with the `Result<T, E>` system. And with a little help from the fantastic `thiserror` crate, it’s easier than you might expect.

## Custom Error Types: The Basics

Here’s how you might define a basic error type in Rust:

```rust
use std::fmt;

#[derive(Debug)]
pub enum MyError {
    NotFound(String),
    InvalidInput(String),
}

impl fmt::Display for MyError {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        match self {
            MyError::NotFound(item) => write!(f, "{} not found", item),
            MyError::InvalidInput(reason) => write!(f, "Invalid input: {}", reason),
        }
    }
}

impl std::error::Error for MyError {}
```

This gives you structured, meaningful errors that play nicely with `Result<T, E>`. But writing `Display` and `Error` by hand every time? A bit tedious.

## Enter `thiserror`: The Easy Button

The `thiserror` crate automates most of the boilerplate, keeping your error definitions clean and readable:

```rust
use thiserror::Error;

#[derive(Error, Debug)]
pub enum MyError {
    #[error("{0} not found")]
    NotFound(String),

    #[error("Invalid input: {0}")]
    InvalidInput(String),
}
```

That’s it. No manual `impl Display`. No manual `impl Error`. `thiserror` handles it all.

## Using Custom Errors in Practice

Here’s how you might use that custom error in a function:

```rust
fn find_user(username: &str) -> Result<String, MyError> {
    if username == "woodydev" {
        Ok(String::from("User found!"))
    } else {
        Err(MyError::NotFound(username.to_string()))
    }
}

fn main() {
    match find_user("unknown") {
        Ok(message) => println!("{}", message),
        Err(e) => println!("Error: {}", e),
    }
}
```

## Compared to C# Exception Hierarchies

In C#, custom exceptions are about creating class hierarchies and relying on inheritance:

```csharp
try
{
    throw new UserNotFoundException("woodydev");
}
catch (UserNotFoundException ex)
{
    Console.WriteLine(ex.Message);
}
```

The downside? Exceptions are runtime beasts, and they need to be caught explicitly and forgetting to catch the right one can lead to unpredictable behavior.

In Rust, because errors are values, you can:

- Compose them.

- Wrap them.

- Chain them.

- Handle them _at compile time_.

It feels more like working with actual data instead of handling side effects.

## Chaining Errors with `#[from]`

Another slick feature of `thiserror` is easy error conversion with the `#[from]` attribute:

```rust
use std::io;
use thiserror::Error;

#[derive(Error, Debug)]
pub enum MyError {
    #[error("IO error: {0}")]
    Io(#[from] io::Error),

    #[error("Invalid data provided")]
    InvalidData,
}
```

Now, anytime a function returns an `io::Error`, Rust can automatically convert it into your `MyError::Io` variant using the `?` operator. No glue code needed.

## Wrapping It Up

Rust’s approach to custom errors is all about making your error handling explicit, structured, and, dare I say, pleasant. With enums, pattern matching, and handy tools like `thiserror`, you get the flexibility of detailed error types without the pain of hand-rolling exception hierarchies.

Next up, we’re looking at logging in Rust because when things go wrong, it’s nice to leave a paper trail. See you there!
