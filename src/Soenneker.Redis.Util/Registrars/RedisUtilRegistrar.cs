using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Redis.Client.Registrars;
using Soenneker.Redis.Util.Abstract;
using Soenneker.Utils.BackgroundQueue.Registrars;

namespace Soenneker.Redis.Util.Registrars;

/// <summary>
/// Registers the Redis utility and its shared transport dependencies.
/// </summary>
public static class RedisUtilRegistrar
{
    /// <summary>
    /// Adds <see cref="IRedisUtil"/>, the Redis client, and the background queue as singleton services.
    /// </summary>
    /// <param name="services">Service collection that receives the registration.</param>
    /// <returns>The same service collection, so additional registrations can be chained.</returns>
    public static IServiceCollection AddRedisUtilAsSingleton(this IServiceCollection services)
    {
        services.AddBackgroundQueueAsSingleton()
                .AddRedisClientAsSingleton()
                .TryAddSingleton<IRedisUtil, RedisUtil>();

        return services;
    }

    /// <summary>
    /// Adds a scoped <see cref="IRedisUtil"/> backed by a singleton Redis client and background queue.
    /// </summary>
    /// <param name="services">Service collection that receives the registration.</param>
    /// <returns>The same service collection, so additional registrations can be chained.</returns>
    public static IServiceCollection AddRedisUtilAsScoped(this IServiceCollection services)
    {
        services.AddBackgroundQueueAsSingleton()
                .AddRedisClientAsSingleton()
                .TryAddScoped<IRedisUtil, RedisUtil>();

        return services;
    }
}
