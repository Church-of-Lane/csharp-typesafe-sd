using System.Net.Http.Headers;
using Jev.Sdk.Models;
using System.Text.Json;
using System.Text;
using Jev.Sdk.Exceptions;
using System.Text.Json.Serialization;

namespace Jev.Sdk;

public sealed class JevClient : IJevClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions RequestOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public JevClient()
    {
        _httpClient = new HttpClient();

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JevClientOptions.GetAPIKey());
    }
    public void Dispose()
    {
        _httpClient.Dispose();
    }
    static Question CreateQuestion(int type, object instructions, object criteria)
    {
        Question question_ = new Question();
        question_.SetQuestionType(type);
        question_.SetInstructions(instructions);
        question_.SetCriteria(criteria);
        return question_;
    }
    static Request CreateRequest(object state, Dictionary<string, Question> questions)
    {
        Request request = new Request();
        request.SetState(state);
        request.SetQuestions(questions);
        request.SetModel(JevClientOptions.GetModel());
        return request;
    }
    static Request CreateRequest(object state, Dictionary<string, Question> questions, string model_)
    {
        Request request = new Request();
        request.SetState(state);
        request.SetQuestions(questions);
        request.SetModel(model_);
        return request;
    }

    async Task SetRequest()    
    {
        Dictionary<string, Question> list = new();
        Question question = CreateQuestion(
            0,
            new
            {
                question = "Does the customer explicitly request a refund?",
                focus = "Look for a direct request to return money."
            },
            new
            {
                @true = "The customer explicitly asks for money back or a refund.",
                @false = "The customer complains but does not request money back."
            }
        );
        object state = new
            {
                customer = new
                {
                    name = "Alex",
                    accountType = "Premium"
                },

                order = new
                {
                    id = "ORD-1234",
                    price = 79.99,
                    status = "Delivered"
                },

                message = "I was charged twice for this order. Please refund the extra charge."
            };
        list["requests_refund"]=question;
        Request request = CreateRequest(state,list);
        Task<Response> response = EvaluateAsync(request);
    }
    public async Task<Response> EvaluateAsync(Request request)
    {
        //SEND REQUEST
        ArgumentNullException.ThrowIfNull(request);
        string jsonRequest = JsonSerializer.Serialize(request, RequestOptions);

        using var content = new StringContent(
            jsonRequest,
            Encoding.UTF8,
            "application/json"
        );

        using HttpResponseMessage httpResponse =
        await _httpClient.PostAsync(
            "https://api.typesafe.ai/v1/systemone",
            content
        );

        //GET RESPONSE
        string jsonResponse = await httpResponse.Content.ReadAsStringAsync();
        if (!httpResponse.IsSuccessStatusCode)
        {
            throw new JevApiException(
                httpResponse.StatusCode,
                jsonResponse
            );
        }
        Response response = JsonSerializer.Deserialize<Response>(jsonResponse) ?? throw new JsonException("Jev API returned an invalid or empty response.");
        

        return response;
    }
}
