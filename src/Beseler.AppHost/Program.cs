using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("Cache")
    //.WithLifetime(ContainerLifetime.Persistent)
    .WithRedisInsight();

var postgres = builder.AddPostgres("postgres")
    //.WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin();

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

var beselerNetApi = builder.AddProject<BeselerNet_Api>("beseler-net-api")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(database)
    .WaitForCompletion(dbMigrator);

builder.AddProject<BeselerNet_Web>("beseler-net-web")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(beselerNetApi)
    .WaitFor(beselerNetApi);

builder.Build().Run();
