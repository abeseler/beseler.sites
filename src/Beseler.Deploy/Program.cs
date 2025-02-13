using Beseler.Deploy.Endpoints;
using Beseler.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureLogging();
builder.AddServiceDefaults();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseRequestLogging();

app.MapOpenApi();
app.MapDeployWebhook();
app.MapDefaultEndpoints();

app.Run();
