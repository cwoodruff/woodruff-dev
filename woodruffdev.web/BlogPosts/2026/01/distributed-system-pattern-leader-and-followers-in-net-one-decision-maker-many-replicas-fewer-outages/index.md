---
title: "Distributed System Pattern: Leader and Followers in .NET - One Decision Maker, Many Replicas, Fewer Outages"
date: 2026-01-29
categories: 
  - "patterns"
tags: 
  - "net"
  - "c"
  - "distributed"
  - "dotnet"
  - "patterns"
  - "programming"
coverImage: "Distributed-System-Pattern-Leader-and-Followers-in-.NET-One-Decision-Maker-Many-Replicas-Fewer-Outages.png"
---

Distributed systems rarely fail because you picked the wrong cloud service. They fail because two nodes believe they are in charge, both act, and both are “correct” from their own perspective. If your domain has any single authority assumption, and most systems do, you need a way to make that authority real.

Leader and Followers is the pattern that turns vague ownership into a concrete contract.

One node is the decision maker for a shard, group, or partition. The rest replicate the leader’s decisions and keep the system available when nodes fail. That is the bargain. The cost is that you must implement leadership as a first-class concept, not an incidental side effect of “whichever instance got there first.”

This post shows a pragmatic .NET implementation using a renewable lease plus a leadership term. You will also see how to fence writes so an old leader cannot corrupt your data after it loses leadership.

## The Pattern in One Sentence

Choose one leader per group to serialize decisions, replicate the resulting state to followers, and make leadership transferable without letting two nodes write as leader at the same time.

## The Failure You Are Trying to Stop

If you have ever seen any of these, you have already paid for this pattern, just in a worse way:

- Two schedulers run the same job and you double charge a customer.

- Two instances process the same command and your inventory goes negative.

- A node stalls for 30 seconds, then resumes and continues writing as if nothing happened.

- You “fixed” it with retries and now the bug happens faster.

Leader and Followers prevents these by making leadership explicit and enforceable.

## Implementation Goals

A solid implementation needs four properties:

1. Exclusive leadership per group with a bounded time window.

3. Fast detection of leadership loss.

5. A monotonic term that changes on each leadership transition.

7. Fencing so stale leaders are rejected at the storage boundary.

The lease gives you the bounded time window. The term and fencing prevent the stale leader problem.

## Core Interface and the Leader Only Boundary

Start by keeping your public surface area honest. Code that must run only on the leader should not be callable without an explicit leadership check.

```csharp
public interface ILeadership
{
    ValueTask<bool> IsLeaderAsync(CancellationToken ct);
    ValueTask<long> TermAsync(CancellationToken ct);
}
```

Now wrap leader-only work behind a small, boring gate:

```csharp
public sealed class LeaderOnlyService(ILeadership leadership)
{
    private readonly ILeadership _leadership = leadership;

    public async Task RunLeaderTaskAsync(Func<CancellationToken, Task> work, CancellationToken ct)
    {
        if (!await _leadership.IsLeaderAsync(ct)) return;
        await work(ct);
    }
}
```

This does not solve leadership by itself. It prevents “accidental leadership” from leaking everywhere.

## Step 1: Lease Backed Leadership in SQL Server

You can implement leases using Redis, etcd, ZooKeeper, or a database. For many .NET teams, SQL Server is already present and operationally understood. A SQL lease is not glamorous, but it is effective.

Create a table that stores the current lease owner, expiry, and term.

```sql
CREATE TABLE dbo.LeadershipLeases
(
    LeaseKey       nvarchar(200) NOT NULL PRIMARY KEY,
    OwnerId        nvarchar(200) NOT NULL,
    Term           bigint        NOT NULL,
    ExpiresAtUtc   datetime2(3)  NOT NULL
);

CREATE INDEX IX_LeadershipLeases_ExpiresAtUtc ON dbo.LeadershipLeases (ExpiresAtUtc);
```

Define the lease rules:

- A node can acquire leadership if the lease is expired or already owned by itself.

- Each successful acquisition increments the term.

- Renew extends ExpiresAtUtc only if the owner matches and the lease has not expired.

### Lease Store Contract

```csharp
public interface ILeaseStore
{
    Task<(bool Acquired, long Term, DateTimeOffset ExpiresAt)> TryAcquireAsync(
        string leaseKey,
        string ownerId,
        TimeSpan ttl,
        CancellationToken ct);

    Task<(bool Renewed, DateTimeOffset ExpiresAt)> TryRenewAsync(
        string leaseKey,
        string ownerId,
        TimeSpan ttl,
        CancellationToken ct);

    Task<(string OwnerId, long Term, DateTimeOffset ExpiresAt)?> ReadAsync(
        string leaseKey,
        CancellationToken ct);
}
```

### SQL Implementation

