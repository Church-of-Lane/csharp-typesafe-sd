using System.Net;


namespace Jev.Sdk.Exceptions;
public sealed class JevApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string ResponseBody { get; }

    public JevApiException(
        HttpStatusCode statusCode,
        string responseBody)
        : base($"Jev API request failed with status code {(int)statusCode} ({statusCode}).")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
