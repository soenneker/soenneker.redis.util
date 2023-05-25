using Microsoft.Extensions.Configuration;
using System.IO;

namespace Soenneker.Redis.Util.Tests;

public class UnitTestUtil
{
    public static IConfiguration BuildConfig(string? childPath = null)
    {
        string directory;
        
        if (childPath != null)
            directory = Path.Combine(Directory.GetCurrentDirectory(), childPath);
        else
            directory = Directory.GetCurrentDirectory();

        const string baseAppSettings = "appsettings.json";
        string? environmentAppSettings = null;

        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(directory)
            .AddJsonFile(baseAppSettings);

        if (environmentAppSettings != null)
            builder.AddJsonFile(environmentAppSettings);

        IConfiguration configRoot = builder.Build();

        return configRoot;
    }
}