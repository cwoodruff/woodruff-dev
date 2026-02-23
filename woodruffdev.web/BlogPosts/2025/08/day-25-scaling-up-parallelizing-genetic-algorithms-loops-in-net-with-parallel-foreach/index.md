---
title: "Day 25: Scaling Up: Parallelizing Genetic Algorithms Loops in .NET with Parallel.ForEach"
date: 2025-08-18
categories: 
  - "genetic-algorithms"
tags: 
  - "net"
  - "ai"
  - "c"
  - "dotnet"
  - "genetic-algorithms"
  - "programming"
coverImage: "Scaling-Up-Parallelizing-GA-Loops-in-.NET-with-Parallel.ForEach.png"
---

As problem complexity grows, so does the cost of evaluating and evolving populations in genetic algorithms. When each individual's fitness computation becomes expensive or the population size increases substantially, runtime performance can become a serious bottleneck. Fortunately, .NET makes it easy to scale genetic algorithms with minimal code changes by using `Parallel.ForEach`.

This post explores how to parallelize the evaluation and evolution stages of a genetic algorithm using the `System.Threading.Tasks.Parallel` library to harness multicore CPUs and accelerate GA performance.

### Why Parallelism Matters in Genetic Algorithms

In most GA implementations, each generation includes:

- Evaluating the fitness of each individual

- Selecting individuals for crossover

- Mutating offspring

- Replacing the population

These operations are usually independent for each individual, especially fitness evaluation, which makes them ideal for data parallelism.

By using `Parallel.ForEach`, we can speed up these operations with simple thread-safe logic.

### Parallelizing Fitness Evaluation

Let’s assume a standard `Chromosome` class with an `Evaluate()` method.

```csharp
public class Chromosome
{
    public int[] Genes { get; set; }
    public double Fitness { get; set; }

    public void Evaluate()
    {
        // Simulate expensive fitness computation
        Fitness = Genes.Sum(); // Replace with real logic
    }
}
```

In a traditional loop, we would evaluate fitness like this:

```csharp
foreach (var individual in population)
{
    individual.Evaluate();
}
```

With `Parallel.ForEach`, this becomes:

```csharp
Parallel.ForEach(population, individual =>
{
    individual.Evaluate();
});
```

If `Evaluate()` It is thread-safe and doesn't depend on external state; this simple change can dramatically reduce evaluation time on multi-core machines.

### Parallelizing Mutation and Crossover

Mutation and crossover can also be parallelized. You must ensure each operation uses a thread-safe random number generator to avoid contention and bias.

Here’s an example of safe mutation using `ThreadLocal<Random>`:

```csharp
var localRand = new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));

Parallel.ForEach(population, individual =>
{
    Mutate(individual, mutationRate, localRand.Value);
});
```

Where `Mutate()` is:

```csharp
public static void Mutate(Chromosome c, double rate, Random rand)
{
    for (int i = 0; i < c.Genes.Length; i++)
    {
        if (rand.NextDouble() < rate)
        {
            c.Genes[i] = 1 - c.Genes[i];
        }
    }
}
```

This ensures each thread has an isolated and seeded instance of `Random`, avoiding race conditions or poor randomness due to shared state.

### Parallelizing Next Generation Creation

We can even parallelize the generation of offspring. Here’s a simplified example using a concurrent collection:

```csharp
ConcurrentBag<Chromosome> nextGen = new();

Parallel.For(0, populationSize, i =>
{
    var parent1 = TournamentSelection(population, localRand.Value);
    var parent2 = TournamentSelection(population, localRand.Value);
    var child = Crossover(parent1, parent2, localRand.Value);
    Mutate(child, mutationRate, localRand.Value);
    child.Evaluate();
    nextGen.Add(child);
});
```

Once complete, convert `nextGen` to a list and continue with the evolutionary loop:

```csharp
population = nextGen.ToList();
```

This approach is handy when generating hundreds or thousands of new individuals per generation.

### Best Practices

- Use `ThreadLocal<Random>` or `Random.Shared` for safe and performant randomness in parallel code

- Avoid shared mutable state inside loops

- Profile your fitness evaluation to verify that it's the actual bottleneck

- Be aware of thread contention when logging or updating UI in parallel regions

### Conclusion

By leveraging `Parallel.ForEach` and related constructs, you can make your genetic algorithm implementation significantly faster and more scalable. This is especially important for large-scale optimization tasks such as image generation, pathfinding, or feature selection, where fitness evaluation dominates runtime.

In the next post, we’ll explore how to use .NET tasks and async patterns to distribute GA workloads across machines and even integrate them with distributed job queues.
