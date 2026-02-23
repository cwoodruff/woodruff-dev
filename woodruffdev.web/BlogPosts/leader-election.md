---
date: 2026-02-10
category: Patterns
description: "Leader election is the pattern that turns 'somebody should run this' into 'exactly one node is allowed to run this, and it must be provable.'"
---
# Leader Election in .NET: Picking One Boss Without Creating Two

*Picking one boss without creating two.*

Leader election is the pattern that turns "somebody should run this" into "exactly one node is allowed to run this, and it must be provable."

## The Problem

In a distributed system, certain tasks should only be performed by one node at a time. Sending a daily report, running a migration, processing a queue—these are operations where duplication causes real harm. Without coordination, you get two nodes both convinced they are the leader, and both acting on that belief.

## How Leader Election Works

The basic protocol:

1. **All candidates compete** for a shared resource (a database row, a distributed lock, a consensus slot)
2. **Exactly one wins** and becomes the leader
3. **The leader periodically renews** its claim (often via a lease)
4. **If the leader fails to renew**, other candidates detect this and a new election begins

## A Simple Database-Based Election

Using a database row as the coordination point:

```csharp
public class DatabaseLeaderElection
{
    private readonly string _nodeId = Guid.NewGuid().ToString();
    private readonly IDbConnection _db;
    private readonly TimeSpan _leaseDuration = TimeSpan.FromSeconds(30);

    public async Task<bool> TryBecomeLeaderAsync(string electionName)
    {
        var now = DateTime.UtcNow;
        var expiry = now + _leaseDuration;

        // Try to insert or update if the lease has expired
        var affected = await _db.ExecuteAsync(
            """
            UPDATE Leaders SET NodeId = @NodeId, ExpiresAt = @Expiry
            WHERE ElectionName = @Name AND ExpiresAt < @Now
            """,
            new { NodeId = _nodeId, Expiry = expiry, Name = electionName, Now = now });

        return affected > 0;
    }

    public async Task<bool> RenewLeaseAsync(string electionName)
    {
        var expiry = DateTime.UtcNow + _leaseDuration;

        var affected = await _db.ExecuteAsync(
            """
            UPDATE Leaders SET ExpiresAt = @Expiry
            WHERE ElectionName = @Name AND NodeId = @NodeId
            """,
            new { Expiry = expiry, Name = electionName, NodeId = _nodeId });

        return affected > 0;
    }
}
```

## Consensus-Based Approaches

For stronger guarantees, consider using a consensus system like etcd, ZooKeeper, or Consul. These systems implement Raft or Paxos under the hood and provide leader election as a first-class primitive.

## Common Pitfalls

- **Split brain** — two nodes both believe they are leader. This usually happens when lease durations are too short relative to network latency.
- **Stale leader** — the leader continues acting after its lease has expired. Always check lease validity before performing leader-only work.
- **Thundering herd** — all nodes compete simultaneously when the leader fails. Add jitter to election retry intervals.

## Key Takeaway

Leader election is not about picking the best node. It is about ensuring exactly one node acts at a time, and recovering gracefully when that node fails. The mechanism must be simple enough to trust, because in a distributed system, trust is earned by surviving failure.
