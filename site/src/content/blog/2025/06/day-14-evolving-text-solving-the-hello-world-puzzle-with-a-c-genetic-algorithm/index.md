---
title: "Day 14: Evolving Text: Solving the \"Hello World\" Puzzle with a C# Genetic Algorithm"
date: 2025-06-30
categories: 
  - "genetic-algorithms"
tags: 
  - "net"
  - "ai"
  - "c"
  - "dotnet"
  - "genetic-algorithms"
  - "programming"
coverImage: "./images/Evolving-Text-Solving-the-22Hello-World22-Puzzle-with-a-GA.png"
---

Now that you’ve built the complete set of genetic algorithm components, chromosomes, fitness functions, mutation, crossover, selection, and a configurable loop, it’s time to apply everything in a hands-on project. In today’s post, we’ll use a genetic algorithm to evolve a string toward a target phrase: `"HELLO WORLD"`.

This classic exercise helps demonstrate how genetic algorithms work in a tangible, visual way. You’ll see the population of strings gradually improve, letter by letter, until they match the target. It’s a powerful example of emergent behavior through selection and variation.

## The Problem

We want to evolve a population of randomly generated character sequences so that over time, one of them becomes the exact string `"HELLO WORLD"`.

Each solution (chromosome) is a sequence of characters. Our fitness function evaluates how closely the chromosome aligns with the target. Over successive generations, we use crossover, mutation, and selection to create better solutions.

## Setting Up the Project

Start by defining the configuration for the algorithm:

```csharp
var config = new GAConfig
{
    PopulationSize = 200,
    MaxGenerations = 1000,
    MutationRate = 0.01,
    EliteCount = 2,
    Target = "HELLO WORLD"
};
```

Create an instance of the engine and run it:

csharpCopyEdit`var ga = new GeneticAlgorithm(config);   var best = ga.Run();      Console.WriteLine($"Final Result: {best} (Fitness: {best.FitnessScore})");   `

## The Chromosome Class

```csharp
public class Chromosome
{
    public char[] Genes { get; private set; }
    public int FitnessScore { get; set; }

    private static readonly string GenePool = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz !";
    private static readonly Random Random = new();

    public Chromosome(int length)
    {
        Genes = Enumerable.Range(0, length)
                          .Select(_ => RandomGene())
                          .ToArray();
    }

    public Chromosome(char[] genes)
    {
        Genes = genes;
    }

    public int GetFitness(string target)
    {
        return Genes.Zip(target, (g, t) => g == t ? 1 : 0).Sum();
    }

    public Chromosome UniformCrossover(Chromosome partner)
    {
        char[] childGenes = new char[Genes.Length];
        for (int i = 0; i < Genes.Length; i++)
        {
            childGenes[i] = Random.NextDouble() < 0.5 ? Genes[i] : partner.Genes[i];
        }
        return new Chromosome(childGenes);
    }

    public void Mutate(double mutationRate)
    {
        for (int i = 0; i < Genes.Length; i++)
        {
            if (Random.NextDouble() < mutationRate)
            {
                Genes[i] = RandomGene();
            }
        }
    }

    private static char RandomGene()
    {
        return GenePool[Random.Next(GenePool.Length)];
    }

    public override string ToString() => new string(Genes);
}
```

## Visualizing the Evolution

Inside the GA loop, log each generation’s best candidate:

```csharp
for (int generation = 0; generation < _config.MaxGenerations; generation++)
{
    EvaluatePopulation(population);

    var best = population.OrderByDescending(c => c.FitnessScore).First();
    Console.WriteLine($"Gen {generation}: {best} (Fitness: {best.FitnessScore})");

    if (best.FitnessScore == _config.Target.Length)
        break;

    population = Evolve(population);
}
```

The output will look something like this:

```
Gen 0: kELlX TnzvM (Fitness: 2)Gen 25: HELLp WORmD (Fitness: 10)Gen 38: HELLO WORLD (Fitness: 11)
```

The algorithm slowly improves from gibberish to the target phrase. Each generation gets closer, proving the effectiveness of selection and variation.

## Why This Works

This experiment illustrates key strengths of genetic algorithms:

- The search space is enormous (95^11 possibilities), but GAs narrow it quickly.

- Mutation maintains diversity and avoids local optima.

- Crossover reuses strong gene segments to improve solutions.

- Elitism protects breakthroughs and accelerates convergence.

All of this comes together through a simple, elegant loop.

## Extending the Idea

Now that you’ve evolved a string, try:

- Evolving multiple target phrases

- Scoring by edit distance instead of exact character matches

- Adding adaptive mutation rates

- Visualizing average population fitness over time

These variations prepare you to evolve more complex structures like code, schedules, or routes.

## Conclusion

Evolving text is a classic introduction to genetic algorithms because it reveals the inner mechanics so clearly. Each generation brings better results, not because the algorithm knows the answer, but because it selects and reuses what works.

This is evolution in action, driven by code.

## Up Next

Tomorrow, we begin solving practical optimization problems using GAs in C#. We’ll start with pathfinding and scheduling, building on the tools and techniques you’ve already developed.

Evolution doesn't stop here. It adapts to every domain.

You can find the code demos for the GA series at [https://github.com/cwoodruff/genetic-algorithms-blog-series](https://github.com/cwoodruff/genetic-algorithms-blog-series)
