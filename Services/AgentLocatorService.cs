using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace FloodApp.Services
{
    public class SlicAgent
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Province { get; set; } = "";
        public string City { get; set; } = "";
        public string Address { get; set; } = "";
        public string Phone { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        
        // Non-JSON property to hold dynamic distance
        public double? DistanceKm { get; set; }
    }

    public class AgentLocatorService
    {
        private List<SlicAgent> _agents = new();

        public AgentLocatorService()
        {
            LoadAgents();
        }

        private void LoadAgents()
        {
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "slic_agents.json");
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var parsed = JsonSerializer.Deserialize<List<SlicAgent>>(json, opts);
                    if (parsed != null)
                    {
                        _agents = parsed;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading agents JSON: {ex.Message}");
            }
        }

        public List<SlicAgent> SearchAgents(string searchTerm, string provinceFilter, double? userLat = null, double? userLng = null)
        {
            var query = _agents.AsEnumerable();

            if (!string.IsNullOrEmpty(provinceFilter) && provinceFilter != "All")
            {
                query = query.Where(a => a.Province.Equals(provinceFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var lowerSearch = searchTerm.ToLower();
                query = query.Where(a => 
                    a.Name.ToLower().Contains(lowerSearch) || 
                    a.City.ToLower().Contains(lowerSearch) || 
                    a.Address.ToLower().Contains(lowerSearch)
                );
            }

            var results = query.ToList();

            // Calculate distance if we have coords
            if (userLat.HasValue && userLng.HasValue)
            {
                foreach(var agent in results)
                {
                    agent.DistanceKm = CalculateDistanceKm(userLat.Value, userLng.Value, agent.Latitude, agent.Longitude);
                }
                
                // Sort by nearest
                results = results.OrderBy(a => a.DistanceKm).ToList();
            }
            else
            {
                // Reset distances
                foreach(var agent in results) agent.DistanceKm = null;
                results = results.OrderBy(a => a.Province).ThenBy(a => a.City).ToList();
            }

            return results;
        }

        private double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            var r = 6371; // Radius of the earth in km
            var dLat = Deg2Rad(lat2 - lat1);
            var dLon = Deg2Rad(lon2 - lon1); 
            var a = 
                Math.Sin(dLat/2) * Math.Sin(dLat/2) +
                Math.Cos(Deg2Rad(lat1)) * Math.Cos(Deg2Rad(lat2)) * 
                Math.Sin(dLon/2) * Math.Sin(dLon/2); 
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a)); 
            return r * c; 
        }

        private double Deg2Rad(double deg)
        {
            return deg * (Math.PI/180);
        }
    }
}
