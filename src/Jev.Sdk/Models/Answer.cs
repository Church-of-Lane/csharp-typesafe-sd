using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jev.Sdk.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(NoulAnswer), "noul")]
[JsonDerivedType(typeof(ChoiceAnswer), "choice")]
[JsonDerivedType(typeof(ScoreAnswer), "score")]
public abstract record Answer
{
    [JsonIgnore]
    public string? Type { get; private set; }

    public void SetAnswerType(string input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        Type = input;
    }

    public string? GetAnswerType() => Type;
}

public sealed record NoulAnswer : Answer
{
    public NoulAnswer() => SetAnswerType("noul");

    [JsonInclude]
    [JsonPropertyName("noul")]
    public double? Noul { get; private set; }

    public void SetNoul(double input) => Noul = input;

    public double? GetNoul() => Noul;
}

public sealed record ChoiceAnswer : Answer
{
    public ChoiceAnswer() => SetAnswerType("choice");

    [JsonInclude]
    [JsonPropertyName("choice")]
    public string? Choice { get; private set; }

    [JsonInclude]
    [JsonPropertyName("probabilities")]
    public Dictionary<string, double>? Probabilities { get; private set; }

    [JsonInclude]
    [JsonPropertyName("confidence")]
    public double? Confidence { get; private set; }

    public void SetChoice(string input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        Choice = input;
    }

    public void SetProbabilities(IReadOnlyDictionary<string, double> input)
    {
        ArgumentNullException.ThrowIfNull(input);

        Probabilities = new Dictionary<string, double>(input);
    }

    public void SetConfidence(double input) => Confidence = input;

    public string? GetChoice() => Choice;

    public Dictionary<string, double>? GetProbabilities() => Probabilities;

    public double? GetConfidence() => Confidence;
}

public sealed record ScoreAnswer : Answer
{
    public ScoreAnswer() => SetAnswerType("score");

    [JsonInclude]
    [JsonPropertyName("score")]
    public double? Score { get; private set; }

    [JsonInclude]
    [JsonPropertyName("legend")]
    [JsonConverter(typeof(LegendConverter))]
    public Dictionary<string, string>? Legend { get; private set; }

    [JsonInclude]
    [JsonPropertyName("probabilities")]
    public Dictionary<string, double>? Probabilities { get; private set; }

    [JsonInclude]
    [JsonPropertyName("confidence")]
    public double? Confidence { get; private set; }

    public void SetScore(double input) => Score = input;

    public void SetLegend(IReadOnlyDictionary<string, string> input)
    {
        ArgumentNullException.ThrowIfNull(input);

        Legend = new Dictionary<string, string>(input);
    }

    public void SetProbabilities(IReadOnlyDictionary<string, double> input)
    {
        ArgumentNullException.ThrowIfNull(input);

        Probabilities = new Dictionary<string, double>(input);
    }

    public void SetConfidence(double input) => Confidence = input;

    public double? GetScore() => Score;

    public Dictionary<string, string>? GetLegend() => Legend;

    public Dictionary<string, double>? GetProbabilities() => Probabilities;

    public double? GetConfidence() => Confidence;
}

// Reads the legend as text. A string value stays as it is; an object or array value is kept as its raw JSON text.
internal sealed class LegendConverter : JsonConverter<Dictionary<string, string>>
{
    public override Dictionary<string, string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("The legend must be a JSON object.");
        }

        Dictionary<string, string> legend = new();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return legend;
            }

            string key = reader.GetString()!;
            reader.Read();
            legend[key] = reader.TokenType == JsonTokenType.String
                ? reader.GetString()!
                : JsonElement.ParseValue(ref reader).GetRawText();
        }

        throw new JsonException("The legend ended unexpectedly.");
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, string> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (KeyValuePair<string, string> entry in value)
        {
            writer.WriteString(entry.Key, entry.Value);
        }

        writer.WriteEndObject();
    }
}
