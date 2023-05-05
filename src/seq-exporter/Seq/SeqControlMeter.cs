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
            Meter.CreateObservableGauge(query.MetricName, () => ToMeasurements(SeqObservableMetrics.MetricResults, query), query.MetricUnit, query.MetricDescription);
        }
    }

    internal static IEnumerable<Measurement<int>> ToMeasurements(ConcurrentDictionary<string, Dictionary<CompositeMetricKey, int>> values, QueryDefinition query)
    {
        var metricName = query.MetricName;

        if (!values.Any() || !values.ContainsKey(metricName))
            return Array.Empty<Measurement<int>>();

        var additionalLabels = query.AdditionalLabels.Select(l => new KeyValuePair<string, object?>(l.Key, l.Value));

        return values[metricName]
            .Select(s =>
            {
                var tags = new List<KeyValuePair<string, object?>>();
                foreach (var key in s.Key)
                    tags.Add(new (key.Key, key.Value));

                tags.AddRange(additionalLabels);

                return new Measurement<int>(s.Value, tags);
            });
    }
}