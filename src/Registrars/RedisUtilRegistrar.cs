using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Redis.Client.Registrars;
using Soenneker.Redis.Util.Abstract;
using Soenneker.Utils.BackgroundQueue.Registrars;
using Soenneker.Utils.MemoryStream.Registrars;

namespace Soenneker.Redis.Util.Registrars;

/// <summary>
/// The general purpose utility library leveraging Redis for all of your caching needs
/// </summary>
public static class RedisUtilRegistrar
{
    /// <summary>
    /// Adds <see cref="IRedisUtil"/> as a singleton service. <para/>
    /// </summary>
    public static void AddRedisUtilAsSingleton(this IServiceCollection services)
    {
        services.AddBackgroundQueue();
        services.AddRedisClientAsSingleton();
        services.TryAddSingleton<IRedisUtil, RedisUtil>();
    }
}