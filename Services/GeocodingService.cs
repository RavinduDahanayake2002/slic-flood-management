using System.Net.Http.Json;
using System.Text.Json;
using FloodApp.Models;

namespace FloodApp.Services;

public class GeocodingService
{
    private readonly HttpClient _httpClient;

    public GeocodingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "SLICFloodManagementApp/1.0 (contact@slic.lk)");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
    }

    /// <summary>
    /// Geocode a free-text address to Lat/Lon.
    /// Returns null if not found or an error occurs.
    /// </summary>
    public async Task<GeocodeResult?> GeocodeAsync(string address)
    {
        try
        {
            // Append Sri Lanka to help narrow results
            var query = address.Contains("Sri Lanka", StringComparison.OrdinalIgnoreCase)
                ? address
                : address + ", Sri Lanka";

            var url = $"search?q={Uri.EscapeDataString(query)}&format=json&limit=3&countrycodes=lk";
            var results = await _httpClient.GetFromJsonAsync<JsonElement[]>(url);

            if (results == null || results.Length == 0)
                return null;

            // Prefer results with "Sri Lanka" in display name
            var best = results.FirstOrDefault(r =>
            {
                var displayName = r.GetProperty("display_name").GetString() ?? "";
                return displayName.Contains("Sri Lanka", StringComparison.OrdinalIgnoreCase);
            });

            // Fallback to the first result
            var element = best.ValueKind == JsonValueKind.Undefined ? results[0] : best;

            var latStr = element.GetProperty("lat").GetString();
            var lonStr = element.GetProperty("lon").GetString();
            var displayNameStr = element.GetProperty("display_name").GetString() ?? address;

            if (double.TryParse(latStr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(lonStr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double lon))
            {
                return new GeocodeResult
                {
                    Lat = lat,
                    Lon = lon,
                    DisplayName = displayNameStr
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GeocodingService] Error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Checks whether coordinates fall within Sri Lanka's bounding box.
    /// </summary>
    public static bool IsInSriLanka(double lat, double lon)
        => lat >= 5.9 && lat <= 9.9 && lon >= 79.5 && lon <= 82.0;
}

public class GeocodeResult
{
    public double Lat { get; set; }
    public double Lon { get; set; }
    public string DisplayName { get; set; } = "";
}
