using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

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
    /// Retrieves a raw string value from a Redis hash field.
    /// </summary>
    /// <param name="redisKey">Redis key that identifies the target value or collection.</param>
    /// <param name="field">Hash field to read, write, or remove.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the text returned by get Hash.</returns>
    ValueTask<string?> GetHash(string redisKey, string field, CancellationToken cancellationToken = default);

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
    /// Counts how many of the specified fully-qualified Redis keys currently exist.
    /// </summary>
    /// <param name="redisKeys">The keys to inspect in one Redis operation.</param>
    /// <param name="cancellationToken">A token to observe while waiting for Redis.</param>
    /// <returns>The number of existing keys, or <c>null</c> when the operation fails.</returns>
    ValueTask<long?> CountExisting(IReadOnlyList<string> redisKeys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores an object of type <typeparamref name="T"/> under a Redis key composed of a base <paramref name="cacheKey"/>
    /// and an optional <paramref name="key"/> segment. The object is serialized to JSON before storage.
    /// </summary>
    /// <typeparam name="T">The type of the object to store. Must be a reference type.</typeparam>
    /// <param name="cacheKey">Base cache key used to build the Redis key.</param>
    /// <param name="key">An optional additional segment to append to <paramref name="cacheKey"/> (separated by “:”). If <c>null</c>, <paramref name="cacheKey"/> alone is used.</param>
    /// <param name="value">Value to serialize or store in the targeted Redis structure.</param>
    /// <param name="expiration">An optional <see cref="TimeSpan"/> after which the key expires. If <c>null</c>, the key never expires.</param>
    /// <param name="useQueue">If <c>true</c>, the set operation is enqueued to run in the background; otherwise, it runs immediately.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the asynchronous operation to complete.</param>
    /// <returns>A task that completes when the set operation is complete.</returns>
    ValueTask Set<T>(string cacheKey, string? key, T value, TimeSpan? expiration = null, bool useQueue = false, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Stores an object of type <typeparamref name="T"/> under the specified Redis key.
    /// The object is serialized to JSON before storage.
    /// </summary>
    /// <typeparam name="T">The type of the object to store. Must be a reference type.</typeparam>
    /// <param name="redisKey">The full Redis key under which to store the object.</param>
    /// <param name="value">Value to serialize or store in the targeted Redis structure.</param>
    /// <param name="expiration">An optional <see cref="TimeSpan"/> after which the key expires. If <c>null</c>, the key never expires.</param>
    /// <param name="useQueue">If <c>true</c>, the set operation is enqueued to run in the background; otherwise, it runs immediately.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the asynchronous operation to complete.</param>
    /// <returns>A task that completes when the set operation is complete.</returns>
    ValueTask Set<T>(string redisKey, T value, TimeSpan? expiration = null, bool useQueue = false, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Stores an object of type <typeparamref name="T"/> under a Redis key composed of a base <paramref name="cacheKey"/>
    /// and an optional <paramref name="key"/> segment only when the key does not already exist. The object is serialized to JSON before storage.
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
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the asynchronous operation to complete.
    /// </param>
    /// <returns><c>true</c> if the key was set; otherwise <c>false</c>.</returns>
    ValueTask<bool> SetIfNotExists<T>(string cacheKey, string? key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Stores an object of type <typeparamref name="T"/> under the specified Redis key only when the key does not already exist.
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
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the asynchronous operation to complete.
    /// </param>
    /// <returns><c>true</c> if the key was set; otherwise <c>false</c>.</returns>
    ValueTask<bool> SetIfNotExists<T>(string redisKey, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Stores a raw string under a Redis key composed of a base <paramref name="cacheKey"/>
    /// and an optional <paramref name="key"/> segment.
    /// </summary>
    /// <param name="cacheKey">Base cache key used to build the Redis key.</param>
    /// <param name="key">An optional additional segment to append to <paramref name="cacheKey"/> (separated by “:”). If <c>null</c>, <paramref name="cacheKey"/> alone is used.</param>
    /// <param name="value">Value to serialize or store in the targeted Redis structure.</param>
    /// <param name="expiration">An optional <see cref="TimeSpan"/> after which the key expires. If <c>null</c>, the key never expires.</param>
    /// <param name="useQueue">If <c>true</c>, the set operation is enqueued to run in the background; otherwise, it runs immediately.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the asynchronous operation to complete.</param>
    /// <returns>A task that completes when the set operation is complete.</returns>
    ValueTask Set(string cacheKey, string? key, string value, TimeSpan? expiration = null, bool useQueue = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a raw string under the specified Redis key.
    /// </summary>
    /// <param name="redisKey">The full Redis key under which to store the string.</param>
    /// <param name="redisValue">Serialized value to store.</param>
    /// <param name="expiration">An optional <see cref="TimeSpan"/> after which the key expires. If <c>null</c>, the key never expires.</param>
    /// <param name="useQueue">If <c>true</c>, the set operation is enqueued to run in the background; otherwise, it runs immediately.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the asynchronous operation to complete.</param>
    /// <returns>A task that completes when the set operation is complete.</returns>
    ValueTask Set(string redisKey, string redisValue, TimeSpan? expiration = null, bool useQueue = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a raw string under a Redis key composed of a base <paramref name="cacheKey"/>
    /// and an optional <paramref name="key"/> segment only when the key does not already exist.
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
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the asynchronous operation to complete.
    /// </param>
    /// <returns><c>true</c> if the key was set; otherwise <c>false</c>.</returns>
    ValueTask<bool> SetIfNotExists(string cacheKey, string? key, string value, TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a raw string under the specified Redis key only when the key does not already exist.
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
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the asynchronous operation to complete.
    /// </param>
    /// <returns><c>true</c> if the key was set; otherwise <c>false</c>.</returns>
    ValueTask<bool> SetIfNotExists(string redisKey, string redisValue, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a single field in a Redis hash under the specified <paramref name="redisKey"/>.
    /// </summary>
    /// <param name="redisKey">Redis key that identifies the target value or collection.</param>
    /// <param name="field">Hash field to read, write, or remove.</param>
    /// <param name="redisValue">Serialized value to store.</param>
    /// <param name="useQueue">If <c>true</c>, the hash set operation is enqueued to run in the background; otherwise, it runs immediately.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the asynchronous operation to complete.</param>
    /// <returns>A task that completes when the hash has been stored.</returns>
    ValueTask SetHash(string redisKey, string field, string redisValue, bool useQueue = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a key composed of a base <paramref name="cacheKey"/> and an optional <paramref name="key"/> segment from Redis.
    /// </summary>
    /// <param name="cacheKey">Base cache key used to build the Redis key.</param>
    /// <param name="key">An optional additional segment to append to <paramref name="cacheKey"/> (separated by “:”). If <c>null</c>, <paramref name="cacheKey"/> alone is removed.</param>
    /// <param name="useQueue">If <c>true</c>, the remove operation is enqueued to run in the background; otherwise, it runs immediately.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the asynchronous operation to complete.</param>
    /// <returns>A task that completes when the remove operation is complete.</returns>
    ValueTask Remove(string cacheKey, string? key, bool useQueue = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the specified Redis key.
    /// </summary>
    /// <param name="redisKey">Redis key that identifies the target value or collection.</param>
    /// <param name="useQueue">If <c>true</c>, the remove operation is enqueued to run in the background; otherwise, it runs immediately.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the asynchronous operation to complete.</param>
    /// <returns>A task that completes when the remove operation is complete.</returns>
    ValueTask Remove(string redisKey, bool useQueue = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a key composed of a base <paramref name="cacheKey"/> and an optional <paramref name="key"/> segment only when its value matches
    /// <paramref name="expectedValue"/>.
    /// </summary>
    /// <param name="cacheKey">The base key to remove.</param>
    /// <param name="key">An optional additional segment to append to <paramref name="cacheKey"/>.</param>
    /// <param name="expectedValue">The value that must currently be stored for the key to be removed.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the asynchronous operation to complete.</param>
    /// <returns><c>true</c> if the value matched and the key was removed; otherwise <c>false</c>.</returns>
    ValueTask<bool> RemoveIfEqual(string cacheKey, string? key, string expectedValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the specified Redis key only when its value matches <paramref name="expectedValue"/>.
    /// </summary>
    /// <param name="redisKey">The full key to remove.</param>
    /// <param name="expectedValue">The value that must currently be stored for the key to be removed.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the asynchronous operation to complete.</param>
    /// <returns><c>true</c> if the value matched and the key was removed; otherwise <c>false</c>.</returns>
    ValueTask<bool> RemoveIfEqual(string redisKey, string expectedValue, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Set an expiration on a key. Returns true if the expiration was set successfully.
    /// </summary>
    /// <param name="cacheKey">The base cache key (without any sub‐key).</param>
    /// <param name="key">An optional sub‐key to append to the cacheKey.</param>
    /// <param name="expiration">The TimeSpan after which the key should expire.</param>
    /// <param name="useQueue">Whether to enqueue this operation in the background queue.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>true if set an expiration on a key. Returns true if the expiration was set successfully; otherwise, false.</returns>
    ValueTask<bool> Expire(string cacheKey, string? key, TimeSpan? expiration, bool useQueue = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set an expiration on a fully‐qualified Redis key. Returns true if the expiration was set successfully.
    /// </summary>
    /// <param name="redisKey">The fully qualified Redis key (e.g. “cacheKey:subKey”).</param>
    /// <param name="expiration">The TimeSpan after which the key should expire.</param>
    /// <param name="useQueue">Whether to enqueue this operation in the background queue.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>true if set an expiration on a fully‐qualified Redis key. Returns true if the expiration was set successfully; otherwise, false.</returns>
    ValueTask<bool> Expire(string redisKey, TimeSpan? expiration, bool useQueue = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically sets an expiration on a key only when its current value matches <paramref name="expectedValue"/>.
    /// </summary>
    /// <param name="cacheKey">The base cache key.</param>
    /// <param name="key">An optional sub-key to append to <paramref name="cacheKey"/>.</param>
    /// <param name="expectedValue">The value that must currently be stored.</param>
    /// <param name="expiration">The new expiration. Must be greater than zero.</param>
    /// <param name="cancellationToken">A token to observe while waiting for Redis.</param>
    /// <returns><c>true</c> when the value matched and the expiration was updated; otherwise <c>false</c>.</returns>
    ValueTask<bool> ExpireIfEqual(string cacheKey, string? key, string expectedValue, TimeSpan expiration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically sets an expiration on a fully-qualified key only when its current value matches <paramref name="expectedValue"/>.
    /// </summary>
    /// <param name="redisKey">The fully-qualified Redis key.</param>
    /// <param name="expectedValue">The value that must currently be stored.</param>
    /// <param name="expiration">The new expiration. Must be greater than zero.</param>
    /// <param name="cancellationToken">A token to observe while waiting for Redis.</param>
    /// <returns><c>true</c> when the value matched and the expiration was updated; otherwise <c>false</c>.</returns>
    ValueTask<bool> ExpireIfEqual(string redisKey, string expectedValue, TimeSpan expiration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the remaining TTL (time to live) for a key, or null if it does not exist or has no expiration.
    /// </summary>
    /// <param name="cacheKey">The base cache key (without any sub‐key).</param>
    /// <param name="key">An optional sub‐key to append to the cacheKey.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task whose result is the requested time Span.</returns>
    ValueTask<TimeSpan?> GetTimeToLive(string cacheKey, string? key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the remaining TTL (time to live) for a fully‐qualified Redis key, or null if it does not exist or has no expiration.
    /// </summary>
    /// <param name="redisKey">The fully qualified Redis key (e.g. “cacheKey:subKey”).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task whose result is the requested time Span.</returns>
    ValueTask<TimeSpan?> GetTimeToLive(string redisKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the number of values in a Redis list.
    /// </summary>
    /// <param name="redisKey">Redis key that identifies the target value or collection.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the requested value.</returns>
    ValueTask<long?> GetListLength(string redisKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value at <paramref name="index"/> in a Redis list.
    /// </summary>
    /// <param name="redisKey">Redis key that identifies the target value or collection.</param>
    /// <param name="index">Zero-based position of the target item.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the text returned by get List Value.</returns>
    ValueTask<string?> GetListValue(string redisKey, long index, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes and returns the first value in a Redis list.
    /// </summary>
    /// <param name="redisKey">Redis key that identifies the target value or collection.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the text returned by pop List Left.</returns>
    ValueTask<string?> PopListLeft(string redisKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pushes a value to the beginning of a Redis list and returns the resulting length.
    /// </summary>
    /// <param name="redisKey">Redis key that identifies the target value or collection.</param>
    /// <param name="value">Value to serialize or store in the targeted Redis structure.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the requested value.</returns>
    ValueTask<long?> PushListLeft(string redisKey, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pushes a value to the end of a Redis list and returns the resulting length.
    /// </summary>
    /// <param name="redisKey">Redis key that identifies the target value or collection.</param>
    /// <param name="value">Value to serialize or store in the targeted Redis structure.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the requested value.</returns>
    ValueTask<long?> PushListRight(string redisKey, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a value to a Redis set.
    /// </summary>
    /// <param name="redisKey">Redis key that identifies the target value or collection.</param>
    /// <param name="value">Value to serialize or store in the targeted Redis structure.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>true if the value was newly added to the set; otherwise, false.</returns>
    ValueTask<bool> AddSetValue(string redisKey, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a value from a Redis set.
    /// </summary>
    /// <param name="redisKey">Redis key that identifies the target value or collection.</param>
    /// <param name="value">Value to serialize or store in the targeted Redis structure.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>true if the value was removed from the set; otherwise, false.</returns>
    ValueTask<bool> RemoveSetValue(string redisKey, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all values in a Redis set.
    /// </summary>
    /// <param name="redisKey">Redis key that identifies the target value or collection.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the collection returned by get Set Values.</returns>
    ValueTask<IReadOnlyList<string>?> GetSetValues(string redisKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets values from a Redis sorted set whose scores are within the specified range.
    /// </summary>
    /// <param name="redisKey">Redis key that identifies the target value or collection.</param>
    /// <param name="minimumScore">Inclusive minimum score to return.</param>
    /// <param name="maximumScore">Inclusive maximum score to return.</param>
    /// <param name="skip">Number of matching sorted-set entries to skip.</param>
    /// <param name="take">Maximum entries to return; -1 returns all remaining entries.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the collection returned by get Sorted Set Values By Score.</returns>
    ValueTask<IReadOnlyList<string>?> GetSortedSetValuesByScore(string redisKey, double minimumScore = double.NegativeInfinity,
        double maximumScore = double.PositiveInfinity, long skip = 0, long take = -1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the score of a value in a Redis sorted set.
    /// </summary>
    /// <param name="redisKey">Redis key that identifies the target value or collection.</param>
    /// <param name="value">Value to serialize or store in the targeted Redis structure.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the requested value.</returns>
    ValueTask<double?> GetSortedSetScore(string redisKey, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates a value in a Redis sorted set.
    /// </summary>
    /// <param name="redisKey">Redis key that identifies the target value or collection.</param>
    /// <param name="value">Value to serialize or store in the targeted Redis structure.</param>
    /// <param name="score">Score associated with the sorted-set value.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>true if a new sorted-set entry was added; false if an existing score was updated.</returns>
    ValueTask<bool> AddSortedSetValue(string redisKey, string value, double score, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a value from a Redis sorted set.
    /// </summary>
    /// <param name="redisKey">Redis key that identifies the target value or collection.</param>
    /// <param name="value">Value to serialize or store in the targeted Redis structure.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>true if the value was removed from the sorted set; otherwise, false.</returns>
    ValueTask<bool> RemoveSortedSetValue(string redisKey, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a field from a Redis hash.
    /// </summary>
    /// <param name="redisKey">Redis key that identifies the target value or collection.</param>
    /// <param name="field">Hash field to read, write, or remove.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>true if the hash field was removed; otherwise, false.</returns>
    ValueTask<bool> RemoveHashField(string redisKey, string field, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes configured Redis operations atomically when every condition added by <paramref name="configure"/> succeeds.
    /// </summary>
    /// <param name="configure">Callback that adds conditions and operations to the Redis transaction.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>true if all conditions passed and the transaction committed; otherwise, false.</returns>
    ValueTask<bool> ExecuteTransaction(Action<ITransaction> configure, CancellationToken cancellationToken = default);
}
