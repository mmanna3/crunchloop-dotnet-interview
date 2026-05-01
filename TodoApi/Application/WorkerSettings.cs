namespace TodoApi.Application;

public class WorkerSettings
{
    public int BatchSize { get; set; } = 100;

    public int DelayMilliseconds { get; set; } = 0;
}
