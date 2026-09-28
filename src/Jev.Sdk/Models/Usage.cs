using System.Text.Json.Serialization;

namespace Jev.Sdk.Models;

public sealed record Usage
{
    public Usage(){}

    [JsonInclude]
    [JsonPropertyName("input_tokens")]
    public int? InputTokens { get; private set; }

    [JsonInclude]
    [JsonPropertyName("output_tokens")]
    public int? OutputTokens { get; private set; }

    public void SetInputTokens(int input) => InputTokens = input;

    public void SetOutputTokens(int input) => OutputTokens = input;

    public int? GetInputTokens() => InputTokens;

    public int? GetOutputTokens() => OutputTokens;
}
