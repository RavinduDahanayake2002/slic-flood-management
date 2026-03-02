using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FloodApp.Services
{
    public class HistoricalFloodEvent
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public string Month { get; set; } = "";
        public string Name { get; set; } = "";
        public List<string> AffectedDistricts { get; set; } = new();
        public string Severity { get; set; } = "";
        public int Deaths { get; set; }
        public int AffectedPeople { get; set; }
        public long DamageEstimateLKR { get; set; }
        public string Description { get; set; } = "";
    }

    public class HistoricalFloodEventService
    {
        private readonly LocationService _locationService;
        private List<HistoricalFloodEvent> _events = new();

        public HistoricalFloodEventService(LocationService locationService)
        {
            _locationService = locationService;
            LoadEvents();
        }

        private void LoadEvents()
        {
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "historical_floods.json");
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var parsed = JsonSerializer.Deserialize<List<HistoricalFloodEvent>>(json, opts);
                    if (parsed != null)
                    {
                        _events = parsed.OrderByDescending(e => e.Year).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading historical floods: {ex.Message}");
            }
        }

        public List<HistoricalFloodEvent> GetFilteredEvents(string provinceName, string yearRange, string severity)
        {
            var filtered = _events.AsEnumerable();

            // Filter by Province by looking up Districts for that province
            if (!string.IsNullOrEmpty(provinceName) && provinceName != "All")
            {
                var province = _locationService.GetProvinces().FirstOrDefault(p => p.Name.Equals(provinceName, StringComparison.OrdinalIgnoreCase));
                if (province != null)
                {
                    var districtNamesInProvince = _locationService.GetDistricts(province.Id).Select(d => d.Name).ToList();
                    filtered = filtered.Where(e => e.AffectedDistricts.Any(ad => districtNamesInProvince.Contains(ad)));
                }
            }

            // Filter by Year Range
            if (!string.IsNullOrEmpty(yearRange) && yearRange != "All")
            {
                if (yearRange == "Before 2000")
                {
                    filtered = filtered.Where(e => e.Year < 2000);
                }
                else if (yearRange == "2000–2010")
                {
                    filtered = filtered.Where(e => e.Year >= 2000 && e.Year <= 2010);
                }
                else if (yearRange == "2011–2020")
                {
                    filtered = filtered.Where(e => e.Year >= 2011 && e.Year <= 2020);
                }
                else if (yearRange == "2020–Present")
                {
                    filtered = filtered.Where(e => e.Year > 2020);
                }
            }

            // Filter by Severity
            if (!string.IsNullOrEmpty(severity) && severity != "All")
            {
                filtered = filtered.Where(e => e.Severity.Equals(severity, StringComparison.OrdinalIgnoreCase));
            }

            return filtered.ToList();
        }

        public int GetTotalEventsCount() => _events.Count;

        public HistoricalFloodEvent? GetDeadliestEvent()
        {
            return _events.OrderByDescending(e => e.Deaths).FirstOrDefault();
        }

        public string GetMostAffectedProvince()
        {
            if (!_events.Any()) return "N/A";

            // Count occurrences of districts across all events
            var districtCounts = new Dictionary<string, int>();
            foreach (var evt in _events)
            {
                foreach (var dist in evt.AffectedDistricts)
                {
                    if (districtCounts.ContainsKey(dist))
                        districtCounts[dist]++;
                    else
                        districtCounts[dist] = 1;
                }
            }

            // Map districts back to provinces and sum
            var provinceHits = new Dictionary<string, int>();
            foreach (var (distName, count) in districtCounts)
            {
                // Find province for district
                var distObj = _locationService.GetProvinces()
                    .SelectMany(p => _locationService.GetDistricts(p.Id).Select(d => new { District = d, Province = p }))
                    .FirstOrDefault(x => x.District.Name.Equals(distName, StringComparison.OrdinalIgnoreCase));

                if (distObj != null)
                {
                    var provName = distObj.Province.Name;
                    if (provinceHits.ContainsKey(provName))
                        provinceHits[provName] += count;
                    else
                        provinceHits[provName] = count;
                }
            }

            if (!provinceHits.Any()) return "N/A";
            
            return provinceHits.OrderByDescending(kv => kv.Value).First().Key;
        }

        // Quick aggregate for decade chart
        public Dictionary<string, int> GetDecadeFrequency()
        {
            var result = new Dictionary<string, int>()
            {
                { "Pre-2000", 0 },
                { "2000s", 0 },
                { "2010s", 0 },
                { "2020s", 0 }
            };

            foreach(var evt in _events)
            {
                if (evt.Year < 2000) result["Pre-2000"]++;
                else if (evt.Year < 2010) result["2000s"]++;
                else if (evt.Year < 2020) result["2010s"]++;
                else result["2020s"]++;
            }
            return result;
        }
    }
}
