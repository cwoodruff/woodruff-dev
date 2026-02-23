---
title: "Leader Election in .NET: Picking One Boss Without Creating Two"
date: 2026-02-04
categories: 
  - "patterns"
tags: 
  - "net"
  - "c"
  - "distributed"
  - "dotnet"
  - "patterns"
  - "programming"
coverImage: "Leader-Election-in-.NET-Picking-One-Boss-Without-Creating-Two.png"
---

If your service runs on more than one node and still has a single instance assumption, you already have leader election. You just do not have it on purpose.

Leader election is the pattern that turns “somebody should run this” into “exactly one node is allowed to run this, and it must keep proving it deserves the role.”

This post walks through a lease based leader election you can implement in C# with a durable coordinator, a renewal loop, backoff, and the checks that keep your cluster from producing two leaders on a bad day.

## The real problem: accidental multi leader

Most teams meet multi leader behavior during a routine event:

- a scale out from one instance to two

- a rolling deployment

- a node pause caused by GC or a noisy neighbor

- a partition that leaves both sides alive and confident

The symptoms are predictable:

- a scheduled job runs twice

- a command handler applies the same side effect twice

- two writers push conflicting updates

- retries amplify the blast radius

When you hear “but we only run one instance of that worker,” treat it as a confession, not a design.

## Intent and mental model

Leader election selects one leader for a group and replaces it quickly when the leader disappears.

The mental model is simple:

- leadership is a lease with an expiry timestamp

- a leader must renew before expiry

- if renewal fails, the node stops acting as leader immediately

- followers keep trying to acquire the lease with backoff and jitter

A lease is not a lock you hold forever. It is a claim you must keep earning.

## The lease store contract

The coordinator can be SQL Server, Redis, etcd, or any durable store that can do an atomic acquire. The interface stays small.

```csharp
public interface ILeaseStore
{
    Task<bool> TryAcquireAsync(string key, string owner, TimeSpan ttl, CancellationToken ct);
    Task<bool> TryRenewAsync(string key, string owner, TimeSpan ttl, CancellationToken ct);
    Task<(string? Owner, DateTimeOffset ExpiresAt)> ReadAsync(string key, CancellationToken ct);
}
```

What this contract implies:

- Acquire must be exclusive for a key while the lease is valid.

- Renew must succeed only for the current owner while the lease is still valid.

- Read is for diagnostics, dashboards, and tests.

## Design choices that matter

A handful of knobs decide whether your election is calm or chaotic.

**TTL**  
Short TTL yields faster failover and more churn. Long TTL yields slower failover and fewer elections. A reasonable starting point is 10 seconds.

**Renew interval  
**Renew at one third of TTL plus small jitter. Renewing at half TTL becomes risky once you include latency spikes.

**Backoff and jitter  
**When a leader dies, every follower will try to acquire. Without backoff and jitter, they slam your coordinator and can trigger flapping.

**Coordinator outage behavior  
**A coordinator outage is a correctness event, not only an availability event. When renew fails, relinquish leadership. The alternative is two leaders writing through different network paths.

**Key scope  
**Use one key per group or shard. A single key for the whole cluster works for singleton jobs, not for partitioned workloads.

## The election loop

Each node runs the same loop:

- If not leader, attempt acquire.

- If leader, attempt renew.

- If renew fails, drop leadership and stop leader only work.

That is the whole state machine. The hard part is honoring it in every code path.

## A concrete .NET implementation

We will build three pieces:

- a lease store backed by SQL Server

- a leadership service that runs the loop

- a leader only gate for work

### SQL lease store

Schema:

```sql
CREATE TABLE dbo.Leases
(
    LeaseKey     nvarchar(200) NOT NULL PRIMARY KEY,
    OwnerId      nvarchar(200) NOT NULL,
    ExpiresAtUtc datetime2(3)  NOT NULL
);
```

Acquire and renew are conditional updates based on expiry and owner.

```csharp
using Microsoft.Data.SqlClient;

public sealed class SqlLeaseStore(string connectionString) : ILeaseStore
{
    public async Task<bool> TryAcquireAsync(string key, string owner, TimeSpan ttl, CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var now = DateTimeOffset.UtcNow;
        var expires = now.Add(ttl);

        // Acquire if missing or expired.
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
MERGE dbo.Leases WITH (HOLDLOCK) AS t
USING (SELECT @k AS LeaseKey) AS s
ON (t.LeaseKey = s.LeaseKey)
WHEN NOT MATCHED THEN
    INSERT (LeaseKey, OwnerId, ExpiresAtUtc) VALUES (@k, @o, @e)
WHEN MATCHED AND t.ExpiresAtUtc <= @now THEN
    UPDATE SET OwnerId = @o, ExpiresAtUtc = @e
OUTPUT $action;
";
        cmd.Parameters.AddWithValue("@k", key);
        cmd.Parameters.AddWithValue("@o", owner);
        cmd.Parameters.AddWithValue("@e", expires);
        cmd.Parameters.AddWithValue("@now", now);

        var action = (string?)await cmd.ExecuteScalarAsync(ct);
        return action is "INSERT" or "UPDATE";
    }

    public async Task<bool> TryRenewAsync(string key, string owner, TimeSpan ttl, CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var now = DateTimeOffset.UtcNow;
        var expires = now.Add(ttl);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
UPDATE dbo.Leases
SET ExpiresAtUtc = @e
WHERE LeaseKey = @k
  AND OwnerId = @o
  AND ExpiresAtUtc > @now;
";
        cmd.Parameters.AddWithValue("@k", key);
        cmd.Parameters.AddWithValue("@o", owner);
        cmd.Parameters.AddWithValue("@e", expires);
        cmd.Parameters.AddWithValue("@now", now);

        return await cmd.ExecuteNonQueryAsync(ct) == 1;
    }

    public async Task<(string? Owner, DateTimeOffset ExpiresAt)> ReadAsync(string key, CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT OwnerId, ExpiresAtUtc
FROM dbo.Leases
WHERE LeaseKey = @k;
";
        cmd.Parameters.AddWithValue("@k", key);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct))
            return (null, DateTimeOffset.MinValue);

        return (r.GetString(0), r.GetDateTimeOffset(1));
    }
}
```

