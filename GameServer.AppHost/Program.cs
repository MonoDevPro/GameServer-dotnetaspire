using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Declaração de um recurso PostgreSQL
var postgresdb = builder.AddPostgres("postgresdb")
    .WithVolume("postgresdb-data", "/var/lib/postgresql/data") // Opcional: persistir dados em um volume Docker
    .WithPgAdmin();

// Declaração de um recurso de messaging RabbitMQ
var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithVolume("rabbitmq-data", "/var/lib/rabbitmq") // Opcional: persistir dados em um volume Docker
    .WithManagementPlugin();

var apiService = builder.AddProject<Projects.GameServer_ApiService>("apiservice")
    .WithHttpsHealthCheck("/health");

var authService = builder.AddProject<Projects.GameServer_AuthService>("authservice")
    .WithHttpsHealthCheck("/health")
    .WithReference(postgresdb); // Referência ao banco de dados PostgreSQL

builder.AddProject<Projects.GameServer_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpsHealthCheck("/health")
    .WithReference(apiService)
    .WithReference(authService)
    .WaitFor(apiService)
    .WaitFor(authService);

builder.Build().Run();

