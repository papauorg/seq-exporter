using Seq.Api;
using Seq.Api.Model.Signals;

namespace SeqExporter;

public class SeqBackgroundService : BackgroundService
{
    public SeqObservableMetrics SeqObservableMetrics { get; }

    public SeqOptions SeqOptions { get; }

    public IEnumerable<QueryDefinition> QueryDefinitions { get; }
    public ILogger<SeqBackgroundService> Logger { get; }

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
            Logger.LogInformation("Attemting to retrieve queries from Seq Server");
            var connection = new SeqConnection (SeqOptions.BaseUrl, SeqOptions.ApiKey);

            foreach (var queryDefinition in QueryDefinitions)
            {
                Logger.LogDebug("Query: {query}", queryDefinition.Query);

                try
                {
                    var result = await connection.Data.QueryAsync(queryDefinition.Query, signal: SignalExpressionPart.Signal(queryDefinition.Signal));

                    for(int e = 0; e < result.Rows.Length; e++)
                    {
                        var key = result.Rows[e][0].ToString() ?? "";
                        var value = Convert.ToInt32 (result.Rows[e][1]);
                        SeqObservableMetrics.MetricResults.AddOrUpdate(
                            queryDefinition.MetricName, 
                            (_) => new Dictionary<string, int>{{key, value}}, 
                            (_, d) =>
                            {
                                d[key!] = value;
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
}