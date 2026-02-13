using System.Text.Json.Serialization;

namespace DotNetExample;

/// <summary>Configuration binding for the Numbeo:ApiKey section in appsettings.json.</summary>
public sealed class NumbeoOptions
{
    public string ApiKey { get; set; } = "";
}

/// <summary>Response from GET /api/items — list of all trackable cost-of-living items.</summary>
public sealed class ItemsResponse
{
    [JsonPropertyName("items")]
    public List<Item> Items { get; set; } = new();
}

/// <summary>A single item (e.g. "Meal at an Inexpensive Restaurant") with its category and sort order.</summary>
public sealed class Item
{
    [JsonPropertyName("item_id")]
    public int ItemId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("display_order")]
    public int DisplayOrder { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";
}

/// <summary>Response from GET /api/city_prices — pricing data for a specific city.</summary>
public sealed class CityPricesResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "";

    [JsonPropertyName("prices")]
    public List<CityPrice> Prices { get; set; } = new();
}

/// <summary>Price statistics for a single item in a city (average, low, high, sample size).</summary>
public sealed class CityPrice
{
    [JsonPropertyName("item_id")]
    public int ItemId { get; set; }

    [JsonPropertyName("lowest_price")]
    public double? LowestPrice { get; set; }

    [JsonPropertyName("average_price")]
    public double? AveragePrice { get; set; }

    [JsonPropertyName("highest_price")]
    public double? HighestPrice { get; set; }

    [JsonPropertyName("data_points")]
    public int? DataPoints { get; set; }
}
