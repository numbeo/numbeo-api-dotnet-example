using System.Net;
using System.Net.Http.Json;

namespace DotNetExample;

/// <summary>
/// HTTP client for the Numbeo API with a 30-second timeout
/// and automatic retry with exponential backoff on transient failures.
/// </summary>
public sealed class NumbeoApiService : IDisposable
{
    private const int MaxRetries = 3;
    private static readonly TimeSpan[] RetryDelays = [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
    ];

    private readonly HttpClient _http;
    private readonly string _apiKey;

    public NumbeoApiService(string apiKey)
    {
        _apiKey = apiKey;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    /// <summary>Fetches the full list of items (names, categories, display order).</summary>
    public async Task<ItemsResponse?> GetItemsAsync()
    {
        var url = $"https://www.numbeo.com/api/items?api_key={Uri.EscapeDataString(_apiKey)}";
        return await SendWithRetryAsync<ItemsResponse>(url);
    }

    /// <summary>Fetches price data for all items in the given city/country.</summary>
    public async Task<CityPricesResponse?> GetCityPricesAsync(string city, string country)
    {
        var query = Uri.EscapeDataString($"{city.Trim()}, {country.Trim()}");
        var url = $"https://www.numbeo.com/api/city_prices?query={query}&api_key={Uri.EscapeDataString(_apiKey)}";
        return await SendWithRetryAsync<CityPricesResponse>(url);
    }

    /// <summary>
    /// Sends a GET request and deserializes the JSON response.
    /// Retries on network errors, timeouts, and 429 (rate limit) responses.
    /// </summary>
    private async Task<T?> SendWithRetryAsync<T>(string url)
    {
        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                using var response = await _http.GetAsync(url);

                // Rate-limited — always worth retrying after a delay
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    if (attempt < MaxRetries)
                    {
                        Console.Error.WriteLine($"Rate limited. Retrying in {RetryDelays[attempt].TotalSeconds}s...");
                        await Task.Delay(RetryDelays[attempt]);
                        continue;
                    }
                    throw new HttpRequestException("Rate limited by Numbeo API after multiple retries.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"Numbeo API returned {(int)response.StatusCode} {response.ReasonPhrase}.");
                }

                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (HttpRequestException) when (attempt < MaxRetries)
            {
                Console.Error.WriteLine($"Request failed. Retrying in {RetryDelays[attempt].TotalSeconds}s...");
                await Task.Delay(RetryDelays[attempt]);
            }
            catch (TaskCanceledException) when (attempt < MaxRetries)
            {
                // TaskCanceledException is thrown when HttpClient.Timeout is exceeded
                Console.Error.WriteLine($"Request timed out. Retrying in {RetryDelays[attempt].TotalSeconds}s...");
                await Task.Delay(RetryDelays[attempt]);
            }
        }

        throw new HttpRequestException("Request failed after multiple retries.");
    }

    public void Dispose() => _http.Dispose();
}
