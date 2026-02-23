---
title: "Lease Pattern in .NET: A Lock With an Expiration Date That Saves Your Data"
date: 2026-02-07
categories: 
  - "patterns"
tags: 
  - "net"
  - "c"
  - "distributed"
  - "dotnet"
  - "patterns"
  - "programming"
coverImage: "Lease-Pattern-in-.NET-A-Lock-With-an-Expiration-Date-That-Saves-Your-Data.png"
---

Indefinite locks belong to a world where processes never crash and networks never split. That world does not exist. In a distributed system, “I hold the lock” can mean “I held the lock before my VM paused for 45 seconds.” A lease fixes that by putting a deadline on ownership and forcing the owner to keep renewing that claim.

A lease is a lock with a time limit. When the clock runs out, other nodes may take over. That single detail prevents stuck work after crashes and limits the damage during partitions.

This post shows a production shaped lease using Redis with atomic acquire, atomic renew, and a rule that most teams avoid until it hurts: when renewal fails, you stop acting like the owner.

## The problem with indefinite locks

Classic locking assumes three things that distributed systems break daily.

- The lock holder can always release the lock.

- Everyone can observe the release.

- Time does not jump.

None of those are reliable. Processes crash and never execute finally blocks. Networks drop packets and hide releases. Pause events stretch “a second” into “a minute.” If your system depends on an indefinite lock, it will either freeze forever or run twice.

A lease does not make failure disappear. It makes failure survivable.

## Pattern definition and intent

A lease is exclusive ownership that expires without renewal.

Intent:  
Provide exclusive access with a bounded duration so the system can recover when an owner crashes or becomes unreachable.

Key property:  
Ownership is valid only until ExpiresAt, not until the owner feels like releasing it.

## Core semantics and hard rules

A lease implementation needs three invariants.

Acquire is atomic and exclusive.  
Renew is atomic and only the current owner can extend the deadline.  
Loss of renew ends ownership immediately.

That last point is the hard rule. If you cannot renew, you are not the owner. Continuing to act after renewal failure is how “singleton” jobs become duplicate work.

## Design choices that matter

### TTL selection

TTL is a trade. Short TTL gives faster recovery and more churn. Long TTL gives slower recovery and less churn. A common starting point is 10 seconds, then adjust after measuring Redis latency, GC pauses, and deployment behavior.

### Renew cadence

Renew earlier than TTL. Many teams renew at one third of TTL with jitter. The jitter avoids every node renewing on the same millisecond, which can create coordinator spikes and election flapping.

### Time model

Let Redis own the timer. Redis TTL is enforced server side. Do not build correctness on local clocks.

### Ownership token

Use a unique token per process instance, stored as the lease value. Renew and release must verify the token. Without this, one node can renew a lease it does not own.

## Redis as the lease store

Redis provides the primitives you need.

Acquire:  
SET key token NX PX ttlMillis

Renew:  
Atomic compare token then extend TTL, implemented with Lua

Release:  
Atomic compare token then delete, implemented with Lua

### Acquire

```csharp
using StackExchange.Redis;

public sealed class RedisLeaseStore
{
    private readonly IDatabase _db;

    public RedisLeaseStore(IConnectionMultiplexer mux) => _db = mux.GetDatabase();

    public async Task<bool> TryAcquireAsync(string key, string token, TimeSpan ttl, CancellationToken ct)
    {
        // StackExchange.Redis does not accept CancellationToken on all calls, so keep the operation small.
        return await _db.StringSetAsync(
            key: key,
            value: token,
            expiry: ttl,
            when: When.NotExists);
    }
}
```

### Renew with Lua

Renew must be compare and extend in one operation. A read then write is not safe because another node could acquire between those steps.

Lua script:

- If the stored value matches the token, extend TTL and return 1

- Else return 0

