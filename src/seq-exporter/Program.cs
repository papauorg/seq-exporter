using OpenTelemetry.Metrics;

using SeqExporter;
using SeqExporter.Metrics;

using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
    
builder.Host.UseSerilog();

builder.Services.AddRazorPages();

builder.Services.AddOpenTelemetry()
    .WithMetrics(builder => builder
        .AddPrometheusExporter(e => 
        {
            e.ScrapeResponseCacheDurationMilliseconds = 0;
        })
        .AddMeter(SeqControlMeter.NAME));

var options = builder.Configuration.GetSection("SeqOptions").Get<SeqOptions>()!;

builder.Services.AddSingleton<SeqOptions>(options);
builder.Services.AddSingleton<SeqControlMeter>();
builder.Services.AddSingleton<SeqObservableMetrics>();
builder.Services.AddHostedService<SeqBackgroundService>();

builder.Services.AddHttpClient("SeqLogServer", c => {
    c.BaseAddress = new Uri(options.BaseUrl);
    c.DefaultRequestHeaders.Add("Accept", "application/json");
});


var queries = builder.Configuration.GetSection("Queries").Get<QueryDefinition[]>();
builder.Services.AddSingleton<IEnumerable<QueryDefinition>>(queries!);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

var meter = app.Services.GetRequiredService<SeqControlMeter>();

app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.UseAuthorization();

app.Run();

