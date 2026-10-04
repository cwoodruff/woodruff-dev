---
title: "Building a CLI App in Rust: My First Project"
date: 2025-05-17
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "./images/Building-a-CLI-App-in-Rust-My-First-Project.png"
---

It's day 36, and today, we are shifting gears from theory to practice. It is time to roll up our sleeves and build something. If you have ever built a command-line tool in C# using .NET Console apps, you will feel right at home. Rust has the same capability with a few extras that make the experience feel pretty slick.

## Why Build a CLI Tool?

CLI apps are a great way to learn Rust because they let you focus on small functional pieces without worrying about GUIs or frameworks. Plus, typing a command into the terminal and seeing your code respond is deeply satisfying.

## Getting Started with cargo new

First up, we create the project using Cargo, Rust’s package manager and build tool:

```bash
cargo new my_cli_app
cd my_cli_app
```

Cargo sets up the directory with a `Cargo.toml` file and a `src/main.rs`. This is your starting point.

## Meet clap: The CLI Toolkit for Rust

The `clap` crate is the go-to solution for parsing command-line arguments in Rust. It stands for Command Line Argument Parser and makes setting up CLI interfaces easy and reliable.

Add it to your `Cargo.toml`:

```json
[dependencies]
clap = { version = "4.5.38", features = ["derive"] }
```

Now you can start defining commands and arguments with the `clap` macros.

## Defining Basic Commands

Here is an example of a simple CLI that greets the user:

```rust
use clap::Parser;

#[derive(Parser)]
#[command(author, version, about, long_about = None)]
struct Cli {
    /// The name of the person to greet
    name: String,
}

fn main() {
    let cli = Cli::parse();
    println!("Hello, {}!", cli.name);
}
```

When you run this app:

```bash
cargo run -- Woody
```

It prints:

```bash
Hello, Woody!
```

That is the basic idea. Define your CLI structure with a struct. Use attributes to set up help text and argument parsing. Let `clap` handle the rest.

## Adding More Options

You can easily add flags and options:

```rust
use clap::Parser;

#[derive(Parser)]
#[command(author, version, about, long_about = None)]
struct Cli {
    /// The name of the person to greet
    name: String,

    /// Adds excitement
    #[arg(short, long)]
    excited: bool,
}

fn main() {
    let cli = Cli::parse();
    if cli.excited {
        println!("HELLO, {}!!!", cli.name.to_uppercase());
    } else {
        println!("Hello, {}.", cli.name);
    }
}
```

Run it like this:

```bash
cargo run -- Woody --excited
```

Output:

```bash
HELLO, WOODY!!!
```

## Why Use clap Instead of Manually Parsing?

If you have ever written manual `args[]` parsing logic in C#, you know how painful it can be. String arrays, positional arguments, lots of switch statements. Clap does the heavy lifting for you and helps you with output and argument validation out of the box.

```bash
cargo run -- --help
```

Gives you:

```bash
Usage: my_cli_app [OPTIONS] <NAME>

Arguments:
  <NAME>  The name of the person to greet

Options:
  -e, --excited  Adds excitement
  -h, --help     Print help
  -V, --version  Print version

```

## Wrapping It Up

Building a CLI app in Rust is a perfect first project. It teaches you how to structure a project, use dependencies like `clap` and get immediate feedback from the terminal.

Next time, we will expand on this, explore argument parsing in more depth, and start handling real work with input and output. See you there!