This implementation uses a transaction and locking hints to keep acquisition atomic.

```csharp
using System.Data;
using Microsoft.Data.SqlClient;

public sealed class SqlLeaseStore(string connectionString) : ILeaseStore
{
    public async Task<(bool Acquired, long Term, DateTimeOffset ExpiresAt)> TryAcquireAsync(
        string leaseKey,
        string ownerId,
        TimeSpan ttl,
        CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var now = DateTimeOffset.UtcNow;
        var expires = now.Add(ttl);

        // Lock the row for the lease key.
        await using var select = conn.CreateCommand();
        select.Transaction = tx;
        select.CommandText = @"
SELECT LeaseKey, OwnerId, Term, ExpiresAtUtc
FROM dbo.LeadershipLeases WITH (UPDLOCK, HOLDLOCK)
WHERE LeaseKey = @k;
";
        select.Parameters.AddWithValue("@k", leaseKey);

        string? currentOwner = null;
        long currentTerm = 0;
        DateTimeOffset currentExpires = DateTimeOffset.MinValue;

        await using (var reader = await select.ExecuteReaderAsync(ct))
        {
            if (await reader.ReadAsync(ct))
            {
                currentOwner = reader.GetString(1);
                currentTerm = reader.GetInt64(2);
                currentExpires = reader.GetDateTimeOffset(3);
            }
        }

        var isExpired = currentOwner is null || currentExpires <= now;
        var isReentrant = currentOwner == ownerId;

        if (currentOwner is null)
        {
            var insertTerm = 1L;
            await using var insert = conn.CreateCommand();
            insert.Transaction = tx;
            insert.CommandText = @"
INSERT INTO dbo.LeadershipLeases(LeaseKey, OwnerId, Term, ExpiresAtUtc)
VALUES (@k, @o, @t, @e);
";
            insert.Parameters.AddWithValue("@k", leaseKey);
            insert.Parameters.AddWithValue("@o", ownerId);
            insert.Parameters.AddWithValue("@t", insertTerm);
            insert.Parameters.AddWithValue("@e", expires);

            await insert.ExecuteNonQueryAsync(ct);
            await tx.CommitAsync(ct);
            return (true, insertTerm, expires);
        }

        if (!isExpired && !isReentrant)
        {
            await tx.RollbackAsync(ct);
            return (false, currentTerm, currentExpires);
        }

        // Acquire by updating owner, bumping term, setting new expiry.
        var newTerm = isReentrant ? currentTerm : currentTerm + 1;

        await using var update = conn.CreateCommand();
        update.Transaction = tx;
        update.CommandText = @"
UPDATE dbo.LeadershipLeases
SET OwnerId = @o,
    Term = @t,
    ExpiresAtUtc = @e
WHERE LeaseKey = @k;
";
        update.Parameters.AddWithValue("@k", leaseKey);
        update.Parameters.AddWithValue("@o", ownerId);
        update.Parameters.AddWithValue("@t", newTerm);
        update.Parameters.AddWithValue("@e", expires);

        await update.ExecuteNonQueryAsync(ct);
        await tx.CommitAsync(ct);

        return (true, newTerm, expires);
    }

    public async Task<(bool Renewed, DateTimeOffset ExpiresAt)> TryRenewAsync(
        string leaseKey,
        string ownerId,
        TimeSpan ttl,
        CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var now = DateTimeOffset.UtcNow;
        var expires = now.Add(ttl);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
UPDATE dbo.LeadershipLeases
SET ExpiresAtUtc = @e
WHERE LeaseKey = @k
  AND OwnerId = @o
  AND ExpiresAtUtc > @now;
";
        cmd.Parameters.AddWithValue("@k", leaseKey);
        cmd.Parameters.AddWithValue("@o", ownerId);
        cmd.Parameters.AddWithValue("@e", expires);
        cmd.Parameters.AddWithValue("@now", now);

        var rows = await cmd.ExecuteNonQueryAsync(ct);
        return (rows == 1, expires);
    }

    public async Task<(string OwnerId, long Term, DateTimeOffset ExpiresAt)?> ReadAsync(string leaseKey, CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT OwnerId, Term, ExpiresAtUtc
FROM dbo.LeadershipLeases
WHERE LeaseKey = @k;
";
        cmd.Parameters.AddWithValue("@k", leaseKey);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return (reader.GetString(0), reader.GetInt64(1), reader.GetDateTimeOffset(2));
    }
}
```

## Step 2: A Leadership Coordinator That Renews the Lease

