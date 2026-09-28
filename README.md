# Jev SDK

A C# client for the [TypeSafe System One API](https://docs.typesafe.ai). You send some text (the *state*) and one or more questions, and the API answers each question with probabilities.

> **Status: early version.** Yes/no (`Noul`) questions have been tested against the live API. `Choice` and `Score` questions follow the API docs but have not been tested live yet.

## Requirements

- .NET 8 SDK or newer (the library targets `net8.0` and `net10.0`)
- A TypeSafe API key

## Installation

The SDK is not on NuGet yet. Add a project reference to it:

```powershell
dotnet add <your-project> reference path/to/Csharp-Jev-SDK/src/Jev.Sdk
```

## Set your API key

The SDK reads the key from the `JEV_API_KEY` setting. Use either of these:

**A `.env` file** in your app's folder (or any parent folder):

```
JEV_API_KEY=your-api-key-here
```

**An environment variable:**

```powershell
$env:JEV_API_KEY = "your-api-key-here"
```

If both exist, the environment variable wins. Never commit your key. This repository already ignores `.env` in its `.gitignore`; add the same line to yours.

## Quick start

```csharp
using Jev.Sdk;
using Jev.Sdk.Exceptions;
using Jev.Sdk.Models;

// 1. Build a question.
var question = new Question();
question.SetQuestionType(Question.QuestionType.Noul);
question.SetInstructions("Does the customer explicitly request a refund?");
question.SetCriteria(new
{
    @true = "The customer explicitly asks for money back or a refund.",
    @false = "The customer complains but does not request money back.",
});

// 2. Build the request. The key ("requests_refund") is a name you choose;
//    the answer comes back under the same key.
var request = new Request();
request.SetState("I was charged twice for this order. Please refund the extra charge.");
request.SetModel(JevClientOptions.GetModel());
request.SetQuestions(new Dictionary<string, Question> { ["requests_refund"] = question });

// 3. Call the API and read the answer.
try
{
    using var client = new JevClient();
    Response response = await client.EvaluateAsync(request);

    if (response.GetAnswers()?["requests_refund"] is NoulAnswer answer)
    {
        Console.WriteLine($"Refund requested? {answer.GetNoul():P0}");
    }

    Console.WriteLine($"Tokens: {response.GetUsage()?.GetInputTokens()} in, {response.GetUsage()?.GetOutputTokens()} out");
}
catch (JevApiException ex)
{
    Console.WriteLine($"API error {(int)ex.StatusCode}: {ex.ResponseBody}");
}
```

The **state** is the content to judge. It can be text, an object or a list (`SetState(object)`). You can ask several questions in one request by adding more entries to the dictionary.

## Question types

| Type | Use it for | Criteria | Answer class | Read it with |
|---|---|---|---|---|
| `Noul` | Yes/no | Required. Keys `true` and `false`, each a description. | `NoulAnswer` | `GetNoul()`: probability of yes, 0 to 1 |
| `Choice` | Pick one option | Required. Option name → description. | `ChoiceAnswer` | `GetChoice()`, `GetProbabilities()`, `GetConfidence()` |
| `Score` | Rate on a scale | Optional. A list of level descriptions. | `ScoreAnswer` | `GetScore()`, `GetLegend()`, `GetProbabilities()`, `GetConfidence()` |

Instructions are either a plain string or an object with a `question` and an optional `focus`. See the [TypeSafe docs](https://docs.typesafe.ai/concepts/how-to-build-with-system-one) for the full list of fields.

**Choice example:**

```csharp
var team = new Question();
team.SetQuestionType(Question.QuestionType.Choice);
team.SetInstructions("Which team should handle this?");
team.SetCriteria(new Dictionary<string, string>
{
    ["billing"] = "Payments, invoicing, refunds",
    ["technical"] = "Bugs, outages, integrations",
    ["sales"] = "Pricing, upgrades, new accounts",
});

// after the call:
if (response.GetAnswers()?["team"] is ChoiceAnswer choice)
{
    Console.WriteLine($"{choice.GetChoice()} ({choice.GetConfidence():P0} confident)");
}
```

**Score example:**

```csharp
var mood = new Question();
mood.SetQuestionType(Question.QuestionType.Score);
mood.SetInstructions("How frustrated is the customer?");
mood.SetCriteria(new List<string> { "Calm", "Frustrated", "Very angry" });

// after the call:
if (response.GetAnswers()?["mood"] is ScoreAnswer score)
{
    Console.WriteLine($"Frustration score: {score.GetScore()}");
}
```

## Choosing the model

The default model is `jev-latest`. To change it for every request you build with `JevClientOptions.GetModel()`:

```csharp
JevClientOptions.SetModel("jev-1.13.0");
```

You can also set it for a single request with `request.SetModel("...")`. The client does not add a model for you, so always call `SetModel` on the request.

## Errors

| Exception | When |
|---|---|
| `JevApiException` | The API returned an error. `StatusCode` and `ResponseBody` tell you what happened, for example `401` for a wrong key. |
| `InvalidOperationException` | `JEV_API_KEY` is not set. Thrown by `new JevClient()`. |
| `JsonException` | The API replied with an empty or unreadable body. |

## Project layout

```
src/Jev.Sdk/
  JevClient.cs, IJevClient.cs   The client and its interface
  JevClientOptions.cs           API key and default model
  Models/                       Request, Question, Response, Answer types, Usage
  Exceptions/                   JevApiException
```

## Building

```powershell
dotnet build
```

Warnings are treated as errors in this repository (see `Directory.Build.props`).

## Not done yet

- Tests
- A dependency injection helper (`AddJevClient`)
- Cancellation tokens
- A NuGet package
