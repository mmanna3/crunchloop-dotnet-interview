namespace TodoApi.Application;

public class SyncSettings
{
    public string ExternalApiBaseUrl { get; set; } = "";
    public int IntervalSeconds { get; set; } = 60;
    public int MaxRetryAttempts { get; set; } = 3;
    public int CircuitBreakerThreshold { get; set; } = 5;
    public int CircuitBreakerDurationSeconds { get; set; } = 30;
    public ConflictResolution ConflictResolution { get; set; } = ConflictResolution.LocalWins;
}

public enum ConflictResolution
{
    LocalWins,
    ExternalWins,
    LastWriteWins,
}
