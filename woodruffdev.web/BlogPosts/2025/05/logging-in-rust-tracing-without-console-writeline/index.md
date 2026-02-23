---
title: "Logging in Rust: Tracing Without Console.WriteLine"
date: 2025-05-07
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "Logging-in-Rust-Tracing-Without-Console.WriteLine.png"
---

Welcome to Day 27, and let’s talk about how Rust handles logging the smart way. If you have been in the .NET world for any amount of time, you are probably used to `ILogger`. You know the drill: inject a logger, use it throughout your code, and stay away from the quick and dirty `Console.WriteLine` scattershot approach.

Rust takes a similar path but with its own flavor. It gives you the `log` and `tracing` crates, both designed to keep your code clean while still giving you rich, structured log output when you need it.

## Why Not Just println!

Sure, you could just sprinkle some `println!` statements all over your Rust code:

```rust
println!("Processing order: {}", order_id);
```

It works fine for quick debugging. But the moment your project grows or you want different log levels or outputs, you are going to wish you had something better. This is where structured logging comes in.

## Meet the log Crate

The `log` crate provides a standard API for logging. It defines macros like `trace!`, `debug!`, `info!`, `warn!`, and `error!`. But it does not decide where the logs go. You can plug in whatever logging backend you like.

Example:

```rust
use log::{info, warn};

fn main() {
    env_logger::init(); // Set up a basic logger backend

    info!("Application started");
    warn!("This is your warning log");
}
```

To control the log level, just set an environment variable when running your app:

```bash
RUST_LOG=info cargo run
```

Compare that to .NET:

```csharp
public class OrderService
{
    private readonly ILogger<OrderService> _logger;

    public OrderService(ILogger<OrderService> logger)
    {
        _logger = logger;
    }

    public void ProcessOrder(int orderId)
    {
        _logger.LogInformation("Processing order {OrderId}", orderId);
    }
}
```

The concepts match up nicely. Both give you level-based logging and keep the log configuration separate from the code that generates the logs.

## Leveling Up with tracing

For more advanced logging needs, Rust offers the `tracing` crate. Think of it as the structured logging and observability solution. It supports things like spans, fields, and subscribers. It is built for async and multithreaded environments where you need to track context across function calls.

Example using `tracing`:

```rust
use tracing::{info, instrument};
use tracing_subscriber;

#[instrument]
fn process_order(order_id: u32) {
    info!(order_id, "Processing order");
}

fn main() {
    tracing_subscriber::fmt::init();

    process_order(42);
}
```

The `#[instrument]` macro automatically captures function arguments as structured log fields. This gives you much richer log data without any extra typing.

Compare this to .NET’s structured logging with scopes:

```csharp
using (_logger.BeginScope("OrderId: {OrderId}", orderId))
{
    _logger.LogInformation("Processing order");
}
```

Both approaches help add meaningful context to your logs so they are not just strings floating around in the dark.

## Separation of Concerns

One of the best parts of Rust’s logging ecosystem is how it separates log generation from log handling. Your code focuses on generating events and data. The logging backend handles where those logs go. You can log to the console today and switch to writing JSON logs to a file tomorrow without touching your core logic.

In .NET, you get a similar benefit through dependency injection and logger providers. Rust accomplishes this through the flexibility of its logger implementations.

## Wrapping It Up

Rust makes it easy to avoid the bad habit of littering your code with println statements. With `log` and `tracing`, you get structured, scalable logging that feels right at home if you are coming from the .NET world. Whether you are just spinning up a CLI tool or building a production-grade service, Rust’s logging approach gives you the power to keep your logs clean and your debugging stress-free.

Tomorrow we will reflect on error handling and the structure we have built so far. See you then!
