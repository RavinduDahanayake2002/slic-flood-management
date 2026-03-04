namespace FloodApp.Models;

public class FloodCheckResponse
{
    public string RiskLevel { get; set; } = "UNKNOWN";
    public double RiskScore { get; set; }
    public double CurrentRainfall { get; set; }
    public string MlPredictedSeverity { get; set; } = "";
    public int PredictedAffected { get; set; }
    public int HistoricalEventCount { get; set; }
    public double HistoricalAvgAffected { get; set; }
    public string Message { get; set; } = "";
    public bool WasEscalated { get; set; }
    public string? EscalationReason { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
