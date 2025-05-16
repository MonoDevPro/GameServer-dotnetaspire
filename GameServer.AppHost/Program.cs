var builder = DistributedApplication.CreateBuilder(args);

// Declaração de um recurso PostgreSQL
var postgresdb = builder.AddPostgres("postgresdb")
    .WithVolume("postgres-data", "/var/lib/postgresql/data") // <-- Volume aplicado CORRETAMENTE ao SERVIDOR
    .WithPgAdmin();
var authdb = postgresdb // Referência ao banco de dados PostgreSQL
    .AddDatabase("authdb", "authdb");
var chardb = postgresdb // Referência ao banco de dados PostgreSQL
    .AddDatabase("chardb", "chardb");

var authService = builder.AddProject<Projects.GameServer_AuthService>("authservice")
    .WithHttpsHealthCheck("/health")
    .WithReference(authdb); // Referência ao banco de dados PostgreSQL

var characterService = builder.AddProject<Projects.GameServer_CharacterService>("characterservice")
    .WithHttpsHealthCheck("/health")
    .WithReference(chardb) // Referência ao banco de dados PostgreSQL
    .WaitFor(authService); // aguarda o serviço de autenticação

var gameServer = builder.AddProject<Projects.GameServer_Web>("webservice")
    .WithHttpsHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WithReference(authService)
    .WithReference(characterService)
    .WaitFor(characterService); // aguarda o serviço de personagens

builder.Build().Run();

