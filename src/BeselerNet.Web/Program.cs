using Beseler.ServiceDefaults;
using BeselerNet.Web.Components;
using BeselerNet.Web.Features.Account;
using BeselerNet.Web.Features.Accounts;
using BeselerNet.Web.Features.Roles;
using BeselerNet.Web.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureLogging();
builder.AddServiceDefaults();
builder.AddRedisOutputCache("Cache");
builder.Services.AddRequestTimeouts();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<SessionActivity>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<AuthCookie>();
builder.Services.AddScoped<TokenRefresher>();
builder.Services.AddScoped<ApiClient>();
builder.Services.AddScoped<AccountSession>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<AccountsService>();
builder.Services.AddScoped<RoleService>();

builder.Services.AddHttpClient(ApiClient.ClientName, client =>
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
app.MapAuthCookieEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
