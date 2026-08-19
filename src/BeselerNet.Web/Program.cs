using Beseler.ServiceDefaults;
using BeselerNet.Shared;
using BeselerNet.Web.Components;
using Microsoft.Extensions.Options;
using BeselerNet.Web.Features.Account;
using BeselerNet.Web.Features.Accounts;
using BeselerNet.Web.Features.Roles;
using BeselerNet.Web.Features.Settings;
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

builder.Services.AddOptions<OAuthOptions>().BindConfiguration(OAuthOptions.SectionName);
builder.Services.AddScoped<AuthCookie>();
builder.Services.AddScoped<TokenRefresher>();
builder.Services.AddScoped<ApiClient>();
builder.Services.AddScoped<AccountSession>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<AccountsService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<AppService>();

builder.Services.AddHttpClient(ApiClient.ClientName, client =>
    {
        client.BaseAddress = new("https+http://beseler-net-api");
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    var oauth = app.Services.GetRequiredService<IOptions<OAuthOptions>>().Value;
    if (string.IsNullOrWhiteSpace(oauth.WebClientSecret))
        throw new InvalidOperationException("OAuth:WebClientSecret is required.");

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
