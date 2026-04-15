using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DotNetEnv;

// Load .env from solution root (two levels up from bin output)
var envPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".env");
envPath = Path.GetFullPath(envPath);
if (!File.Exists(envPath))
{
    Console.Error.WriteLine($".env file not found at {envPath}");
    return 1;
}
Env.Load(envPath);

var username = Environment.GetEnvironmentVariable("USERNAME");
var password = Environment.GetEnvironmentVariable("PASSWORD");
var baseUrl = Environment.GetEnvironmentVariable("API_BASE_URL")?.Trim();

if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(baseUrl))
{
    Console.Error.WriteLine("Missing required .env variables: USERNAME, PASSWORD, API_BASE_URL");
    return 1;
}

using var httpClient = new HttpClient();

// ROPC token request
var tokenUrl = $"{baseUrl.TrimEnd('/')}/auth/ropc";
var tokenRequestBody = new { username, password };

var tokenResponse = await httpClient.PostAsJsonAsync(tokenUrl, tokenRequestBody);
if (!tokenResponse.IsSuccessStatusCode)
{
    var err = await tokenResponse.Content.ReadAsStringAsync();
    Console.Error.WriteLine($"Token request failed ({tokenResponse.StatusCode}): {err}");
    return 1;
}

var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
var accessToken = tokenJson.GetProperty("accessToken").GetString();

// GET /assets
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
var assetsResponse = await httpClient.GetAsync($"{baseUrl.TrimEnd('/')}/assets");

if (!assetsResponse.IsSuccessStatusCode)
{
    var err = await assetsResponse.Content.ReadAsStringAsync();
    Console.Error.WriteLine($"GET /assets failed ({assetsResponse.StatusCode}): {err}");
    return 1;
}

var payload = await assetsResponse.Content.ReadAsStringAsync();
var formatted = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonElement>(payload), new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText("assets.json", formatted);
Console.WriteLine("Wrote assets.json");
return 0;
