var builder = DistributedApplication.CreateBuilder(args);

// Declaração de um recurso PostgreSQL
var postgresdb = builder.AddPostgres("postgresdb")
    .WithVolume("postgres-data", "/var/lib/postgresql/data") // <-- Volume aplicado CORRETAMENTE ao SERVIDOR
    .WithPgAdmin();

// Adiciona os bancos de dados
var authdb = postgresdb // Referência ao banco de dados PostgreSQL
    .AddDatabase("authdb", "authdb");
var worlddb = postgresdb
    .AddDatabase("worlddb", "worlddb");

// Authorization Server (OpenIddict)
var authServer = builder.AddProject<Projects.GameServer_AuthServer>("authserver")
    .WithHttpsHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WithReference(authdb); // Referência ao banco de dados PostgreSQL

var worldService = builder.AddProject<Projects.GameServer_WorldSimulationService>("worldservice")
    .WithHttpsHealthCheck("/health")
    .WithReference(worlddb); // Referência ao banco de dados PostgreSQL

builder.Build().Run();

