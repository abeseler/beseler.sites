using Beseler.ServiceDefaults;
using BeselerNet.Web.Components;
using BeselerNet.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureLogging();
builder.AddServiceDefaults();
builder.AddRedisOutputCache("Cache");
builder.Services.AddRequestTimeouts();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<LocalStorageAccessor>();
builder.Services.AddSingleton<FeatureManager>();

builder.Services.AddHttpClient("beseler-net-api", client =>
    {
        client.BaseAddress = new("https+http://beseler-net-api");
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseRequestLogging();
app.UseAntiforgery();
app.UseOutputCache();

app.MapDefaultEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
