# Numbeo CLI

A .NET 9.0 command-line tool that queries the [Numbeo API](https://www.numbeo.com/api/) for cost-of-living data and displays it as a formatted table.

It fetches item metadata (names, categories) and city-specific prices (average, low, high), joins them by item ID, and prints the results sorted by display order.

## Prerequisites

- [.NET SDK 9.0](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- A Numbeo API key — get one at <https://www.numbeo.com/common/api.jsp>

## Setup

Create an `appsettings.json` in the project root (this file is git-ignored to avoid leaking secrets):

```json
{
  "Numbeo": {
    "ApiKey": "YOUR_API_KEY"
  }
}
```

Alternatively, pass the key directly via `--api-key` (see below).

## Usage

```bash
dotnet run -- --city "San Francisco, CA" --country "United States"
```

### Override the API key on the command line

```bash
dotnet run -- --city "London" --country "United Kingdom" --api-key YOUR_KEY
```

### Arguments

| Argument      | Required | Description                                      |
|---------------|----------|--------------------------------------------------|
| `--city`      | Yes      | City name (e.g. `"San Francisco, CA"`, `"London"`)|
| `--country`   | Yes      | Country name (e.g. `"United States"`)             |
| `--api-key`   | No       | Overrides the key from `appsettings.json`         |

## Sample output

```text
Prices for San Francisco, CA, United States
Currency: USD

Order |               Category |                                                    Item |        Avg |        Low |       High | Data Points
--------------------------------------------------------------------------------------------------------------------------------------
    1 | Restaurants            | Meal at an Inexpensive Restaurant                       |         25 |         16 |         45 |          25
    2 | Restaurants            | Meal for Two at a Mid-Range Restaurant (Three Courses... |        145 |        100 |        200 |          20
    3 | Restaurants            | Combo Meal at McDonald's (or Equivalent Fast-Food Meal) |         15 |         11 |         15 |          13
  ...
```

## API endpoints used

| Endpoint | Purpose |
|----------|---------|
| `GET /api/items` | Fetches item metadata (names, categories, display order) |
| `GET /api/city_prices` | Fetches price data for all items in a given city |

See the [Numbeo API docs](https://www.numbeo.com/api/doc.jsp) for details.

## Project structure

```
├── Program.cs            Entry point — argument parsing, orchestration, table rendering
├── NumbeoApiService.cs   HTTP client with 30s timeout and retry with exponential backoff
├── NumbeoModels.cs       DTOs for Numbeo API JSON responses
├── DotNetExample.csproj  Project configuration (.NET 9.0, no external dependencies)
├── appsettings.json      API key config (git-ignored)
└── .gitignore
```

## Troubleshooting

- **"Missing API key"** — Create `appsettings.json` with your key (see Setup) or pass `--api-key`.
- **"Numbeo did not return price data"** — Check that the city/country spelling matches what Numbeo expects.
- **Timeout / retry messages** — The tool retries up to 3 times with exponential backoff (1s, 2s, 4s). If it still fails, check your network or API key validity.
- **Framework not found** — Ensure .NET 9.0 SDK is installed (`dotnet --list-sdks`).
