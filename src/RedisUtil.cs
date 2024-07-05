using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Soenneker.Enums.JsonOptions;
using Soenneker.Extensions.String;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.Redis.Client.Abstract;
using Soenneker.Redis.Util.Abstract;
using Soenneker.Utils.BackgroundQueue.Abstract;
using Soenneker.Utils.Json;
using Soenneker.Utils.Method;
using StackExchange.Redis;

namespace Soenneker.Redis.Util;

// TODO: Time to break this up
/// <inheritdoc cref="IRedisUtil"/>
public class RedisUtil : IRedisUtil
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

    public ValueTask<T?> Get<T>(string cacheKey, string? key, CancellationToken cancellationToken = default) where T : class
    {
        string redisKey = BuildKey(cacheKey, key);

        return Get<T>(redisKey, cancellationToken);
    }

    public async ValueTask<T?> Get<T>(string redisKey, CancellationToken cancellationToken = default) where T : class
    {
        string? cacheValue = await GetString(redisKey, cancellationToken).NoSync();

        if (cacheValue == null)
            return default;

        try
        {
            var deserialized = JsonUtil.Deserialize<T>(cacheValue);
            return deserialized;
        }
        catch (Exception e)
        {
            _logger.LogError(e, ">> REDIS: Error deserializing object with key: {key} and value: {value}", redisKey, cacheValue);

            return null;
        }
    }

    public async ValueTask<T?> GetHash<T>(string redisKey, string field, CancellationToken cancellationToken = default) where T : class
    {
        string? cacheValue = await GetHash(redisKey, field, cancellationToken).NoSync();

        if (cacheValue == null)
            return default;

        try
        {
            var deserialized = JsonUtil.Deserialize<T>(cacheValue);

            return deserialized;
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
            _logger.LogError(">> REDIS: Skipping {method} because the redisValue is null or empty", MethodUtil.Get());
            return null;
        }

        try
        {
            // this is a cheap pass-thru object, and does not need to be stored
            IDatabase database = (await _redisClient.Get(cancellationToken).NoSync()).GetDatabase();

            string? value = await database.StringGetAsync(redisKey).NoSync();

            if (!_log)
                return value;

            if (value == null)
                _logger.LogDebug(">> REDIS: Key {key} does not exist", redisKey);
            else
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
            _logger.LogError(">> REDIS: Skipping {method} because the redisValue is null or empty", MethodUtil.Get());
            return null;
        }

        try
        {
            // this is a cheap pass-thru object, and does not need to be stored
            IDatabase database = (await _redisClient.Get(cancellationToken).NoSync()).GetDatabase();

            Lease<byte>? lease = await database.StringGetLeaseAsync(redisKey).NoSync();

            if (!_log)
                return lease;

            if (lease == null)
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
            _logger.LogError(">> REDIS: Skipping {method} because the redisValue is null or empty", MethodUtil.Get());
            return null;
        }

        try
        {
            // this is a cheap pass-thru object, and does not need to be stored
            IDatabase database = (await _redisClient.Get(cancellationToken).NoSync()).GetDatabase();

            string? value = await database.HashGetAsync(redisKey, field).NoSync();

            if (!_log)
                return value;

            if (value == null)
                _logger.LogDebug(">> REDIS: Key {key} does not exist", redisKey);
            else
                _logger.LogDebug(">> REDIS: Retrieved key: {key} \r\n {result}", redisKey, value);

            return value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, ">> REDIS: Error getting key: {key}", redisKey);

            return null;
        }
    }

    public ValueTask Set<T>(string cacheKey, string? key, T value, TimeSpan? expiration = null, bool useQueue = false, CancellationToken cancellationToken = default) where T : class
    {
        string redisKey = BuildKey(cacheKey, key);

        return Set(redisKey, value, expiration, useQueue, cancellationToken);
    }

    public async ValueTask Set<T>(string redisKey, T value, TimeSpan? expiration = null, bool useQueue = false, CancellationToken cancellationToken = default) where T : class
    {
        if (redisKey.IsNullOrEmpty())
        {
            _logger.LogError(">> REDIS: Skipping {method} because the key is null or empty", MethodUtil.Get());
            return;
        }

        RedisValue? redisValue = SerializeIntoValue(redisKey, value);

        if (redisValue == null)
            return;

        if (useQueue)
        {
            await _backgroundQueue.QueueValueTask(_ => InternalRedisValueSet(redisKey, redisValue.Value, expiration, cancellationToken)).NoSync();
            return;
        }

        await InternalRedisValueSet(redisKey, redisValue.Value, expiration, cancellationToken).NoSync();
    }

    public ValueTask Set(string cacheKey, string? key, string value, TimeSpan? expiration = null, bool useQueue = false, CancellationToken cancellationToken = default)
    {
        string redisKey = BuildKey(cacheKey, key);

        return Set(redisKey, value, expiration, useQueue, cancellationToken);
    }

    public ValueTask Set(string redisKey, string redisValue, TimeSpan? expiration = null, bool useQueue = false, CancellationToken cancellationToken = default)
    {
        if (redisKey.IsNullOrEmpty())
        {
            _logger.LogError(">> REDIS: Skipping {method} because the key is null or empty", MethodUtil.Get());
            return ValueTask.CompletedTask;
        }

        if (redisValue.IsNullOrEmpty())
        {
            _logger.LogError(">> REDIS: Skipping {method} because the redisValue is null or empty", MethodUtil.Get());
            return ValueTask.CompletedTask;
        }

        if (useQueue)
            return _backgroundQueue.QueueValueTask(token => InternalRedisValueSet(redisKey, redisValue, expiration, token));

        return InternalRedisValueSet(redisKey, redisValue, expiration, cancellationToken);
    }

    private RedisValue? SerializeIntoValue<T>(RedisKey redisKey, T value)
    {
        RedisValue redisValue;

        try
        {
            string? serialized = JsonUtil.Serialize(value, _jsonOptionType);
            redisValue = new RedisValue(serialized!);
        }
        catch (Exception e)
        {
            _logger.LogError(e, ">> REDIS: Error serializing object with key: {key}", redisKey);
            return null;
        }

        return redisValue;
    }

    private async ValueTask InternalRedisValueSet(RedisKey redisKey, RedisValue redisValue, TimeSpan? expiration, CancellationToken cancellationToken)
    {
        try
        {
            IDatabase database = (await _redisClient.Get(cancellationToken).NoSync()).GetDatabase();

            await database.StringSetAsync(redisKey, redisValue, expiration).NoSync();

            if (_log)
            {
                string expirationStr = expiration == null ? "never" : expiration.Value.Humanize();
                _logger.LogDebug(">> REDIS: Set key: {key} (expires: {datetime}) \r\n {redisValue}", redisKey, expirationStr, redisValue);
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
            _logger.LogError(">> REDIS: Skipping {method} because the key is null or empty", MethodUtil.Get());
            return ValueTask.CompletedTask;
        }

        if (redisValue.IsNullOrEmpty())
        {
            _logger.LogError(">> REDIS: Skipping {method} because the redisValue is null or empty", MethodUtil.Get());
            return ValueTask.CompletedTask;
        }

        if (useQueue)
            return _backgroundQueue.QueueValueTask(token => InternalHashSet(redisKey, field, redisValue, token));

        return InternalHashSet(redisKey, field, redisValue, cancellationToken);
    }

    private async ValueTask InternalHashSet(string redisKey, string field, string redisValue, CancellationToken cancellationToken)
    {
        try
        {
            IDatabase database = (await _redisClient.Get(cancellationToken).NoSync()).GetDatabase();

            await database.HashSetAsync(redisKey, field, redisValue).NoSync();

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
            _logger.LogError(">> REDIS: Skipping {method} because the key is null or empty", MethodUtil.Get());
            return ValueTask.CompletedTask;
        }

        if (useQueue)
            return _backgroundQueue.QueueValueTask(token => InternalKeyDelete(redisKey, token));

        return InternalKeyDelete(redisKey, cancellationToken);
    }

    private async ValueTask InternalKeyDelete(string redisKey, CancellationToken cancellationToken)
    {
        try
        {
            IDatabase database = (await _redisClient.Get(cancellationToken).NoSync()).GetDatabase();

            await database.KeyDeleteAsync(redisKey).NoSync();

            if (_log)
                _logger.LogDebug(">> REDIS: Removed key: {key}", redisKey);
        }
        catch (Exception e)
        {
            _logger.LogError(e, ">> REDIS: Error removing key: {key}", redisKey);
        }
    }

    /// <summary>
    /// Escapes the keys for safety
    /// </summary>
    [Pure]
    public static string BuildKey(string cacheKey, string? key)
    {
        if (key == null)
            return cacheKey;

        ReadOnlySpan<char> cacheKeySpan = cacheKey.AsSpan();
        ReadOnlySpan<char> escapedKeySpan = key.ToEscaped();

        int totalLength = cacheKeySpan.Length + 1 + escapedKeySpan.Length; // Length of cacheKey + separator + length of escaped key

        var buffer = new char[totalLength];
        cacheKeySpan.CopyTo(buffer);
        buffer[cacheKeySpan.Length] = ':';
        escapedKeySpan.CopyTo(buffer.AsSpan(cacheKeySpan.Length + 1));

        return new string(buffer);
    }

    /// <summary>
    /// Escapes the keys for safety. Optimized for speed.
    /// </summary>
    [Pure]
    public static string BuildKey(string cacheKey, params string?[] keys)
    {
        if (keys.Length == 0)
            return cacheKey;

        // Calculate the length of the resulting string
        int totalLength = cacheKey.Length;

        for (var i = 0; i < keys.Length; i++)
        {
            string? key = keys[i];
            if (key != null)
            {
                totalLength += 1 + key.ToEscaped().Length; // Add 1 for the ':' separator
            }
        }

        // Create a character span to store the result
        var resultArray = new char[totalLength];

        Span<char> result = resultArray;

        // Copy cacheKey into the result array
        cacheKey.AsSpan().CopyTo(result);
        result = result.Slice(cacheKey.Length);

        // Append keys to the result array
        for (var i = 0; i < keys.Length; i++)
        {
            string? key = keys[i];

            if (key == null)
                continue;

            result[0] = ':';
            result = result.Slice(1);

            string escaped = key.ToEscaped();

            escaped.AsSpan().CopyTo(result);
            result = result.Slice(escaped.Length);
        }

        return new string(resultArray);
    }
}