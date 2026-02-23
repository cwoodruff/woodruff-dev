---
date: 2026-02-15
category: Patterns
description: "Indefinite locks belong to a world where processes never crash and networks never split. That world does not exist. A lease fixes that by putting a deadline on ownership."
---
# Lease Pattern in .NET: A Lock With an Expiration Date That Saves Your Data

*A lock with an expiration date that saves your data.*

Indefinite locks belong to a world where processes never crash and networks never split. That world does not exist. A lease fixes that by putting a deadline on ownership.

## Why Leases Matter

Every distributed system eventually has to answer the question: **who owns this resource right now?** Traditional locks answer that question permanently—until someone explicitly releases them. The trouble is, processes crash, networks partition, and that "someone" may never come back to release anything.

A lease is a lock with a built-in expiration. The holder must periodically renew it or lose ownership. If the holder disappears, the lease simply expires and another node can claim it.

## The Core Idea

A lease has three properties:

1. **An owner** — the node or process that currently holds it
2. **A duration** — how long the lease is valid
3. **A renewal mechanism** — a way for the owner to extend the lease before it expires

When a lease expires without renewal, ownership is automatically revoked. No cleanup required. No orphaned locks.

## Implementing a Lease in .NET

Here is a simplified lease implementation using a `ConcurrentDictionary` and timestamps:

```csharp
public class LeaseManager
{
    private readonly ConcurrentDictionary<string, LeaseEntry> _leases = new();

    public bool TryAcquire(string resource, string owner, TimeSpan duration)
    {
        var now = DateTime.UtcNow;
        var entry = new LeaseEntry(owner, now + duration);

        return _leases.AddOrUpdate(resource, entry, (key, existing) =>
            existing.ExpiresAt < now ? entry : existing
        ).Owner == owner;
    }

    public bool TryRenew(string resource, string owner, TimeSpan duration)
    {
        if (!_leases.TryGetValue(resource, out var existing))
            return false;

        if (existing.Owner != owner || existing.ExpiresAt < DateTime.UtcNow)
            return false;

        var renewed = new LeaseEntry(owner, DateTime.UtcNow + duration);
        return _leases.TryUpdate(resource, renewed, existing);
    }

    public void Release(string resource, string owner)
    {
        if (_leases.TryGetValue(resource, out var existing) && existing.Owner == owner)
            _leases.TryRemove(resource, out _);
    }
}

public record LeaseEntry(string Owner, DateTime ExpiresAt);
```

## When to Use Leases

- **Distributed job scheduling** — ensure only one worker processes a job at a time
- **Leader election** — the leader holds a lease and renews it; if it fails, another node takes over
- **Resource reservation** — hold a resource for a limited time while processing

## When Not to Use Leases

- If your system is single-process and in-memory, a simple `lock` statement is fine
- If you need transactions with rollback semantics, leases are not the right tool

## Key Takeaway

A lease is a lock that respects reality. It acknowledges that failures happen and builds recovery into the mechanism itself. In distributed systems, that is not just a convenience—it is a requirement.
