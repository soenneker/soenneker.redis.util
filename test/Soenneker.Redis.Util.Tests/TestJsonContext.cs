using System.Text.Json;
using System.Text.Json.Serialization;

namespace 
Soenneker.Redis.Util.Tests
;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(Dtos.TestDocument))]
[JsonSerializable(typeof(string))]
internal partial class TestJsonContext : JsonSerializerContext;
