using DotNetEnv;

namespace Jev.Sdk;

public static class JevClientOptions
{
    private static string s_apiKeyVariable = "JEV_API_KEY";
    private static string? s_apiKey;
    private static string s_model = "jev-latest";
    private static string s_baseUrl = "https://api.typesafe.ai";

    public static void SetModel(string model) => s_model = model;
    public static string GetModel() => s_model;

    public static void SetBaseUrl(string baseUrl) => s_baseUrl = baseUrl;
    public static string GetBaseUrl() => s_baseUrl;

    /// <summary>
    /// Override the environment variable name used to load the API key.
    /// Call before the first <see cref="GetAPIKey"/> call (or call <see cref="SetApiKey"/> directly).
    /// </summary>
    public static void SetApiKeyVariable(string variableName)
    {
        s_apiKeyVariable = variableName;
        s_apiKey = null; // force reload on next GetAPIKey()
    }

    /// <summary>Set the API key directly instead of reading it from the environment.</summary>
    public static void SetApiKey(string apiKey) => s_apiKey = apiKey;

    public static string GetAPIKey() => s_apiKey ??= LoadApiKey();

    /// <summary>
    /// Configure the client to use OpenRouter as the backend.
    /// Equivalent to setting base URL, API key variable, and model individually.
    /// </summary>
    public static void UseOpenRouter(string? model = null)
    {
        SetBaseUrl("https://openrouter.ai/api");
        SetApiKeyVariable("OPENROUTER_API_KEY");
        SetModel(model ?? "~typesafe/jev-latest");
    }

    private static string LoadApiKey()
    {
        Env.NoClobber().TraversePath().Load();

        return Environment.GetEnvironmentVariable(s_apiKeyVariable)
            ?? throw new InvalidOperationException(
                $"{s_apiKeyVariable} is not set. Add it to a .env file or your environment variables.");
    }
}
