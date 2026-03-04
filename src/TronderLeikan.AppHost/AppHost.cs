var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL — database for TrønderLeikan
var postgres = builder.AddPostgres("postgres")
    .AddDatabase("tronderleikan");

// DbMigrator kjøres automatisk ved oppstart, etter at PostgreSQL er klar
// API venter til migrations er fullfort
var migrator = builder.AddProject<Projects.TronderLeikan_DbMigrator>("migrator")
    .WithReference(postgres)
    .WaitFor(postgres);

var api = builder.AddProject<Projects.TronderLeikan_API>("api")
    .WithReference(postgres)
    .WaitFor(migrator)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
