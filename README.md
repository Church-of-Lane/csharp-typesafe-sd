# Jev SDK

A C# client for the [TypeSafe System One API](https://docs.typesafe.ai). You send some text (the *state*) and one or more questions, and the API answers each question with probabilities.

> **Status: early version.** Yes/no (`Noul`) questions have been tested against the live API. `Choice` and `Score` questions follow the API docs but have not been tested live yet.

## Requirements

- .NET 8 or newer (the library targets `net8.0` and `net10.0`)
- A TypeSafe API key — or an OpenRouter API key (see [OpenRouter](#openrouter))

## Installation

```powershell
dotnet add package Jev.Sdk
```

## Set your API key

The SDK reads the key from the `JEV_API_KEY` environment variable. Use either of these:

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

You can also set it per request with `request.SetModel("...")`.

## Changing the base URL

By default the SDK calls `https://api.typesafe.ai`. To point it somewhere else (a proxy, a staging server, or another provider that implements the same API), call `SetBaseUrl` once at startup, before creating any client:

```csharp
JevClientOptions.SetBaseUrl("https://my-proxy.example.com");

using var client = new JevClient();
```

The base URL is read when `new JevClient()` runs, so clients created before the call keep the old URL. Requests are sent to `/v1/systemone` on that host.

## Using your own `HttpClient`

If you want to control the `HttpClient` yourself (timeouts, proxies, custom handlers, logging), pass it to the constructor:

```csharp
var http = new HttpClient
{
    BaseAddress = new Uri(JevClientOptions.GetBaseUrl()),
    Timeout = TimeSpan.FromSeconds(30),
};
http.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", JevClientOptions.GetAPIKey());

using var client = new JevClient(http);
```

This constructor uses the client exactly as you give it. Unlike `new JevClient()`, it does **not** set the base address or the API key, so you must set `BaseAddress` and the `Authorization` header yourself.

`JevClient` disposes the `HttpClient` when it is disposed. Don't pass in a shared `HttpClient` that other code still needs.

## OpenRouter

The SDK can route requests through [OpenRouter](https://openrouter.ai) instead of calling TypeSafe directly. OpenRouter implements the same API spec, so no other code changes are needed.

**1. Set your OpenRouter API key** in `.env` or the environment:

```
OPENROUTER_API_KEY=your-openrouter-key-here
```

**2. Call `UseOpenRouter()` once at startup**, before creating any client:

```csharp
JevClientOptions.UseOpenRouter();

using var client = new JevClient();
// use exactly as normal
```

To use a specific model:

```csharp
JevClientOptions.UseOpenRouter("~typesafe/jev-1.13.0");
```

Or configure each option individually:

```csharp
JevClientOptions.SetBaseUrl("https://openrouter.ai/api");
JevClientOptions.SetApiKeyVariable("OPENROUTER_API_KEY");
JevClientOptions.SetModel("~typesafe/jev-latest");
```

## `JevClientOptions` reference

| Method | Description |
|---|---|
| `SetModel(string)` | Set the default model for all requests |
| `GetModel()` | Get the current default model |
| `SetBaseUrl(string)` | Set the API base URL (default `https://api.typesafe.ai`) |
| `GetBaseUrl()` | Get the current base URL |
| `SetApiKeyVariable(string)` | Change the environment variable name used to load the API key |
| `SetApiKey(string)` | Set the API key directly in code |
| `GetAPIKey()` | Get the current API key (loads from env on first call) |
| `UseOpenRouter(string?)` | Switch to OpenRouter — sets base URL, key variable, and model in one call |

## Errors

| Exception | When |
|---|---|
| `JevApiException` | The API returned an error. `StatusCode` and `ResponseBody` tell you what happened, for example `401` for a wrong key. |
| `InvalidOperationException` | The API key environment variable is not set. Thrown by `new JevClient()`. |
| `JsonException` | The API replied with an empty or unreadable body. |

## Project layout

```
src/Jev.Sdk/
  JevClient.cs, IJevClient.cs   The client and its interface
  JevClientOptions.cs           API key, base URL, and default model
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
