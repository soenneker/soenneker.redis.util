using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Redis.Util.Abstract;

/// <summary>
/// The general purpose utility library leveraging Redis for all of your caching needs <para/>
/// There are several "key types" here. "redisKey" means the actual key being sent to the redis server. "cacheKey" is the prefix key. "key" is the suffix key. <para/>
/// The design here is it should be completely safe and transparent - we don't want to throw exceptions from this and should handle every situation thrown at it. <para/>
/// Scoped IoC.
/// </summary>
public interface IRedisUtil
{
    /// <summary>
    /// Includes deserialization of the redisValue. Will return null if error deserializing.
    /// </summary>
    ValueTask<T?> Get<T>(string redisKey, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Includes deserialization of the redisValue. Will return null if error deserializing. Key is optional.
    /// </summary>
    ValueTask<T?> Get<T>(string cacheKey, string? key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Key is optional.
    /// </summary>
    [Pure]
    ValueTask<string?> GetString(string cacheKey, string? key, CancellationToken cancellationToken = default);

    /// <summary> Base method for all Get() methods. Does not build a redis key. </summary>
    [Pure]
    ValueTask<string?> GetString(string redisKey, CancellationToken cancellationToken = default);

    [Pure]
    ValueTask<string?> GetHash(string redisKey, string field, CancellationToken cancellationToken = default);

    /// <summary>
    /// Includes deserialization of the redisValue. Will return null if error deserializing.
    /// </summary>
    ValueTask<T?> GetHash<T>(string redisKey, string field, CancellationToken cancellationToken = default) where T : class;


    /// <summary> Includes serialization of the object. Key is optional. </summary>
    ValueTask Set<T>(string cacheKey, string? key, T value, TimeSpan? expiration = null, bool useQueue = false, CancellationToken cancellationToken = default) where T : class;

    /// <summary> Includes serialization of the object. </summary>
    ValueTask Set<T>(string redisKey, T value, TimeSpan? expiration = null, bool useQueue = false, CancellationToken cancellationToken = default) where T : class;

    /// <summary> Key is optional. </summary>
    ValueTask Set(string cacheKey, string? key, string value, TimeSpan? expiration = null, bool useQueue = false, CancellationToken cancellationToken = default);

    /// <summary> Base method for all Set() methods. Does not build a redis key. </summary>
    ValueTask Set(string redisKey, string redisValue, TimeSpan ? expiration = null, bool useQueue = false, CancellationToken cancellationToken = default);

    ValueTask SetHash(string redisKey, string field, string redisValue, bool useQueue = false, CancellationToken cancellationToken = default);

    /// <summary> Key is optional. </summary>
    ValueTask Remove(string cacheKey, string? key, bool useQueue = false, CancellationToken cancellationToken = default);

    /// <summary> Base method for Remove()</summary>
    ValueTask Remove(string redisKey, bool useQueue = false, CancellationToken cancellationToken = default);
}