---
title: "Day 4: Designing Your First Chromosome Class in C#"
date: 2025-06-06
categories: 
  - "genetic-algorithms"
tags: 
  - "net"
  - "ai"
  - "c"
  - "dotnet"
  - "genetic-algorithms"
  - "programming"
coverImage: "./images/Designing-Your-First-Chromosome-Class-in-C.png"
---

Now that we’ve explored the concept of genes and chromosomes in the context of genetic algorithms, it’s time to write some real code. Today’s goal is to design a reusable, extensible `Chromosome` class in C# that can serve as the foundation for solving optimization problems using genetic algorithms.

We will not only model the chromosome itself, but also lay the groundwork for operations such as initialization, crossover, mutation, and evaluation. Think of this class as the central actor in your evolutionary simulation.

## Defining the Purpose

A chromosome represents a candidate solution. It needs to encapsulate the data that defines that solution and provide mechanisms to modify and evaluate itself. For our initial implementation, we’ll assume that each gene is a `char` selected from a fixed gene pool, and the chromosome is trying to evolve toward a target string.

This mirrors a common GA example: string evolution. It’s simple but powerful enough to illustrate all the necessary components.

## First Iteration: Basic Structure

Let’s begin with a basic `Chromosome` class:

```csharp
public class Chromosome
{
    private static readonly string GenePool = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ ,!";
    private static readonly Random Random = new Random();

    public char[] Genes { get; private set; }

    public Chromosome(int length)
    {
        Genes = new char[length];
        for (int i = 0; i < length; i++)
        {
            Genes[i] = RandomGene();
        }
    }

    private char RandomGene()
    {
        return GenePool[Random.Next(GenePool.Length)];
    }

    public string GetPhrase()
    {
        return new string(Genes);
    }

    public override string ToString()
    {
        return GetPhrase();
    }
}
```

This class gives us a randomly generated chromosome of a given length. Each gene is a character picked from a configurable gene pool. This is the genetic “DNA” of our solution.

## Adding Fitness Evaluation

To guide the selection process, each chromosome must be evaluated for fitness. For string evolution, fitness can be defined as the number of characters that match the target string in the correct position.

```csharp
public int GetFitness(string target)
{
    int score = 0;
    for (int i = 0; i < Genes.Length; i++)
    {
        if (Genes[i] == target[i])
        {
            score++;
        }
    }
    return score;
}
```

This allows us to score and rank the population. The higher the score, the closer the chromosome is to the target.

## Supporting Crossover

Next, we implement a crossover method to produce a child chromosome by mixing genes from two parents:

```csharp
public Chromosome Crossover(Chromosome partner)
{
    char[] childGenes = new char[Genes.Length];
    int midpoint = Genes.Length / 2;

    for (int i = 0; i < Genes.Length; i++)
    {
        childGenes[i] = i < midpoint ? Genes[i] : partner.Genes[i];
    }

    return new Chromosome(childGenes);
}

public Chromosome(char[] genes)
{
    Genes = genes;
}
```

This simple one-point crossover splits the genes at the midpoint. More advanced techniques, like two-point or uniform crossover, can be added later for more diversity.

## Adding Mutation Support

Mutation prevents the population from becoming too homogenous and getting stuck in local optima. We add a method to randomly change some genes based on a mutation rate:

```csharp
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
```

Mutation rates are typically kept low—around 1 to 5 percent—to maintain stability while still exploring the solution space.

## Wrapping It Up

Our final `Chromosome` class now supports:

- Initialization with random genes

- Conversion to string for display

- Fitness scoring against a target

- Crossover between parents

- Mutation of random genes

This is the engine behind your evolving code. Everything else in your genetic algorithm is the population, selection, and reproduction loop. We will build around this foundation.

## Up Next

Tomorrow we will dive into fitness functions in more detail. You will learn how to craft scoring mechanisms that accurately reflect the goals of your problem and drive evolution in the right direction.

With a chromosome class in place, your code is ready to evolve.
