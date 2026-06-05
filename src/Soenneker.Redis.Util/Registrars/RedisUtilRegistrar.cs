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
    public static IServiceCollection AddRedisUtilAsSingleton(this IServiceCollection services)
    {
        services.AddBackgroundQueueAsSingleton()
                .AddRedisClientAsSingleton()
                .TryAddSingleton<IRedisUtil, RedisUtil>();

        return services;
    }

    /// <summary>
    /// Adds redis util as scoped.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The result of the operation.</returns>
    public static IServiceCollection AddRedisUtilAsScoped(this IServiceCollection services)
    {
        services.AddBackgroundQueueAsSingleton()
                .AddRedisClientAsSingleton()
                .TryAddScoped<IRedisUtil, RedisUtil>();

        return services;
    }
}