namespace SeqExporter;

public record QueryDefinition
{
    public string Query { get; init; } = "";
    public string Signal { get; init; } = "";
    public string MetricName { get; init; } = "";
    public string MetricUnit { get; init; } = "";
    public string MetricDescription { get; init; } = "";
}