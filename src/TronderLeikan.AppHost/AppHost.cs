var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.TronderLeikan_API>("api")
    .WithHttpHealthCheck("/health");

builder.Build().Run();
