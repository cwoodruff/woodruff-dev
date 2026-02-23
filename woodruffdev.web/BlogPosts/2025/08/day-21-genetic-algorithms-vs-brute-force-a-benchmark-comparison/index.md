---
title: "Day 21: Genetic Algorithms vs. Brute Force: A Benchmark Comparison"
date: 2025-08-06
categories: 
  - "genetic-algorithms"
tags: 
  - "net"
  - "ai"
  - "c"
  - "dotnet"
  - "genetic-algorithms"
  - "programming"
coverImage: "Genetic-Algorithms-vs.-Brute-Force-A-Benchmark-Comparison.png"
---

To conclude Week 3, let’s address one of the most common questions developers ask when learning about genetic algorithms: How do they perform compared to brute-force solutions? This is especially relevant when working on combinatorial problems, such as the Traveling Salesperson Problem (TSP) or scheduling tasks. Genetic algorithms promise that they offer reasonable solutions in a fraction of the time it takes brute-force methods to find optimal ones. But how does this actually play out?

Today, we’ll benchmark a simple scenario using both approaches and compare execution time and solution quality. We’ll use the TSP with 8 cities as our problem space. This size is large enough to make brute-force non-trivial, but still solvable within a reasonable time.

Let’s start by defining our city coordinates and computing the distance matrix:

```csharp
public static PointF[] GenerateCities()
{
    return new[]
    {
        new PointF(0, 0), new PointF(1, 5), new PointF(5, 1),
        new PointF(2, 2), new PointF(3, 6), new PointF(6, 3),
        new PointF(7, 1), new PointF(4, 4)
    };
}

public static double[,] ComputeDistanceMatrix(PointF[] cities)
{
    int n = cities.Length;
    var matrix = new double[n, n];

    for (int i = 0; i < n; i++)
    {
        for (int j = 0; j < n; j++)
        {
            var dx = cities[i].X - cities[j].X;
            var dy = cities[i].Y - cities[j].Y;
            matrix[i, j] = Math.Sqrt(dx * dx + dy * dy);
        }
    }

    return matrix;
}
```

### Brute Force

A brute-force approach evaluates every possible permutation of city visits and returns the one with the lowest total distance:

```csharp
public static (int[] bestRoute, double bestDistance) BruteForceTSP(double[,] distanceMatrix)
{
    int n = distanceMatrix.GetLength(0);
    var cities = Enumerable.Range(0, n).ToArray();
    var permutations = GetPermutations(cities);

    double bestDistance = double.MaxValue;
    int[] bestRoute = null;

    foreach (var route in permutations)
    {
        double dist = 0;
        for (int i = 0; i < n - 1; i++)
            dist += distanceMatrix[route[i], route[i + 1]];
        dist += distanceMatrix[route[n - 1], route[0]];

        if (dist < bestDistance)
        {
            bestDistance = dist;
            bestRoute = route.ToArray();
        }
    }

    return (bestRoute, bestDistance);
}

// Helper for generating permutations
public static IEnumerable<int[]> GetPermutations(int[] items)
{
    return Permute(items, 0, items.Length - 1);

    IEnumerable<int[]> Permute(int[] array, int l, int r)
    {
        if (l == r)
            yield return array.ToArray();
        else
        {
            for (int i = l; i <= r; i++)
            {
                Swap(ref array[l], ref array[i]);
                foreach (var perm in Permute(array, l + 1, r))
                    yield return perm;
                Swap(ref array[l], ref array[i]);
            }
        }
    }

    void Swap(ref int a, ref int b)
    {
        (a, b) = (b, a);
    }
}
```

This approach guarantees the optimal solution, but its complexity is O(n!) and becomes infeasible beyond 10 cities.

### Genetic Algorithm

Now compare it to a basic GA implementation using permutation chromosomes:

```csharp
public static double EvaluateTSPFitness(int[] route, double[,] distanceMatrix)
{
    double dist = 0;
    for (int i = 0; i < route.Length - 1; i++)
        dist += distanceMatrix[route[i], route[i + 1]];
    dist += distanceMatrix[route[^1], route[0]];

    return 1.0 / dist;
}
```

You initialize a population, select parents with tournament selection, apply ordered crossover, and mutate by swapping two cities. After a set number of generations, you select the best result.

Let’s now benchmark both methods using `Stopwatch`:

```csharp
var cities = GenerateCities();
var distanceMatrix = ComputeDistanceMatrix(cities);

var stopwatch = Stopwatch.StartNew();
var (bfRoute, bfDistance) = BruteForceTSP(distanceMatrix);
stopwatch.Stop();
Console.WriteLine($"Brute Force: {bfDistance:F2} in {stopwatch.ElapsedMilliseconds} ms");

stopwatch.Restart();
var gaBest = RunGeneticTSP(distanceMatrix, generations: 500, populationSize: 100);
stopwatch.Stop();
Console.WriteLine($"Genetic Algorithm: {1.0 / gaBest.FitnessScore:F2} in {stopwatch.ElapsedMilliseconds} ms");
```

### Results

You will typically see the brute-force approach taking hundreds or thousands of milliseconds to complete, while the genetic algorithm finishes in under 100 milliseconds. The GA solution is often within 1–5% of the optimal route found by brute force, especially with well-tuned parameters.

Below is what an 11-city search would look like.

```
Running Genetic Algorithm...GA Best Path: 4 -> 8 -> 3 -> 9 -> 7 -> 2 -> 10 -> 1 -> 5 -> 0 -> 6GA Distance: 275.25GA Time: 321msRunning Brute Force...Brute Best Path: 0 -> 5 -> 1 -> 10 -> 2 -> 7 -> 9 -> 3 -> 8 -> 4 -> 6Brute Distance: 275.25Brute Time: 10832ms
```

### Conclusion

Genetic algorithms do not guarantee an optimal solution, but they are exceptionally good at producing efficient solutions, especially when the solution space becomes too large for exhaustive methods. For small problems, brute force may still be feasible. But for real-world applications involving dozens or hundreds of variables and constraints, genetic algorithms offer a practical and scalable alternative.

In many scenarios, near-optimal is more than good enough when it comes with significant time savings. Genetic algorithms excel by guiding the search process through randomness, selection, and evolutionary pressure, producing high-quality solutions with a fraction of the computational cost.

You can find the code demos for the GA series at [https://github.com/cwoodruff/genetic-algorithms-blog-series](https://github.com/cwoodruff/genetic-algorithms-blog-series)
