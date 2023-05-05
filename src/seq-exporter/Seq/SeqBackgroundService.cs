using Seq.Api;
using Seq.Api.Model.Data;
using Seq.Api.Model.Signals;

namespace SeqExporter;

public class SeqBackgroundService : BackgroundService
{
    private SeqObservableMetrics SeqObservableMetrics { get; }
    private SeqOptions SeqOptions { get; }
    private IEnumerable<QueryDefinition> QueryDefinitions { get; }
    private ILogger<SeqBackgroundService> Logger { get; }

    public SeqBackgroundService(SeqObservableMetrics seqObservableMetrics, SeqOptions seqOptions, IEnumerable<QueryDefinition> queryDefinitions, ILogger<SeqBackgroundService> logger)

    {
        SeqObservableMetrics = seqObservableMetrics ?? throw new ArgumentNullException(nameof(seqObservableMetrics));
        SeqOptions = seqOptions ?? throw new ArgumentNullException(nameof(seqOptions));
        QueryDefinitions = queryDefinitions ?? throw new ArgumentNullException(nameof(queryDefinitions));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(SeqOptions.RequestInterval);
        do
        {
            Logger.LogInformation("Attempting to retrieve queries from Seq Server");
            var connection = new SeqConnection(SeqOptions.BaseUrl, SeqOptions.ApiKey);

            foreach (var queryDefinition in QueryDefinitions)
            {
                Logger.LogDebug("Query: {query}", queryDefinition.Query);

                try
                {
                    var result = await connection.Data.QueryAsync(queryDefinition.Query, signal: SignalExpressionPart.Signal(queryDefinition.Signal));
                    var convertedValues = ConvertValuePairs(result);

                    SeqObservableMetrics.MetricResults.AddOrUpdate(
                        queryDefinition.MetricName,
                        (_) => convertedValues.ToDictionary(c => c.Key, c => c.Value),
                        (_, d) =>
                        {
                            // set all counters to 0 to make sure that no longer available errors from the query are not reported with
                            // their previous values
                            foreach (var key in d.Keys)
                                d[key] = 0;

                            foreach (var newValue in convertedValues)
                                d[newValue.Key] = newValue.Value;

                            return d;
                        }
                    );
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error retrieving data from Seq");
                }
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public static IEnumerable<KeyValuePair<CompositeMetricKey, int>> ConvertValuePairs(QueryResultPart result)
    {
        //Get all columns except for count column
        var columnIndexes = Enumerable.Range(0, result.Columns.Length - 1);

        foreach (var row in result.Rows)
        {
            var compositeMetricKeys = columnIndexes
                .Select(x => new KeyValuePair<string, object>(result.Columns[x], MaxLabelValue(row[x])));

            var compositeMetricKey = new CompositeMetricKey(compositeMetricKeys);

            //Assuming the count column is always last in the list
            var value = Convert.ToInt32(row[result.Columns.Length - 1]);

            yield return new KeyValuePair<CompositeMetricKey, int>(compositeMetricKey, value);
        }
    }

    private static object MaxLabelValue(object value)
    {
        if (value is string v)
        {
            v = v.Trim();
            v = v.Substring(0, Math.Min(200, v.Length)); // cut to max length
            var indexOfNewline = v.IndexOfAny(new [] {'\r', '\n'});
            if (indexOfNewline >= 0)
                v = v.Substring(0, indexOfNewline);

            return v;
        }

        return value;
    }
}