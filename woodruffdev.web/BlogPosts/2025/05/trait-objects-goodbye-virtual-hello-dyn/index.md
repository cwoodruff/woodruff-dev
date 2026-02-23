---
title: "Trait Objects: Goodbye virtual, Hello dyn"
date: 2025-05-10
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "Trait-Objects-Goodbye-virtual-Hello-dyn.png"
---

Welcome to Day 30 and today we are going to explore how Rust handles dynamic dispatch with trait objects. If you are used to the world of C# this is the part where you usually reach for `abstract` classes or `virtual` methods. Maybe you sprinkle in some interfaces and let polymorphism do the heavy lifting at runtime.

In Rust dynamic dispatch works a little differently and it is all thanks to `dyn`.

## Virtual and Abstract in C#: The Classic Approach

In C# you might define an abstract base class like this:

```csharp
public abstract class Animal
{
    public abstract void Speak();
}

public class Dog : Animal
{
    public override void Speak()
    {
        Console.WriteLine("Woof!");
    }
}

public class Cat : Animal
{
    public override void Speak()
    {
        Console.WriteLine("Meow!");
    }
}
```

Then at runtime you can work with the base class reference and the correct method is called via the virtual table:

```csharp
Animal pet = new Dog();
pet.Speak(); // Woof!
```

## The Rust Way: Trait Objects and dyn

Rust does not have classes or virtual methods but it has traits and something called **trait objects**. A trait object lets you store different types that implement the same trait and call methods on them using dynamic dispatch.

Here is the same idea in Rust:

```rust
pub trait Animal {
    fn speak(&self);
}

pub struct Dog;

impl Animal for Dog {
    fn speak(&self) {
        println!("Woof!");
    }
}

pub struct Cat;

impl Animal for Cat {
    fn speak(&self) {
        println!("Meow!");
    }
}
```

Instead of using an abstract base class we can use `dyn Animal`:

```rust
fn main() {
    let pets: Vec<Box<dyn Animal>> = vec![
        Box::new(Dog),
        Box::new(Cat),
    ];

    for pet in pets {
        pet.speak();
    }
}
```

The key here is `Box<dyn Animal>`. This tells Rust to store a pointer to a type that implements `Animal` but resolve the actual method calls at runtime.

## Static vs Dynamic Dispatch

Rust loves static dispatch because it means the compiler can optimize everything at compile time. But when you need flexibility Rust lets you choose dynamic dispatch explicitly with `dyn`.

- **Static dispatch**: Chosen at compile time using generics and trait bounds

- **Dynamic dispatch**: Chosen at runtime using `dyn Trait`

Compare this to C# where dynamic dispatch is the default when you use virtual methods or interfaces.

## When to Reach for dyn

Use `dyn Trait` when:

- You need to work with different types that implement the same behavior

- You do not know the concrete type at compile time

- You want to store a collection of mixed types

Stick with generics and trait bounds when:

- You want maximum performance

- You know the concrete types at compile time

- You do not need runtime flexibility

## Trait Objects Cannot Do Everything

Not all traits can be turned into trait objects. Traits must be **object safe** to be used with `dyn`. That means:

- No generic methods in the trait definition

- Methods cannot return `Self`

If your trait does not meet these rules Rust will let you know at compile time.

## Wrapping It Up

Trait objects and `dyn` give Rust a way to support dynamic dispatch without the overhead of a full object-oriented system. You get just enough flexibility to share behavior across types when you need it but without sacrificing Rust’s usual focus on safety and performance.

Next up we will explore generics in Rust and how they compare to generics in C#. See you tomorrow!
