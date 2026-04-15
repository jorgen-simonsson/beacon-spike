# beacon-spike

A .NET 10 solution containing utility CLIs for the Beacon API.

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
