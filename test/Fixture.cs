using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Soenneker.Fixtures.Unit;
using Soenneker.Redis.Util.Registrars;
using Soenneker.Utils.Test;

namespace Soenneker.Redis.Util.Tests;

public class Fixture : UnitFixture
{
    public override async Task InitializeAsync()
    {
        SetupIoC(Services);

        await base.InitializeAsync();
    }

    private static void SetupIoC(IServiceCollection services)
    {
        services.AddLogging(builder => { builder.AddSerilog(dispose: true); });

        IConfiguration config = TestUtil.BuildConfig();
        services.AddSingleton(config);

        services.AddRedisUtilAsSingleton();
    }
}