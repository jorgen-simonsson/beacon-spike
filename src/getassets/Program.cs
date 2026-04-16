using Common;

try
{
    ApiClient.LoadEnv();
    var (username, password, baseUrl) = ApiClient.GetCredentials();

    using var httpClient = new HttpClient();
    await ApiClient.AuthenticateAsync(httpClient, baseUrl, username, password);

    var payload = await ApiClient.GetEndpointAsync(httpClient, baseUrl, "assets");
    await ApiClient.WriteFormattedJsonAsync(payload, "assets.json");
    return 0;
}
catch (Exception ex)
{
    await Console.Error.WriteLineAsync(ex.Message);
    return 1;
}
