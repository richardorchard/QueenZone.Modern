using System.Text.Json;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class FlexibleStringListJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new FlexibleStringListJsonConverter(),
            new FlexibleNullableStringListJsonConverter()
        }
    };

    [Fact]
    public void NullableConverter_read_null_returns_null()
    {
        var result = JsonSerializer.Deserialize<NullableHolder>("""{"items":null}""", Options);

        Assert.NotNull(result);
        Assert.Null(result.Items);
    }

    [Fact]
    public void NullableConverter_read_empty_array_returns_empty_list()
    {
        var result = JsonSerializer.Deserialize<NullableHolder>("""{"items":[]}""", Options);

        Assert.NotNull(result);
        Assert.Empty(result!.Items!);
    }

    [Fact]
    public void Converter_read_null_returns_empty_list()
    {
        var converter = new FlexibleStringListJsonConverter();
        var reader = new Utf8JsonReader("null"u8);
        Assert.True(reader.Read());

        var result = converter.Read(ref reader, typeof(IReadOnlyList<string>), Options);

        Assert.Empty(result);
    }

    [Fact]
    public void NullableConverter_write_null_writes_json_null()
    {
        var converter = new FlexibleNullableStringListJsonConverter();
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        converter.Write(writer, null, Options);
        writer.Flush();

        Assert.Equal("null", System.Text.Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public void Converter_write_serializes_string_array()
    {
        var converter = new FlexibleStringListJsonConverter();
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        converter.Write(writer, ["Queen", "Brian May"], Options);
        writer.Flush();

        Assert.Equal("""["Queen","Brian May"]""", System.Text.Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public void Parser_coerces_single_string_entity()
    {
        var result = JsonSerializer.Deserialize<NullableHolder>("""{"items":"Queen"}""", Options);

        Assert.Equal(["Queen"], result!.Items);
    }

    [Fact]
    public void Parser_coerces_numbers_booleans_and_object_shapes()
    {
        const string json = """
            {
              "items": [
                42,
                3.5,
                true,
                false,
                { "entity": "Brian May" },
                { "title": "Roger Taylor" },
                { "label": "Adam Lambert" },
                { "custom": "John Deacon" },
                { "type": "band", "id": 1 }
              ]
            }
            """;

        var result = JsonSerializer.Deserialize<NullableHolder>(json, Options);

        Assert.Equal(
        [
            "42",
            "3.5",
            "true",
            "false",
            "Brian May",
            "Roger Taylor",
            "Adam Lambert",
            "John Deacon",
            "band"
        ],
        result!.Items);
    }

    [Fact]
    public void Parser_skips_blank_entries_in_arrays()
    {
        var result = JsonSerializer.Deserialize<NullableHolder>("""{"items":["Queen","","  "]}""", Options);

        Assert.Equal(["Queen"], result!.Items);
    }

    [Fact]
    public void Converters_round_trip_serialized_lists()
    {
        var nullable = new NullableHolder(["Queen", "Brian May"]);
        var nullableJson = JsonSerializer.Serialize(nullable, Options);
        Assert.Contains(""""Queen"""", nullableJson);
        Assert.Equal(nullable.Items, JsonSerializer.Deserialize<NullableHolder>(nullableJson, Options)!.Items);

        var required = new RequiredHolder(["Queen"]);
        var requiredJson = JsonSerializer.Serialize(required, Options);
        Assert.Equal(required.Items, JsonSerializer.Deserialize<RequiredHolder>(requiredJson, Options)!.Items);

        var nullHolder = new NullableHolder(null);
        var nullJson = JsonSerializer.Serialize(nullHolder, Options);
        Assert.Contains("null", nullJson);
        Assert.Null(JsonSerializer.Deserialize<NullableHolder>(nullJson, Options)!.Items);
    }

    private sealed record NullableHolder(IReadOnlyList<string>? Items);

    private sealed record RequiredHolder(IReadOnlyList<string> Items);
}
