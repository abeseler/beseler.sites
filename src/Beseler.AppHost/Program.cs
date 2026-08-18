using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("Cache")
    .WithRedisInsight();

var postgres = builder.AddPostgres("postgres")
    .WithPgWeb();

var database = postgres.AddDatabase("Database", "app")
    .WithParentRelationship(postgres);

var dbMigrator = builder.AddContainer("ratchet", "abeseler/ratchet", "6.0.1")
    .WithOtlpExporter()
    .WithEnvironment("Ratchet__Command", "update")
    .WithEnvironment("Ratchet__ConnectionString", database)
    .WithBindMount("../../data", "/app/Migrations")
    .WithParentRelationship(postgres)
    .WaitFor(database);

builder.AddProject<BeselerDev_Web>("beseler-dev-web");

var azureCommunicationService = builder.AddParameter("AzureCommunicationService", secret: true);
var beselerNetApi = builder.AddProject<BeselerNet_Api>("beseler-net-api")
    .WithUrls(ctx =>
    {
        foreach (var url in ctx.Urls)
        {
            url.DisplayLocation = UrlDisplayLocation.DetailsOnly;
        }
        ctx.Urls.Add(new ResourceUrlAnnotation
        {
            Url = "/swagger",
            DisplayText = "OpenAPI Docs",
            Endpoint = ctx.GetEndpoint("https")
        });
    })
    .WithEnvironment("Azure__CommunicationConnectionString", azureCommunicationService)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(database)
    .WaitForCompletion(dbMigrator);

var beselerNetWeb = builder.AddProject<BeselerNet_Web>("beseler-net-web")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(beselerNetApi)
    .WaitFor(beselerNetApi)
    .WithUrls(ctx =>
    {
        foreach (var url in ctx.Urls)
        {
            url.DisplayLocation = UrlDisplayLocation.DetailsOnly;
        }
        ctx.Urls.Add(new ResourceUrlAnnotation
        {
            Url = "/",
            DisplayText = "Landing",
            Endpoint = ctx.GetEndpoint("https")
        });
        ctx.Urls.Add(new ResourceUrlAnnotation
        {
            Url = "/dashboard",
            DisplayText = "Dashboard",
            Endpoint = ctx.GetEndpoint("https")
        });
        ctx.Urls.Add(new ResourceUrlAnnotation
        {
            Url = "/account/login",
            DisplayText = "Login",
            Endpoint = ctx.GetEndpoint("https")
        });
        ctx.Urls.Add(new ResourceUrlAnnotation
        {
            Url = "/budget",
            DisplayText = "Budget",
            Endpoint = ctx.GetEndpoint("https")
        });
        ctx.Urls.Add(new ResourceUrlAnnotation
        {
            Url = "/roles",
            DisplayText = "Roles",
            Endpoint = ctx.GetEndpoint("https")
        });
        ctx.Urls.Add(new ResourceUrlAnnotation
        {
            Url = "/account",
            DisplayText = "Account",
            Endpoint = ctx.GetEndpoint("https")
        });
        ctx.Urls.Add(new ResourceUrlAnnotation
        {
            Url = "/accounts",
            DisplayText = "Users",
            Endpoint = ctx.GetEndpoint("https")
        });
        ctx.Urls.Add(new ResourceUrlAnnotation
        {
            Url = "/account/confirm-email",
            DisplayText = "Confirm email",
            Endpoint = ctx.GetEndpoint("https")
        });
    });

beselerNetApi
    .WithEnvironment("Communication__ConfirmEmailUrl", ReferenceExpression.Create($"{beselerNetWeb.GetEndpoint("https")}/account/confirm-email"))
    .WithEnvironment("Communication__ResetPasswordUrl", ReferenceExpression.Create($"{beselerNetWeb.GetEndpoint("https")}/account/reset-password"));

builder.Build().Run();
