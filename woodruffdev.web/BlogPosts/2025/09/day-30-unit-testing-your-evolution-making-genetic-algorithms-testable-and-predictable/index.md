---
title: "Day 30: Unit Testing Your Evolution: Making Genetic Algorithms Testable and Predictable"
date: 2025-09-11
categories: 
  - "genetic-algorithms"
tags: 
  - "net"
  - "ai"
  - "c"
  - "dotnet"
  - "genetic-algorithms"
  - "programming"
coverImage: "Unit-Testing-Your-Evolution-Making-Genetic-Algorithms-Testable-and-Predictable.png"
---

Genetic Algorithms are inherently stochastic. Mutation introduces randomness. Crossover combines genes in unpredictable ways. Selection strategies often rely on probabilities. While this is essential to their power, it presents a challenge when it comes to unit testing. How can you reliably test behavior when the outcome changes on every run?

The answer lies in isolating deterministic logic and controlling randomness with test seams. Today’s post focuses on making your GA codebase testable using clean design principles and writing effective unit tests in C# with xUnit.

### Why Unit Test Genetic Algorithms?

A typical GA involves:

- Initialization of chromosomes

- Evaluation of fitness

- Parent selection

- Crossover and mutation

- Iterative execution across generations

Not all of these need to be tested with assertions on evolutionary outcomes. Instead, we focus on:

- Verifying mutation and crossover logic behave as expected

- Ensuring fitness evaluation returns consistent values

- Validating selection strategies pick high-fitness parents

- Confirming GA loops honor configuration and convergence rules

### Strategy 1: Isolate and Abstract Randomness

Use dependency injection to control random number generation.

```csharp
public interface IRandomProvider
{
    int Next(int minValue, int maxValue);
    double NextDouble();
}

```

A default implementation using `System.Random`:

```csharp
public class DefaultRandomProvider : IRandomProvider
{
    private readonly Random _random = new();

    public int Next(int minValue, int maxValue) => _random.Next(minValue, maxValue);
    public double NextDouble() => _random.NextDouble();
}

```

In your GA components:

```csharp
public class BitFlipMutation : IMutationStrategy
{
    private readonly IRandomProvider _random;

    public BitFlipMutation(IRandomProvider random)
    {
        _random = random;
    }

    public void Mutate(IChromosome chromosome)
    {
        if (chromosome is BinaryChromosome bc)
        {
            int index = _random.Next(0, bc.Genes.Length);
            bc.Genes[index] = !bc.Genes[index];
            bc.EvaluateFitness();
        }
    }
}

```

Now in tests you can mock the randomness.

### Strategy 2: Use Deterministic Chromosomes for Testing

You can define a chromosome with a fixed outcome:

```csharp
public class TestChromosome : IChromosome
{
    public double Fitness { get; private set; }
    public int[] Genes { get; private set; }

    public TestChromosome(int[] genes)
    {
        Genes = genes;
        EvaluateFitness();
    }

    public void EvaluateFitness()
    {
        Fitness = Genes.Sum(); // predictable
    }

    public IChromosome Crossover(IChromosome partner)
    {
        return Clone(); // no actual crossover for test
    }

    public void Mutate() { } // no-op for testing
    public IChromosome Clone() => new TestChromosome((int[])Genes.Clone());
}

```

### Example: Testing a Selection Strategy

Here’s an xUnit test that verifies `TournamentSelection` chooses the fittest chromosome:

```csharp
[Fact]
public void TournamentSelection_PrefersFitterChromosomes()
{
    var population = new List<IChromosome>
    {
        new TestChromosome(new[] { 1, 1, 1 }),  // Fitness = 3
        new TestChromosome(new[] { 1, 1, 0 }),  // Fitness = 2
        new TestChromosome(new[] { 1, 0, 0 })   // Fitness = 1
    };

    var selector = new TournamentSelection(3);
    var parents = selector.SelectParents(population).ToList();

    Assert.All(parents, p => Assert.True(p.Fitness >= 2));
}

```

### Example: Testing the Mutation Logic

Using a fixed random provider:

```csharp
public class FixedRandomProvider : IRandomProvider
{
    private readonly int _fixedIndex;

    public FixedRandomProvider(int index)
    {
        _fixedIndex = index;
    }

    public int Next(int minValue, int maxValue) => _fixedIndex;
    public double NextDouble() => 0.0;
}

```

And the test:

```csharp
[Fact]
public void Mutation_FlipsSpecificGene()
{
    var chromosome = new BinaryChromosome(new[] { false, false, false });
    var mutator = new BitFlipMutation(new FixedRandomProvider(1));

    mutator.Mutate(chromosome);

    Assert.False(chromosome.Genes[0]);
    Assert.True(chromosome.Genes[1]);
    Assert.False(chromosome.Genes[2]);
}

```

### Testing the GA Loop

You can test if the GA loop evolves a population to reach a known fitness threshold within a fixed number of generations:

```csharp
[Fact]
public void GA_ImprovesFitnessOverTime()
{
    var ga = new GeneticAlgorithm(
        chromosomeFactory: () => new BinaryChromosome(RandomGeneArray(10)),
        selection: new TournamentSelection(),
        crossover: new OnePointCrossover(),
        mutation: new BitFlipMutation(new DefaultRandomProvider()),
        populationSize: 50,
        generations: 100);

    ga.Run();

    Assert.True(ga.Best.Fitness >= 8);
}

```

For full reliability, use a fixed seed or inject test-friendly randomness.

### Final Thoughts

With the right abstractions and test seams, you can write meaningful, repeatable tests for your genetic algorithm components. You won't test randomness itself, but you can ensure that your logic responds correctly to it. This gives you the confidence to scale up, refactor, or add features without fearing regression. Tomorrow we’ll look at injecting domain knowledge through heuristics and external data to guide evolution more intelligently.
