using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DotNetEnv;

if (args.Length == 0)
{
    await Console.Error.WriteLineAsync("Usage: getinfo <endpoint>");
    await Console.Error.WriteLineAsync("Example: getinfo assets");
    return 1;
}

var endpoint = args[0].TrimStart('/');

// Load .env from current directory
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (!File.Exists(envPath))
{
    await Console.Error.WriteLineAsync($".env file not found at {envPath}");
    return 1;
}
Env.Load(envPath);

var username = Environment.GetEnvironmentVariable("USERNAME");
var password = Environment.GetEnvironmentVariable("PASSWORD");
var baseUrl = Environment.GetEnvironmentVariable("API_BASE_URL")?.Trim();

if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(baseUrl))
{
    await Console.Error.WriteLineAsync("Missing required .env variables: USERNAME, PASSWORD, API_BASE_URL");
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
    await Console.Error.WriteLineAsync($"Token request failed ({tokenResponse.StatusCode}): {err}");
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
    await Console.Error.WriteLineAsync($"GET /{endpoint} failed ({response.StatusCode}): {err}");
    return 1;
}

var payload = await response.Content.ReadAsStringAsync();
var formatted = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonElement>(payload), new JsonSerializerOptions { WriteIndented = true });
var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "output");
Directory.CreateDirectory(outputDir);
var outputFile = Path.Combine(outputDir, $"{endpoint.Replace('/', '_')}.json");
await File.WriteAllTextAsync(outputFile, formatted);
Console.WriteLine($"Wrote {outputFile}");
return 0;
