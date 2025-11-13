using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("Cache")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithRedisInsight();

var postgres = builder.AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgWeb();

var database = postgres.AddDatabase("Database", "bnet")
    .WithParentRelationship(postgres);

var dbMigrator = builder.AddContainer("dbdeploy", "abeseler/dbdeploy", "3.1.10")
    .WithOtlpExporter()
    .WithEnvironment("Deploy__Command", "update")
    .WithEnvironment("Deploy__StartingFile", "migrations.json")
    .WithEnvironment("Deploy__DatabaseProvider", "postgres")
    .WithEnvironment("Deploy__Contexts", "local")
    .WithEnvironment("Deploy__ConnectionString", database)
    .WithEnvironment("Deploy__ConnectionAttempts", "10")
    .WithEnvironment("Deploy__ConnectionRetryDelaySeconds", "1")
    .WithBindMount("../../data", "/app/Migrations")
    .WithParentRelationship(postgres)
    .WaitFor(database);

builder.AddProject<Beseler_Deploy>("beseler-deploy")
    .WithExplicitStart();

builder.AddProject<BeselerDev_Web>("beseler-dev-web")
    .WithReference(cache)
    .WaitFor(cache)
    .WithExplicitStart();

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

builder.AddProject<BeselerNet_Web>("beseler-net-web")
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
    });

builder.Build().Run();
