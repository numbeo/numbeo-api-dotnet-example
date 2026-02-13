using System.Text.Json;
using DotNetExample;

// --- Validate inputs ---

var apiKey = GetApiKey(args);
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("Missing API key. Set Numbeo:ApiKey in appsettings.json or pass --api-key.");
    return 1;
}

var (city, country) = GetLocation(args);
if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(country))
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run -- --city \"San Francisco, CA\" --country \"United States\"");
    Console.WriteLine("  dotnet run -- --city \"San Francisco, CA\" --country \"United States\" --api-key YOUR_KEY");
    return 1;
}

// --- Fetch data and render ---

try
{
    using var api = new NumbeoApiService(apiKey);

    var items = await api.GetItemsAsync();
    if (items is null || items.Items.Count == 0)
    {
        Console.Error.WriteLine("Numbeo did not return item metadata.");
        return 1;
    }

    var prices = await api.GetCityPricesAsync(city, country);
    if (prices is null || prices.Prices.Count == 0)
    {
        Console.Error.WriteLine("Numbeo did not return price data for this city.");
        return 1;
    }

    RenderTable(items, prices);
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to fetch data: {ex.Message}");
    return 1;
}

// --- Helper functions ---

/// <summary>
/// Resolves the API key from --api-key argument or appsettings.json (Numbeo:ApiKey).
/// CLI argument takes priority over the config file.
/// </summary>
static string? GetApiKey(string[] args)
{
    var argKey = GetArgValue(args, "--api-key");
    if (!string.IsNullOrWhiteSpace(argKey))
    {
        return argKey;
    }

    if (!File.Exists("appsettings.json"))
    {
        return null;
    }

    using var stream = File.OpenRead("appsettings.json");
    using var doc = JsonDocument.Parse(stream);
    if (doc.RootElement.TryGetProperty("Numbeo", out var numbeo) &&
        numbeo.TryGetProperty("ApiKey", out var key))
    {
        return key.GetString();
    }

    return null;
}

/// <summary>Extracts --city and --country from command-line arguments.</summary>
static (string? City, string? Country) GetLocation(string[] args)
{
    var city = GetArgValue(args, "--city");
    var country = GetArgValue(args, "--country");
    return (city, country);
}

/// <summary>Returns the value following a named argument (e.g. --city "London"), or null if not found.</summary>
static string? GetArgValue(string[] args, string name)
{
    var index = Array.FindIndex(args, arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));
    if (index >= 0 && index + 1 < args.Length)
    {
        return args[index + 1];
    }

    return null;
}

/// <summary>
/// Joins item metadata with city price data and prints an aligned table to stdout.
/// Items are sorted by their display_order from the Numbeo API.
/// </summary>
static void RenderTable(ItemsResponse itemsResponse, CityPricesResponse pricesResponse)
{
    // Build a lookup so we can match each item to its price row by ItemId
    var lookup = pricesResponse.Prices.ToDictionary(price => price.ItemId);
    var rows = itemsResponse.Items
        .OrderBy(item => item.DisplayOrder)
        .Select(item =>
        {
            lookup.TryGetValue(item.ItemId, out var price);
            return new
            {
                item.DisplayOrder,
                item.Category,
                item.Name,
                Average = price?.AveragePrice,
                Lowest = price?.LowestPrice,
                Highest = price?.HighestPrice,
                DataPoints = price?.DataPoints
            };
        })
        .ToList();

    var currency = pricesResponse.Currency;
    var cityName = pricesResponse.Name;

    Console.WriteLine($"Prices for {cityName}");
    Console.WriteLine($"Currency: {currency}\n");
    var header = $"{"Order",5} | {"Category",22} | {"Item",55} | {"Avg",10} | {"Low",10} | {"High",10} | {"Data Points",11}";
    Console.WriteLine(header);
    Console.WriteLine(new string('-', header.Length));

    foreach (var row in rows)
    {
        Console.WriteLine($"{row.DisplayOrder,5} | {Trim(row.Category, 22),-22} | {Trim(row.Name, 55),-55} | {Format(row.Average),10} | {Format(row.Lowest),10} | {Format(row.Highest),10} | {FormatInt(row.DataPoints),11}");
    }
}

/// <summary>Formats a nullable double as "0.##" or "N/A".</summary>
static string Format(double? value)
{
    return value.HasValue ? value.Value.ToString("0.##") : "N/A";
}

/// <summary>Formats a nullable int as its string value or "N/A".</summary>
static string FormatInt(int? value)
{
    return value.HasValue ? value.Value.ToString() : "N/A";
}

/// <summary>Truncates a string to <paramref name="max"/> characters, adding "..." if trimmed.</summary>
static string Trim(string? value, int max)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return "";
    }

    return value.Length <= max ? value : value[..(max - 1)] + "…";
}
