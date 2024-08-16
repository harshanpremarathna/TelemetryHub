using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Diagnostics;
using System.Diagnostics.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
builder.Host.UseSerilog(
    (context, services, configuration) =>
        configuration.ReadFrom
            .Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .WriteTo.Console()
);

// Create multiple Meters for different purposes
var meter = new Meter("ObservableApi.Interaction", "1.0.0");

// Create multiple ActivitySources for different parts of the app
var dbActivitySource = new ActivitySource("ObservableApi.Database", "1.0.0");
var apiActivitySource = new ActivitySource("ObservableApi.ExternalApi", "1.0.0");

// Configure OpenTelemetry with Azure Monitor
var otel = builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Api"))
    .UseAzureMonitor();

// Add metrics and tracing
otel.WithMetrics(metrics =>
    {
        metrics.AddMeter(meter.Name);
        metrics.AddMeter("Microsoft.AspNetCore.Hosting");
        metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
    })
    .WithTracing(tracing =>
    {
        tracing.AddSource(dbActivitySource.Name);
        tracing.AddSource(apiActivitySource.Name);
        tracing.AddSource("Microsoft.AspNetCore");
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
    })
    .UseOtlpExporter();

// Register the meters and activity sources with DI
builder.Services.AddSingleton(meter);
builder.Services.AddSingleton(dbActivitySource);
builder.Services.AddSingleton(apiActivitySource);

builder.Services.AddHttpClient();
builder.Services.AddControllers();

var app = builder.Build();

app.UseSerilogRequestLogging();

//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Ensure telemetry is flushed before application exit
app.Lifetime.ApplicationStopping.Register(() =>
{
    var tracerProvider = app.Services.GetRequiredService<TracerProvider>();
    tracerProvider?.ForceFlush(); // Flush all telemetry data

    var meterProvider = app.Services.GetRequiredService<MeterProvider>();
    meterProvider?.ForceFlush(); // Flush all metric data

    Log.CloseAndFlush(); // Ensure Serilog flushes its logs
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapGet("/", () => Results.Ok(new { Status = "Healthy", Version = "1.0.0" }));

app.Run();
