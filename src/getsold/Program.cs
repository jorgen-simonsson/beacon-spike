using System.Text.Json;
using Common;

const string AssetId = "D43D39950AD5";
const string TimeseriesName = "v2_8_0";
const double Tolerance = 0.001;

try
{
    ApiClient.LoadEnv();
    var (username, password, baseUrl) = ApiClient.GetCredentials();

    using var httpClient = new HttpClient();
    await ApiClient.AuthenticateAsync(httpClient, baseUrl, username, password);

    var payload = new
    {
        items = new[]
        {
            new
            {
                assetId = AssetId,
                timeseries = new[]
                {
                    new { name = TimeseriesName, type = "telemetry" }
                }
            }
        }
    };

    double? lastValue = null;

    while (true)
    {
        var result = await ApiClient.PostEndpointAsync(httpClient, baseUrl, "timeseries/get/last", payload);
        var doc = JsonSerializer.Deserialize<JsonElement>(result);

        var item = doc.GetProperty("items").EnumerateArray()
            .FirstOrDefault(i => i.GetProperty("timeseriesName").GetString() == TimeseriesName);

        if (item.ValueKind != JsonValueKind.Undefined)
        {
            var value = item.GetProperty("value").GetDouble();
            if (lastValue == null || Math.Abs(value - lastValue.Value) > Tolerance)
            {
                var timestamp = item.GetProperty("timestamp").GetString();
                Console.WriteLine($"{timestamp} {TimeseriesName} = {value}");
                lastValue = value;
            }
        }

        await Task.Delay(1000);
    }
}
catch (Exception ex)
{
    await Console.Error.WriteLineAsync(ex.Message);
    return 1;
}
