[![](https://img.shields.io/nuget/v/Soenneker.Redis.Util.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Redis.Util/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.redis.util/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.redis.util/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Redis.Util.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Redis.Util/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.redis.util/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.redis.util/actions/workflows/codeql.yml)

# Soenneker.Redis.Util

Defines a set of operations for interacting with Redis, including getting, setting, removing, incrementing, and decrementing values.

## Install

```bash
dotnet add package Soenneker.Redis.Util
```

## Quick start

```csharp
using Soenneker.Redis.Util.Registrars;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
var result = services.AddRedisUtilAsSingleton();
```

Adds `IRedisUtil` as a singleton service.

## What you get

- `IRedisUtil` — Defines a set of operations for interacting with Redis, including getting, setting, removing, incrementing, and decrementing values.
- `RedisUtilRegistrar` — The general purpose utility library leveraging Redis for all of your caching needs.

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `IRedisUtil.Get(cacheKey, key, cancellationToken)` | Retrieves an object of type `T` from a Redis key composed of a base `cacheKey` and an optional `key` segment. The stored value is deserialized from JSON. | A `ValueTask{TResult}` whose result is: `null` if the Redis key does not exist or deserialization fails. An instance of `T` otherwise. |
| `IRedisUtil.Get(redisKey, cancellationToken)` | Retrieves an object of type `T` from a Redis key specified by `redisKey`. The stored value is deserialized from JSON. | A `ValueTask{TResult}` whose result is: `null` if the Redis key does not exist or deserialization fails. An instance of `T` otherwise. |
| `IRedisUtil.GetHash(redisKey, field, cancellationToken)` | Retrieves an object of type `T` from a Redis hash field. The stored hash field value is deserialized from JSON. | A `ValueTask{TResult}` whose result is: `null` if the field does not exist or deserialization fails. An instance of `T` otherwise. |
| `IRedisUtil.GetString(cacheKey, key, cancellationToken)` | Retrieves a raw string value from a Redis key composed of a base `cacheKey` and an optional `key` segment. | A `ValueTask{TResult}` whose result is: `null` if the Redis key does not exist or an error occurs. The raw stored string otherwise. |
| `IRedisUtil.GetString(redisKey, cancellationToken)` | Retrieves a raw string value from a Redis key specified by `redisKey`. | A `ValueTask{TResult}` whose result is: `null` if the Redis key does not exist or an error occurs. The raw stored string otherwise. |
| `IRedisUtil.CountExisting(redisKeys, cancellationToken)` | Counts how many of the specified fully-qualified Redis keys currently exist. | The number of existing keys, or `null` when the operation fails. |
| `IRedisUtil.Set(cacheKey, key, value, expiration, useQueue, cancellationToken)` | Stores an object of type `T` under a Redis key composed of a base `cacheKey` and an optional `key` segment. The object is serialized to JSON before storage. | A task that completes when the set operation is complete. |
| `IRedisUtil.Set(redisKey, value, expiration, useQueue, cancellationToken)` | Stores an object of type `T` under the specified Redis key. The object is serialized to JSON before storage. | A task that completes when the set operation is complete. |
| `IRedisUtil.SetIfNotExists(cacheKey, key, value, expiration, cancellationToken)` | Stores an object of type `T` under a Redis key composed of a base `cacheKey` and an optional `key` segment only when the key does not already exist. The object is serialized to JSON before storage. | `true` if the key was set; otherwise `false`. |
| `IRedisUtil.SetIfNotExists(redisKey, value, expiration, cancellationToken)` | Stores an object of type `T` under the specified Redis key only when the key does not already exist. The object is serialized to JSON before storage. | `true` if the key was set; otherwise `false`. |
| `IRedisUtil.Set(redisKey, redisValue, expiration, useQueue, cancellationToken)` | Stores a raw string under the specified Redis key. | A task that completes when the set operation is complete. |
| `IRedisUtil.SetIfNotExists(redisKey, redisValue, expiration, cancellationToken)` | Stores a raw string under the specified Redis key only when the key does not already exist. | `true` if the key was set; otherwise `false`. |
| `IRedisUtil.SetHash(redisKey, field, redisValue, useQueue, cancellationToken)` | Stores a single field in a Redis hash under the specified `redisKey`. | A task that completes when the hash has been stored. |
| `IRedisUtil.Remove(cacheKey, key, useQueue, cancellationToken)` | Removes a key composed of a base `cacheKey` and an optional `key` segment from Redis. | A task that completes when the remove operation is complete. |
| `IRedisUtil.Remove(redisKey, useQueue, cancellationToken)` | Removes the specified Redis key. | A task that completes when the remove operation is complete. |
| `IRedisUtil.RemoveIfEqual(cacheKey, key, expectedValue, cancellationToken)` | Removes a key composed of a base `cacheKey` and an optional `key` segment only when its value matches `expectedValue`. | `true` if the value matched and the key was removed; otherwise `false`. |
| `IRedisUtil.RemoveIfEqual(redisKey, expectedValue, cancellationToken)` | Removes the specified Redis key only when its value matches `expectedValue`. | `true` if the value matched and the key was removed; otherwise `false`. |
| `IRedisUtil.Decrement(cacheKey, key, delta, useQueue, cancellationToken)` | Decrements the numeric value stored at a Redis key composed of a base `cacheKey` and an optional `key` segment. If the key does not exist, it is initialized to 0 before decrementing. | A `ValueTask{TResult}` whose result is: The new value after decrement on success. `null` if an error occurs. |

## Practical notes

- Cancellation stops pending work; it does not undo work that has already completed.
