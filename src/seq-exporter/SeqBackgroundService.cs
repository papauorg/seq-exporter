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
            var connection = new SeqConnection (SeqOptions.BaseUrl, SeqOptions.ApiKey);

            foreach (var queryDefinition in QueryDefinitions)
            {
                Logger.LogDebug("Query: {query}", queryDefinition.Query);

                try
                {
                    var result = await connection.Data.QueryAsync(queryDefinition.Query, signal: SignalExpressionPart.Signal(queryDefinition.Signal));

                    var convertedValues = ConvertValuePairs(result);

                    foreach (var valuePair in convertedValues)
                    {
                        SeqObservableMetrics.MetricResults.AddOrUpdate(
                            queryDefinition.MetricName,
                            (_) => new Dictionary<CompositeMetricKey, int> { { valuePair.Key, valuePair.Value } },
                            (_, d) =>
                            {
                                d[valuePair.Key!] = valuePair.Value;
                                return d;
                            });
                    }
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
        foreach (var row in result.Rows)
        {
            //Get all columns except for count column
            var columnIndexes = Enumerable.Range(0, result.Columns.Length - 1);

            var compositeMetricKeys = columnIndexes
                .Select(x => new KeyValuePair<string, object>(result.Columns[x], row[x]));

            var compositeMetricKey = new CompositeMetricKey(compositeMetricKeys);

            //Assuming the count column is always last in the list
            var value = Convert.ToInt32(row[result.Columns.Length - 1]);

            yield return new KeyValuePair<CompositeMetricKey, int>(compositeMetricKey, value);
        }
    }
}