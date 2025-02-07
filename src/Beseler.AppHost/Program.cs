var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.BeselerNet_Api>("apiservice");

builder.AddProject<Projects.BeselerNet_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddProject<Projects.BeselerDev_Web>("beselerdev-web");

builder.Build().Run();
