using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace SeqExporter;

public class SeqControlMeter
{
    public const string NAME = "SeqExporter.SeqControlMeter";

    private Meter Meter { get; }
    private IEnumerable<QueryDefinition> Queries { get; }
    private SeqObservableMetrics SeqObservableMetrics { get; }

    public SeqControlMeter(SeqObservableMetrics seqObservableMetrics, IEnumerable<QueryDefinition> queries)
    {
        SeqObservableMetrics = seqObservableMetrics ?? throw new ArgumentNullException(nameof(seqObservableMetrics));
        Queries = queries ?? throw new ArgumentNullException(nameof(queries));

        Meter = new(SeqControlMeter.NAME, "1.0");

        foreach (var query in Queries)
        {
            Meter.CreateObservableGauge(query.MetricName, () => ToMeasurements(SeqObservableMetrics.MetricResults, query.MetricName, query.LabelName), query.MetricUnit, query.MetricDescription);
        }
    }

    internal static IEnumerable<Measurement<int>> ToMeasurements(ConcurrentDictionary<string, Dictionary<CompositeMetricKey, int>> values, string metricName, string tagName)
    {
        if (!values.Any() || !values.ContainsKey(metricName))
            return Array.Empty<Measurement<int>>();

        return values[metricName]
            .Select(s => new Measurement<int>(s.Value, s.Key));
    }
   
}