using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.Grafana.Loki;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSerilog();

const string ServiceName = "sample-net-app";
var Appresource = ResourceBuilder.CreateDefault().AddService(ServiceName, serviceVersion: "v1.0.0");

string otlpEndpoint = "http://localhost:4317"; // URL de OpenTelemetry Collector

// Configurar Logging con OpenTelemetry
builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyDotNetAPI"));
    options.AddOtlpExporter(opt => opt.Endpoint = new Uri(otlpEndpoint));
});

// Configurar Tracing con OpenTelemetry
builder
    .Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyDotNetAPI"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation()
            .AddOtlpExporter(opt => opt.Endpoint = new Uri(otlpEndpoint));
    })
    .WithMetrics(metrics =>
    {
        metrics
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyDotNetAPI"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddOtlpExporter(opt => opt.Endpoint = new Uri(otlpEndpoint));
    });

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = "http://localhost:4317"; // OpenTelemetry Collector
        options.ResourceAttributes = new Dictionary<string, object>
        {
            { "service.name", "MyDotNetAPI" },
        };
    })
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Host.UseSerilog();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing",
    "Bracing",
    "Chilly",
    "Cool",
    "Mild",
    "Warm",
    "Balmy",
    "Hot",
    "Sweltering",
    "Scorching",
};

app.MapGet(
        "/weatherforecast",
        async (ILogger<Program> _logger) =>
        {
            var forecast = Enumerable
                .Range(1, 5)
                .Select(index => new WeatherForecast(
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
                .ToArray();

            _logger.LogInformation("GetWeatherForecast called");

            var client = new HttpClient();
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "https://jsonplaceholder.typicode.com/todos/1"
            );
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var resp = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("GetWeatherForecast called {resp}", resp);
            Console.WriteLine(resp);

            return forecast;
        }
    )
    .WithName("GetWeatherForecast")
    .WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
