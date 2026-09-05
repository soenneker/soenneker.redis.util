using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Soenneker.Redis.Util.Atomics;

/// <summary>A single-use native Redis transaction that observes every queued command result. No scripts are used.</summary>
/// <remarks>Keys must share a cluster slot. Redis does not roll back command errors; callers must ensure key types and
/// command arguments are valid. Transport errors may leave the commit outcome unknown and are never converted to conflicts.</remarks>
public sealed class RedisAtomicTransaction(IDatabase database)
{
    private readonly ITransaction _transaction = database.CreateTransaction();
    private readonly List<Task> _commands = new();
    private bool _executed;

    /// <summary>Adds a condition that must hold when committing, including ownership checks on expiring keys.</summary>
    public void Require(Condition condition)
    {
        if (_executed) throw new InvalidOperationException("Transaction already executed.");
        _transaction.AddCondition(condition);
    }

    /// <summary>Queues one command and tracks its result. Do not await commands inside the callback or enqueue unreturned tasks.</summary>
    public void Queue(Func<ITransaction, Task> command)
    {
        if (_executed) throw new InvalidOperationException("Transaction already executed.");
        _commands.Add(command(_transaction));
    }

    /// <summary>Returns false only for a condition conflict. Throws for command or connection errors.
    /// Cancellation is checked before dispatch; once dispatched, waits for the outcome without cancellation.</summary>
    public async Task<bool> Execute(CancellationToken cancellationToken = default)
    {
        if (_executed) throw new InvalidOperationException("Transaction already executed.");
        cancellationToken.ThrowIfCancellationRequested();
        _executed = true;
        bool committed;
        try { committed = await _transaction.ExecuteAsync().ConfigureAwait(false); }
        catch
        {
            // Observe pending command failures even if the connection failed before EXEC returned.
            _ = Task.WhenAll(_commands).ContinueWith(t => { _ = t.Exception; }, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            throw;
        }
        if (!committed)
        {
            try { await Task.WhenAll(_commands).ConfigureAwait(false); }
            catch (OperationCanceledException) { } // Redis cancels commands when transaction conditions fail.
            return false;
        }
        await Task.WhenAll(_commands).ConfigureAwait(false);
        return true;
    }
}