```csharp
public sealed partial class RedisLeaseStore
{
    private static readonly LuaScript RenewScript = LuaScript.Prepare(@"
if redis.call('GET', KEYS[1]) == ARGV[1] then
  return redis.call('PEXPIRE', KEYS[1], ARGV[2])
else
  return 0
end
");

    public async Task<bool> TryRenewAsync(string key, string token, TimeSpan ttl, CancellationToken ct)
    {
        var result = (long)await _db.ScriptEvaluateAsync(
            RenewScript,
            new RedisKey[] { key },
            new RedisValue[] { token, (long)ttl.TotalMilliseconds });

        return result == 1;
    }
}
```

### Release with Lua

Release is optional for safety, but useful for fast handoff during graceful shutdown. It must be token checked to avoid deleting another node’s lease.

```csharp
public sealed partial class RedisLeaseStore
{
    private static readonly LuaScript ReleaseScript = LuaScript.Prepare(@"
if redis.call('GET', KEYS[1]) == ARGV[1] then
  return redis.call('DEL', KEYS[1])
else
  return 0
end
");

    public async Task<bool> ReleaseAsync(string key, string token, CancellationToken ct)
    {
        var result = (long)await _db.ScriptEvaluateAsync(
            ReleaseScript,
            new RedisKey[] { key },
            new RedisValue[] { token });

        return result == 1;
    }
}
```

## A lease abstraction you can use everywhere

Wrap the store behind a small interface. Keep it focused on behavior, not plumbing.

```csharp
public interface ILease
{
    string Key { get; }
    string Token { get; }
    TimeSpan Ttl { get; }

    Task<bool> AcquireAsync(CancellationToken ct);
    Task<bool> RenewAsync(CancellationToken ct);
    Task ReleaseAsync(CancellationToken ct);
}
```

Implementation that uses the Redis store:

```csharp
public sealed class RedisLease(RedisLeaseStore store, string key, TimeSpan ttl, string? token = null)
    : ILease
{
    public string Key { get; } = key;
    public string Token { get; } = token ?? Guid.NewGuid().ToString("N");
    public TimeSpan Ttl { get; } = ttl;

    public Task<bool> AcquireAsync(CancellationToken ct) => store.TryAcquireAsync(Key, Token, Ttl, ct);
    public Task<bool> RenewAsync(CancellationToken ct) => store.TryRenewAsync(Key, Token, Ttl, ct);

    public async Task ReleaseAsync(CancellationToken ct)
    {
        await store.ReleaseAsync(Key, Token, ct);
    }
}
```

## Using the lease for leader only work

A lease does nothing until you build your control flow around it. The safest approach is a background loop that:

- tries to acquire

- renews on a schedule

- cancels leader work immediately when renew fails

### Leader work gate

```csharp
public sealed class LeaderOnlyService
{
    private volatile bool _isLeader;

    public bool IsLeader => _isLeader;

    public async Task RunIfLeaderAsync(Func<CancellationToken, Task> work, CancellationToken ct)
    {
        if (!_isLeader) return;
        await work(ct);
    }

    internal void SetLeader(bool value) => _isLeader = value;
}
```

