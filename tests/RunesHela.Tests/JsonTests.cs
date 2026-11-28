using System.Text;
using RunesHela;
using Xunit;

namespace RunesHela.Tests;

public sealed class JsonTests
{
    [Fact]
    public void ParsesNestedDocuments()
    {
        var root = JsonParser.Parse("""
            {
              "deploy": {
                "environment": "live",
                "count": 3,
                "ratio": -1.5e2,
                "enabled": true,
                "missing": null,
                "tags": ["a", "b"]
              }
            }
            """);

        Assert.Equal(JsonKind.Object, root.Kind);
        Assert.True(root.TryGetProperty("deploy", out var deploy));
        Assert.True(deploy.TryGetProperty("environment", out var environment));
        Assert.Equal("live", environment.Text);
        Assert.True(deploy.TryGetProperty("count", out var count));
        Assert.True(count.TryGetInt32(out var number));
        Assert.Equal(3, number);
        Assert.True(deploy.TryGetProperty("ratio", out var ratio));
        Assert.Equal("-1.5e2", ratio.Text);
        Assert.True(deploy.TryGetProperty("enabled", out var enabled));
        Assert.Equal(JsonKind.Boolean, enabled.Kind);
        Assert.True(enabled.Text == "true");
        Assert.True(deploy.TryGetProperty("missing", out var missing));
        Assert.Equal(JsonKind.Null, missing.Kind);
        Assert.True(deploy.TryGetProperty("tags", out var tags));
        Assert.Equal(2, tags.Items!.Count);
        Assert.Equal("b", tags.Items[1].Text);
    }

    [Fact]
    public void HandlesEscapesAndUnicode()
    {
        var root = JsonParser.Parse("""{"text":"line\nquote\"slash\\tab\t\u4e2d"}""");

        Assert.True(root.TryGetProperty("text", out var text));
        Assert.Equal("line\nquote\"slash\\tab\t中", text.Text);
    }

    [Fact]
    public void AllowsTrailingCommas()
    {
        var root = JsonParser.Parse("""{"a":1,"b":[1,2,],}""");

        Assert.True(root.TryGetProperty("b", out var items));
        Assert.Equal(2, items.Items!.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("""{"a":}""")]
    [InlineData("""{"a":1}extra""")]
    [InlineData("""{"a":01}""")]
    [InlineData("""{"a":"unterminated}""")]
    public void RejectsInvalidDocuments(string json)
    {
        var exception = Assert.Throws<JsonParseException>(() => JsonParser.Parse(json));

        Assert.True(exception.Line >= 1);
        Assert.True(exception.Column >= 1);
    }

    [Fact]
    public void WritesEscapedJson()
    {
        var builder = new StringBuilder();
        JsonWriter.WriteString(builder, "a\n\"b\"\\c");

        Assert.Equal("\"a\\n\\\"b\\\"\\\\c\"", builder.ToString());
    }

    [Fact]
    public void RoundTripsValues()
    {
        var root = JsonParser.Parse("""{"a":[1,true,null,"x"]}""");
        var json = JsonWriter.ToJson(root);

        Assert.Equal("""{"a":[1,true,null,"x"]}""", json);
    }
}
