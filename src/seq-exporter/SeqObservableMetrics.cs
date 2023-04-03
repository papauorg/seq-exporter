using System.Collections.Concurrent;

public class SeqObservableMetrics
{
    public long ShippedOrders { get; set; } 

    public ConcurrentDictionary<string, int> IncidentsCountedByLevel { get; } = new ConcurrentDictionary<string, int>();

    public ConcurrentDictionary<string, Dictionary<string, int>> MetricResults { get; } = new ConcurrentDictionary<string, Dictionary<string, int>>();
}