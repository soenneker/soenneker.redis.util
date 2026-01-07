using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Soenneker.Enums.JsonOptions;
using Soenneker.Extensions.String;
using Soenneker.Extensions.ValueTask;
using Soenneker.Redis.Client.Abstract;
using Soenneker.Redis.Util.Abstract;
using Soenneker.Utils.BackgroundQueue.Abstract;
using Soenneker.Utils.Json;
using Soenneker.Utils.PooledStringBuilders;
using StackExchange.Redis;
using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Redis.Util;

// TODO: Time to break this up
/// <inheritdoc cref="IRedisUtil"/>
public sealed class RedisUtil : IRedisUtil
{
    private readonly bool _log;
    private readonly JsonOptionType _jsonOptionType;
    private readonly ILogger<RedisUtil> _logger;
    private readonly IRedisClient _redisClient;
    private readonly IBackgroundQueue _backgroundQueue;

    public RedisUtil(IConfiguration config, ILogger<RedisUtil> logger, IRedisClient redisClient, IBackgroundQueue backgroundQueue)
    {
        _log = config.GetValue<bool>("Azure:Redis:Log");
        _logger = logger;
        _jsonOptionType = _log ? JsonOptionType.Pretty : JsonOptionType.Web;
        _redisClient = redisClient;
        _backgroundQueue = backgroundQueue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ValueTask<IDatabase> GetDb(CancellationToken ct)
    {
        ValueTask<ConnectionMultiplexer> vt = _redisClient.Get(ct);

        if (vt.IsCompletedSuccessfully)
            return new ValueTask<IDatabase>(vt.Result.GetDatabase());

        return AwaitSlow(vt);

        static async ValueTask<IDatabase> AwaitSlow(ValueTask<ConnectionMultiplexer> vt) => (await vt.NoSync()).GetDatabase();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ValueTask<T> Await<T>(Task<T> task, CancellationToken ct)
    {
        // Avoid WaitAsync overhead when token is default / not cancelable
        if (!ct.CanBeCanceled)
            return new ValueTask<T>(task);

        return new ValueTask<T>(task.WaitAsync(ct));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LogSkipKeyEmpty([CallerMemberName] string? method = null) =>
        _logger.LogError(">> REDIS: Skipping {method} because the key is null or empty", method);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LogSkipValueEmpty([CallerMemberName] string? method = null) =>
        _logger.LogError(">> REDIS: Skipping {method} because the redisValue is null or empty", method);

    public ValueTask<T?> Get<T>(string cacheKey, string? key, CancellationToken cancellationToken = default) where T : class
    {
        string redisKey = BuildKey(cacheKey, key);
        return Get<T>(redisKey, cancellationToken);
    }

    public async ValueTask<T?> Get<T>(string redisKey, CancellationToken cancellationToken = default) where T : class
    {
        // Fast path: read raw bytes via lease to avoid string alloc + UTF8 re-encode
        using Lease<byte>? lease = await GetLease(redisKey, cancellationToken)
            .NoSync();

        if (lease is null)
            return null;

        try
        {
            return JsonUtil.Deserialize<T>(lease.Span);
        }
        catch (Exception e)
        {
            if (_log)
                _logger.LogError(e, ">> REDIS: Error deserializing object with key: {key}", redisKey);
            return null;
        }
    }

    public async ValueTask<T?> GetHash<T>(string redisKey, string field, CancellationToken cancellationToken = default) where T : class
    {
        string? cacheValue = await GetHash(redisKey, field, cancellationToken)
            .NoSync();
        if (cacheValue == null)
            return null;

        try
        {
            return JsonUtil.Deserialize<T>(cacheValue);
        }
        catch (Exception e)
        {
            _logger.LogError(e, ">> REDIS: Error deserializing object with key: {key} and value: {value}", redisKey, cacheValue);
            return null;
        }
    }

    public ValueTask<string?> GetString(string cacheKey, string? key, CancellationToken cancellationToken = default)
    {
        string redisKey = BuildKey(cacheKey, key);
        return GetString(redisKey, cancellationToken);
    }

    public async ValueTask<string?> GetString(string redisKey, CancellationToken cancellationToken = default)
    {
        if (redisKey.IsNullOrEmpty())
        {
            LogSkipKeyEmpty();
            return null;
        }

        try
        {
            IDatabase db = await GetDb(cancellationToken)
                .NoSync();

            // Get as RedisValue first; avoid extra work on miss
            RedisValue rv = await Await(db.StringGetAsync(redisKey), cancellationToken)
                .NoSync();

            if (rv.IsNull)
            {
                if (_log)
                    _logger.LogDebug(">> REDIS: Key {key} does not exist", redisKey);
                return null;
            }

            string? value = (string?)rv;

            if (_log)
                _logger.LogDebug(">> REDIS: Retrieved key: {key} \r\n {result}", redisKey, value);

            return value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, ">> REDIS: Error getting key: {key}", redisKey);
            return null;
        }
    }

    private async ValueTask<Lease<byte>?> GetLease(string redisKey, CancellationToken cancellationToken)
    {
        if (redisKey.IsNullOrEmpty())
        {
            LogSkipKeyEmpty();
            return null;
        }

        try
        {
            IDatabase db = await GetDb(cancellationToken)
                .NoSync();

            Lease<byte>? lease = await Await(db.StringGetLeaseAsync(redisKey), cancellationToken)
                .NoSync();

            if (_log && lease is null)
                _logger.LogDebug(">> REDIS: Key {key} does not exist", redisKey);

            return lease;
        }
        catch (Exception e)
        {
            _logger.LogError(e, ">> REDIS: Error getting key: {key}", redisKey);
            return null;
        }
    }

    public async ValueTask<string?> GetHash(string redisKey, string field, CancellationToken cancellationToken = default)
    {
        if (redisKey.IsNullOrEmpty())
        {
            LogSkipKeyEmpty();
            return null;
        }

        try
        {
            IDatabase db = await GetDb(cancellationToken)
                .NoSync();

            RedisValue rv = await Await(db.HashGetAsync(redisKey, field), cancellationToken)
                .NoSync();

            if (rv.IsNull)
            {
                if (_log)
                    _logger.LogDebug(">> REDIS: Key {key} does not exist", redisKey);
                return null;
            }

            string? value = (string?)rv;

            if (_log)
                _logger.LogDebug(">> REDIS: Retrieved key: {key} \r\n {result}", redisKey, value);

            return value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, ">> REDIS: Error getting key: {key}", redisKey);
            return null;
        }
    }

    public ValueTask Set<T>(string cacheKey, string? key, T value, TimeSpan? expiration = null, bool useQueue = false,
        CancellationToken cancellationToken = default) where T : class
    {
        string redisKey = BuildKey(cacheKey, key);
        return Set(redisKey, value, expiration, useQueue, cancellationToken);
    }

    public async ValueTask Set<T>(string redisKey, T value, TimeSpan? expiration = null, bool useQueue = false, CancellationToken cancellationToken = default)
        where T : class
    {
        if (redisKey.IsNullOrEmpty())
        {
            LogSkipKeyEmpty();
            return;
        }

        RedisValue? redisValue = SerializeIntoValue((RedisKey)redisKey, value);

        if (redisValue is null)
            return;

        if (useQueue)
        {
            await _backgroundQueue.QueueValueTask((util: this, key: (RedisKey)redisKey, value: redisValue.Value, exp: expiration),
                                      static (s, ct) => s.util.InternalRedisValueSet(s.key, s.value, s.exp, ct), cancellationToken)
                                  .NoSync();
            return;
        }

        await InternalRedisValueSet((RedisKey)redisKey, redisValue.Value, expiration, cancellationToken)
            .NoSync();
    }

    public ValueTask Set(string cacheKey, string? key, string value, TimeSpan? expiration = null, bool useQueue = false,
        CancellationToken cancellationToken = default)
    {
        string redisKey = BuildKey(cacheKey, key);
        return Set(redisKey, value, expiration, useQueue, cancellationToken);
    }

    public ValueTask Set(string redisKey, string redisValue, TimeSpan? expiration = null, bool useQueue = false, CancellationToken cancellationToken = default)
    {
        if (redisKey.IsNullOrEmpty())
        {
            LogSkipKeyEmpty();
            return ValueTask.CompletedTask;
        }

        if (redisValue.IsNullOrEmpty())
        {
            LogSkipValueEmpty();
            return ValueTask.CompletedTask;
        }

        if (useQueue)
        {
            return _backgroundQueue.QueueValueTask((util: this, key: (RedisKey)redisKey, value: (RedisValue)redisValue, exp: expiration),
                static (s, ct) => s.util.InternalRedisValueSet(s.key, s.value, s.exp, ct), cancellationToken);
        }

        return InternalRedisValueSet((RedisKey)redisKey, (RedisValue)redisValue, expiration, cancellationToken);
    }

    private RedisValue? SerializeIntoValue<T>(RedisKey redisKey, T value)
    {
        try
        {
            byte[] utf8 = JsonUtil.SerializeToUtf8Bytes(value!, _jsonOptionType);
            RedisValue redisValue = utf8;
            return redisValue;
        }
        catch (Exception e)
        {
            _logger.LogError(e, ">> REDIS: Error serializing object with key: {key}", redisKey);
            return null;
        }
    }

    private async ValueTask InternalRedisValueSet(RedisKey redisKey, RedisValue redisValue, TimeSpan? expiration, CancellationToken cancellationToken)
    {
        try
        {
            IDatabase db = await GetDb(cancellationToken)
                .NoSync();

            _ = await Await(db.StringSetAsync(redisKey, redisValue, expiration, false), cancellationToken)
                .NoSync();

            if (_log)
            {
                string expirationStr = expiration == null ? "never" : expiration.Value.ToString("c");
                _logger.LogDebug(">> REDIS: Set key: {key} (expires in: {expiration}) \r\n {redisValue}", redisKey, expirationStr, redisValue);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, ">> REDIS: Error setting key: {key} with value: {value}", redisKey, redisValue);
        }
    }

    public ValueTask SetHash(string redisKey, string field, string redisValue, bool useQueue = false, CancellationToken cancellationToken = default)
    {
        if (redisKey.IsNullOrEmpty())
        {
            LogSkipKeyEmpty();
            return ValueTask.CompletedTask;
        }

        if (redisValue.IsNullOrEmpty())
        {
            LogSkipValueEmpty();
            return ValueTask.CompletedTask;
        }

        if (useQueue)
        {
            return _backgroundQueue.QueueValueTask((util: this, key: redisKey, field, value: redisValue),
                static (s, ct) => s.util.InternalHashSet(s.key, s.field, s.value, ct), cancellationToken);
        }

        return InternalHashSet(redisKey, field, redisValue, cancellationToken);
    }

    private async ValueTask InternalHashSet(string redisKey, string field, string redisValue, CancellationToken cancellationToken)
    {
        try
        {
            IDatabase db = await GetDb(cancellationToken)
                .NoSync();

            await Await(db.HashSetAsync(redisKey, field, redisValue), cancellationToken)
                .NoSync();

            if (_log)
                _logger.LogDebug(">> REDIS: Set HASH key: {key} \r\n {redisValue}", redisKey, redisValue);
        }
        catch (Exception e)
        {
            _logger.LogError(e, ">> REDIS: Error setting key: {key}", redisKey);
        }
    }

    public ValueTask Remove(string cacheKey, string? key, bool useQueue = false, CancellationToken cancellationToken = default)
    {
        string redisKey = BuildKey(cacheKey, key);
        return Remove(redisKey, useQueue, cancellationToken);
    }

    public ValueTask Remove(string redisKey, bool useQueue = false, CancellationToken cancellationToken = default)
    {
        if (redisKey.IsNullOrEmpty())
        {
            LogSkipKeyEmpty();
            return ValueTask.CompletedTask;
        }

        if (useQueue)
        {
            return _backgroundQueue.QueueValueTask((util: this, key: redisKey), static (s, ct) => s.util.InternalKeyDelete(s.key, ct), cancellationToken);
        }

        return InternalKeyDelete(redisKey, cancellationToken);
    }

    private async ValueTask InternalKeyDelete(string redisKey, CancellationToken cancellationToken)
    {
        try
        {
            IDatabase db = await GetDb(cancellationToken)
                .NoSync();

            _ = await Await(db.KeyDeleteAsync(redisKey), cancellationToken)
                .NoSync();

            if (_log)
                _logger.LogDebug(">> REDIS: Removed key: {key}", redisKey);
        }
        catch (Exception e)
        {
            _logger.LogError(e, ">> REDIS: Error removing key: {key}", redisKey);
        }
    }

    public ValueTask<long?> Decrement(string cacheKey, string? key, long delta = 1, bool useQueue = false, CancellationToken cancellationToken = default)
    {
        string redisKey = BuildKey(cacheKey, key);
        return Decrement(redisKey, delta, useQueue, cancellationToken);
    }

    public async ValueTask<long?> Decrement(string redisKey, long delta = 1, bool useQueue = false, CancellationToken cancellationToken = default)
    {
        if (redisKey.IsNullOrEmpty())
        {
            _logger.LogError(">> REDIS: Skipping Decrement because the key is null or empty");
            return null;
        }

        if (useQueue)
        {
            // fire-and-forget; queued path does not return result
            await _backgroundQueue.QueueValueTask((util: this, key: redisKey, delta),
                                      static (s, ct) => s.util.InternalStringDecrementNoThrow(s.key, s.delta, ct), cancellationToken)
                                  .NoSync();

            return null;
        }

        return await InternalStringDecrement(redisKey, delta, cancellationToken)
            .NoSync();
    }

    /// <summary>
    /// Queued path: no returned result; no async state machine inside delegate.
    /// </summary>
    private async ValueTask InternalStringDecrementNoThrow(string redisKey, long delta, CancellationToken cancellationToken)
    {
        try
        {
            IDatabase db = await GetDb(cancellationToken)
                .NoSync();

            long newValue = await Await(db.StringDecrementAsync(redisKey, delta), cancellationToken)
                .NoSync();

            if (_log)
                _logger.LogDebug(">> REDIS: Decremented key: {key} by {delta}. New value: {newValue}", redisKey, delta, newValue);
        }
        catch (Exception e)
        {
            _logger.LogError(e, ">> REDIS: Error decrementing key: {key} by {delta}", redisKey, delta);
        }
    }

    /// <summary>
    /// Direct path: returns new long value (or null on error).
    /// </summary>
    private async ValueTask<long?> InternalStringDecrement(string redisKey, long delta, CancellationToken cancellationToken)
    {
        try
        {
            IDatabase db = await GetDb(cancellationToken)
                .NoSync();

            long newValue = await Await(db.StringDecrementAsync(redisKey, delta), cancellationToken)
                .NoSync();

            if (_log)
                _logger.LogDebug(">> REDIS: Decremented key: {key} by {delta}. New value: {newValue}", redisKey, delta, newValue);

            return newValue;
        }
        catch (Exception e)
        {
            _logger.LogError(e, ">> REDIS: Error decrementing key: {key} by {delta}", redisKey, delta);
            return null;
        }
    }

    public ValueTask<long?> Increment(string cacheKey, string? key, long delta = 1, bool useQueue = false, CancellationToken cancellationToken = default)
    {
        string redisKey = BuildKey(cacheKey, key);
        return Increment(redisKey, delta, useQueue, cancellationToken);
    }

    public async ValueTask<long?> Increment(string redisKey, long delta = 1, bool useQueue = false, CancellationToken cancellationToken = default)
    {
        if (redisKey.IsNullOrEmpty())
        {
            _logger.LogError(">> REDIS: Skipping Increment because the key is null or empty");
            return null;
        }

        if (useQueue)
        {
            await _backgroundQueue.QueueValueTask((util: this, key: redisKey, delta),
                                      static (s, ct) => s.util.InternalStringIncrementNoThrow(s.key, s.delta, ct), cancellationToken)
                                  .NoSync();

            return null;
        }

        return await InternalStringIncrement(redisKey, delta, cancellationToken)
            .NoSync();
    }

    private async ValueTask InternalStringIncrementNoThrow(string redisKey, long delta, CancellationToken cancellationToken)
    {
        try
        {
            IDatabase db = await GetDb(cancellationToken)
                .NoSync();

            long newValue = await Await(db.StringIncrementAsync(redisKey, delta), cancellationToken)
                .NoSync();

            if (_log)
                _logger.LogDebug(">> REDIS: Incremented key: {key} by {delta}. New value: {newValue}", redisKey, delta, newValue);
        }
        catch (Exception e)
        {
            _logger.LogError(e, ">> REDIS: Error incrementing key: {key} by {delta}", redisKey, delta);
        }
    }

    private async ValueTask<long?> InternalStringIncrement(string redisKey, long delta, CancellationToken cancellationToken)
    {
        try
        {
            IDatabase db = await GetDb(cancellationToken)
                .NoSync();

            long newValue = await Await(db.StringIncrementAsync(redisKey, delta), cancellationToken)
                .NoSync();

            if (_log)
                _logger.LogDebug(">> REDIS: Incremented key: {key} by {delta}. New value: {newValue}", redisKey, delta, newValue);

            return newValue;
        }
        catch (Exception e)
        {
            _logger.LogError(e, ">> REDIS: Error incrementing key: {key} by {delta}", redisKey, delta);
            return null;
        }
    }

    public ValueTask<bool> Expire(string cacheKey, string? key, TimeSpan? expiration, bool useQueue = false, CancellationToken cancellationToken = default)
    {
        string redisKey = BuildKey(cacheKey, key);
        return Expire(redisKey, expiration, useQueue, cancellationToken);
    }

    public async ValueTask<bool> Expire(string redisKey, TimeSpan? expiration, bool useQueue = false, CancellationToken cancellationToken = default)
    {
        if (redisKey.IsNullOrEmpty())
        {
            _logger.LogError(">> REDIS: Skipping Expire because the key is null or empty");
            return false;
        }

        if (expiration == null)
        {
            _logger.LogError(">> REDIS: Skipping Expire because the expiration is null");
            return false;
        }

        if (useQueue)
        {
            await _backgroundQueue.QueueValueTask((util: this, key: redisKey, exp: expiration),
                                      static (s, ct) => s.util.InternalKeyExpireNoThrow(s.key, s.exp, ct), cancellationToken)
                                  .NoSync();

            return false;
        }

        return await InternalKeyExpire(redisKey, expiration, cancellationToken)
            .NoSync();
    }

    private async ValueTask InternalKeyExpireNoThrow(string redisKey, TimeSpan? expiration, CancellationToken cancellationToken)
    {
        try
        {
            IDatabase db = await GetDb(cancellationToken)
                .NoSync();

            bool result = await Await(db.KeyExpireAsync(redisKey, expiration), cancellationToken)
                .NoSync();

            if (_log)
            {
                string expirationStr = expiration!.Value.ToString("c");
                _logger.LogDebug(">> REDIS: Set expiration on key: {key} (expires in: {timespan}) Result: {result}", redisKey, expirationStr, result);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, ">> REDIS: Error setting expiration on key: {key}", redisKey);
        }
    }

    private async ValueTask<bool> InternalKeyExpire(string redisKey, TimeSpan? expiration, CancellationToken cancellationToken)
    {
        try
        {
            IDatabase db = await GetDb(cancellationToken)
                .NoSync();

            bool result = await Await(db.KeyExpireAsync(redisKey, expiration), cancellationToken)
                .NoSync();

            if (_log)
            {
                string expirationStr = expiration!.Value.ToString("c");
                _logger.LogDebug(">> REDIS: Set expiration on key: {key} (expires in: {timespan}) Result: {result}", redisKey, expirationStr, result);
            }

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, ">> REDIS: Error setting expiration on key: {key}", redisKey);
            return false;
        }
    }

