var builder = DistributedApplication.CreateBuilder(args);

// Declaração de um recurso PostgreSQL
var postgresdb = builder.AddPostgres("postgresdb")
    .WithVolume("postgres-data", "/var/lib/postgresql/data") // <-- Volume aplicado CORRETAMENTE ao SERVIDOR
    .WithPgAdmin();
// Adiciona o banco de dados de identidade
var authdb = postgresdb // Referência ao banco de dados PostgreSQL
    .AddDatabase("accountdb", "accountdb");
var chardb = postgresdb // Referência ao banco de dados PostgreSQL
    .AddDatabase("chardb", "chardb");
var worlddb = postgresdb
    .AddDatabase("worlddb", "worlddb");

var authService = builder.AddProject<Projects.GameServer_AccountService>("accountservice")
    .WithHttpsHealthCheck("/health")
    .WithReference(authdb); // Referência ao banco de dados PostgreSQL
                            //.WithExternalHttpEndpoints();

var characterService = builder.AddProject<Projects.GameServer_CharacterService>("characterservice")
    .WithHttpsHealthCheck("/health")
    .WithReference(chardb) // Referência ao banco de dados PostgreSQL
    .WaitFor(authService); // aguarda o serviço de autenticação
                           //.WithExternalHttpEndpoints();

var worldService = builder.AddProject<Projects.GameServer_WorldSimulationService>("worldservice")
    .WithHttpsHealthCheck("/health")
    .WithReference(worlddb) // Referência ao banco de dados PostgreSQL
    .WaitFor(characterService); // aguarda o serviço de personagens
                                //.WithExternalHttpEndpoints();

var gameServer = builder.AddProject<Projects.GameServer_Web>("webservice")
    .WithHttpsHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WithReference(authService)
    .WithReference(characterService)
    .WaitFor(worldService); // aguarda o serviço de personagens

builder.Build().Run();

