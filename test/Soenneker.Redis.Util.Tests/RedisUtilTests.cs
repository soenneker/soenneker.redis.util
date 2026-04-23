using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Soenneker.Extensions.String;
using Soenneker.Redis.Util.Abstract;
using Soenneker.Redis.Util.Tests.Dtos;
using Soenneker.Tests.HostedUnit;


namespace Soenneker.Redis.Util.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public class RedisUtilTests : HostedUnitTest
{
    private readonly IRedisUtil _util;

    public RedisUtilTests(Host host) : base(host)
    {
        _util = Resolve<IRedisUtil>();
    }

    [Test]
    public async Task Set_item_should_exist()
    {
        string key = Faker.Random.AlphaNumeric(20);
        string? value = Faker.Random.AlphaNumeric(20);

        await _util.Set("test", key, value, cancellationToken: System.Threading.CancellationToken.None);

        Logger.LogInformation("Testing");

        string? rtnValue = await _util.GetString("test", key, System.Threading.CancellationToken.None);
        rtnValue.Should().Be(value);
    }

    [Test]
    public async Task Set_without_key_should_resolve_with_get()
    {
        await _util.Set("test", null, "1", cancellationToken: System.Threading.CancellationToken.None);

        string? rtnValue = await _util.GetString("test", System.Threading.CancellationToken.None);

        rtnValue.Should().Be("1");
    }

    [Test]
    public async Task Set_json_item_should_exist()
    {
        var doc = AutoFaker.Generate<TestDocument>();
        await _util.Set("test", doc.Id, doc, cancellationToken: System.Threading.CancellationToken.None);

        var result = await _util.Get<TestDocument>("test", doc.Id, System.Threading.CancellationToken.None);
        result.Should().NotBeNull();
        result!.CreatedAt.Should().Be(doc.CreatedAt);
    }

    [Test]
    public async Task Removed_cache_item_should_not_exist()
    {
        string key = Faker.Random.AlphaNumeric(20);
        string? value = Faker.Random.AlphaNumeric(20);

        await _util.Set("test", key, value, cancellationToken: System.Threading.CancellationToken.None);

        await _util.Remove("test", key, cancellationToken: System.Threading.CancellationToken.None);

        string? rtnValue = await _util.GetString("test", key, System.Threading.CancellationToken.None);
        rtnValue.Should().BeNull();
    }

    [Test]
    public void BuildKey_should_produce_expected()
    {
        string? key = Faker.Random.AlphaNumeric(25);

        string result = RedisUtil.BuildKey("test", key);

        result.Should().Be($"test:{key}");
    }

    [Test]
    public void BuildKey_multiple_should_produce_expected()
    {
        string? key1 = Faker.Random.AlphaNumeric(25);
        string? key2 = Faker.Random.AlphaNumeric(25);

        string result = RedisUtil.BuildKey("test", key1, key2);

        result.Should().Be($"test:{key1}:{key2}");
    }

    [Test]
    public void BuildKey_with_malicious_key_should_produce_expected()
    {
        const string key = " ; ' test";
        string result = RedisUtil.BuildKey("test", key);

        string? escaped = key.ToEscaped();

        result.Should().Be($"test:{escaped}");
    }

    [Test]
    public void Get_key_with_multiple_should_produce_expected()
    {
        string? key1 = Faker.Random.AlphaNumeric(25);
        string? key2 = Faker.Random.AlphaNumeric(25);

        string result = RedisUtil.BuildKey("test", key1, key2);

        result.Should().Be($"test:{key1}:{key2}");
    }
}

