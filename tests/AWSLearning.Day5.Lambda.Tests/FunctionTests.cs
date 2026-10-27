using System.Text.Json;
using Amazon.Lambda.TestUtilities;
using Xunit;

namespace AWSLearning.Day5.Lambda.Tests;

public sealed class FunctionTests
{
    [Fact]
    public void LambdaSerializerRoundTripsJsonStringAndSummary()
    {
        var serializer = new Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer();
        using var input = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("\"Hello AWS Lambda\""));
        string value = serializer.Deserialize<string>(input);
        var result = new Function().FunctionHandler(value, new TestLambdaContext());
        using var output = new MemoryStream();
        serializer.Serialize(result, output);
        using var json = JsonDocument.Parse(output.ToArray());

        Assert.Equal(3, json.RootElement.GetProperty("WordCount").GetInt32());
        Assert.Equal(16, json.RootElement.GetProperty("CharacterCount").GetInt32());
        Assert.Equal("Hello AWS Lambda", json.RootElement.GetProperty("Preview").GetString());
    }

    [Theory]
    [InlineData("Hello AWS Lambda", 3, 16)]
    [InlineData("", 0, 0)]
    [InlineData(" \t\r\n ", 0, 5)]
    [InlineData("one\ttwo\r\nthree", 3, 14)]
    [InlineData("😀", 1, 2)]
    public void SummarizesWordsAndUtf16Units(string input, int words, int units)
    {
        var result = new Function().FunctionHandler(input, new TestLambdaContext());
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result));

        Assert.Equal(words, json.RootElement.GetProperty("WordCount").GetInt32());
        Assert.Equal(units, json.RootElement.GetProperty("CharacterCount").GetInt32());
        Assert.Equal(input, json.RootElement.GetProperty("Preview").GetString());
    }

    [Theory]
    [InlineData(99)]
    [InlineData(100)]
    [InlineData(101)]
    public void LimitsPreviewToOneHundredUtf16Units(int length)
    {
        var result = new Function().FunctionHandler(new string('a', length), new TestLambdaContext());
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result));

        Assert.Equal(new string('a', Math.Min(length, 100)), json.RootElement.GetProperty("Preview").GetString());
        Assert.Equal(length, json.RootElement.GetProperty("CharacterCount").GetInt32());
    }

    [Fact]
    public void PreviewDoesNotSplitSurrogatePair()
    {
        var input = new string('a', 99) + "😀tail";
        var result = new Function().FunctionHandler(input, new TestLambdaContext());
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result));

        Assert.Equal(new string('a', 99), json.RootElement.GetProperty("Preview").GetString());
    }

    [Fact]
    public void RejectsNullInput()
    {
        Assert.Throws<ArgumentNullException>(() => new Function().FunctionHandler(null!, new TestLambdaContext()));
    }

    [Fact]
    public void RejectsNullContext()
    {
        Assert.Throws<ArgumentNullException>(() => new Function().FunctionHandler("hello", null!));
    }

    [Fact]
    public void WritesInvocationLog()
    {
        var logger = new TestLambdaLogger();
        new Function().FunctionHandler("hello world", new TestLambdaContext { Logger = logger });

        Assert.Contains("2 words", logger.Buffer.ToString());
    }
}
