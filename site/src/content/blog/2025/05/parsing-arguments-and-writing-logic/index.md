---
title: "Parsing Arguments and Writing Logic in Rust"
date: 2025-05-17
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "./images/Parsing-Arguments-and-Writing-Logic.png"
---

We are up to Day 37, and today, we are continuing to build out our Rust CLI app. Last time, we set up a simple command-line tool using the clap crate. Now, it is time to dig a little deeper into parsing arguments, handling input validation, and structuring our logic cleanly.

If you are coming from the C# world, this is where you would probably set up your Program.cs to parse args\[\], maybe use a library like CommandLineParser, and then branch out into your application logic. Rust gives you similar tools but with its own flavor.

## Adding More Arguments and Options

Here is where we left off last time:

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

Now, let us say we want to add a `repeat` option, so the user can specify how many times to print the message. This means adding another argument:

```rust
use clap::Parser;

#[derive(Parser)]
#[command(author, version, about, long_about = None)]
struct Cli {
    name: String,

    #[arg(short, long)]
    excited: bool,

    /// Number of times to repeat the message
    #[arg(short, long, default_value_t = 1)]
    repeat: u8,
}

fn main() {
    let cli = Cli::parse();

    for _ in 0..cli.repeat {
        if cli.excited {
            println!("HELLO, {}!!!", cli.name.to_uppercase());
        } else {
            println!("Hello, {}.", cli.name);
        }
    }
}
```

Now you can run it like this:

```bash
cargo run -- Woody --excited --repeat 3
```

Output:

```bash
HELLO, WOODY!!!
HELLO, WOODY!!!
HELLO, WOODY!!!
```

## Handling Validation

Sometimes, you want to ensure that the input makes sense. For example, what if the user enters a negative number or a huge value? `clap` has validation built in.

You can restrict the range with `value_parser`:

```rust
#[arg(short, long, value_parser = clap::value_parser!(u8).range(1..=10))]
repeat: u8,
```

This limits the repeat value between 1 and 10. If the user tries anything outside that range, the CLI will reject it with a friendly message.

## Structuring Logic Cleanly

Just like in C#, you want to avoid stuffing all your logic into `main`. Break out the work into separate functions.

```rust
fn print_greeting(name: &str, excited: bool, repeat: u8) {
    for _ in 0..repeat {
        if excited {
            println!("HELLO, {}!!!", name.to_uppercase());
        } else {
            println!("Hello, {}.", name);
        }
    }
}

fn main() {
    let cli = Cli::parse();
    print_greeting(&cli.name, cli.excited, cli.repeat);
}
```

This keeps `main` as your entry point and moves the business logic into its own function. Cleaner and easier to test.

## Why This Approach Works Well in Rust

- Clear argument parsing with built-in validation

- No messy manual parsing or switch statements

- Easy to add help output and usage instructions

- Logic stays organized and easy to maintain

Rust’s type system and `clap` macros work together to make your CLI apps robust and easy to build.

## Wrapping It Up

Today, you learned how to go beyond basic argument parsing and handle input validation while keeping your logic structured and readable. Next, we will work with files and the filesystem, adding some real-world usefulness to our CLI project. See you then!
