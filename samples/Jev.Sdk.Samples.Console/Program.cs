using Jev.Sdk;
using Jev.Sdk.Exceptions;
using Jev.Sdk.Models;

// 1. Build the question.
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

// 3. Call the API and print the answer.
try
{
    using var client = new JevClient();
    Response response = await client.EvaluateAsync(request);

    if (response.GetAnswers()?["requests_refund"] is NoulAnswer answer)
    {
        Console.WriteLine($"Refund requested? {answer.GetNoul():P0} likely");
    }

    Console.WriteLine($"Model: {response.GetModel()}");
    Console.WriteLine($"Tokens: {response.GetUsage()?.GetInputTokens()} in, {response.GetUsage()?.GetOutputTokens()} out");
}
catch (JevApiException ex)
{
    Console.WriteLine($"API error {(int)ex.StatusCode}: {ex.ResponseBody}");
}
