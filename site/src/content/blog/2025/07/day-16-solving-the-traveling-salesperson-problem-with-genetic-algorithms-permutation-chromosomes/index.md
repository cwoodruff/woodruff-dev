---
title: "Day 16: Solving the Traveling Salesperson Problem with Genetic Algorithms Permutation Chromosomes"
date: 2025-07-29
categories: 
  - "genetic-algorithms"
tags: 
  - "net"
  - "ai"
  - "c"
  - "dotnet"
  - "genetic-algorithms"
  - "programming"
coverImage: "./images/Solving-the-Traveling-Salesperson-Problem-with-Permutation-Chromosomes.png"
---

The Traveling Salesperson Problem, also known as TSP, is one of the most extensively studied combinatorial optimization problems in computer science. Given a set of cities and the distances between them, the objective is to find the shortest possible route that visits each city exactly once and returns to the starting point. This problem is computationally difficult to solve using brute-force methods because the number of possible routes grows factorially with the number of cities.

Genetic algorithms provide a compelling approach to solving the TSP efficiently by utilizing permutation-based chromosomes to represent city visit sequences. Unlike bitstrings or character arrays, permutation chromosomes maintain the unique ordering required by TSP constraints. In this post, you’ll learn how to model TSP chromosomes in C#, implement crossover and mutation for permutations, and apply a GA to find short routes in a graph of cities.

A permutation chromosome represents a tour as an ordered list of city indices. For example, a chromosome with values \[0, 3, 1, 2\] represents a route where the salesperson starts at city 0, travels to city 3, then to city 1, and finally to city 2 before returning to city 0.

To begin, define the chromosome:

```csharp
public class PermutationChromosome
{
    public int[] Genes { get; private set; }
    public double FitnessScore { get; set; }

    private static readonly Random Random = new();

    public PermutationChromosome(int cityCount)
    {
        Genes = Enumerable.Range(0, cityCount)
                          .OrderBy(_ => Random.Next())
                          .ToArray();
    }

    public PermutationChromosome(int[] genes)
    {
        Genes = genes.ToArray();
    }

    public double CalculateDistance(double[,] distanceMatrix)
    {
        double total = 0;
        for (int i = 0; i < Genes.Length - 1; i++)
        {
            total += distanceMatrix[Genes[i], Genes[i + 1]];
        }
        total += distanceMatrix[Genes[^1], Genes[0]]; // return to start
        return total;
    }

    public void Mutate(double mutationRate)
    {
        for (int i = 0; i < Genes.Length; i++)
        {
            if (Random.NextDouble() < mutationRate)
            {
                int j = Random.Next(Genes.Length);
                (Genes[i], Genes[j]) = (Genes[j], Genes[i]);
            }
        }
    }

    public PermutationChromosome OrderCrossover(PermutationChromosome other)
    {
        int length = Genes.Length;
        int start = Random.Next(length);
        int end = Random.Next(start, length);

        var childGenes = new int[length];
        Array.Fill(childGenes, -1);

        for (int i = start; i < end; i++)
        {
            childGenes[i] = Genes[i];
        }

        int otherIndex = 0;
        for (int i = 0; i < length; i++)
        {
            if (childGenes[i] == -1)
            {
                while (childGenes.Contains(other.Genes[otherIndex]))
                {
                    otherIndex++;
                }
                childGenes[i] = other.Genes[otherIndex];
            }
        }

        return new PermutationChromosome(childGenes);
    }

    public override string ToString() => string.Join(" -> ", Genes);
}
```

In this model, the fitness function is based on route length. Lower distances are better, so we often use the inverse distance as the fitness score:

```csharp
public void EvaluateFitness(double[,] distanceMatrix)
{
    double distance = CalculateDistance(distanceMatrix);
    FitnessScore = 1.0 / distance;
}
```

For the GA loop, use a typical configuration:

```csharp
const int cityCount = 10;
const int populationSize = 100;
const int generations = 500;
const double mutationRate = 0.02;
const int eliteCount = 2;
double[,] distanceMatrix = GenerateRandomDistances(cityCount);

var population = Enumerable.Range(0, populationSize)
    .Select(_ => new PermutationChromosome(cityCount))
    .ToList();

for (int gen = 0; gen < generations; gen++)
{
    foreach (var c in population)
        c.EvaluateFitness(distanceMatrix);

    var elites = population.OrderByDescending(c => c.FitnessScore)
                           .Take(eliteCount)
                           .Select(e => new PermutationChromosome(e.Genes))
                           .ToList();

    var nextGen = new List<PermutationChromosome>(elites);
    while (nextGen.Count < populationSize)
    {
        var parent1 = TournamentSelect(population);
        var parent2 = TournamentSelect(population);
        var child = parent1.OrderCrossover(parent2);
        child.Mutate(mutationRate);
        nextGen.Add(child);
    }

    population = nextGen;

    var best = population.OrderByDescending(c => c.FitnessScore).First();
    Console.WriteLine($"Gen {gen}: {best} Distance: {1 / best.FitnessScore:F2}");
}
```

The TournamentSelect method randomly samples a few chromosomes and returns the fittest:

```csharp
static PermutationChromosome TournamentSelect(List<PermutationChromosome> pop, int size = 5)
{
    var group = pop.OrderBy(_ => Guid.NewGuid()).Take(size);
    return group.OrderByDescending(c => c.FitnessScore).First();
}
```

The GenerateRandomDistances method initializes a symmetric distance matrix between cities:

```csharp
static double[,] GenerateRandomDistances(int size)
{
    var rand = new Random();
    var matrix = new double[size, size];
    for (int i = 0; i < size; i++)
    {
        for (int j = i + 1; j < size; j++)
        {
            double dist = rand.Next(10, 100);
            matrix[i, j] = dist;
            matrix[j, i] = dist;
        }
    }
    return matrix;
}
```

Using permutation chromosomes ensures each gene (city) appears exactly once. Crossover methods, such as Order Crossover, and mutation strategies, like swap mutation, help maintain validity without requiring manual repair. This makes genetic algorithms a strong match for solving problems like TSP, where permutation integrity is critical.

Solving TSP with a GA is an ideal application of evolutionary computing. The problem is simple to describe, hard to solve by brute force, and benefits directly from techniques that explore diverse permutations and preserve useful partial solutions. With careful design, your GA will discover routes that surpass random search and provide practical solutions to complex optimization challenges.

You can find the code demos for the GA series at [https://github.com/cwoodruff/genetic-algorithms-blog-series](https://github.com/cwoodruff/genetic-algorithms-blog-series)