    public ValueTask<TimeSpan?> GetTimeToLive(string cacheKey, string? key, CancellationToken cancellationToken = default)
    {
        string redisKey = BuildKey(cacheKey, key);
        return GetTimeToLive(redisKey, cancellationToken);
    }

    public async ValueTask<TimeSpan?> GetTimeToLive(string redisKey, CancellationToken cancellationToken = default)
    {
        if (redisKey.IsNullOrEmpty())
        {
            _logger.LogError(">> REDIS: Skipping GetTimeToLive because the key is null or empty");
            return null;
        }

        try
        {
            IDatabase db = await GetDb(cancellationToken)
                .NoSync();

            TimeSpan? ttl = await Await(db.KeyTimeToLiveAsync(redisKey), cancellationToken)
                .NoSync();

            if (_log)
            {
                if (ttl == null)
                    _logger.LogDebug(">> REDIS: Key {key} does not exist or has no expiration", redisKey);
                else
                    _logger.LogDebug(">> REDIS: TTL for key: {key} is {timespan}", redisKey, ttl.Value.ToString("c"));
            }

            return ttl;
        }
        catch (Exception e)
        {
            _logger.LogError(e, ">> REDIS: Error getting TTL for key: {key}", redisKey);
            return null;
        }
    }

    /// <summary>
    /// Escapes the keys for safety.
    /// </summary>
    [Pure]
    public static string BuildKey(string cacheKey, string? key)
    {
        if (key == null)
            return cacheKey;

        string escaped = key.ToEscaped();
        using var psb = new PooledStringBuilder(cacheKey.Length + 1 + escaped.Length);
        psb.Append(cacheKey);
        psb.Append(':');
        psb.Append(escaped);
        return psb.ToString();
    }

    /// <summary>
    /// Escapes the keys for safety. Optimized for speed.
    /// NOTE: params callsites allocate an array; for hot paths consider adding fixed-arity overloads.
    /// </summary>
    [Pure]
    public static string BuildKey(string cacheKey, params string?[] keys)
    {
        if (keys.Length == 0)
            return cacheKey;

        int total = cacheKey.Length;

        var escaped = new string?[keys.Length];

        for (var i = 0; i < keys.Length; i++)
        {
            string? k = keys[i];
            if (k is null)
                continue;

            string e = k.ToEscaped();
            escaped[i] = e;
            total += 1 + e.Length;
        }

        using var psb = new PooledStringBuilder(total);
        psb.Append(cacheKey);

        for (var i = 0; i < escaped.Length; i++)
        {
            string? e = escaped[i];
            if (e is null)
                continue;

            psb.Append(':');
            psb.Append(e);
        }

        return psb.ToString();
    }
}