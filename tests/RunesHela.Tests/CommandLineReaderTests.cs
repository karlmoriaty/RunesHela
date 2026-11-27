using RunesHela.Parsing;
using Xunit;

namespace RunesHela.Tests;

public sealed class CommandLineReaderTests
{
    [Fact]
    public void ReadsLongOptionWithSeparateValue()
    {
        var reader = new CommandLineReader(new[] { "--name", "runes" });

        Assert.True(reader.TryRead(out var token));
        Assert.Equal(CommandLineTokenKind.Option, token.Kind);
        Assert.Equal("name", token.Name.ToString());
        Assert.False(token.HasValue);

        Assert.True(reader.TryReadValue(token, out var value));
        Assert.Equal("runes", value.ToString());
        Assert.False(reader.TryRead(out _));
    }

    [Fact]
    public void ReadsLongOptionWithInlineValue()
    {
        var reader = new CommandLineReader(new[] { "--name=runes" });

        Assert.True(reader.TryRead(out var token));
        Assert.Equal("name", token.Name.ToString());
        Assert.True(token.HasValue);
        Assert.Equal("runes", token.Value.ToString());

        Assert.True(reader.TryReadValue(token, out var value));
        Assert.Equal("runes", value.ToString());
    }

    [Fact]
    public void ReadsEmptyInlineValue()
    {
        var reader = new CommandLineReader(new[] { "--name=" });

        Assert.True(reader.TryRead(out var token));
        Assert.True(token.HasValue);
        Assert.Equal(string.Empty, token.Value.ToString());
    }

    [Fact]
    public void ReadsShortOptionWithSeparateValue()
    {
        var reader = new CommandLineReader(new[] { "-n", "runes" });

        Assert.True(reader.TryRead(out var token));
        Assert.Equal("n", token.Name.ToString());
        Assert.False(token.HasValue);

        Assert.True(reader.TryReadValue(token, out var value));
        Assert.Equal("runes", value.ToString());
    }

    [Fact]
    public void ReadsShortOptionWithInlineValue()
    {
        var reader = new CommandLineReader(new[] { "-n=runes" });

        Assert.True(reader.TryRead(out var token));
        Assert.Equal("n", token.Name.ToString());
        Assert.True(token.HasValue);
        Assert.Equal("runes", token.Value.ToString());
    }

    [Fact]
    public void ReadsFlagWithoutConsumingValue()
    {
        var reader = new CommandLineReader(new[] { "--verbose", "build" });

        Assert.True(reader.TryRead(out var flag));
        Assert.Equal(CommandLineTokenKind.Option, flag.Kind);
        Assert.Equal("verbose", flag.Name.ToString());

        Assert.True(reader.TryRead(out var positional));
        Assert.Equal(CommandLineTokenKind.Positional, positional.Kind);
        Assert.Equal("build", positional.Value.ToString());
    }

    [Fact]
    public void ReadsNegativeNumbersAsPositional()
    {
        var reader = new CommandLineReader(new[] { "-1", "-2.5", "-.5" });

        Assert.True(reader.TryRead(out var first));
        Assert.Equal(CommandLineTokenKind.Positional, first.Kind);
        Assert.Equal("-1", first.Value.ToString());

        Assert.True(reader.TryRead(out var second));
        Assert.Equal(CommandLineTokenKind.Positional, second.Kind);
        Assert.Equal("-2.5", second.Value.ToString());

        Assert.True(reader.TryRead(out var third));
        Assert.Equal(CommandLineTokenKind.Positional, third.Kind);
        Assert.Equal("-.5", third.Value.ToString());
    }

    [Fact]
    public void TreatsSingleDashAsPositional()
    {
        var reader = new CommandLineReader(new[] { "-" });

        Assert.True(reader.TryRead(out var token));
        Assert.Equal(CommandLineTokenKind.Positional, token.Kind);
        Assert.Equal("-", token.Value.ToString());
    }

    [Fact]
    public void TerminatorTurnsFollowingOptionsIntoPositional()
    {
        var reader = new CommandLineReader(new[] { "--", "--name", "runes" });

        Assert.True(reader.TryRead(out var terminator));
        Assert.Equal(CommandLineTokenKind.Terminator, terminator.Kind);

        Assert.True(reader.TryRead(out var first));
        Assert.Equal(CommandLineTokenKind.Positional, first.Kind);
        Assert.Equal("--name", first.Value.ToString());

        Assert.True(reader.TryRead(out var second));
        Assert.Equal("runes", second.Value.ToString());
    }

    [Fact]
    public void MissingValueReturnsFalse()
    {
        var reader = new CommandLineReader(new[] { "--name" });

        Assert.True(reader.TryRead(out var token));
        Assert.False(reader.TryReadValue(token, out _));
    }

    [Fact]
    public void TerminatorIsNotConsumedAsValue()
    {
        var reader = new CommandLineReader(new[] { "--name", "--", "value" });

        Assert.True(reader.TryRead(out var token));
        Assert.False(reader.TryReadValue(token, out _));

        Assert.True(reader.TryRead(out var terminator));
        Assert.Equal(CommandLineTokenKind.Terminator, terminator.Kind);

        Assert.True(reader.TryRead(out var positional));
        Assert.Equal("value", positional.Value.ToString());
    }

    [Fact]
    public void OptionLikeValueIsConsumedWhenSeparated()
    {
        var reader = new CommandLineReader(new[] { "--name", "--other" });

        Assert.True(reader.TryRead(out var token));
        Assert.True(reader.TryReadValue(token, out var value));
        Assert.Equal("--other", value.ToString());
    }

    [Fact]
    public void KeepsRepeatedOptionsAsSeparateTokens()
    {
        var reader = new CommandLineReader(new[] { "--tag", "a", "--tag", "b" });

        Assert.True(reader.TryRead(out var first));
        Assert.Equal("tag", first.Name.ToString());
        Assert.True(reader.TryReadValue(first, out var firstValue));
        Assert.Equal("a", firstValue.ToString());

        Assert.True(reader.TryRead(out var second));
        Assert.Equal("tag", second.Name.ToString());
        Assert.True(reader.TryReadValue(second, out var secondValue));
        Assert.Equal("b", secondValue.ToString());
    }

    [Theory]
    [InlineData("--name", "name")]
    [InlineData("--timeout-seconds", "timeout-seconds")]
    [InlineData("-t", "t")]
    public void ReturnsOptionNameWithoutDashes(string argument, string expected)
    {
        var reader = new CommandLineReader(new[] { argument });

        Assert.True(reader.TryRead(out var token));
        Assert.Equal(expected, token.Name.ToString());
    }
}
