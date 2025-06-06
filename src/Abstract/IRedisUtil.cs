using System;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Redis.Util.Abstract;

/// <summary>
/// Defines a set of operations for interacting with Redis, 
/// including getting, setting, removing, incrementing, and decrementing values.
/// </summary>
public interface IRedisUtil
{
    /// <summary>
    /// Retrieves an object of type <typeparamref name="T"/> from a Redis key composed of a base <paramref name="cacheKey"/> 
    /// and an optional <paramref name="key"/> segment. 
    /// The stored value is deserialized from JSON.
    /// </summary>
    /// <typeparam name="T">
    /// The type to deserialize the stored JSON into. Must be a reference type.
    /// </typeparam>
    /// <param name="cacheKey">
    /// The base key under which the object is cached.
    /// </param>
    /// <param name="key">
    /// An optional additional segment to append to <paramref name="cacheKey"/> (separated by “:”). 
    /// If <c>null</c>, <paramref name="cacheKey"/> alone is used.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the asynchronous operation to complete.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> whose result is:
    /// <list type="bullet">
    ///   <item><c>null</c> if the Redis key does not exist or deserialization fails.</item>
    ///   <item>An instance of <typeparamref name="T"/> otherwise.</item>
    /// </list>
    /// </returns>
    ValueTask<T?> Get<T>(string cacheKey, string? key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Retrieves an object of type <typeparamref name="T"/> from a Redis key specified by <paramref name="redisKey"/>. 
    /// The stored value is deserialized from JSON.
    /// </summary>
    /// <typeparam name="T">
    /// The type to deserialize the stored JSON into. Must be a reference type.
    /// </typeparam>
    /// <param name="redisKey">
    /// The full Redis key under which the object is cached.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the asynchronous operation to complete.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> whose result is:
    /// <list type="bullet">
    ///   <item><c>null</c> if the Redis key does not exist or deserialization fails.</item>
    ///   <item>An instance of <typeparamref name="T"/> otherwise.</item>
    /// </list>
    /// </returns>
    ValueTask<T?> Get<T>(string redisKey, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Retrieves an object of type <typeparamref name="T"/> from a Redis hash field. 
    /// The stored hash field value is deserialized from JSON.
    /// </summary>
    /// <typeparam name="T">
    /// The type to deserialize the stored JSON into. Must be a reference type.
    /// </typeparam>
    /// <param name="redisKey">
    /// The Redis hash key.
    /// </param>
    /// <param name="field">
    /// The specific field within the hash to retrieve.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the asynchronous operation to complete.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> whose result is:
    /// <list type="bullet">
    ///   <item><c>null</c> if the field does not exist or deserialization fails.</item>
    ///   <item>An instance of <typeparamref name="T"/> otherwise.</item>
    /// </list>
    /// </returns>
    ValueTask<T?> GetHash<T>(string redisKey, string field, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Retrieves a raw string value from a Redis key composed of a base <paramref name="cacheKey"/> 
    /// and an optional <paramref name="key"/> segment.
    /// </summary>
    /// <param name="cacheKey">
    /// The base key under which the string is cached.
    /// </param>
    /// <param name="key">
    /// An optional additional segment to append to <paramref name="cacheKey"/> (separated by “:”). 
    /// If <c>null</c>, <paramref name="cacheKey"/> alone is used.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the asynchronous operation to complete.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> whose result is:
    /// <list type="bullet">
    ///   <item><c>null</c> if the Redis key does not exist or an error occurs.</item>
    ///   <item>The raw stored string otherwise.</item>
    /// </list>
    /// </returns>
    ValueTask<string?> GetString(string cacheKey, string? key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a raw string value from a Redis key specified by <paramref name="redisKey"/>.
    /// </summary>
    /// <param name="redisKey">
    /// The full Redis key under which the string is cached.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the asynchronous operation to complete.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> whose result is:
    /// <list type="bullet">
    ///   <item><c>null</c> if the Redis key does not exist or an error occurs.</item>
    ///   <item>The raw stored string otherwise.</item>
    /// </list>
    /// </returns>
    ValueTask<string?> GetString(string redisKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores an object of type <typeparamref name="T"/> under a Redis key composed of a base <paramref name="cacheKey"/> 
    /// and an optional <paramref name="key"/> segment. The object is serialized to JSON before storage. 
    /// </summary>
    /// <typeparam name="T">
    /// The type of the object to store. Must be a reference type.
    /// </typeparam>
    /// <param name="cacheKey">
    /// The base key under which to store the object.
    /// </param>
    /// <param name="key">
    /// An optional additional segment to append to <paramref name="cacheKey"/> (separated by “:”). 
    /// If <c>null</c>, <paramref name="cacheKey"/> alone is used.
    /// </param>
    /// <param name="value">
    /// The object to serialize and store.
    /// </param>
    /// <param name="expiration">
    /// An optional <see cref="TimeSpan"/> after which the key expires. 
    /// If <c>null</c>, the key never expires.
    /// </param>
    /// <param name="useQueue">
    /// If <c>true</c>, the set operation is enqueued to run in the background; 
    /// otherwise, it runs immediately.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the asynchronous operation to complete.
    /// </param>
    ValueTask Set<T>(string cacheKey, string? key, T value, TimeSpan? expiration = null, bool useQueue = false,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Stores an object of type <typeparamref name="T"/> under the specified Redis key. 
    /// The object is serialized to JSON before storage.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the object to store. Must be a reference type.
    /// </typeparam>
    /// <param name="redisKey">
    /// The full Redis key under which to store the object.
    /// </param>
    /// <param name="value">
    /// The object to serialize and store.
    /// </param>
    /// <param name="expiration">
    /// An optional <see cref="TimeSpan"/> after which the key expires. 
    /// If <c>null</c>, the key never expires.
    /// </param>
    /// <param name="useQueue">
    /// If <c>true</c>, the set operation is enqueued to run in the background; 
    /// otherwise, it runs immediately.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the asynchronous operation to complete.
    /// </param>
    ValueTask Set<T>(string redisKey, T value, TimeSpan? expiration = null, bool useQueue = false, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Stores a raw string under a Redis key composed of a base <paramref name="cacheKey"/> 
    /// and an optional <paramref name="key"/> segment.
    /// </summary>
    /// <param name="cacheKey">
    /// The base key under which to store the string.
    /// </param>
    /// <param name="key">
    /// An optional additional segment to append to <paramref name="cacheKey"/> (separated by “:”). 
    /// If <c>null</c>, <paramref name="cacheKey"/> alone is used.
    /// </param>
    /// <param name="value">
    /// The string to store.
    /// </param>
    /// <param name="expiration">
    /// An optional <see cref="TimeSpan"/> after which the key expires. 
    /// If <c>null</c>, the key never expires.
    /// </param>
    /// <param name="useQueue">
    /// If <c>true</c>, the set operation is enqueued to run in the background; 
    /// otherwise, it runs immediately.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the asynchronous operation to complete.
    /// </param>
    ValueTask Set(string cacheKey, string? key, string value, TimeSpan? expiration = null, bool useQueue = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a raw string under the specified Redis key.
    /// </summary>
    /// <param name="redisKey">
    /// The full Redis key under which to store the string.
    /// </param>
    /// <param name="redisValue">
    /// The string to store.
    /// </param>
    /// <param name="expiration">
    /// An optional <see cref="TimeSpan"/> after which the key expires. 
    /// If <c>null</c>, the key never expires.
    /// </param>
    /// <param name="useQueue">
    /// If <c>true</c>, the set operation is enqueued to run in the background; 
    /// otherwise, it runs immediately.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the asynchronous operation to complete.
    /// </param>
    ValueTask Set(string redisKey, string redisValue, TimeSpan? expiration = null, bool useQueue = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a single field in a Redis hash under the specified <paramref name="redisKey"/>.
    /// </summary>
    /// <param name="redisKey">
    /// The Redis hash key.
    /// </param>
    /// <param name="field">
    /// The field within the hash to set.
    /// </param>
    /// <param name="redisValue">
    /// The string value to store in the hash field.
    /// </param>
    /// <param name="useQueue">
    /// If <c>true</c>, the hash set operation is enqueued to run in the background; 
    /// otherwise, it runs immediately.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the asynchronous operation to complete.
    /// </param>
    ValueTask SetHash(string redisKey, string field, string redisValue, bool useQueue = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a key composed of a base <paramref name="cacheKey"/> and an optional <paramref name="key"/> segment from Redis.
    /// </summary>
    /// <param name="cacheKey">
    /// The base key to remove.
    /// </param>
    /// <param name="key">
    /// An optional additional segment to append to <paramref name="cacheKey"/> (separated by “:”). 
    /// If <c>null</c>, <paramref name="cacheKey"/> alone is removed.
    /// </param>
    /// <param name="useQueue">
    /// If <c>true</c>, the remove operation is enqueued to run in the background; 
    /// otherwise, it runs immediately.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the asynchronous operation to complete.
    /// </param>
    ValueTask Remove(string cacheKey, string? key, bool useQueue = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the specified Redis key.
    /// </summary>
    /// <param name="redisKey">
    /// The full key to remove.
    /// </param>
    /// <param name="useQueue">
    /// If <c>true</c>, the remove operation is enqueued to run in the background; 
    /// otherwise, it runs immediately.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the asynchronous operation to complete.
    /// </param>
    ValueTask Remove(string redisKey, bool useQueue = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrements the numeric value stored at a Redis key composed of a base <paramref name="cacheKey"/> and an optional <paramref name="key"/> segment.
    /// If the key does not exist, it is initialized to 0 before decrementing.
    /// </summary>
    /// <param name="cacheKey">
    /// The base key under which the numeric value is stored.
    /// </param>
    /// <param name="key">
    /// An optional additional segment to append to <paramref name="cacheKey"/> (separated by “:”). 
    /// If <c>null</c>, <paramref name="cacheKey"/> alone is used.
    /// </param>
    /// <param name="delta">
    /// The amount by which to decrement. Default is 1.
    /// </param>
    /// <param name="useQueue">
    /// If <c>true</c>, the decrement operation is enqueued to run in the background; 
    /// otherwise, it runs immediately.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the asynchronous operation to complete.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> whose result is:
    /// <list type="bullet">
    ///   <item>The new value after decrement on success.</item>
    ///   <item><c>null</c> if an error occurs.</item>
    /// </list>
    /// </returns>
    ValueTask<long?> Decrement(string cacheKey, string? key, long delta = 1, bool useQueue = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrements the numeric value stored at the specified Redis key. 
    /// If the key does not exist, it is initialized to 0 before decrementing.
    /// </summary>
    /// <param name="redisKey">
    /// The full key under which the numeric value is stored.
    /// </param>
    /// <param name="delta">
    /// The amount by which to decrement. Default is 1.
    /// </param>
    /// <param name="useQueue">
    /// If <c>true</c>, the decrement operation is enqueued to run in the background; 
    /// otherwise, it runs immediately.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the asynchronous operation to complete.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> whose result is:
    /// <list type="bullet">
    ///   <item>The new value after decrement on success.</item>
    ///   <item><c>null</c> if an error occurs.</item>
    /// </list>
    /// </returns>
    ValueTask<long?> Decrement(string redisKey, long delta = 1, bool useQueue = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the numeric value stored at a Redis key composed of a base <paramref name="cacheKey"/> and an optional <paramref name="key"/> segment.
    /// If the key does not exist, it is initialized to 0 before incrementing.
    /// </summary>
    /// <param name="cacheKey">
    /// The base key under which the numeric value is stored.
    /// </param>
    /// <param name="key">
    /// An optional additional segment to append to <paramref name="cacheKey"/> (separated by “:”). 
    /// If <c>null</c>, <paramref name="cacheKey"/> alone is used.
    /// </param>
    /// <param name="delta">
    /// The amount by which to increment. Default is 1.
    /// </param>
    /// <param name="useQueue">
    /// If <c>true</c>, the increment operation is enqueued to run in the background; 
    /// otherwise, it runs immediately.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the asynchronous operation to complete.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> whose result is:
    /// <list type="bullet">
    ///   <item>The new value after increment on success.</item>
    ///   <item><c>null</c> if an error occurs.</item>
    /// </list>
    /// </returns>
    ValueTask<long?> Increment(string cacheKey, string? key, long delta = 1, bool useQueue = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the numeric value stored at the specified Redis key. 
    /// If the key does not exist, it is initialized to 0 before incrementing.
    /// </summary>
    /// <param name="redisKey">
    /// The full key under which the numeric value is stored.
    /// </param>
    /// <param name="delta">
    /// The amount by which to increment. Default is 1.
    /// </param>
    /// <param name="useQueue">
    /// If <c>true</c>, the increment operation is enqueued to run in the background; 
    /// otherwise, it runs immediately.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the asynchronous operation to complete.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> whose result is:
    /// <list type="bullet">
    ///   <item>The new value after increment on success.</item>
    ///   <item><c>null</c> if an error occurs.</item>
    /// </list>
    /// </returns>
    ValueTask<long?> Increment(string redisKey, long delta = 1, bool useQueue = false, CancellationToken cancellationToken = default);
}