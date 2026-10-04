---
title: "Day 28: Building a Pluggable Genetic Algorithms Framework in C#"
date: 2025-09-09
categories: 
  - "genetic-algorithms"
tags: 
  - "net"
  - "ai"
  - "c"
  - "dotnet"
  - "genetic-algorithms"
  - "programming"
coverImage: "./images/Building-a-Pluggable-Genetic-Algorithms-Framework-in-C.png"
---

As you reach the final week of our Genetic Algorithms series, it is time to shift from experimentation to engineering. Instead of writing one-off implementations tailored to specific problems, the focus now turns to creating a flexible and pluggable genetic algorithm (GA) framework. This architecture allows developers to reuse core evolutionary components across different problem domains.

In this post, we will walk through the structure of a modular GA framework in C# that supports swappable implementations for selection, crossover, mutation, and fitness evaluation. By abstracting core logic into interfaces and strategy classes, we can build highly adaptable systems without rewriting boilerplate GA logic.

### Why Build a GA Framework?

Most GA codebases begin as monolithic scripts with tightly coupled logic. But as you scale and tackle diverse problems like string evolution, TSP, or class scheduling, this approach becomes a liability. A framework helps to:

- Reuse generic GA logic

- Plug in different operators with minimal code changes

- Separate concerns (selection vs. mutation vs. fitness)

- Support testing and maintainability

### Core Design: Strategy Interfaces

We begin by defining a set of interfaces that abstract each key component.

```csharp
public interface IChromosome
{
    double Fitness { get; }
    void EvaluateFitness();
    IChromosome Crossover(IChromosome partner);
    void Mutate();
    IChromosome Clone();
}

```

This represents an individual in the population. It is problem-specific, so users of the framework will implement their own chromosome logic.

```csharp
public interface ISelectionStrategy
{
    IEnumerable<IChromosome> SelectParents(List<IChromosome> population);
}

public interface IMutationStrategy
{
    void Mutate(IChromosome chromosome);
}

public interface ICrossoverStrategy
{
    IChromosome Crossover(IChromosome parent1, IChromosome parent2);
}

```

Each interface defines a contract for how the GA should handle evolution. Now we can define a generic `GeneticAlgorithm` engine.

### The GA Engine

```csharp
public class GeneticAlgorithm(
    Func<IChromosome> chromosomeFactory,
    ISelectionStrategy selection,
    ICrossoverStrategy crossover,
    IMutationStrategy mutation,
    int populationSize,
    int generations)
{
    private List<IChromosome> _population = Enumerable.Range(0, populationSize)
        .Select(_ => chromosomeFactory()).ToList();

    public void Run()
    {
        for (int gen = 0; gen < generations; gen++)
        {
            foreach (var individual in _population)
                individual.EvaluateFitness();

            _population = _population.OrderByDescending(c => c.Fitness).ToList();
            Console.WriteLine($"Gen {gen}: Best = {_population.First().Fitness}");

            var newPopulation = new List<IChromosome>();

            while (newPopulation.Count < populationSize)
            {
                var parents = selection.SelectParents(_population).ToList();
                var child = crossover.Crossover(parents[0], parents[1]);
                mutation.Mutate(child);
                newPopulation.Add(child);
            }

            _population = newPopulation;
        }
    }
}
```

### Implementing a Problem

Let’s say you want to evolve the string "HELLO WORLD". Your chromosome might look like this:

```csharp
public class StringChromosome : IChromosome
{
    private static readonly Random _random = new();
    private const string Target = "HELLO WORLD";
    private const string Charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZ ";
    private string Genes { get; private set; }
    public double Fitness { get; private set; }

    private StringChromosome(string genes)
    {
        Genes = genes;
        EvaluateFitness();
    }

    public static StringChromosome CreateRandom()
    {
        var genes = new string(Enumerable.Range(0, Target.Length)
            .Select(_ => Charset[_random.Next(Charset.Length)]).ToArray());
        return new StringChromosome(genes);
    }

    public void EvaluateFitness()
    {
        int score = 0;
        for (int i = 0; i < Target.Length; i++)
            if (Genes[i] == Target[i]) score++;

        Fitness = score;
    }

    public IChromosome Crossover(IChromosome partner)
    {
        var p2 = (StringChromosome)partner;
        char[] childGenes = new char[Genes.Length];
        for (int i = 0; i < Genes.Length; i++)
        {
            childGenes[i] = _random.NextDouble() < 0.5 ? Genes[i] : p2.Genes[i];
        }
        return new StringChromosome(new string(childGenes));
    }

    public void Mutate()
    {
        char[] chars = Genes.ToCharArray();
        int index = _random.Next(chars.Length);
        chars[index] = Charset[_random.Next(Charset.Length)];
        Genes = new string(chars);
        EvaluateFitness();
    }

    public IChromosome Clone()
    {
        return new StringChromosome(Genes);
    }
}

```

### Sample Strategies

```csharp
public class TournamentSelection : ISelectionStrategy
{
    private readonly Random _random = new();

    public IEnumerable<IChromosome> SelectParents(List<IChromosome> population)
    {
        return Enumerable.Range(0, 2)
            .Select(_ => population
                .OrderBy(_ => _random.Next())
                .Take(5)
                .OrderByDescending(c => c.Fitness)
                .First());
    }
}

public class DefaultMutation : IMutationStrategy
{
    public void Mutate(IChromosome chromosome) => chromosome.Mutate();
}

public class DefaultCrossover : ICrossoverStrategy
{
    public IChromosome Crossover(IChromosome p1, IChromosome p2) => p1.Crossover(p2);
}

```

### Running the Framework

```csharp
var ga = new GeneticAlgorithm(
    chromosomeFactory: StringChromosome.CreateRandom,
    selection: new TournamentSelection(),
    crossover: new DefaultCrossover(),
    mutation: new DefaultMutation(),
    populationSize: 100,
    generations: 50);

ga.Run();

```

### Final Thoughts

By abstracting the components of a Genetic Algorithm into swappable interfaces, you now have a reusable, maintainable, and testable GA framework. You can create new problems by simply implementing a new `IChromosome`, and swap out operators without rewriting your loop. In future posts, we will extend this framework to support logging, NSGA-II, and hybrid techniques like memetic search.

If you're solving optimization problems across domains, a pluggable design like this accelerates experimentation and enhances production readiness.
