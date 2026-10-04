---
title: "Enums: Discriminated Unions Done Right"
date: 2025-04-26
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "./images/Enums-Discriminated-Unions-Done-Right.png"
---

Welcome to day 17! Today, we're diving into Rust's enums—and spoiler alert—they're not your typical enums from C#. Rust enums are powerful, flexible, and genuinely fun. If you've ever looked longingly at F#'s discriminated unions (or scratched your head at C#'s enums), you're about to discover something delightful.

## Not Your Grandma's Enum

When you think of enums in C#, you probably picture something like this:

```csharp
public enum Direction
{
    North,
    East,
    South,
    West
}

var direction = Direction.North;
Console.WriteLine(direction); // Prints: North
```

Sure, this works fine for simple scenarios, but Rust's enums are on a whole other level. They allow you to encapsulate multiple types and states under a single, coherent type.

Check this out:

```rust
enum Message {
    Quit,
    Move { x: i32, y: i32 },
    Write(String),
    ChangeColor(i32, i32, i32),
}

fn main() {
    let msg = Message::Move { x: 10, y: 20 };

    match msg {
        Message::Quit => println!("Quit"),
        Message::Move { x, y } => println!("Move to x:{} y:{}", x, y),
        Message::Write(text) => println!("Write: {}", text),
        Message::ChangeColor(r, g, b) => println!("Change color to: {}, {}, {}", r, g, b),
    }
}
```

Rust enums aren't just labels—they’re fully-fledged types that hold data!

## Discriminated Unions? What's That?

If you've dabbled in F#, you might recognize discriminated unions (DUs). They allow storing multiple related data structures in a single type, making them powerful for modeling complex states. Rust enums offer the same capability, bringing this powerful functional programming idea into the mainstream systems programming realm.

Here's how an F# discriminated union might look:

```csharp
type Shape =
    | Circle of radius: float
    | Rectangle of width: float * height: float
    | Square of side: float

let shape = Circle 10.0

match shape with
| Circle radius -> printfn "Circle with radius: %f" radius
| Rectangle (w, h) -> printfn "Rectangle (%f, %f)" w h
| Square side -> printfn "Square with side: %f" side
```

And here’s a similar concept translated into Rust:

```rust
enum Shape {
    Circle(f64),
    Rectangle(f64, f64),
    Square(f64),
}

fn main() {
    let shape = Shape::Circle(10.0);

    match shape {
        Shape::Circle(radius) => println!("Circle with radius: {}", radius),
        Shape::Rectangle(w, h) => println!("Rectangle ({}, {})", w, h),
        Shape::Square(side) => println!("Square with side: {}", side),
    }
}
```

Both F# and Rust support this powerful approach, but Rust enums integrate this functionality seamlessly into the language’s type system and safety guarantees.

## Why Rust Enums Rock

- **Safety**: Rust enums are type-safe, meaning you'll never accidentally treat one variant as another.

- **Expressive**: You can clearly represent complex scenarios, states, and behaviors.

- **Pattern Matching**: Built-in support for pattern matching makes handling enum types elegant and intuitive.

## Compared to C#

In C#, you’d typically represent these complex scenarios using class hierarchies, inheritance, and polymorphism. But this approach can become cumbersome, making your code harder to follow and maintain:

```csharp
abstract class Shape {}

class Circle : Shape {
    public double Radius { get; set; }
}

class Rectangle : Shape {
    public double Width { get; set; }
    public double Height { get; set; }
}

// usage
var shape = new Circle { Radius = 10.0 };

switch (shape) {
    case Circle c:
        Console.WriteLine($"Circle with radius: {c.Radius}");
        break;
    case Rectangle r:
        Console.WriteLine($"Rectangle ({r.Width}, {r.Height})");
        break;
}
```

While the C# pattern matching has improved greatly, it still lacks the elegance and simplicity of Rust’s enums.

## Wrap It Up

Rust enums are discriminated unions done right—expressive, safe, and seamlessly integrated into the language. They simplify your code and give you powerful tools to represent complex logic clearly and safely.

Tomorrow, we're going deeper into Rust's powerful `match` statement—get ready to see just how strong your pattern-matching muscles can become!
