using Beseler.ServiceDefaults;
using BeselerNet.Api;
using BeselerNet.Api.OAuth;
using BeselerNet.Api.Webhooks;
using Dapper;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisOutputCache("Cache");
builder.AddNpgsqlDataSource("Database");
builder.Services.AddRequestTimeouts();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHostedService<StartupService>();

DefaultTypeMap.MatchNamesWithUnderscores = true;

var app = builder.Build();

app.UseExceptionHandler();
app.UseOutputCache();
app.MapOpenApi();

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapOAuthEndpoints();
app.MapWebhookEndpoints();
app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
