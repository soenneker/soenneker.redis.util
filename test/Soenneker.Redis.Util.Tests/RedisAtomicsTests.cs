using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Redis.Util.Atomics;
using StackExchange.Redis;

namespace Soenneker.Redis.Util.Tests;

public sealed class RedisAtomicsTests
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

    private static async Task WithDatabase(Func<IDatabase, string, Task> test)
    {
        using var connection = await ConnectionMultiplexer.ConnectAsync(Environment.GetEnvironmentVariable("REDIS_TEST_CONNECTION") ?? Environment.GetEnvironmentVariable("FLYWHEEL_TEST_REDIS") ?? "localhost:6379");
        var db = connection.GetDatabase();
        string prefix = "atomics:{" + Guid.NewGuid().ToString("N") + "}:";
        try { await test(db, prefix); }
        finally
        {
            var server = connection.GetServer((await db.IdentifyEndpointAsync(prefix))!);
            await foreach (var key in server.KeysAsync(pattern: prefix + "*")) await db.KeyDeleteAsync(key);
        }
    }

    [Test]
    public Task CompareExchangeHasOneWinner() => WithDatabase(async (db, prefix) =>
    {
        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(i => RedisAtomics.CompareExchange(db, prefix + "value", RedisValue.Null, i)));
        Check(results.Count(x => x) == 1, "Multiple compare-exchange winners");
        var previous = await db.StringGetAsync(prefix + "value");
        Check(await RedisAtomics.CompareExchange(db, prefix + "value", previous, "updated"), "Compare-exchange failed");
        Check(!await RedisAtomics.CompareExchange(db, prefix + "value", previous, "stale"), "Stale compare-exchange succeeded");
    });

    [Test]
    public Task ConflictDoesNotApplyAnyQueuedMutation() => WithDatabase(async (db, prefix) =>
    {
        var transaction = new RedisAtomicTransaction(db);
        transaction.Require(Condition.StringEqual(prefix + "owner", "old"));
        transaction.Queue(t => t.HashSetAsync(prefix + "jobs", "job", "running"));
        transaction.Queue(t => t.SortedSetAddAsync(prefix + "running", "job", 1));
        await db.StringSetAsync(prefix + "owner", "new");
        Check(!await transaction.Execute(), "Conflict committed");
        Check(!await db.KeyExistsAsync(prefix + "jobs") && !await db.KeyExistsAsync(prefix + "running"), "Conflict partially mutated state");
    });

    [Test]
    public Task ExpiredOwnershipCannotCommitRenewOrDeleteSuccessor() => WithDatabase(async (db, prefix) =>
    {
        await db.StringSetAsync(prefix + "owner", "old", TimeSpan.FromMilliseconds(50), false);
        var transaction = new RedisAtomicTransaction(db);
        transaction.Require(Condition.StringEqual(prefix + "owner", "old"));
        transaction.Queue(t => t.StringSetAsync(prefix + "result", "stale"));
        await Task.Delay(100);
        Check(!await transaction.Execute(), "Expired ownership committed");
        await db.StringSetAsync(prefix + "owner", "new");
        Check(!await RedisAtomics.CompareExpire(db, prefix + "owner", "old", TimeSpan.FromMinutes(1)), "Stale renewal succeeded");
        Check(!await RedisAtomics.CompareDelete(db, prefix + "owner", "old"), "Stale release succeeded");
        Check(await RedisAtomics.CompareExpire(db, prefix + "owner", "new", TimeSpan.FromMinutes(1)), "Owner could not renew");
        Check(await RedisAtomics.CompareDelete(db, prefix + "owner", "new"), "Owner could not release");
    });

    [Test]
    public Task QueuedCommandErrorsAreNotReportedAsSuccess() => WithDatabase(async (db, prefix) =>
    {
        await db.StringSetAsync(prefix + "wrong-type", "string");
        var transaction = new RedisAtomicTransaction(db);
        transaction.Queue(t => t.HashSetAsync(prefix + "wrong-type", "field", "value"));
        try { await transaction.Execute(); throw new Exception("Command error was hidden"); }
        catch (RedisServerException) { }
    });

    [Test]
    public Task CancellationBeforeDispatchDoesNotWrite() => WithDatabase(async (db, prefix) =>
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var transaction = new RedisAtomicTransaction(db);
        transaction.Queue(t => t.StringSetAsync(prefix + "value", "unexpected"));
        try { await transaction.Execute(cancellation.Token); throw new Exception("Cancellation was ignored"); }
        catch (OperationCanceledException) { }
        Check(!await db.KeyExistsAsync(prefix + "value"), "Cancelled transaction wrote state");
    });
}
