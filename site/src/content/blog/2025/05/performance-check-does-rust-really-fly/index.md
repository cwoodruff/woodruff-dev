---
title: "Performance Check: Does Rust Really Fly?"
date: 2025-05-21
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "./images/Performance-Check-Does-Rust-Really-Fly.png"
---

Welcome to Day 41, and we are almost done! Today, we are putting Rust’s performance reputation to the test. Rust has a reputation for being fast. But how fast? If you have been living in the C# world where the JIT and garbage collector handle things for you this is a good chance to see how Rust stacks up when it comes to raw speed.

## The Test Scenario

Let us keep it simple. We will compare a Rust CLI app and a C# console app that both sum the numbers from 1 to 100 million.

## The C# Version

Here is our basic C# example:

```csharp
class Program
{
    static void Main()
    {
        long sum = 0;
        for (long i = 1; i <= 100_000_000; i++)
        {
            sum += i;
        }

        Console.WriteLine($"Sum: {sum}");
    }
}
```

To run this with release optimizations:

```bash
dotnet build -c Release
```

Then, use your shell or PowerShell tools.

## The Rust Version

Now the Rust equivalent:

```rust
fn main() {
    let sum: u64 = (1..=100_000_000).sum();
    println!("Sum: {}", sum);
}
```

Build it with:

```bash
cargo build --release
```

And time it the same way using `time ./target/release/my_cli_app` or equivalent.

## Measuring Performance

In PowerShell:

```bash
Measure-Command { ./MyApp.exe }
```

In Linux or macOS:

```bash
time ./my_cli_app
```

## Benchmarking Tools for Rust

If you want to go deeper than just timing a single run, check out the `criterion` crate. It gives you statistically reliable benchmarking.

Add to `Cargo.toml`:

```yaml
[dev-dependencies]
criterion = "0.6.0"
```

Example benchmark setup:

```rust
use criterion::{criterion_group, criterion_main, Criterion};

fn sum_benchmark(c: &mut Criterion) {
    c.bench_function("sum 1 to 100 million", |b| {
        b.iter(|| (1..=100_000_000).sum::<u64>())
    });
}

criterion_group!(benches, sum_benchmark);
criterion_main!(benches);
```

Run the benchmarks:

```bash
cargo bench
```

## Results from Benchmark

### C#

```
Days              : 0
Hours             : 0
Minutes           : 0
Seconds           : 0
Milliseconds      : 301
Ticks             : 3016339
TotalDays         : 3.49113310185185E-06
TotalHours        : 8.37871944444444E-05
TotalMinutes      : 0.00502723166666667
TotalSeconds      : 0.3016339
TotalMilliseconds : 301.6339

```

### Rust

```
test result: ok. 0 passed; 0 failed; 0 ignored; 0 measured; 0 filtered out; finished in 0.00s

     Running benches\sum_benchmark.rs (target\release\deps\sum_benchmark-d096bb33fce078cb.exe)
Gnuplot not found, using plotters backend
sum_benchmark           time:   [232.00 ps 236.39 ps 241.24 ps]

```

## Why Rust Often Wins on Speed

- No garbage collector pause

- Ahead of time compilation with full optimization

- Tight control over memory allocation

- Minimal runtime overhead

In C# the JIT and garbage collector are efficient but there is still some runtime overhead especially if you allocate a lot of objects or cross gen boundaries.

Rust compiles directly to machine code without requiring JIT. It uses LLVM to optimize aggressively at compile time.

## What About Memory Usage?

Rust tends to use less memory, especially for small utilities where GC overhead would otherwise kick in. C# can get very close to performance for CPU-bound tasks, but memory-bound scenarios often favor Rust.

## Wrapping It Up

Rust really can fly when it comes to performance. It shines for CPU-heavy workloads or tools that need to squeeze out every bit of speed. C# is no slouch, especially with the improvements in .NET Core and .NET 8, but Rust’s zero-cost abstractions and lack of GC give it an edge for lean CLI tools.

Tomorrow, we will wrap up this whole series with some final reflections and lessons learned from the Rust journey. See you then!
