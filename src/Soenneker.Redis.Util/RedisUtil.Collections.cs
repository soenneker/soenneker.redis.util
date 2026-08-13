using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Soenneker.Extensions.String;
using Soenneker.Extensions.ValueTask;
using StackExchange.Redis;

namespace Soenneker.Redis.Util;

public sealed partial class RedisUtil
{
    public async ValueTask<long?> GetListLength(string redisKey, CancellationToken cancellationToken = default)
    {
        if (redisKey.IsNullOrEmpty())
        {
            LogSkipKeyEmpty();
            return null;
        }

        try
        {
            IDatabase db = await GetDb(cancellationToken).NoSync();
            return await Await(db.ListLengthAsync(redisKey), cancellationToken).NoSync();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, ">> REDIS: Error getting list length for key: {key}", redisKey);
            return null;
        }
    }

    public async ValueTask<string?> GetListValue(string redisKey, long index, CancellationToken cancellationToken = default)
    {
        if (redisKey.IsNullOrEmpty())
        {
            LogSkipKeyEmpty();
            return null;
        }

        try
        {
            IDatabase db = await GetDb(cancellationToken).NoSync();
            RedisValue value = await Await(db.ListGetByIndexAsync(redisKey, index), cancellationToken).NoSync();
            return value.IsNull ? null : value.ToString();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, ">> REDIS: Error getting list value for key: {key}", redisKey);
            return null;
        }
    }

    public async ValueTask<string?> PopListLeft(string redisKey, CancellationToken cancellationToken = default)
    {
        if (redisKey.IsNullOrEmpty())
        {
            LogSkipKeyEmpty();
            return null;
        }

        try
        {
            IDatabase db = await GetDb(cancellationToken).NoSync();
            RedisValue value = await Await(db.ListLeftPopAsync(redisKey), cancellationToken).NoSync();
            return value.IsNull ? null : value.ToString();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, ">> REDIS: Error popping list value for key: {key}", redisKey);
            return null;
        }
    }

    public ValueTask<long?> PushListLeft(string redisKey, string value, CancellationToken cancellationToken = default) =>
        PushList(redisKey, value, left: true, cancellationToken);

    public ValueTask<long?> PushListRight(string redisKey, string value, CancellationToken cancellationToken = default) =>
        PushList(redisKey, value, left: false, cancellationToken);

    private async ValueTask<long?> PushList(string redisKey, string value, bool left, CancellationToken cancellationToken)
    {
        if (redisKey.IsNullOrEmpty() || value.IsNullOrEmpty())
        {
            if (redisKey.IsNullOrEmpty()) LogSkipKeyEmpty(); else LogSkipValueEmpty();
            return null;
        }

        try
        {
            IDatabase db = await GetDb(cancellationToken).NoSync();
            Task<long> operation = left ? db.ListLeftPushAsync(redisKey, value) : db.ListRightPushAsync(redisKey, value);
            return await Await(operation, cancellationToken).NoSync();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, ">> REDIS: Error pushing list value for key: {key}", redisKey);
            return null;
        }
    }

    public ValueTask<bool> AddSetValue(string redisKey, string value, CancellationToken cancellationToken = default) =>
        ChangeSet(redisKey, value, add: true, cancellationToken);

    public ValueTask<bool> RemoveSetValue(string redisKey, string value, CancellationToken cancellationToken = default) =>
        ChangeSet(redisKey, value, add: false, cancellationToken);

    private async ValueTask<bool> ChangeSet(string redisKey, string value, bool add, CancellationToken cancellationToken)
    {
        if (redisKey.IsNullOrEmpty() || value.IsNullOrEmpty())
        {
            if (redisKey.IsNullOrEmpty()) LogSkipKeyEmpty(); else LogSkipValueEmpty();
            return false;
        }

        try
        {
            IDatabase db = await GetDb(cancellationToken).NoSync();
            Task<bool> operation = add ? db.SetAddAsync(redisKey, value) : db.SetRemoveAsync(redisKey, value);
            return await Await(operation, cancellationToken).NoSync();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, ">> REDIS: Error changing set value for key: {key}", redisKey);
            return false;
        }
    }

    public async ValueTask<IReadOnlyList<string>?> GetSetValues(string redisKey, CancellationToken cancellationToken = default)
    {
        if (redisKey.IsNullOrEmpty())
        {
            LogSkipKeyEmpty();
            return null;
        }

        try
        {
            IDatabase db = await GetDb(cancellationToken).NoSync();
            RedisValue[] values = await Await(db.SetMembersAsync(redisKey), cancellationToken).NoSync();
            return ConvertValues(values);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, ">> REDIS: Error getting set values for key: {key}", redisKey);
            return null;
        }
    }

    public async ValueTask<IReadOnlyList<string>?> GetSortedSetValuesByScore(string redisKey, double minimumScore = double.NegativeInfinity,
        double maximumScore = double.PositiveInfinity, long skip = 0, long take = -1, CancellationToken cancellationToken = default)
    {
        if (redisKey.IsNullOrEmpty())
        {
            LogSkipKeyEmpty();
            return null;
        }

        try
        {
            IDatabase db = await GetDb(cancellationToken).NoSync();
            RedisValue[] values = await Await(db.SortedSetRangeByScoreAsync(redisKey, minimumScore, maximumScore, skip: skip, take: take),
                cancellationToken).NoSync();
            return ConvertValues(values);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, ">> REDIS: Error getting sorted set values for key: {key}", redisKey);
            return null;
        }
    }

    public async ValueTask<double?> GetSortedSetScore(string redisKey, string value, CancellationToken cancellationToken = default)
    {
        if (redisKey.IsNullOrEmpty() || value.IsNullOrEmpty())
        {
            if (redisKey.IsNullOrEmpty()) LogSkipKeyEmpty(); else LogSkipValueEmpty();
            return null;
        }

        try
        {
            IDatabase db = await GetDb(cancellationToken).NoSync();
            return await Await(db.SortedSetScoreAsync(redisKey, value), cancellationToken).NoSync();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, ">> REDIS: Error getting sorted set score for key: {key}", redisKey);
            return null;
        }
    }

    public ValueTask<bool> AddSortedSetValue(string redisKey, string value, double score, CancellationToken cancellationToken = default) =>
        ChangeSortedSet(redisKey, value, score, add: true, cancellationToken);

    public ValueTask<bool> RemoveSortedSetValue(string redisKey, string value, CancellationToken cancellationToken = default) =>
        ChangeSortedSet(redisKey, value, 0, add: false, cancellationToken);

    private async ValueTask<bool> ChangeSortedSet(string redisKey, string value, double score, bool add, CancellationToken cancellationToken)
    {
        if (redisKey.IsNullOrEmpty() || value.IsNullOrEmpty())
        {
            if (redisKey.IsNullOrEmpty()) LogSkipKeyEmpty(); else LogSkipValueEmpty();
            return false;
        }

        try
        {
            IDatabase db = await GetDb(cancellationToken).NoSync();
            Task<bool> operation = add ? db.SortedSetAddAsync(redisKey, value, score) : db.SortedSetRemoveAsync(redisKey, value);
            return await Await(operation, cancellationToken).NoSync();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, ">> REDIS: Error changing sorted set value for key: {key}", redisKey);
            return false;
        }
    }

    public async ValueTask<bool> RemoveHashField(string redisKey, string field, CancellationToken cancellationToken = default)
    {
        if (redisKey.IsNullOrEmpty() || field.IsNullOrEmpty())
        {
            if (redisKey.IsNullOrEmpty()) LogSkipKeyEmpty(); else LogSkipValueEmpty();
            return false;
        }

        try
        {
            IDatabase db = await GetDb(cancellationToken).NoSync();
            return await Await(db.HashDeleteAsync(redisKey, field), cancellationToken).NoSync();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, ">> REDIS: Error removing hash field for key: {key}", redisKey);
            return false;
        }
    }

    public async ValueTask<bool> ExecuteTransaction(Action<ITransaction> configure, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);

        try
        {
            IDatabase db = await GetDb(cancellationToken).NoSync();
            ITransaction transaction = db.CreateTransaction();
            configure(transaction);
            return await Await(transaction.ExecuteAsync(), cancellationToken).NoSync();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, ">> REDIS: Error executing transaction");
            return false;
        }
    }

    private static IReadOnlyList<string> ConvertValues(RedisValue[] values)
    {
        var result = new string[values.Length];
        for (var i = 0; i < values.Length; i++)
            result[i] = values[i].ToString();

        return result;
    }
}
