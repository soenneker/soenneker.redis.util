using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Soenneker.Extensions.String;
using Soenneker.Redis.Util.Abstract;
using Soenneker.Redis.Util.Tests.Dtos;
using Soenneker.Tests.FixturedUnit;
using Xunit;


namespace Soenneker.Redis.Util.Tests;

[Collection("Collection")]
public class RedisUtilTests : FixturedUnitTest
{
    private readonly IRedisUtil _util;

    public RedisUtilTests(Fixture fixture, ITestOutputHelper outputHelper) : base(fixture, outputHelper)
    {
        _util = Resolve<IRedisUtil>();
    }

    [Fact]
    public async Task Set_item_should_exist()
    {
        string key = Faker.Random.AlphaNumeric(20);
        string? value = Faker.Random.AlphaNumeric(20);

        await _util.Set("test", key, value, cancellationToken: CancellationToken);

        Logger.LogInformation("Testing");

        string? rtnValue = await _util.GetString("test", key, CancellationToken);
        rtnValue.Should().Be(value);
    }

    [Fact]
    public async Task Set_without_key_should_resolve_with_get()
    {
        await _util.Set("test", null, "1", cancellationToken: CancellationToken);

        string? rtnValue = await _util.GetString("test", CancellationToken);

        rtnValue.Should().Be("1");
    }

    [Fact]
    public async Task Set_json_item_should_exist()
    {
        var doc = AutoFaker.Generate<TestDocument>();
        await _util.Set("test", doc.Id, doc, cancellationToken: CancellationToken);

        var result = await _util.Get<TestDocument>("test", doc.Id, CancellationToken);
        result.Should().NotBeNull();
        result!.CreatedAt.Should().Be(doc.CreatedAt);
    }

    [Fact]
    public async Task Removed_cache_item_should_not_exist()
    {
        string key = Faker.Random.AlphaNumeric(20);
        string? value = Faker.Random.AlphaNumeric(20);

        await _util.Set("test", key, value, cancellationToken: CancellationToken);

        await _util.Remove("test", key, cancellationToken: CancellationToken);

        string? rtnValue = await _util.GetString("test", key, CancellationToken);
        rtnValue.Should().BeNull();
    }

    [Fact]
    public void BuildKey_should_produce_expected()
    {
        string? key = Faker.Random.AlphaNumeric(25);

        string result = RedisUtil.BuildKey("test", key);

        result.Should().Be($"test:{key}");
    }

    [Fact]
    public void BuildKey_multiple_should_produce_expected()
    {
        string? key1 = Faker.Random.AlphaNumeric(25);
        string? key2 = Faker.Random.AlphaNumeric(25);

        string result = RedisUtil.BuildKey("test", key1, key2);

        result.Should().Be($"test:{key1}:{key2}");
    }

    [Fact]
    public void BuildKey_with_malicious_key_should_produce_expected()
    {
        const string key = " ; ' test";
        string result = RedisUtil.BuildKey("test", key);

        string? escaped = key.ToEscaped();

        result.Should().Be($"test:{escaped}");
    }

    [Fact]
    public void Get_key_with_multiple_should_produce_expected()
    {
        string? key1 = Faker.Random.AlphaNumeric(25);
        string? key2 = Faker.Random.AlphaNumeric(25);

        string result = RedisUtil.BuildKey("test", key1, key2);

        result.Should().Be($"test:{key1}:{key2}");
    }
}