### Lease driven leader loop

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed class LeaseLeaderLoop : BackgroundService
{
    private readonly ILease _lease;
    private readonly LeaderOnlyService _leaderOnly;
    private readonly ILogger<LeaseLeaderLoop> _log;
    private readonly TimeSpan _renewEvery;
    private readonly Random _rng = new();

    public LeaseLeaderLoop(ILease lease, LeaderOnlyService leaderOnly, ILogger<LeaseLeaderLoop> log)
    {
        _lease = lease;
        _leaderOnly = leaderOnly;
        _log = log;
        _renewEvery = TimeSpan.FromMilliseconds(_lease.Ttl.TotalMilliseconds / 3);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_leaderOnly.IsLeader)
                {
                    var acquired = await _lease.AcquireAsync(stoppingToken);
                    if (acquired)
                    {
                        _leaderOnly.SetLeader(true);
                        _log.LogInformation("Lease acquired for {Key}", _lease.Key);
                    }
                }
                else
                {
                    var renewed = await _lease.RenewAsync(stoppingToken);
                    if (!renewed)
                    {
                        _leaderOnly.SetLeader(false);
                        _log.LogWarning("Lease lost for {Key}", _lease.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                _leaderOnly.SetLeader(false);
                _log.LogError(ex, "Lease loop error for {Key}", _lease.Key);
            }

            var jitterMs = _rng.Next(0, 250);
            await Task.Delay(_renewEvery + TimeSpan.FromMilliseconds(jitterMs), stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _leaderOnly.SetLeader(false);
        await _lease.ReleaseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
```

### Example: singleton outbox dispatcher

```csharp
public sealed class OutboxDispatcher(LeaderOnlyService leaderOnly)
{
    public Task TickAsync(CancellationToken ct) =>
        leaderOnly.RunIfLeaderAsync(async innerCt =>
        {
            // Fetch pending messages, publish them, mark sent.
            await Task.CompletedTask;
        }, ct);
}
```

Leader loss is immediate because RunIfLeaderAsync checks a volatile flag. Your leader work still needs to be cancellable and to check cancellation regularly.

## Failure stories the lease prevents

### Crash and stuck lock

With an indefinite lock, a crash can freeze the work forever. With a lease, the lease expires and another node can acquire it.

### Pause and stale owner

A node can pause longer than TTL. When it wakes, it might still believe it owns the work. With proper renew checks, it fails renew and stops. That avoids duplicate work. If the paused node keeps writing anyway, you need fencing tokens at the write boundary. Leases reduce risk, they do not block every stale write by themselves.

### Partition

A partition can isolate the leader from Redis. Renew fails. The node relinquishes leadership. Another node on the healthy side acquires the lease after expiry. If the isolated node keeps acting, that is a code bug. The lease pattern gives you a clear rule to enforce.

## Testing strategy

Integration tests against Redis are worth the time.

Tests to include:

- only one node can acquire the lease at a time

- renew succeeds for the token holder

- renew fails after expiry

- release deletes only when token matches

- simulated pause longer than TTL results in lease loss

Sketch of a basic acquisition test:

```csharp
public async Task OnlyOneOwnerGetsTheLease()
{
    var store = new RedisLeaseStore(ConnectionMultiplexer.Connect("localhost:6379"));
    var ttl = TimeSpan.FromSeconds(5);

    var a = new RedisLease(store, "lease:group-a", ttl, token: "A");
    var b = new RedisLease(store, "lease:group-a", ttl, token: "B");

    var gotA = await a.AcquireAsync(CancellationToken.None);
    var gotB = await b.AcquireAsync(CancellationToken.None);

    if (gotA == gotB) throw new Exception("Expected exclusive acquisition");
}
```

Add a pause test by acquiring, waiting past TTL without renewing, then acquiring from another lease.

## Operational checklist

Metrics:

- acquire success rate

- renew success rate

- lease loss events

- contention rate per key

- Redis latency for SET and EVAL

Alerts:

- repeated lease loss for the same key

- frequent leader changes for a key

- renew failures correlated with Redis latency spikes

Logs:

- key, token prefix, acquire vs renew, latency, and exception details

## Common mistakes

- using a lock library without understanding token checks

- renewing without compare and extend

- treating release as the safety mechanism

- ignoring renew failures and continuing work

- TTL set without measuring coordinator latency

- assuming leases prevent stale writes without fencing

## Wrap up

A lease is a lock with a deadline. It is safer because it admits reality: owners crash and networks split. Implement atomic acquire, atomic renew, and token checked release. Then enforce the hard rule. If you cannot renew, you stop acting like the owner.

Next up is fencing tokens. A lease determines who should lead. Fencing determines who is allowed to write.
