---
title: "Syntax Showdown: A Look at Common Constructs in C# and Rust"
date: 2025-01-19
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "./images/Syntax-Smackdown-Comparing-Constructs-in-C-and-Rust.png"
---

## Posts in this Series on Rust for C# Developers

- Part 1: [Why Every C# Developer Should Explore Rust](https://woodruff.dev/why-every-c-developer-should-explore-rust/)

- Part 2: [Exploring Programming Paradigms: C# and Rust Side by Side](https://woodruff.dev/exploring-programming-paradigms-c-and-rust-side-by-side/)

- Part 3: **_Syntax Smackdown: Comparing Constructs in C# and Rus_**

- Part 4: Memory Wars: Garbage Collection in C# vs. Ownership in Rust

- Part 5: Threads, Tasks, and Ownership: C# and Rust Concurrency Explored

- Part 6: Building with .NET and Rust: A Tale of Two Ecosystems

- Part 7: Level Up Your Skills: Learning Rust as a C# Dev

- Part 8: Rust's Superpower: Speed Meets Smarts

Programming languages are like different cuisines. Some stick to comfort and tradition, like C# with its familiar object-oriented flavors, while others, like Rust, spice things up with bold new ingredients like ownership and borrowing. In this post, we’ll compare the “syntax recipes” for common constructs in C# and Rust, highlighting the delicious differences and similarities along the way.

### Variable Declarations: Setting the Stage

**C#: Explicit, with a Touch of Magic**

C# likes to keep things straightforward—most of the time. You declare variables with clear types, or if you’re feeling fancy, use `var` for type inference:

```csharp
int number = 10;
string message = "Hello, World!";
bool isActive = true;
var inferred = 42; // Compiler figures out it's an int
```

**Rust: Explicit by Default**

Rust is all about clarity and safety. You’ll explicitly declare types when needed, though the compiler’s type inference is pretty smart:

```rust
let number: i32 = 10;
let message: &str = "Hello, World!";
let is_active: bool = true;
let inferred = 42; // Compiler infers it's an i32
```

Bonus points: Rust’s variables are immutable by default. To make them mutable, you add a little `mut`:

```rust
let mut counter = 0;
counter += 1; // Now it's allowed
```

### Functions: Getting Things Done

**C#: Polished and Practical**

C# functions look sleek and professional. You declare a return type, name your function, and jump right into the action:

```csharp
int Add(int a, int b)
{
    return a + b;
}

string Greet(string name)
{
    return $"Hello, {name}!";
}
```

**Rust: Concise and Composable**

Rust’s functions are compact and often omit the return keyword for simplicity:

```rust
fn add(a: i32, b: i32) -> i32 {
    a + b // Implicit return (no semicolon means "return")
}

fn greet(name: &str) -> String {
    format!("Hello, {}!", name)
}
```

### Conditionals: Making Decisions

**C#: Classic Control Flow**

C# sticks to the tried-and-true `if-else` format:

```csharp
int number = 10;
if (number > 5)
{
    Console.WriteLine("Number is greater than 5");
}
else
{
    Console.WriteLine("Number is 5 or less");
}
```

**Rust: Lean and Expressive**

Rust keeps conditionals clean and concise, with a slight twist—`if` is an expression, so it can return values:

```rust
let number = 10;
if number > 5 {
    println!("Number is greater than 5");
} else {
    println!("Number is 5 or less");
}
```

Or, returning a value from `if`:

```rust
let message = if number > 5 {
    "Greater than 5"
} else {
    "5 or less"
};
println!("{}", message);
```

### Loops: Repeating the Fun

**C#: Iteration Made Easy**

C# has all the classic looping constructs:

```csharp
for (int i = 0; i < 5; i++)
{
    Console.WriteLine($"Iteration {i}");
}

int count = 0;
while (count < 5)
{
    Console.WriteLine($"Count {count}");
    count++;
}
```

**Rust: Loops with a Twist**

Rust’s looping syntax is simple, with `for` loops often taking center stage:

```rust
for i in 0..5 {
    println!("Iteration {}", i);
}

let mut count = 0;
while count < 5 {
    println!("Count {}", count);
    count += 1;
}
```

And, for infinite loops, Rust has a dedicated keyword:

```rust
loop {
    println!("This runs forever unless you break it");
    break; // Escape hatch
}
```

### Error Handling: Keeping It Safe

**C#: Try and Catch**

C# uses exceptions to handle errors. It’s straightforward and familiar:

```csharp
try
{
    int result = 10 / 0;
}
catch (DivideByZeroException e)
{
    Console.WriteLine("Cannot divide by zero");
}
```

**Rust: Results That Mean Business**

Rust avoids exceptions in favor of `Result` and `Option`, forcing you to handle errors explicitly:

```rust
fn divide(a: i32, b: i32) -> Result<i32, &'static str> {
    if b == 0 {
        Err("Cannot divide by zero")
    } else {
        Ok(a / b)
    }
}

match divide(10, 0) {
    Ok(result) => println!("Result: {}", result),
    Err(e) => println!("Error: {}", e),
}
```

### Final Thoughts

Both C# and Rust have unique syntax styles reflecting their design philosophies. C# aims for productivity and developer comfort, while Rust prioritizes safety and performance. By exploring both, you’ll expand your programming toolkit and appreciate the beauty in each language’s approach.

So, which syntax style do you prefer? Let us know in the comments below!
