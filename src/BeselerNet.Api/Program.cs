using Beseler.ServiceDefaults;
using BeselerNet.Api.Accounts;
using BeselerNet.Api.Core;
using BeselerNet.Api.Registrars;
using BeselerNet.Api.Webhooks;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureLogging();
builder.AddServiceDefaults();
builder.AddAuthentication();
builder.AddDataSources();
builder.AddCaches();
builder.AddHostedServices();

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddRequestTimeouts();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<Cookies>();
builder.Services.AddTransient<IPasswordHasher<Account>, PasswordHasher<Account>>();

builder.Services.AddSingleton<JwtGenerator>();
builder.Services.AddOptions<JwtOptions>().BindConfiguration(JwtOptions.SectionName);

var app = builder.Build();

app.UseExceptionHandler();
app.UseRequestLogging();

app.MapOpenApi();
app.MapOAuthEndpoints();
app.MapWebhookEndpoints();
app.MapDefaultEndpoints();

app.Run();
