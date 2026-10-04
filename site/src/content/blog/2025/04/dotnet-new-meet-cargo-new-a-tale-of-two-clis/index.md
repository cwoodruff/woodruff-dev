---
title: "dotnet new, Meet cargo new: A Tale of Two CLIs"
date: 2025-04-12
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "./images/dotnet-new-Meet-cargo-new-A-Tale-of-Two-CLIs.png"
---

It’s Day 2 of my journey learning Rust as a longtime C# developer, and today I took the plunge: I installed Rust and created my first project. The whole thing felt a bit like unpacking a new toolbox—familiar enough to recognize the tools, but different enough that I had to check the manual.

Let’s walk through how installing Rust compares to getting started with .NET and how `cargo` stacks up against our old friend `dotnet`.

## Step One: Installing Rust (Yes, It’s Just One Command)

C# devs are used to Visual Studio installers and SDK juggling. With Rust, it’s... shockingly simple.

```bash
curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh
```

That’s it. One command. `rustup` is Rust’s version manager, installer, and updater all in one. It even sets up your environment variables and gives you `cargo`, the Rust CLI by default.

I didn’t need a separate runtime or SDK or debate which version of Visual Studio to install, which was refreshing.

Want to verify the install? Just run:

```bash
rustc --version
cargo --version
```

Boom. You’re ready to code.

## Step Two: Project Bootstrapping — `cargo` vs `dotnet`

In .NET land, we’ve all typed this at some point:

```bash
dotnet new console -n HelloWorld
cd HelloWorld
dotnet run
```

Rust’s version?

```bash
cargo new hello_world
cd hello_world
cargo run
```

It’s almost identical in spirit. But `cargo` does a few nice things right out of the gate:

- It initializes a Git repo for you by default.

- It gives you a `Cargo.toml` file (like `.csproj`, but... readable).

- It keeps source code in a `src` folder, which feels neat and tidy.

Here’s what you get from `cargo new hello_world`:

```
hello_world/├── Cargo.toml└── src    └── main.rs
```

And the default `main.rs`:

```rust
fn main() {
    println!("Hello, world!");
}
```

No using directives, no `class Program`, no `static void Main`. Just a function and some curly braces. Minimalism, meet productivity.

## Why `cargo` Feels Like the CLI I Always Wanted

If you’re coming from the .NET world, you’ll notice `cargo` bundles everything: it builds, runs, tests, adds dependencies, and even publishes your packages.

Need a new dependency? No NuGet commands, no UI dialogs—just:

```bash
cargo add serde
```

And `cargo` grabs it for you and updates your `Cargo.toml`. It’s as if `dotnet`, `nuget`, and MSBuild got together and decided to be one happy command. FYI, the _serde_ package is a generic serialization/deserialization framework.

## The Verdict: Setup That Gets Out of the Way

Installing Rust and getting started with `cargo` was fast and frictionless. It reminded me how refreshing it is when tooling _just works_ and doesn’t require me to babysit it.

Tomorrow, I’ll get my hands dirty writing “Hello, World!” in both C# and Rust and start teasing apart the syntax differences.

Spoiler: One of them makes you think harder about semicolons.
