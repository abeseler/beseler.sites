var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("Cache")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithRedisInsight();

var postgresPassword = builder.AddParameter("postgres-password", "default_password", secret: true);
var postgres = builder.AddPostgres("postgres", password: postgresPassword, port: 15432)
    .WithBindMount("../../data", "/docker-entrypoint-initdb.d")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgWeb();

var database = postgres.AddDatabase("Database", "bnet")
    .WithParentRelationship(postgres);

var dbMigrator = builder.AddContainer("dbdeploy", "abeseler/dbdeploy")
    .WithOtlpExporter()
    .WithEnvironment("Deploy__Command", "update")
    .WithEnvironment("Deploy__StartingFile", "migrations.json")
    .WithEnvironment("Deploy__DatabaseProvider", "postgres")
    .WithEnvironment("Deploy__Contexts", "local")
    .WithEnvironment("Deploy__ConnectionString", $"Host=host.docker.internal;Port=15432;Database={database.Resource.DatabaseName};Username=postgres;Password={postgresPassword.Resource.Value}")
    .WithEnvironment("Deploy__ConnectionAttempts", "10")
    .WithEnvironment("Deploy__ConnectionRetryDelaySeconds", "1")
    .WithBindMount("../../data", "/app/Migrations")
    .WithParentRelationship(postgres)
    .WaitFor(database);

builder.AddProject<Projects.Beseler_Deploy>("beseler-deploy")
    .WithExplicitStart();

builder.AddProject<Projects.BeselerDev_Web>("beseler-dev-web")
    .WithReference(cache)
    .WaitFor(cache)
    .WithExplicitStart();

var beselerNetApi = builder.AddProject<Projects.BeselerNet_Api>("beseler-net-api")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(database)
    .WaitForCompletion(dbMigrator);

builder.AddProject<Projects.BeselerNet_Web>("beseler-net-web")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(beselerNetApi)
    .WaitFor(beselerNetApi);

builder.Build().Run();
