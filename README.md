# beacon-spike

A demo of REST-based timeseries access of the Beacon Tower API, implemented as a set of .NET 10 CLI tools.

The `getsold` CLI polls the Beacon Tower timeseries endpoint for sold energy readings on an asset, printing value changes to the console in real time. Additional CLIs are included for fetching asset properties, listing assets, and querying arbitrary API endpoints.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Setup

Copy `.env.example` to `.env` in the repo root and fill in your credentials:

```
USERNAME=your-username
PASSWORD=your-password
API_BASE_URL=https://your-api-base-url/api/v1
```

## CLIs

### getsold

Polls the Beacon Tower `timeseries/get/last` endpoint every second for the `v2_8_0` telemetry value on asset `D43D39950AD5`. Only prints to the console when the value changes.

```sh
dotnet run --project src/getsold
```

Example output:

```
2026-04-18T06:55:48Z v2_8_0 = 50194.396
2026-04-18T07:10:12Z v2_8_0 = 50195.012
```

Press `Ctrl+C` to stop.

### getprops

Fetches all reported properties for asset `D43D39950AD5` and prints the JSON to the console.

```sh
dotnet run --project src/getprops
```

### getassets

Authenticates via ROPC and fetches `/assets`, writing the result to `assets.json`.

```sh
dotnet run --project src/getassets
```

### getinfo

Authenticates via ROPC and fetches any endpoint passed as a command-line argument. The JSON response is written to `<endpoint>.json`.

```sh
dotnet run --project src/getinfo -- assets
dotnet run --project src/getinfo -- models
```
