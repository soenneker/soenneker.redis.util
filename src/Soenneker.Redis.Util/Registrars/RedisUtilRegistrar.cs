using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Redis.Client.Registrars;
using Soenneker.Redis.Util.Abstract;
using Soenneker.Utils.BackgroundQueue.Registrars;

namespace Soenneker.Redis.Util.Registrars;

/// <summary>
/// The general purpose utility library leveraging Redis for all of your caching needs
/// </summary>
public static class RedisUtilRegistrar
{
    /// <summary>
    /// Adds <see cref="IRedisUtil"/> as a singleton service. <para/>
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
    /// Registers Redis Util with a scoped lifetime.
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
