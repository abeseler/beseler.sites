using Beseler.ServiceDefaults;
using BeselerDev.Web;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureLogging();
builder.AddServiceDefaults();
builder.Services.AddRequestTimeouts();
builder.Services.AddHostedService<StartupService>();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

var app = builder.Build();

app.UseResponseCompression();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseRequestLogging();

app.MapDefaultEndpoints();
app.MapFallbackToFile("index.html");

app.Run();