Leadership needs a background loop that acquires and renews the lease and exposes the current status to the rest of the process.

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed class LeaseLeadership : BackgroundService, ILeadership
{
    private readonly ILeaseStore _store;
    private readonly ILogger<LeaseLeadership> _log;
    private readonly string _leaseKey;
    private readonly string _ownerId;

    private readonly TimeSpan _ttl;
    private readonly TimeSpan _renewEvery;

    private volatile bool _isLeader;
    private volatile long _term;

    public LeaseLeadership(
        ILeaseStore store,
        ILogger<LeaseLeadership> log,
        string leaseKey,
        string ownerId,
        TimeSpan? ttl = null)
    {
        _store = store;
        _log = log;
        _leaseKey = leaseKey;
        _ownerId = ownerId;

        _ttl = ttl ?? TimeSpan.FromSeconds(10);
        _renewEvery = TimeSpan.FromMilliseconds(_ttl.TotalMilliseconds / 3);
    }

    public ValueTask<bool> IsLeaderAsync(CancellationToken ct) => ValueTask.FromResult(_isLeader);
    public ValueTask<long> TermAsync(CancellationToken ct) => ValueTask.FromResult(_term);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_isLeader)
                {
                    var acquired = await _store.TryAcquireAsync(_leaseKey, _ownerId, _ttl, stoppingToken);
                    _isLeader = acquired.Acquired;
                    _term = acquired.Term;

                    if (_isLeader)
                        _log.LogInformation("Became leader for {LeaseKey} with term {Term}", _leaseKey, _term);
                }
                else
                {
                    var renewed = await _store.TryRenewAsync(_leaseKey, _ownerId, _ttl, stoppingToken);

                    if (!renewed.Renewed)
                    {
                        _isLeader = false;
                        _log.LogWarning("Lost leadership for {LeaseKey}", _leaseKey);
                    }
                }
            }
            catch (Exception ex)
            {
                _isLeader = false;
                _log.LogError(ex, "Leadership loop error for {LeaseKey}", _leaseKey);
            }

            await Task.Delay(_renewEvery, stoppingToken);
        }
    }
}
```

Important behavior: if renewal fails, the node stops acting as a leader immediately. That is non-negotiable.

## Step 3: Fencing Writes With the Term

A renewable lease is necessary but not sufficient. A node can pause, GC can stall, networking can get weird, and you can end up with a stale leader still connected to your database. The lease can expire and a new leader can be elected while the old leader remains alive and unaware.

The fix is fencing: every leader term is a token. Every write from the leader includes it. Storage rejects writes with a stale term.

### Example: Fenced Stream Writes in SQL Server

Create a table for writes that includes the term, and enforce monotonic terms per stream or per aggregate.

```sql
CREATE TABLE dbo.StreamWrites
(
    StreamId     nvarchar(200) NOT NULL,
    SequenceNo   bigint        NOT NULL,
    Term         bigint        NOT NULL,
    Payload      varbinary(max) NOT NULL,
    CreatedAtUtc datetime2(3)  NOT NULL,
    CONSTRAINT PK_StreamWrites PRIMARY KEY(StreamId, SequenceNo)
);

CREATE INDEX IX_StreamWrites_StreamId_Term ON dbo.StreamWrites(StreamId, Term);
```

Now a fenced writer that rejects stale terms:

```csharp
using Microsoft.Data.SqlClient;

public sealed class FencedStreamWriter(string connectionString)
{
    public async Task AppendAsync(string streamId, long sequenceNo, long term, byte[] payload, CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);

        // Reject if the stream has any write with a greater term.
        await using var check = conn.CreateCommand();
        check.Transaction = tx;
        check.CommandText = @"
DECLARE @maxTerm BIGINT =
(
    SELECT ISNULL(MAX(Term), 0)
    FROM dbo.StreamWrites WITH (UPDLOCK, HOLDLOCK)
    WHERE StreamId = @s
);

IF (@maxTerm > @t)
    THROW 50001, 'Stale leader term', 1;
";
        check.Parameters.AddWithValue("@s", streamId);
        check.Parameters.AddWithValue("@t", term);
        await check.ExecuteNonQueryAsync(ct);

        await using var insert = conn.CreateCommand();
        insert.Transaction = tx;
        insert.CommandText = @"
INSERT INTO dbo.StreamWrites(StreamId, SequenceNo, Term, Payload, CreatedAtUtc)
VALUES (@s, @n, @t, @p, SYSUTCDATETIME());
";
        insert.Parameters.AddWithValue("@s", streamId);
        insert.Parameters.AddWithValue("@n", sequenceNo);
        insert.Parameters.AddWithValue("@t", term);
        insert.Parameters.AddWithValue("@p", payload);

