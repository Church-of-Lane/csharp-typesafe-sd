using DotNetEnv;

namespace Jev.Sdk;


//Stores API and model that will be used by default
public static class JevClientOptions
{
    private const string ApiKeyVariable = "JEV_API_KEY";

    private static string? s_apiKey;
    private static string s_model = "jev-latest";

    public static void SetModel(string model) => s_model = model;

    public static string GetAPIKey() => s_apiKey ??= LoadApiKey();

    public static string GetModel() => s_model;

    private static string LoadApiKey()
    {
        Env.NoClobber().TraversePath().Load();

        return Environment.GetEnvironmentVariable(ApiKeyVariable)
            ?? throw new InvalidOperationException(
                $"{ApiKeyVariable} is not set. Add it to a .env file or your environment variables.");
    }
}
