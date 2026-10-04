---
title: "Day 20: Constraint Handling in Fitness Functions: Penalizing Bad Solutions"
date: 2025-08-05
categories: 
  - "genetic-algorithms"
tags: 
  - "net"
  - "ai"
  - "c"
  - "dotnet"
  - "genetic-algorithms"
  - "programming"
coverImage: "./images/Constraint-Handling-in-Fitness-Functions-Penalizing-Bad-Solution.png"
---

Genetic algorithms are powerful optimization tools, but real-world problems often involve constraints that cannot be ignored. In scheduling, routing, resource allocation, and layout optimization, constraints like resource limits, timing conflicts, and exclusivity rules define what makes a solution valid. Without a mechanism to enforce or guide adherence to these rules, a genetic algorithm may evolve highly fit but completely infeasible solutions.

One effective way to handle this is by integrating **constraint penalties directly into the fitness function**. By penalizing violations, we make sure that invalid solutions are less likely to survive and propagate in future generations. This method requires designing fitness functions that are both accurate evaluators and adaptive enforcers of your problem's rules.

Let’s explore this in the context of a scheduling problem. A chromosome represents assignments of classes to time slots and rooms. Hard constraints might include:

- No room can be assigned to more than one class at the same time

- No instructor can teach more than one class at the same time

- No student group can attend more than one class at a time

We begin with a base fitness function that counts the number of complex constraints violated. The fewer the violations, the higher the fitness.

```csharp
public double EvaluateFitness(List<Course> courses, List<StudentGroup> groups)
{
    int violations = 0;

    var roomOccupancy = new Dictionary<string, HashSet<string>>();
    var instructorSchedule = new Dictionary<string, HashSet<string>>();
    var studentGroupSchedule = new Dictionary<string, HashSet<string>>();

    foreach (var gene in Genes)
    {
        var key = $"{gene.Slot.Day}-{gene.Slot.Hour}";

        // Room conflicts
        string roomKey = $"{gene.RoomId}:{key}";
        if (!roomOccupancy.TryAdd(roomKey, new HashSet<string> { gene.CourseId }))
            violations++;

        // Instructor conflicts
        var course = courses.First(c => c.Id == gene.CourseId);
        string instructorKey = $"{course.Instructor}:{key}";
        if (!instructorSchedule.TryAdd(instructorKey, new HashSet<string> { gene.CourseId }))
            violations++;

        // Student group conflicts
        foreach (var groupId in course.StudentGroupIds)
        {
            string groupKey = $"{groupId}:{key}";
            if (!studentGroupSchedule.TryAdd(groupKey, new HashSet<string> { gene.CourseId }))
                violations++;
        }
    }

    // Convert violations to fitness
    return 1.0 / (1 + violations);
}
```

This simple rule ensures that perfect schedules (with zero violations) have the highest fitness of 1.0, while infeasible schedules score lower as the number of violations increases. You can make this more flexible by assigning different weights to different constraints:

```csharp
public double EvaluateWeightedFitness(List<Course> courses)
{
    int roomConflicts = 0;
    int instructorConflicts = 0;

    var roomMap = new Dictionary<string, HashSet<string>>();
    var instructorMap = new Dictionary<string, HashSet<string>>();

    foreach (var gene in Genes)
    {
        var key = $"{gene.Slot.Day}-{gene.Slot.Hour}";
        var course = courses.First(c => c.Id == gene.CourseId);

        if (!roomMap.TryAdd($"{gene.RoomId}:{key}", new HashSet<string> { gene.CourseId }))
            roomConflicts++;

        if (!instructorMap.TryAdd($"{course.Instructor}:{key}", new HashSet<string> { gene.CourseId }))
            instructorConflicts++;
    }

    double penalty = roomConflicts * 10 + instructorConflicts * 5;
    return 1.0 / (1 + penalty);
}
```

This scoring system reflects the severity of each type of violation. A room conflict is more expensive than an instructor overlap. This helps the GA learn which rules are most critical and prioritize fixing those first.

Another powerful technique is **adaptive penalty scaling**. This involves increasing the severity of penalties over generations. Early in the process, diversity and exploration are more important, so minor violations are tolerated. As evolution progresses, constraints are enforced more strictly to fine-tune solutions.

```csharp
public double EvaluateAdaptiveFitness(List<Course> courses, int generation)
{
    int violations = CountViolations(courses);
    double penaltyWeight = 1.0 + generation / 50.0; // Increase over time
    return 1.0 / (1 + penaltyWeight * violations);
}
```

Adaptive penalties help prevent premature convergence by allowing exploratory solutions in early generations, then tightening enforcement as the population stabilizes.

Penalties can also be combined with **soft preferences**. A class may prefer certain times or instructors may prefer fewer late sessions. These preferences are not hard constraints, but satisfying them improves overall quality.

```csharp
public double EvaluateWithPreferences(List<Course> courses)
{
    int violations = CountHardViolations(courses);
    int preferenceScore = CountSatisfiedPreferences();

    double baseFitness = 1.0 / (1 + violations);
    return baseFitness + 0.01 * preferenceScore;
}
```

Soft constraints can drive improvements once feasibility is reached, encouraging the GA to optimize for quality beyond just validity.

When dealing with constraints in GAs, the goal is not only to reject poor solutions, but also to gently guide the population toward valid, high-performing regions of the search space. Well-designed fitness functions serve as both a compass and a filter, rewarding desirable traits and discouraging undesirable ones.

Constraint handling via fitness penalties is an elegant and effective way to mold the evolutionary process. It provides developers with the flexibility to encode complex rules directly into the natural selection mechanism, allowing them to evolve solutions that are not only optimized but also compliant with the problem's domain logic.

You can find the code demos for the GA series at [https://github.com/cwoodruff/genetic-algorithms-blog-series](https://github.com/cwoodruff/genetic-algorithms-blog-series)
