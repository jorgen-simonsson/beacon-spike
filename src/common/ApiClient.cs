using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DotNetEnv;

namespace Common;

public static class ApiClient
{
    private static readonly JsonSerializerOptions IndentedJsonOptions = new() { WriteIndented = true };

    public static void LoadEnv()
    {
        var envPath = Path.GetFullPath(".env");
        if (!File.Exists(envPath))
            throw new FileNotFoundException($".env file not found at {envPath}");
        Env.Load(envPath);
    }

    public static (string username, string password, string baseUrl) GetCredentials()
    {
        var username = Environment.GetEnvironmentVariable("USERNAME");
        var password = Environment.GetEnvironmentVariable("PASSWORD");
        var baseUrl = Environment.GetEnvironmentVariable("API_BASE_URL")?.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(baseUrl))
            throw new InvalidOperationException("Missing required .env variables: USERNAME, PASSWORD, API_BASE_URL");

        return (username, password, baseUrl);
    }

    public static async Task<string> AuthenticateAsync(HttpClient httpClient, string baseUrl, string username, string password)
    {
        var tokenUrl = $"{baseUrl.TrimEnd('/')}/auth/ropc";
        var tokenRequestBody = new { username, password };

        var tokenResponse = await httpClient.PostAsJsonAsync(tokenUrl, tokenRequestBody);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            var err = await tokenResponse.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Token request failed ({tokenResponse.StatusCode}): {err}");
        }

        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = tokenJson.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("accessToken not found in response");

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return accessToken;
    }

    public static async Task<string> GetEndpointAsync(HttpClient httpClient, string baseUrl, string endpoint)
    {
        var url = $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
        var response = await httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"GET /{endpoint} failed ({response.StatusCode}): {err}");
        }

        return await response.Content.ReadAsStringAsync();
    }

    public static async Task<string> PostEndpointAsync(HttpClient httpClient, string baseUrl, string endpoint, object payload)
    {
        var url = $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
        var response = await httpClient.PostAsJsonAsync(url, payload);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"POST /{endpoint} failed ({response.StatusCode}): {err}");
        }

        return await response.Content.ReadAsStringAsync();
    }

    public static async Task WriteFormattedJsonAsync(string payload, string outputFile)
    {
        var formatted = JsonSerializer.Serialize(
            JsonSerializer.Deserialize<JsonElement>(payload),
            IndentedJsonOptions);
        await File.WriteAllTextAsync(outputFile, formatted);
        await Console.Out.WriteLineAsync($"Wrote {outputFile}");
    }
}
