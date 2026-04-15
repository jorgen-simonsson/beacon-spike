using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DotNetEnv;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: getinfo <endpoint>");
    Console.Error.WriteLine("Example: getinfo assets");
    return 1;
}

var endpoint = args[0].TrimStart('/');

// Load .env from solution root
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

// GET /<endpoint>
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
var response = await httpClient.GetAsync($"{baseUrl.TrimEnd('/')}/{endpoint}");

if (!response.IsSuccessStatusCode)
{
    var err = await response.Content.ReadAsStringAsync();
    Console.Error.WriteLine($"GET /{endpoint} failed ({response.StatusCode}): {err}");
    return 1;
}

var payload = await response.Content.ReadAsStringAsync();
var formatted = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonElement>(payload), new JsonSerializerOptions { WriteIndented = true });
var outputFile = $"{endpoint.Replace('/', '_')}.json";
File.WriteAllText(outputFile, formatted);
Console.WriteLine($"Wrote {outputFile}");
return 0;
