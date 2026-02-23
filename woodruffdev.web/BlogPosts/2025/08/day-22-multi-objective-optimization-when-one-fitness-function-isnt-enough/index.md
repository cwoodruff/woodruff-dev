---
title: "Day 22: Multi-Objective Optimization: When One Fitness Function Isn't Enough"
date: 2025-08-11
categories: 
  - "genetic-algorithms"
tags: 
  - "net"
  - "ai"
  - "c"
  - "dotnet"
  - "genetic-algorithms"
  - "programming"
coverImage: "Multi-Objective-Optimization-When-One-Fitness-Function-Isnt-Enough.png"
---

In many real-world problems, a single fitness function is insufficient to capture the complexity of the solution space. Applications in engineering, logistics, finance, and machine learning often involve trade-offs among competing objectives. For example, minimizing cost while maximizing performance, or reducing power consumption without sacrificing accuracy. In these cases, genetic algorithms (GAs) can be extended to support multi-objective optimization.

This post introduces the concept of multi-objective GAs, discusses how to represent solutions, and walks through how to score candidates using multiple criteria using a Pareto-based approach.

### Why Multiple Objectives?

Let’s consider a scheduling scenario. You might want to:

- Minimize the number of schedule conflicts

- Maximize the number of preferred assignments met

- Ensure fairness in the distribution of work

Each of these is a valid and important goal. But how do we evolve solutions that balance all of them?

Multi-objective optimization doesn’t attempt to collapse all goals into a single fitness value. Instead, it evaluates trade-offs across objectives and maintains a _Pareto front_ of solutions that are non-dominated, meaning no other solution is better in all objectives.

### Modeling Objectives in Code

Let’s define a basic structure for multi-objective chromosomes in C#. We’ll assume two objectives for simplicity.

```csharp
public class MultiObjectiveChromosome
{
    public int[] Genes { get; set; }
    public double Objective1 { get; set; }
    public double Objective2 { get; set; }

    public static MultiObjectiveChromosome Random(int length, Random rand)
    {
        return new MultiObjectiveChromosome
        {
            Genes = Enumerable.Range(0, length).Select(_ =&gt; rand.Next(0, 2)).ToArray()
        };
    }

    public void Evaluate()
    {
        Objective1 = CalculateConflicts();
        Objective2 = CalculateFairness();
    }

    private double CalculateConflicts()
    {
        return Genes.Count(g =&gt; g == 1); // dummy example
    }

    private double CalculateFairness()
    {
        return 1.0 / (1 + Genes.Distinct().Count()); // dummy example
    }
}
```

Each chromosome now has multiple objectives rather than a single fitness value. Our next step is to determine how we compare individuals.

### Pareto Dominance

Pareto dominance is the core concept behind comparing individuals in multi-objective optimization. A chromosome A dominates chromosome B if:

- A is no worse in all objectives, and

- A is better in at least one objective

Here’s how to implement a dominance comparison:

```csharp
public static bool Dominates(MultiObjectiveChromosome a, MultiObjectiveChromosome b)
{
    bool betterInAny = false;

    if (a.Objective1 < b.Objective1) betterInAny = true;
    else if (a.Objective1 > b.Objective1) return false;

    if (a.Objective2 < b.Objective2) betterInAny = true;
    else if (a.Objective2 > b.Objective2) return false;

    return betterInAny;
}
```

With this rule, we can build the Pareto front, the set of individuals not dominated by any other in the population.

### Evolving a Pareto Front

A basic version of NSGA-II (Non-dominated Sorting Genetic Algorithm II) would:

1. Sort individuals by rank (Pareto fronts)

3. Use crowding distance to maintain diversity

5. Select individuals based on their rank and crowding distance

For simplicity, we’ll select non-dominated individuals as elite survivors.

```csharp
public static List<MultiObjectiveChromosome> GetParetoFront(List<MultiObjectiveChromosome> population)
{
    var front = new List<MultiObjectiveChromosome>();

    foreach (var candidate in population)
    {
        bool dominated = population.Any(other => other != candidate && Dominates(other, candidate));
        if (!dominated) front.Add(candidate);
    }

    return front;
}
```

This method ensures the selected chromosomes are those that represent the best trade-offs between the objectives.

### Conclusion

Single-objective optimization simplifies problem solving but can miss the nuances of real-world decision making. Multi-objective genetic algorithms provide a powerful tool to navigate complex trade-offs, guiding you toward a diverse set of optimal solutions.

In the next post, we will explore how to scale your GA to larger populations and longer evolution cycles, balancing performance and resource constraints.

You can find the code demos for the GA series at [https://github.com/cwoodruff/genetic-algorithms-blog-series](https://github.com/cwoodruff/genetic-algorithms-blog-series)