### Leadership service with renewal loop

This service implements a tiny state machine and exposes a leader flag and current lease expiry.

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public interface ILeadership
{
    ValueTask<bool> IsLeaderAsync(CancellationToken ct);
    ValueTask<DateTimeOffset> ExpiresAtAsync(CancellationToken ct);
}

public sealed class LeaseLeadership(
    ILeaseStore store,
    ILogger<LeaseLeadership> log,
    string key,
    string owner,
    TimeSpan ttl)
    : BackgroundService, ILeadership
{
    private readonly ILeaseStore _store = store;

    private volatile bool _leader;
    private DateTimeOffset _expiresAt;

    public ValueTask<bool> IsLeaderAsync(CancellationToken ct) => ValueTask.FromResult(_leader);
    public ValueTask<DateTimeOffset> ExpiresAtAsync(CancellationToken ct) => ValueTask.FromResult(_expiresAt);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rng = Random.Shared;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_leader)
                {
                    var acquired = await _store.TryAcquireAsync(key, owner, ttl, stoppingToken);
                    if (acquired)
                    {
                        _leader = true;
                        _expiresAt = DateTimeOffset.UtcNow.Add(ttl);
                        log.LogInformation("Leader acquired for {Key} by {Owner}", key, owner);
                    }
                    else
                    {
                        _leader = false;
                    }
                }
                else
                {
                    var renewed = await _store.TryRenewAsync(key, owner, ttl, stoppingToken);
                    if (!renewed)
                    {
                        _leader = false;
                        log.LogWarning("Leader lost for {Key} by {Owner}", key, owner);
                    }
                    else
                    {
                        _expiresAt = DateTimeOffset.UtcNow.Add(ttl);
                    }
                }
            }
            catch (Exception ex)
            {
                _leader = false;
                log.LogError(ex, "Leadership loop error for {Key}", key);
            }

            var baseDelay = TimeSpan.FromMilliseconds(ttl.TotalMilliseconds / 3);
            var jitter = TimeSpan.FromMilliseconds(rng.Next(0, 250));
            await Task.Delay(baseDelay + jitter, stoppingToken);
        }
    }
}
```

This loop is opinionated: if renew fails, leadership ends. That choice prevents a coordinator outage from turning into a split brain event.

### Leader only work gate

Once you have ILeadership, guard leader only work in one place.

```csharp
public sealed class LeaderOnlyService(ILeadership leadership)
{
    private readonly ILeadership _leadership = leadership;

    public async Task RunAsync(Func<CancellationToken, Task> work, CancellationToken ct)
    {
        if (!await _leadership.IsLeaderAsync(ct)) return;
        await work(ct);
    }
}
```

Use it for outbox dispatchers, schedulers, and partition owners.

```csharp
public sealed class OutboxDispatcher(LeaderOnlyService leaderOnly)
{
    private readonly LeaderOnlyService _leaderOnly = leaderOnly;

    public Task TickAsync(CancellationToken ct) =>
        _leaderOnly.RunAsync(async innerCt =>
        {
            // Read pending outbox rows, publish, mark sent.
        }, ct);
}
```

## Observability and operational signals

Leader election is a control plane. Treat it like production code with production signals.

Metrics:

- leader status per key

- renew failures per minute

- election wins per hour

- time until expiry for the current lease

- coordinator latency

Alerts:

- rapid flapping on a key

- renew failures across all nodes

- repeated acquire failures with high coordinator latency

Logs worth keeping:

- key, owner, action (acquire, renew, lose), and latency

## Testing plan

Integration tests that pay for themselves:

- Start two nodes, assert only one becomes leader.

- Stop the leader, wait for TTL, assert the follower becomes leader.

- Introduce renewal failures by forcing command timeouts, assert the leader steps down.

- Pause the leader process long enough to miss renew, assert takeover happens after expiry.

A good property to assert:  
No two nodes report leadership for the same key while the lease is valid.

## Common mistakes

- Calling it leader election while using an in memory lock

- Doing long leader work that keeps running after leadership loss

- Skipping jitter and creating synchronized acquire storms

- Choosing TTL without measuring coordinator latency

- Treating coordinator failure as permission to keep acting like leader

## Wrap up

Leader election is a correctness feature. It exists to prevent your system from producing competing decisions. Use a lease, renew it often, relinquish on failure, and test the takeover path until it is boring.

Next up is Lease and then Fencing Token. Election decides who is leader. Fencing decides whether an old leader can still write after it loses the role.
