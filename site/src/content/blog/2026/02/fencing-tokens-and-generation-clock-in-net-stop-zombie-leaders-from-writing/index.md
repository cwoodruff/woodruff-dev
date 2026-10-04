---
title: "Fencing Tokens and Generation Clock in .NET: Stop Zombie Leaders From Writing"
date: 2026-02-27
description: "Leader election and leases answer a comforting question: who should be in charge right now. They do not fully answer the dangerous question: who can still write right now. A node can lose its lease…"
categories:
  - "patterns"
tags:
  - "net"
  - "c"
  - "distributed"
  - "dotnet"
  - "patterns"
  - "programming"
coverImage: "./images/fencing-tokens-and-generation-clock-in-net-stop-zombie-leaders-from-writing.png"
---
Leader election and leases answer a comforting question: who should be in charge right now. They do not fully answer the dangerous question: who can still write right now.

A node can lose its lease, another node can become leader, and the old leader can still push writes through an existing database connection. When that happens, your system is not highly available. It is producing competing truths.

Fencing tokens exist to end that story. Every leadership term gets a monotonically increasing number. Every leader write carries that number. The storage layer rejects stale numbers. The check sits at the boundary where corruption would otherwise enter.

## The Failure That Leases Do Not Prevent

Picture this sequence.

1.  Node A acquires the lease for `group-a` and starts writing to SQL Server.
2.  Node A hits a long GC pause or a network stall. Its lease renewal stops.
3.  The lease expires. Node B acquires the lease and becomes the new leader.
4.  Node A resumes. Its SQL connection never died. It keeps writing as if nothing changed.

Both nodes are alive. Both can reach the database. Without fencing, the database will accept both writers.

If you have ever wondered how a “singleton” job ran twice with a lease in place, this is the missing piece.

## Pattern Definition and Intent

A **fencing token** is a monotonically increasing value associated with leadership. A **generation clock** is the mechanism that produces those increasing values for a key, group, or shard.

**Intent:** make it so only the current leader can perform writes, even when older leaders are still running.

**Rule:** every write includes the token, and storage rejects writes with a smaller token than what it has already accepted.

This is not a polite convention between services. This is an enforced guardrail at the write boundary.

## Mental Model

Split responsibility into two contracts.

-   The lease decides who is expected to lead.
-   The fencing token decides who is permitted to write.

Leases provide liveness. Fencing provides safety. You need both when correctness matters.

## What You Need to Build

You need four things.

1.  A token generator tied to leadership terms.
2.  Token propagation through every leader write path.
3.  A storage check that rejects stale tokens.
4.  Telemetry for rejected writes so you can spot instability fast.

Where the token goes depends on your architecture.

-   HTTP request headers for synchronous APIs
-   message metadata for asynchronous commands
-   database commands for direct writes
-   log append requests for event streams

The goal stays the same: the write boundary sees the token and enforces monotonic progress.

## Generation Clock Options

A generation clock must produce a number that only goes up for a given scope. Common options follow.

### Store the Term With the Lease Record

When a leader takes over, increment the term stored alongside the lease. The lease and term move together.

Good fit when you already have a durable coordinator.

### Dedicated Epoch Store With Atomic Increment

Maintain an `epoch:{group}` value and bump it atomically on takeover.

Good fit when you want to separate leadership liveness from write authority.

### Database Sequence per Group

Use SQL Server to issue terms, either with a sequence object or a per-group row you update atomically.

Good fit when SQL Server is the source of truth and you want the check and the clock in one system.

### Redis INCR per Key

`INCR epoch:group-a` returns a monotonic value.

Good fit when Redis is already your coordinator and you want a simple clock.

No matter which clock you pick, one constraint stays: the term must come from an atomic operation, not from local memory.

## C# Model Types and Interfaces

Keep the token explicit and hard to ignore.

```csharp
public sealed record FencingToken(long Value);

public interface IFencedWriter
{
    Task AppendAsync(FencingToken token, string stream, ReadOnlyMemory<byte> payload, CancellationToken ct);
}
```

You also need a source for the current token that leader code can ask for.

```csharp
public interface IFencingTokenSource
{
    ValueTask<FencingToken> CurrentAsync(CancellationToken ct);
}
```

## A SQL Server Fenced Writer That Rejects Stale Leaders

Below is a fenced writer that appends to a stream table. The table stores the highest token it has accepted. Any write carrying a smaller token is rejected.

Schema example:

```csharp
CREATE TABLE dbo.StreamWrites
(
    StreamId nvarchar(200) NOT NULL,
    Token    bigint        NOT NULL,
    Data     varbinary(max) NOT NULL,
    CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_StreamWrites_CreatedAtUtc DEFAULT SYSUTCDATETIME()
);

CREATE INDEX IX_StreamWrites_StreamId_Token ON dbo.StreamWrites(StreamId, Token);
```

Writer implementation:

```csharp
using Microsoft.Data.SqlClient;

public sealed record FencingToken(long Value);

public interface IFencedWriter
{
    Task AppendAsync(FencingToken token, string stream, ReadOnlyMemory<byte> payload, CancellationToken ct);
}

public sealed class SqlFencedWriter : IFencedWriter
{
    private readonly SqlConnection _conn;

    public SqlFencedWriter(SqlConnection conn) => _conn = conn;

    public async Task AppendAsync(FencingToken token, string stream, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
DECLARE @current BIGINT = (SELECT ISNULL(MAX(Token), 0)
                          FROM dbo.StreamWrites WITH (UPDLOCK, HOLDLOCK)
                          WHERE StreamId = @stream);
IF (@current > @token) THROW 50001, 'Stale fencing token', 1;

INSERT INTO dbo.StreamWrites(StreamId, Token, Data)
VALUES (@stream, @token, @data);
";
        cmd.Parameters.AddWithValue("@stream", stream);
        cmd.Parameters.AddWithValue("@token", token.Value);
        cmd.Parameters.AddWithValue("@data", payload.ToArray());
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
```

