using System.Collections.Concurrent;

namespace SeqExporter;

public class SeqObservableMetrics
{
    public ConcurrentDictionary<string, Dictionary<CompositeMetricKey, int>> MetricResults { get; } = new ConcurrentDictionary<string, Dictionary<CompositeMetricKey, int>>();
}