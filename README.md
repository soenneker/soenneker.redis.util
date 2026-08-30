[![](https://img.shields.io/nuget/v/Soenneker.Redis.Util.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Redis.Util/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.redis.util/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.redis.util/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Redis.Util.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Redis.Util/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.redis.util/build-and-test.yml?label=build%20and%20test&style=for-the-badge)](https://github.com/soenneker/soenneker.redis.util/actions/workflows/build-and-test.yml)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.redis.util/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.redis.util/actions/workflows/codeql.yml)

# Soenneker.Redis.Util

High-level Redis operations for JSON objects, strings, hashes, counters, expiration, lists, sets, sorted sets, and transactions.

## Installation

```bash
dotnet add package Soenneker.Redis.Util
```

## Configuration and registration

```json
{
  "Azure": {
    "Redis": {
      "ConnectionString": "localhost:6379,abortConnect=false",
      "Log": false
    }
  }
}
```

```csharp
using Soenneker.Redis.Util.Registrars;

services.AddRedisUtilAsScoped();
```

The scoped registration deliberately uses a singleton Redis client and background queue. A scope can dispose its utility wrapper without closing the shared `ConnectionMultiplexer`. `AddRedisUtilAsSingleton()` is also available.

## JSON values

```csharp
await redis.Set(
    "orders",
    order.Id,
    order,
    expiration: TimeSpan.FromHours(1),
    cancellationToken: cancellationToken);

Order? cached = await redis.Get<Order>("orders", order.Id, cancellationToken);
```

The `(cacheKey, key)` overloads build `cacheKey:escaped-key`; a `null` child key targets the base key itself. Object overloads serialize as JSON. Use `GetString` and the string `Set` overloads when the stored value must remain raw text.

## Conditional ownership operations

```csharp
bool acquired = await redis.SetIfNotExists(
    "jobs:daily-report",
    ownerToken,
    TimeSpan.FromMinutes(2),
    cancellationToken);

bool renewed = await redis.ExpireIfEqual(
    "jobs:daily-report",
    ownerToken,
    TimeSpan.FromMinutes(2),
    cancellationToken);

bool released = await redis.RemoveIfEqual(
    "jobs:daily-report",
    ownerToken,
    cancellationToken);
```

Compare-and-expire and compare-and-delete execute through Redis transactions, so another owner's value is not renewed or removed.

## Collections and counters

`IRedisUtil` also exposes list push/pop/index operations, set and sorted-set membership operations, hash field operations, atomic increments/decrements, TTL inspection, multi-key existence counts, and `ExecuteTransaction` for configuring a StackExchange.Redis transaction.

## Background writes and failure behavior

Set, hash-set, remove, increment/decrement, and expiration methods with `useQueue: true` enqueue work through the shared background queue. Result-returning queued operations return `null` or `false` because the Redis result is not awaited; use the default direct path when the outcome matters.

This utility is cache-oriented: many invalid-input, serialization, deserialization, and Redis failures are logged and represented as `null`, `false`, or a completed write call. Treat those values as “missing or unavailable,” not always as proof that a key did not exist. Use StackExchange.Redis directly when an operation must surface every server failure distinctly.