Why the locking hints matter:

-   `UPDLOCK` and `HOLDLOCK` serialize writers for the same stream.
-   The max-token check and the insert execute as one critical section per stream.
-   A stale leader gets a hard failure at the database boundary.

This is the moment where correctness becomes enforceable, not aspirational.

## Integrating With Leader Election and Leases

The integration pattern is straightforward:

1.  Acquire the lease.
2.  Obtain a new fencing token for this leadership term.
3.  Store that token in memory for leader-only components.
4.  Include the token on every write.
5.  If lease renewal fails, stop leader work and discard the token.

A tiny token source can hold the current token.

```csharp
public sealed class InMemoryFencingTokenSource : IFencingTokenSource
{
    private long _value;

    public void Set(FencingToken token) => Interlocked.Exchange(ref _value, token.Value);

    public ValueTask<FencingToken> CurrentAsync(CancellationToken ct) =>
        ValueTask.FromResult(new FencingToken(Interlocked.Read(ref _value)));
}
```

Your leadership loop sets the token after takeover and clears it on leadership loss.

```csharp
public interface IEpochStore
{
    Task<long> NextAsync(string key, CancellationToken ct);
}

public sealed class LeaderTermCoordinator
{
    private readonly IEpochStore _epoch;
    private readonly InMemoryFencingTokenSource _tokens;

    public LeaderTermCoordinator(IEpochStore epoch, InMemoryFencingTokenSource tokens)
    {
        _epoch = epoch;
        _tokens = tokens;
    }

    public async Task OnBecameLeaderAsync(string groupKey, CancellationToken ct)
    {
        var next = await _epoch.NextAsync($"epoch:{groupKey}", ct);
        _tokens.Set(new FencingToken(next));
    }

    public void OnLostLeadership()
    {
        _tokens.Set(new FencingToken(0));
    }
}
```

Leader-only code now has to provide a token every time it writes.

```csharp
public sealed class StreamAppender
{
    private readonly IFencingTokenSource _tokens;
    private readonly IFencedWriter _writer;

    public StreamAppender(IFencingTokenSource tokens, IFencedWriter writer)
    {
        _tokens = tokens;
        _writer = writer;
    }

    public async Task AppendAsync(string stream, byte[] payload, CancellationToken ct)
    {
        var token = await _tokens.CurrentAsync(ct);
        if (token.Value == 0) throw new InvalidOperationException("Not leader");
        await _writer.AppendAsync(token, stream, payload, ct);
    }
}
```

The key point is not the in-memory storage. The key point is that every write carries the token and the database enforces monotonic progress.

## Testing Strategy

Treat stale token rejection as required behavior, not an edge case.

Integration tests to write:

1.  Token increases on takeover.
2.  Writes with the latest token succeed.
3.  Writes with an older token fail with the expected error.
4.  Concurrent writes from two tokens result in only the newer token being accepted.

A minimal test sketch:

```csharp
using Microsoft.Data.SqlClient;

public async Task StaleWriterIsRejected()
{
    var conn = new SqlConnection("Server=.;Database=TestDb;Trusted_Connection=True;Encrypt=False;");
    await conn.OpenAsync();

    var writer = new SqlFencedWriter(conn);

    await writer.AppendAsync(new FencingToken(10), "orders", new byte[] { 1, 2, 3 }, CancellationToken.None);

    try
    {
        await writer.AppendAsync(new FencingToken(9), "orders", new byte[] { 9 }, CancellationToken.None);
        throw new Exception("Expected stale token rejection");
    }
    catch (SqlException ex) when (ex.Number == 50001)
    {
        // expected
    }
}
```

Fault injection test worth running:

-   Acquire lease and token on Node A.
-   Pause Node A longer than TTL so Node B takes over and gets a higher token.
-   Resume Node A and attempt a write with the old token.
-   Verify the database rejects it.

That test is your insurance policy against zombie leaders.

## Operational Checklist

You want these signals visible.

-   current leader identity per group
-   current token per group
-   rate of stale token rejections
-   leader changes per hour
-   SQL latency for fenced writes

Alerts to treat seriously:

-   sustained stale token rejections
-   rapidly increasing tokens for a group, which indicates flapping
-   failed renewals correlated with coordinator latency

A stale token rejection is not noise. It is proof that the guardrail is catching real risk.

## Common Mistakes

-   generating tokens locally without atomic increment
-   checking tokens in application code but not at the database boundary
-   forgetting to propagate tokens through async paths and background jobs
-   allowing followers to write without a token
-   logging stale token errors and continuing as if nothing happened

If the storage layer does not reject stale tokens, you do not have fencing. You have a comment.

## Wrap Up and What Comes Next

Leases decide who should lead. Fencing tokens decide who is allowed to write.

If you already ship leader election and leases, add fencing before you trust the system with money, inventory, or anything that triggers a compliance audit. It is the simplest way to prevent a stalled process from rewriting reality.
