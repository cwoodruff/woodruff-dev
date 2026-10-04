---
title: "Day 19: Scheduling with DNA: Using Genetic Algorthms for Class and Work Timetables"
date: 2025-08-04
categories: 
  - "genetic-algorithms"
tags: 
  - "net"
  - "ai"
  - "c"
  - "dotnet"
  - "genetic-algorithms"
  - "programming"
coverImage: "./images/Scheduling-with-DNA-Using-GAs-for-Class-and-Work-Timetables.png"
---

Scheduling is a classic example of a constraint satisfaction problem that often becomes too complex for brute-force or greedy solutions. Whether you're designing class timetables for a university or shift schedules for employees, the number of constraints quickly increases, making traditional methods inefficient. Genetic Algorithms provide a flexible and powerful approach to finding feasible and optimized schedules by evolving solutions that balance multiple constraints over time.

In a scheduling problem, the chromosome represents a potential timetable. Each gene can represent a time-slot assignment for a course, instructor, room, or shift. Unlike simple optimization problems, scheduling must respect both hard constraints (such as no double-booking of rooms or overlapping classes for a teacher) and soft constraints (such as preferring certain time blocks).

Let's begin by designing a basic chromosome for a simplified class scheduling problem. Assume we need to schedule a set of courses into time slots and rooms, making sure no room or instructor is double-booked.

Define a data model for the basic entities:

```csharp
public class Course
{
    public string Id { get; set; }
    public string Instructor { get; set; }
    public int DurationSlots { get; set; }
}

public class TimeSlot
{
    public int Day { get; set; }  // 0 = Monday, 1 = Tuesday, ...
    public int Hour { get; set; } // 0 = 8am, 1 = 9am, ...
}

public class Room
{
    public string Id { get; set; }
    public int Capacity { get; set; }
}
```

Define the chromosome as a mapping of course assignments to room and time:

```csharp
public class ScheduleGene
{
    public string CourseId { get; set; }
    public string RoomId { get; set; }
    public TimeSlot Slot { get; set; }
}

public class ScheduleChromosome
{
    public List<ScheduleGene> Genes { get; set; }
    public double FitnessScore { get; set; }

    public ScheduleChromosome(List<Course> courses, List<Room> rooms, List<TimeSlot> timeSlots)
    {
        var rand = new Random();
        Genes = courses.Select(course => new ScheduleGene
        {
            CourseId = course.Id,
            RoomId = rooms[rand.Next(rooms.Count)].Id,
            Slot = timeSlots[rand.Next(timeSlots.Count)]
        }).ToList();
    }

    public ScheduleChromosome DeepCopy()
    {
        return new ScheduleChromosome
        {
            Genes = Genes.Select(g => new ScheduleGene
            {
                CourseId = g.CourseId,
                RoomId = g.RoomId,
                Slot = new TimeSlot { Day = g.Slot.Day, Hour = g.Slot.Hour }
            }).ToList()
        };
    }

    private ScheduleChromosome() { }
}
```

Next, implement a fitness function. Penalize the chromosome for violating constraints such as overlapping classes for instructors or rooms:

```csharp
public void EvaluateFitness(List<Course> courses)
{
    int violations = 0;

    var roomSchedule = new Dictionary<string, HashSet<string>>();
    var instructorSchedule = new Dictionary<string, HashSet<string>>();

    foreach (var gene in Genes)
    {
        var key = $"{gene.Slot.Day}-{gene.Slot.Hour}";

        var course = courses.First(c => c.Id == gene.CourseId);
        var instructor = course.Instructor;

        // Check room conflict
        string roomKey = $"{gene.RoomId}:{key}";
        if (!roomSchedule.TryAdd(roomKey, new HashSet<string> { gene.CourseId }))
            violations++;

        // Check instructor conflict
        string instructorKey = $"{instructor}:{key}";
        if (!instructorSchedule.TryAdd(instructorKey, new HashSet<string> { gene.CourseId }))
            violations++;
    }

    FitnessScore = 1.0 / (1 + violations);
}
```

Crossover can be implemented by taking half of the assignments from one parent and filling the rest from the other:

```csharp
public static ScheduleChromosome Crossover(ScheduleChromosome parent1, ScheduleChromosome parent2)
{
    var rand = new Random();
    var child = parent1.DeepCopy();

    for (int i = 0; i < child.Genes.Count; i++)
    {
        if (rand.NextDouble() < 0.5)
        {
            child.Genes[i].RoomId = parent2.Genes[i].RoomId;
            child.Genes[i].Slot = new TimeSlot
            {
                Day = parent2.Genes[i].Slot.Day,
                Hour = parent2.Genes[i].Slot.Hour
            };
        }
    }

    return child;
}
```

Mutation randomly reassigns a course to a new time slot or room:

```csharp
public void Mutate(List<Room> rooms, List<TimeSlot> slots, double mutationRate)
{
    var rand = new Random();
    foreach (var gene in Genes)
    {
        if (rand.NextDouble() < mutationRate)
        {
            gene.RoomId = rooms[rand.Next(rooms.Count)].Id;
            gene.Slot = slots[rand.Next(slots.Count)];
        }
    }
}
```

Now run the genetic algorithm loop as usual:

```csharp
List<ScheduleChromosome> population = InitializePopulation();
for (int gen = 0; gen < maxGenerations; gen++)
{
    foreach (var chromosome in population)
        chromosome.EvaluateFitness(courses);

    var elites = population.OrderByDescending(c => c.FitnessScore)
                           .Take(eliteCount)
                           .Select(e => e.DeepCopy())
                           .ToList();

    var nextGen = new List<ScheduleChromosome>(elites);

    while (nextGen.Count < populationSize)
    {
        var parent1 = TournamentSelect(population);
        var parent2 = TournamentSelect(population);
        var child = ScheduleChromosome.Crossover(parent1, parent2);
        child.Mutate(rooms, slots, mutationRate);
        nextGen.Add(child);
    }

    population = nextGen;
    Console.WriteLine($"Gen {gen}: Best Fitness = {population.Max(p => p.FitnessScore):F4}");
}
```

The scheduling problem demonstrates the real power of genetic algorithms. Constraints can be layered and diverse, yet the GA adapts through evolutionary pressure. Unlike handcrafted, rule-based schedulers, your solution can be easily adjusted to new requirements by tweaking the constraint logic or fitness scoring.

As the complexity of scheduling scenarios increases, your GA will continue to scale. Add room capacities, student course selections, or instructor preferences. The genetic algorithm doesn't require re-engineering from scratch. It simply evolves with your definition of fitness.

You can find the code demos for the GA series at [https://github.com/cwoodruff/genetic-algorithms-blog-series](https://github.com/cwoodruff/genetic-algorithms-blog-series)
