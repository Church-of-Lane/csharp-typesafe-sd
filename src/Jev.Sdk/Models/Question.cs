using System.Text.Json.Serialization;

namespace Jev.Sdk.Models;
public sealed record Question
{
    public enum QuestionType
    {
        Noul,
        Choice,
        Score,
    }
    private static readonly Dictionary<QuestionType, string> Converter = new()
    {
        {QuestionType.Noul, "noul"},
        {QuestionType.Choice, "choice"},
        {QuestionType.Score, "score"},
    };

    public Question(){}
    [JsonInclude]
    [JsonPropertyName("type")]
    public string? Type { get; private set; }
    [JsonInclude]
    [JsonPropertyName("instructions")]
    public object? Instructions { get; private set; }
    [JsonInclude]
    [JsonPropertyName("criteria")]
    public object? Criteria { get; private set; }

    public void SetQuestionType(QuestionType input)
    {
        if (!Enum.IsDefined(input))
        {
            throw new ArgumentOutOfRangeException(nameof(input), input, "Unknown question type.");
        }

        Type = Converter[input];
    }
    public void SetQuestionType(int type) => SetQuestionType((QuestionType)type);

    public void SetCriteria(object input)
    {
        ArgumentNullException.ThrowIfNull(input);

        Criteria = input;
    }

    public void SetInstructions(object input)
    {
        ArgumentNullException.ThrowIfNull(input);

        Instructions = input;
    }
}
