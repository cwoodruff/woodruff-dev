---
title: "Day 7: Putting It Together: Simulating Your First Genetic Algorthm Cycle in .NET"
date: 2025-06-10
categories: 
  - "genetic-algorithms"
tags: 
  - "net"
  - "ai"
  - "c"
  - "dotnet"
  - "genetic-algorithms"
  - "programming"
coverImage: "./images/Putting-It-Together-Simulating-Your-First-GA-Cycle-in-.NET_.png"
---

By now, you’ve learned the foundational components of genetic algorithms: chromosomes, genes, fitness functions, mutation, crossover, and selection. Today, it’s time to bring those elements together and run your first complete GA cycle using C# and .NET.

This post walks you through the structure of a single evolutionary loop, initialization, evaluation, selection, reproduction, and mutation, so you can simulate a working genetic algorithm and evolve real solutions.

## Recap: The Genetic Algorithm Workflow

Each generation in a genetic algorithm follows this pattern:

1. **Initialize** a population of chromosomes

3. **Evaluate** their fitness

5. **Select** parents based on fitness

7. **Crossover** to produce offspring

9. **Mutate** offspring to introduce variation

11. **Replace** the old population with a new generation

13. Repeat for a fixed number of generations or until a solution is found

## Setting Up the Population

Let’s start with a simple goal: evolve a population of strings to match the phrase `"HELLO WORLD"`.

We’ll use a basic `Chromosome` class like the one we’ve developed in previous posts.

### Initializing the Population

```csharp
const string target = "HELLO WORLD";
const int populationSize = 100;
const int generations = 1000;
const double mutationRate = 0.01;

var population = new List<Chromosome>();
for (int i = 0; i < populationSize; i++)
{
    population.Add(new Chromosome(target.Length));
}
```

## Evaluating Fitness

Each chromosome receives a fitness score based on its similarity to the target.

```csharp
foreach (var chromosome in population)
{
    chromosome.FitnessScore = chromosome.GetFitness(target);
}
```

Make sure `FitnessScore` is a public property or field in your `Chromosome` class.

## Selection and Reproduction

We'll use elitism and tournament selection to build the next generation.

```csharp
List<Chromosome> Evolve(List<Chromosome> currentPopulation)
{
    var nextGeneration = new List<Chromosome>();

    // Elitism: preserve the top 2
    var elites = currentPopulation.OrderByDescending(c => c.FitnessScore).Take(2).ToList();
    nextGeneration.AddRange(elites);

    while (nextGeneration.Count < currentPopulation.Count)
    {
        var parent1 = TournamentSelection(currentPopulation, 5);
        var parent2 = TournamentSelection(currentPopulation, 5);

        var child = parent1.Crossover(parent2);
        child.Mutate(mutationRate);
        nextGeneration.Add(child);
    }

    return nextGeneration;
}
```

## Running the Full Evolution Cycle

Now we’ll loop over multiple generations and evolve toward the target.

```csharp
for (int gen = 0; gen < generations; gen++)
{
    foreach (var chromosome in population)
    {
        chromosome.FitnessScore = chromosome.GetFitness(target);
    }

    var best = population.OrderByDescending(c => c.FitnessScore).First();
    Console.WriteLine($"Generation {gen}: {best.GetPhrase()} (Fitness: {best.FitnessScore})");

    if (best.FitnessScore == target.Length)
    {
        Console.WriteLine("Solution found!");
        break;
    }

    population = Evolve(population);
}
```

## Example Output

```bash
Generation 0: KELXO ZRLAQ (Fitness: 2)
Generation 50: HELTO WORHD (Fitness: 10)
Generation 104: HELLO WORLD (Fitness: 11)
Solution found!
```

The algorithm gradually evolves closer to the target string as the population improves.

## Wrap-Up

You’ve now built a full genetic algorithm loop in C#. It may be simple, but it models all the essential behaviors of evolution: variation, selection, and survival of the fittest. From this foundation, you can scale the algorithm to solve more advanced problems, such as route planning, resource scheduling, game AI, and more.

Next week, we’ll explore **crossover techniques** in more detail and look at how different strategies influence genetic diversity.

Your evolutionary engine is running. Now it’s time to refine how traits are passed along.

You can find the code demos for the GA series at [https://github.com/cwoodruff/genetic-algorithms-blog-series](https://github.com/cwoodruff/genetic-algorithms-blog-series)
