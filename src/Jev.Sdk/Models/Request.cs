using System.Text.Json.Serialization;

namespace Jev.Sdk.Models;
public sealed record Request
{
    public Request(){}
    [JsonInclude]
    [JsonPropertyName("state")]
    public object? State { get; private set; }
    [JsonInclude]
    [JsonPropertyName("model")]
    public string? Model { get; private set; }
    [JsonInclude]
    [JsonPropertyName("questions")]
    public Dictionary<string, Question>? Questions { get; private set; }

    public void SetState(object input)
    {
        ArgumentNullException.ThrowIfNull(input);

        State = input;
    }

    public void SetModel(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        Model = input;
    }

    public void SetQuestions(Dictionary<string, Question> input)
    {
        ArgumentNullException.ThrowIfNull(input);

        Questions = input;
    }
}
