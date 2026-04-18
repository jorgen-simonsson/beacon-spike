using System.Net.Http.Json;
using System.Text.Json;
using Common;
using Microsoft.AspNetCore.SignalR.Client;

const string AssetId = "D43D39950AD5";

try
{
    ApiClient.LoadEnv();
    var (username, password, baseUrl) = ApiClient.GetCredentials();

    using var httpClient = new HttpClient();
    await ApiClient.AuthenticateAsync(httpClient, baseUrl, username, password);

    // 1. Negotiate SignalR connection
    var negotiateUrl = $"{baseUrl.TrimEnd('/')}/push/negotiate";
    var negotiateResponse = await httpClient.PostAsync(negotiateUrl, null);
    if (!negotiateResponse.IsSuccessStatusCode)
    {
        var err = await negotiateResponse.Content.ReadAsStringAsync();
        throw new HttpRequestException($"Negotiate failed ({negotiateResponse.StatusCode}): {err}");
    }

    var negotiateJson = await negotiateResponse.Content.ReadFromJsonAsync<JsonElement>();
    var hubUrl = negotiateJson.GetProperty("url").GetString()
        ?? throw new InvalidOperationException("Missing 'url' in negotiate response");
    var accessToken = negotiateJson.GetProperty("accessToken").GetString()
        ?? throw new InvalidOperationException("Missing 'accessToken' in negotiate response");

    // 2. Connect to SignalR hub
    var connection = new HubConnectionBuilder()
        .WithUrl(hubUrl, options =>
        {
            options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
        })
        .WithAutomaticReconnect()
        .Build();

    connection.On<JsonElement>("telemetry", message =>
    {
        // Message arrives as a double-encoded JSON string — parse it
        var json = message.ValueKind == JsonValueKind.String
            ? JsonSerializer.Deserialize<JsonElement>(message.GetString()!)
            : message;

        var subject = json.GetProperty("subject").GetString();
        var timestamp = json.GetProperty("timestamp").GetString();
        var msg = json.GetProperty("message");

        // Extract the value using the subject name as key
        var valueText = msg.TryGetProperty(subject!, out var val) ? val.ToString() : "?";

        Console.WriteLine($"{timestamp}  {subject} = {valueText}");
    });

    connection.Reconnecting += ex =>
    {
        Console.WriteLine($"Reconnecting... {ex?.Message}");
        return Task.CompletedTask;
    };

    connection.Reconnected += connectionId =>
    {
        Console.WriteLine($"Reconnected: {connectionId}");
        return Task.CompletedTask;
    };

    connection.Closed += ex =>
    {
        Console.WriteLine($"Connection closed: {ex?.Message}");
        return Task.CompletedTask;
    };

    await connection.StartAsync();
    Console.WriteLine($"Connected to SignalR hub (ConnectionId: {connection.ConnectionId})");

    // 3. Subscribe to telemetry for the asset
    var subscriptionPayload = new
    {
        connectionId = connection.ConnectionId,
        topic = "telemetry",
        assets = new[] { AssetId },
        samplingType = "none"
    };

    var subResponse = await httpClient.PostAsJsonAsync(
        $"{baseUrl.TrimEnd('/')}/push/subscription", subscriptionPayload);
    if (!subResponse.IsSuccessStatusCode)
    {
        var err = await subResponse.Content.ReadAsStringAsync();
        throw new HttpRequestException($"Subscription failed ({subResponse.StatusCode}): {err}");
    }

    var subJson = await subResponse.Content.ReadFromJsonAsync<JsonElement>();
    var formatted = JsonSerializer.Serialize(subJson, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine(formatted);
    var subscriptionId = subJson.GetProperty("id").GetString();
    Console.WriteLine($"Subscribed (subscriptionId: {subscriptionId})");
    Console.WriteLine("Listening for signals... Press Ctrl+C to stop.");

    // 4. Keep alive — renew subscription before TTL expires (600s)
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    while (!cts.Token.IsCancellationRequested)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(8), cts.Token);
            // Renew subscription
            await httpClient.PutAsync(
                $"{baseUrl.TrimEnd('/')}/push/subscription/{subscriptionId}", null);
        }
        catch (OperationCanceledException)
        {
            break;
        }
    }

    // Cleanup
    await httpClient.DeleteAsync($"{baseUrl.TrimEnd('/')}/push/subscription/{subscriptionId}");
    await connection.StopAsync();
    return 0;
}
catch (Exception ex)
{
    await Console.Error.WriteLineAsync(ex.Message);
    return 1;
}
