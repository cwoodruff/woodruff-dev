---
date: 2026-01-28
category: Patterns
description: "Distributed systems rarely fail because you picked the wrong cloud service. They fail because two nodes believe they are in charge."
---
# Distributed System Pattern: Leader and Followers in .NET

*One decision-maker, many replicas, fewer outages.*

Distributed systems rarely fail because you picked the wrong cloud service. They fail because two nodes believe they are in charge.

## What Is Leader and Followers?

The Leader and Followers pattern designates one node as the authoritative decision-maker while other nodes replicate its state and stand ready to take over. The leader handles all writes. Followers handle reads and serve as hot standbys.

This is the backbone of most database replication systems, message brokers, and coordination services.

## Why Not Just Let Everyone Write?

Multi-writer systems are possible, but they introduce conflict resolution, ordering problems, and complexity that most teams do not need. A single leader eliminates write conflicts by definition. Every write goes through one node, which determines the order.

## The Replication Model

```
Client → Leader → [Write to local log]
                → [Replicate to Follower 1]
                → [Replicate to Follower 2]
                → [Acknowledge to Client]
```

The leader writes to its own log first, then replicates to followers. The question of when to acknowledge the client depends on your consistency requirements:

- **Synchronous replication** — wait for all followers before acknowledging. Strong consistency, higher latency.
- **Asynchronous replication** — acknowledge immediately, replicate in the background. Lower latency, risk of data loss if the leader fails.
- **Semi-synchronous** — wait for at least one follower. A practical middle ground.

## Failover

When the leader fails, a follower must be promoted. This is where leader election comes in. The followers detect the leader's absence (usually via a lease or heartbeat timeout) and run an election to choose a new leader.

```csharp
public class FollowerNode
{
    private readonly TimeSpan _heartbeatTimeout = TimeSpan.FromSeconds(10);
    private DateTime _lastHeartbeat = DateTime.UtcNow;

    public void OnHeartbeatReceived()
    {
        _lastHeartbeat = DateTime.UtcNow;
    }

    public bool IsLeaderAlive()
    {
        return DateTime.UtcNow - _lastHeartbeat < _heartbeatTimeout;
    }

    public async Task MonitorAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (!IsLeaderAlive())
            {
                await InitiateElectionAsync();
            }
            await Task.Delay(1000, ct);
        }
    }

    private Task InitiateElectionAsync()
    {
        // Begin leader election process
        return Task.CompletedTask;
    }
}
```

## Read Scaling

One of the key benefits: you can scale reads by adding followers. Since followers have a copy of the data, they can serve read requests independently. This is how most read-heavy applications achieve scale without sharding.

## Common Pitfalls

- **Replication lag** — followers may serve stale data. Make this explicit in your API contracts.
- **Split brain during failover** — the old leader comes back online and both nodes think they are leader. Use fencing tokens or lease-based leader election to prevent this.
- **Follower promotion gaps** — a follower promoted to leader may be missing the most recent writes if replication was asynchronous.

## Key Takeaway

The Leader and Followers pattern trades write throughput for simplicity and reliability. One writer means no conflicts, clear ordering, and straightforward replication. For most systems, that trade-off is worth it.
