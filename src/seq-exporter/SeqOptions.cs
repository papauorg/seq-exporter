namespace SeqExporter;

public record SeqOptions
{
    public string BaseUrl { get; init; } = "http://localhost:5341/";
    public string ApiKey { get; init; } = "";
    public TimeSpan RequestInterval { get; init; } = TimeSpan.FromMinutes(1);
}
