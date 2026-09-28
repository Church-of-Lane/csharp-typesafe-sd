using Jev.Sdk.Models;

namespace Jev.Sdk;

public interface IJevClient
{
    Task<Response> EvaluateAsync(Request request);
}
