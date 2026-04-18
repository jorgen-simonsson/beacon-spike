using System.Text.Json;
using Common;

try
{
    ApiClient.LoadEnv();
    var (username, password, baseUrl) = ApiClient.GetCredentials();

    using var httpClient = new HttpClient();
    await ApiClient.AuthenticateAsync(httpClient, baseUrl, username, password);

    var result = await ApiClient.GetEndpointAsync(httpClient, baseUrl, "assets/D43D39950AD5/properties/reported");
    var formatted = JsonSerializer.Serialize(
        JsonSerializer.Deserialize<JsonElement>(result),
        new JsonSerializerOptions { WriteIndented = true });
    await Console.Out.WriteLineAsync(formatted);
    return 0;
}
catch (Exception ex)
{
    await Console.Error.WriteLineAsync(ex.Message);
    return 1;
}
