---
title: "Writing Tests in Rust: Familiar and Fast"
date: 2025-05-19
categories: 
  - "rust"
tags: 
  - "net"
  - "c"
  - "dotnet"
  - "programming"
  - "rust"
coverImage: "Writing-Tests-in-Rust-Familiar-and-Fast.png"
---

Onward to Day 39. Today, we're discussing testing in Rust. If you are a C# developer, you have probably spent time with xUnit, NUnit, or MSTest. You know the usual `[TestMethod]` or `[Fact]` attributes and `Assert.Equal` calls. Rust’s testing system is going to feel pretty familiar with a bit of Rust flair.

## The Basic Rust Test

In Rust, writing tests is built right into the language and toolchain. No extra test runner needed. Just use the `#[test]` attribute and the `assert_eq!` macro.

Example:

```rust
pub fn add(a: i32, b: i32) -> i32 {
    a + b
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_add() {
        assert_eq!(add(2, 3), 5);
    }
}
```

The `#[cfg(test)]` block tells Rust to only compile these tests when running tests, not in normal builds.

To run your tests:

```bash
cargo test
```

## Compare to xUnit or NUnit

In C#, your test might look like this with xUnit:

```csharp
public class MathTests
{
    [Fact]
    public void Add_ReturnsSum()
    {
        Assert.Equal(5, Add(2, 3));
    }

    public int Add(int a, int b) => a + b;
}
```

The feel is the same. Annotate your test method, assert your expectations, and let the framework run your tests.

## Other Assertions

Rust gives you a few handy macros for testing:

- `assert!`: Checks that a condition is true

- `assert_eq!`: Checks that two values are equal

- `assert_ne!`: Checks that two values are not equal

Example:

```rust
#[test]
fn test_positive_number() {
    let value = 10;
    assert!(value > 0);
}
```

## Testing Error Conditions

You can also check for expected panics using `#[should_panic]`:

```rust
#[test]
#[should_panic(expected = "division by zero")]
fn test_divide_by_zero() {
    let _ = 1 / 0;
}
```

Compare that to C#’s `[ExpectedException]` in older MSTest or `Assert.Throws` in xUnit:

```csharp
[Fact]
public void DivideByZero_ThrowsException()
{
    Assert.Throws<DivideByZeroException>(() => { var result = 1 / 0; });
}
```

## Organizing Tests

Rust lets you group tests inside modules. You can also create a `tests` folder at the root of your project for integration tests.

Example unit test organization:

```rust
#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_add_positive() {
        assert_eq!(add(2, 3), 5);
    }

    #[test]
    fn test_add_negative() {
        assert_eq!(add(-2, -3), -5);
    }
}
```

Integration test example:

```
my_project/
├── src/
│   └── lib.rs
├── tests/
│   └── integration_test.rs
```

Inside `integration_test.rs`:

```rust
use my_project::add;

#[test]
fn test_add_integration() {
    assert_eq!(add(5, 7), 12);
}
```

Run all tests with:

```bash
cargo test
```

## Why Rust’s Testing Feels Smooth

- Built into Cargo with zero setup

- No extra dependencies required

- Fast compile and test cycles

- Clear assertion macros

## Wrapping It Up

Testing in Rust feels familiar if you come from the .NET world but with the benefit of being built right into the language and its package manager. You get fast feedback and easy organization without extra tooling.

Tomorrow, we will discuss packaging and releasing your CLI app, making it easy to share your work. See you then!
