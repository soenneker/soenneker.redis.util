using System;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Soenneker.Redis.Util.Atomics;

/// <summary>Script-free atomic primitives for an explicitly selected database. Failures propagate to the caller.</summary>
public static class RedisAtomics
{
    /// <summary>Builds an equality or absence condition for an observed string value.</summary>
    public static Condition StringMatches(RedisKey key, RedisValue expected) =>
        expected.IsNull ? Condition.KeyNotExists(key) : Condition.StringEqual(key, expected);

    /// <summary>Builds an equality or absence condition for an observed hash field.</summary>
    public static Condition HashMatches(RedisKey key, RedisValue field, RedisValue expected) =>
        expected.IsNull ? Condition.HashNotExists(key, field) : Condition.HashEqual(key, field, expected);

    /// <summary>Replaces a string only if its value still matches; a null expected value requires absence.</summary>
    public static Task<bool> CompareExchange(IDatabase database, RedisKey key, RedisValue expected, RedisValue value,
        TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var transaction = new RedisAtomicTransaction(database);
        transaction.Require(StringMatches(key, expected));
        transaction.Queue(t => t.StringSetAsync(key, value, expiry, false));
        return transaction.Execute(cancellationToken);
    }

    /// <summary>Deletes a key only while it still contains the supplied ownership token.</summary>
    public static Task<bool> CompareDelete(IDatabase database, RedisKey key, RedisValue token, CancellationToken cancellationToken = default)
    {
        if (token.IsNull) throw new ArgumentException("An ownership token is required.", nameof(token));
        var transaction = new RedisAtomicTransaction(database);
        transaction.Require(Condition.StringEqual(key, token));
        transaction.Queue(t => t.KeyDeleteAsync(key));
        return transaction.Execute(cancellationToken);
    }

    /// <summary>Renews a key only while it still contains the supplied ownership token.</summary>
    public static Task<bool> CompareExpire(IDatabase database, RedisKey key, RedisValue token, TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        if (token.IsNull || expiry <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(expiry));
        var transaction = new RedisAtomicTransaction(database);
        transaction.Require(Condition.StringEqual(key, token));
        transaction.Queue(t => t.KeyExpireAsync(key, expiry));
        return transaction.Execute(cancellationToken);
    }
}
