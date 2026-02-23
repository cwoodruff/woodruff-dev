---
title: "Final Reflections: What Rust Taught Me as a C# Dev"
date: 2025-05-22
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "Final-Reflections-What-Rust-Taught-Me-as-a-C-Dev.png"
---

Day 42, and here we are. Six weeks of learning Rust from the perspective of a C# developer. We covered the basics, wrestled with ownership, danced with traits and lifetimes, and shipped a working CLI app. Along the way, there were moments of frustration, lightbulb moments, and more than a few “why is this so hard” conversations with the compiler.

This final reflection is about stepping back and asking the big questions. What did Rust really teach me? What am I taking back to my C# projects? What might be next?

## Week 1: Getting Our Feet Wet

We started with the basics. Hello World, variables, functions, and data types. Coming from C#, most of this felt familiar. But right away, Rust made its philosophy clear.

Immutability is the default. You have to opt into mutability by explicitly using `mut`. That small change set the tone for how Rust wants you to think. Be deliberate. Be clear about your intent.

C# lets you sprinkle in `readonly` where you feel like it. Rust makes you consider it every time you declare a variable.

## Week 2: Ownership and Borrowing

Ownership and borrowing were the first significant mental challenges. As a C# developer, you let the garbage collector handle memory. Rust forced me to confront questions I usually avoid.

- Who owns this value?

- When does it go away?

- Can I safely share it?

This week was tough. The borrow checker and I had some heated arguments. But every error message was a gentle (or not so gentle) reminder that Rust is keeping me honest.

I came away from this week with a deeper understanding and respect for memory management. Even though C# handles this for me, I now see opportunities to be more mindful about when I allocate, copy, or share data.

## Week 3: Structs, Enums, and Data Modeling

This was the week when Rust started to win me over. Structs were straightforward, but enums felt like a superpower. Discriminated unions in Rust are more flexible than C# enums and feel closer to what F# offers.

Instead of building class hierarchies with inheritance, Rust lets you model your data directly:

```rust
enum PaymentMethod {
    CreditCard { number: String, cvv: String },
    Paypal { email: String },
    WireTransfer { iban: String },
}
```

Pattern matching with `match` makes the logic safe and straightforward. No casting. No guessing. The compiler makes sure I handle every case.

This approach prompted me to reconsider some of the instances where I utilize class hierarchies in C#. Could I use simpler data models with clearer state transitions? Probably.

## Week 4: Modules, Crates, and Error Handling

This was the week when I began to appreciate Rust’s emphasis on explicitness. Modules and visibility rules are not just about organization. They enforce API boundaries.

Error handling with `Result<T,E>` felt like a big shift from C#'s try-catch world. Instead of hoping you catch every exception, Rust makes you handle failure right at the function signature:

```rust
fn read_file(path: &str) -> Result<String, io::Error> {
    fs::read_to_string(path)
}
```

This approach makes error handling part of the design, not just an afterthought.

Back in C#, I am now thinking more about where exceptions should be used and where returning explicit results might lead to clearer code.

## Week 5: Traits, Generics, and Lifetimes

This was the heavy week. Traits felt familiar coming from interfaces in C#, but with some extra flexibility. I loved how traits allow shared behavior without inheritance chains.

Lifetimes were the real workout. It was uncomfortable at first, but the more I worked with them, the more I understood why they exist. They are not meant to make my life difficult. They are about making sure my code is correct.

C# shields me from most of these concerns, but I now have a deeper understanding of what the GC handles for me. It also made me appreciate `ref` and `Span<T>` in a new light.

## Week 6: Building and Shipping

We built a CLI app. We packaged it. We benchmarked it. The tooling around Rust is excellent. Cargo handles project setup, dependency management, testing, and release builds all in one place.

Compared to the .NET CLI and NuGet, it felt lean and focused. I appreciated how easy it was to write tests right alongside my code and run them with `cargo test`.

The performance tests confirmed what I had heard: Rust is fast, like really fast. But they also reinforced that performance comes from thoughtful design, not just the language itself.

## What I'm Taking Back to C#

- Be more explicit about mutability

- Consider modeling with enums and pattern matching where it makes sense

- Rethink error handling and be more deliberate about where exceptions are appropriate

- Appreciate what the GC does, but also know when to think about allocations

- Write more tests alongside my code, not as an afterthought

- Lean into the power of structs and readonly types

## What’s Next?

I am not giving up C#. However, I am bringing a bit of the Rust mindset back with me. This journey expanded my perspective on safety, design, and performance.

I will probably continue to explore Rust for CLI tools and system code, where it excels. And who knows, maybe a side project or two will graduate from C# to Rust just for fun.

Thanks for following along on this journey. Whether you are a die-hard C# fan curious about Rust or already dabbling in both, I hope this series helped bridge the gap between these two awesome languages.

See you out there and happy coding!
