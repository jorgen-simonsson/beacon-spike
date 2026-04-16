using Common;

if (args.Length == 0)
{
    await Console.Error.WriteLineAsync("Usage: getinfo <endpoint>");
    await Console.Error.WriteLineAsync("Example: getinfo assets");
    return 1;
}

var endpoint = args[0].TrimStart('/');

try
{
    ApiClient.LoadEnv();
    var (username, password, baseUrl) = ApiClient.GetCredentials();

    using var httpClient = new HttpClient();
    await ApiClient.AuthenticateAsync(httpClient, baseUrl, username, password);

    var payload = await ApiClient.GetEndpointAsync(httpClient, baseUrl, endpoint);
    var outputFile = $"{endpoint.Replace('/', '_')}.json";
    await ApiClient.WriteFormattedJsonAsync(payload, outputFile);
    return 0;
}
catch (Exception ex)
{
    await Console.Error.WriteLineAsync(ex.Message);
    return 1;
}
