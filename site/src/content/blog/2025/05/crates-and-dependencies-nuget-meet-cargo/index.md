---
title: "Crates and Dependencies: NuGet, Meet Cargo"
date: 2025-05-03
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "./images/Crates-and-Dependencies-NuGet-Meet-Cargo.png"
---

Day 23... let’s talk about how Rust gets its packages! If you’re coming from the .NET world, you’re no stranger to NuGet. It’s been your trusty sidekick for pulling in libraries, managing versions, and bloating that `csproj` file with package references.

In Rust, the equivalent is **Cargo**, and its packages are called **crates**. But here’s the twist: Cargo doesn’t just handle your dependencies. It’s your project manager, your build system, your tester, and your publisher, all rolled into one delightful tool.

## The Rust Package Manager: Cargo

When you create a new Rust project with:

```bash
cargo new my_app
```

Cargo spins up a neat little project for you, complete with:

```
my_app/
├── Cargo.toml  # Like your .csproj file
└── src/
    └── main.rs
```

The `Cargo.toml` file is where your project metadata and dependencies live. Think of it as the Rust cousin to your `csproj` file, but simpler and less noisy.

Here’s a basic `Cargo.toml`:

```
[package]
name = "my_app"
version = "0.1.0"
edition = "2021"

[dependencies]
```

Need a dependency? You can add it directly to `[dependencies]`, or just let Cargo handle it for you.

## Adding Dependencies: cargo add

Let’s say you want to use the popular `rand` crate for random number generation. You could manually edit `Cargo.toml`, but why not let Cargo do the heavy lifting?

```bash
cargo add rand
```

This updates your `Cargo.toml` like so:

```
[dependencies]
rand = "0.8"
```

Compare that with adding a package via NuGet CLI:

```bash
dotnet add package Newtonsoft.Json
```

Which updates your `csproj` file:

```xml
<ItemGroup>
  <PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
</ItemGroup>
```

The concept is familiar, but Cargo’s TOML format feels a bit lighter and easier on the eyes.

## Working with Dependencies

Want to use that `rand` crate in your code? Easy:

```rust
use rand::Rng;

fn main() {
    let mut rng = rand::thread_rng();
    let n: u8 = rng.gen_range(1..=10);
    println!("Random number: {}", n);
}
```

No fuss with manual `using` statements or hunting for assembly references. Cargo takes care of everything.

## Locking It Down

When you build your project (`cargo build`), Cargo creates a `Cargo.lock` file. This is similar to `.csproj`’s `packages.lock.json` in .NET. It pins exact versions of your dependencies to ensure reproducible builds.

Cargo.lock example snippet:

```
[[package]]
name = "rand"
version = "0.8.5"
```

## Publishing Your Own Crate

When you’re ready to share your library with the world, Cargo makes it super simple to publish to [crates.io](https://crates.io):

```bash
cargo publish
```

Compare that to pushing your package to a NuGet feed:

```bash
dotnet pack
nuget push MyLibrary.nupkg -Source https://api.nuget.org/v3/index.json
```

Both ecosystems are solid here, but again, Cargo makes it feel a bit more integrated and less ceremony-heavy.

## Why Cargo Feels Like a Breath of Fresh Air

- **Batteries included**: Dependency management, building, testing, and publishing all with one tool.

- **Minimal configuration**: TOML files are clean and easy to read.

- **Version resolution is predictable**: Thanks to `Cargo.lock`.

- **No project file XML gymnastics**: Dependencies are just a few lines away.

## Wrapping It Up

Cargo may be new to you, but it’s easy to fall in love with. If you’re used to juggling NuGet, `csproj` edits, and package managers as separate tasks, Rust’s approach will feel refreshingly cohesive.

Tomorrow, we’re going to talk about error propagation with the `?` operator, so simple, so smart. Don’t miss it!
