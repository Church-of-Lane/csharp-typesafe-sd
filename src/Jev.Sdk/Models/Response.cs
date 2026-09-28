using System.Text.Json.Serialization;

namespace Jev.Sdk.Models;

public sealed record Response
{
    public Response(){}

    [JsonInclude]
    [JsonPropertyName("model")]
    public string? Model { get; private set; }

    [JsonInclude]
    [JsonPropertyName("answers")]
    public Dictionary<string, Answer>? Answers { get; private set; }

    [JsonInclude]
    [JsonPropertyName("usage")]
    public Usage? Usage { get; private set; }

    public void SetModel(string input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        Model = input;
    }

    public void SetAnswers(IReadOnlyDictionary<string, Answer> input)
    {
        ArgumentNullException.ThrowIfNull(input);

        Answers = new Dictionary<string, Answer>(input);
    }

    public void SetUsage(Usage input)
    {
        ArgumentNullException.ThrowIfNull(input);

        Usage = input;
    }

    public string? GetModel() => Model;

    public Dictionary<string, Answer>? GetAnswers() => Answers;

    public Usage? GetUsage() => Usage;
}