        await insert.ExecuteNonQueryAsync(ct);
        await tx.CommitAsync(ct);
    }
}
```

This is the storage level refusal that prevents corruption. If you do not fence, you are trusting timing and luck.

## Putting It Together: A Leader Only Job Runner

Here is a practical use case: one scheduled job should run per cluster, and it must not run twice.

```csharp
public sealed class BillingReconciler(LeaderOnlyService leaderOnly, ILeadership leadership, FencedStreamWriter writer)
{
    private readonly LeaderOnlyService _leaderOnly = leaderOnly;
    private readonly ILeadership _leadership = leadership;
    private readonly FencedStreamWriter _writer = writer;

    public Task RunAsync(CancellationToken ct) =>
        _leaderOnly.RunLeaderTaskAsync(async innerCt =>
        {
            var term = await _leadership.TermAsync(innerCt);

            // Example decision: emit a reconciliation command into a stream
            var streamId = "billing-reconcile";
            var nextSeq = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); // placeholder, use real sequencing in production
            var payload = System.Text.Encoding.UTF8.GetBytes($"reconcile:{DateTimeOffset.UtcNow:O}");

            await _writer.AppendAsync(streamId, nextSeq, term, payload, innerCt);
        }, ct);
}
```

If a stale leader tries to write, the fenced writer throws, and the damage stops there.

## Replicating State to Followers

Leader and Followers does not require a specific replication mechanism. It requires that the leader’s decisions can be observed and applied by followers.

Here are three pragmatic replication models in .NET, in increasing sophistication:

### Model A: Shared durable state, followers read it

Leader writes to a database. Followers serve reads from the same database or compute projections from it. Replication is “free” because the database is the replication point.

This is often the right starting point. It is also a reminder: your database is part of the distributed system, whether you want it to be or not.

### Model B: Leader appends to a log, followers tail the log

Leader writes decisions into an append only log table. Followers poll for new rows and apply them to a local projection.

```csharp
public sealed class FollowerApplier(string connectionString) : BackgroundService
{
    private long _lastSeq;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var batch = await ReadNextBatchAsync(_lastSeq, stoppingToken);

            foreach (var row in batch)
            {
                Apply(row);
                _lastSeq = row.SequenceNo;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), stoppingToken);
        }
    }

    private async Task<IReadOnlyList<(string StreamId, long SequenceNo, long Term, byte[] Payload)>> ReadNextBatchAsync(
        long afterSeq,
        CancellationToken ct)
    {
        var results = new List<(string, long, long, byte[])>();

        await using var conn = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT TOP (100) StreamId, SequenceNo, Term, Payload
FROM dbo.StreamWrites
WHERE SequenceNo > @n
ORDER BY SequenceNo;
";
        cmd.Parameters.AddWithValue("@n", afterSeq);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add((
                reader.GetString(0),
                reader.GetInt64(1),
                reader.GetInt64(2),
                (byte[])reader["Payload"]
            ));
        }

        return results;
    }

    private static void Apply((string StreamId, long SequenceNo, long Term, byte[] Payload) row)
    {
        // Apply to a local projection, cache, or in-memory state machine.
        // Keep it deterministic and idempotent.
    }
}
```

### Model C: Leader publishes events, followers subscribe

This is the same idea as Model B but moved to a message broker. It works when ordering per partition is guaranteed and consumers track offsets.

If you already use Azure Service Bus, Kafka, or similar, this model can scale well.

## Reads and Correctness

Followers can be used for reads, but only if you are honest about staleness.

Common approach:

- Writes go to the leader.

- Reads go to followers if their replication lag is within a threshold.

- Otherwise, route reads to the leader for correctness.

This is the point where teams get sloppy. “Eventually consistent” is not a license to guess.

## Testing: Prove You Do Not Get Two Leaders

You can integration test this without heroic infrastructure.

1. Start two instances with the same lease key and different owner ids.

3. Verify only one becomes leader.

5. Pause the leader process long enough to miss renewals.

7. Verify the follower becomes leader with a higher term.

9. Resume the old leader and attempt a fenced write.

11. Verify the write is rejected as stale.

If your tests do not include stale leader rejection, you do not have a safety story.

## Operational Checklist

Metrics you want on a dashboard:

- IsLeader boolean per instance

- Current term per instance

- Lease renew latency and failures

- Leadership flaps per hour

- Fenced write rejections per hour

- Follower lag if you implement tailing replication

Alerts worth paging on:

- Leadership flapping

- Lease renew failures across many nodes

- Any sustained rate of stale leader write rejections

Those rejections are not “noise.” They are proof that you avoided corruption today.

## Closing

Leader and Followers is not a fancy pattern. It is a statement of responsibility. One node decides. Everyone else follows. Leadership changes are explicit, bounded, and fenced. If you build a system that assumes a single decision maker but never implements one, you are not simplifying the architecture. You are outsourcing correctness to chance.

If you want to extend this post next, the natural follow on is Fencing Tokens and Generation Clock as a deeper treatment of terms, plus a replication chapter that builds a small replicated log in .NET with a committed index and follower acknowledgements.
