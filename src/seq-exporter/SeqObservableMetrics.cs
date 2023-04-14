using System.Collections.Concurrent;

public class SeqObservableMetrics
{
    public ConcurrentDictionary<string, Dictionary<string, int>> MetricResults { get; } = new ConcurrentDictionary<string, Dictionary<string, int>>();
}