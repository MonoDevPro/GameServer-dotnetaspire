using GameServer.Shared.CQRS.Commands;
using GameServer.Shared.CQRS.Pipeline;
using GameServer.Shared.CQRS.Queries;
using GameServer.Shared.CQRS.Results;
using GameServer.Shared.CQRS.Validation;

namespace GameServer.Shared.CQRS.Examples;

// Exemplos de Commands e Queries
public record CreatePlayerCommand(string Name, string Email) : ICommand<int>;
public record UpdatePlayerCommand(int Id, string Name) : ICommand;
public record GetPlayerQuery(int Id) : IQuery<PlayerDto>;
public record PlayerDto(int Id, string Name, string Email);

// Exemplos de Handlers
public class CreatePlayerCommandHandler : ICommandHandler<CreatePlayerCommand, int>
{
    public async Task<Result<int>> HandleAsync(CreatePlayerCommand command, CancellationToken cancellationToken = default)
    {
        // Simular criação do player
        await Task.Delay(100, cancellationToken);
        return Result<int>.Success(Random.Shared.Next(1, 1000));
    }
}

public class UpdatePlayerCommandHandler : ICommandHandler<UpdatePlayerCommand>
{
    public async Task<Result> HandleAsync(UpdatePlayerCommand command, CancellationToken cancellationToken = default)
    {
        // Simular atualização
        await Task.Delay(50, cancellationToken);
        return Result.Success();
    }
}

public class GetPlayerQueryHandler : IQueryHandler<GetPlayerQuery, PlayerDto>
{
    public async Task<Result<PlayerDto>> HandleAsync(GetPlayerQuery query, CancellationToken cancellationToken = default)
    {
        // Simular busca
        await Task.Delay(25, cancellationToken);
        var player = new PlayerDto(query.Id, $"Player {query.Id}", $"player{query.Id}@game.com");
        return Result<PlayerDto>.Success(player);
    }
}

// Exemplo de Validator
public class CreatePlayerCommandValidator : BaseValidator<CreatePlayerCommand>
{
    public CreatePlayerCommandValidator()
    {
        RuleForNotEmpty(cmd => cmd.Name, "Nome é obrigatório");
        RuleForMinLength(cmd => cmd.Name, 3, "Nome deve ter pelo menos 3 caracteres");
        RuleFor(cmd => cmd.Email, email => ValidationUtils.IsValidEmail(email), "Email deve ser válido");
    }
}

// Exemplo de Behavior Customizado
public class AuditPipelineBehavior<TCommand> : IPipelineBehavior<TCommand, Result>
    where TCommand : ICommand
{
    public async Task<Result> Handle(
        TCommand request,
        RequestHandlerDelegate<Result> next,
        CancellationToken cancellationToken)
    {
        // Log de auditoria antes
        Console.WriteLine($"[AUDIT] Executando comando: {typeof(TCommand).Name}");

        var result = await next();

        // Log de auditoria depois
        Console.WriteLine($"[AUDIT] Comando {typeof(TCommand).Name} - Status: {(result.IsSuccess ? "SUCESSO" : "FALHA")}");

        return result;
    }
}

// Exemplo de Behavior para Cache (Query)
public class CachePipelineBehavior<TQuery, TResult> : IPipelineBehavior<TQuery, Result<TResult>>
    where TQuery : IQuery<TResult>
{
    public async Task<Result<TResult>> Handle(
        TQuery request,
        RequestHandlerDelegate<Result<TResult>> next,
        CancellationToken cancellationToken)
    {
        var cacheKey = GenerateCacheKey(request);

        // Verificar cache (simulado)
        Console.WriteLine($"[CACHE] Verificando cache para chave: {cacheKey}");

        // Se não estiver no cache, executar query
        var result = await next();

        if (result.IsSuccess)
        {
            Console.WriteLine($"[CACHE] Armazenando resultado no cache: {cacheKey}");
        }

        return result;
    }

    private string GenerateCacheKey<T>(T request)
    {
        return $"{typeof(T).Name}_{request?.GetHashCode()}";
    }
}

// Exemplo de uso em um serviço
public class PlayerService
{
    private readonly ICommandBus _commandBus;
    private readonly IQueryBus _queryBus;

    public PlayerService(ICommandBus commandBus, IQueryBus queryBus)
    {
        _commandBus = commandBus;
        _queryBus = queryBus;
    }

    public async Task<Result<int>> CreatePlayerAsync(string name, string email)
    {
        var command = new CreatePlayerCommand(name, email);
        return await _commandBus.SendAsync<CreatePlayerCommand, int>(command);
    }

    public async Task<Result> UpdatePlayerAsync(int id, string name)
    {
        var command = new UpdatePlayerCommand(id, name);
        return await _commandBus.SendAsync(command);
    }

    public async Task<Result<PlayerDto>> GetPlayerAsync(int id)
    {
        var query = new GetPlayerQuery(id);
        return await _queryBus.SendAsync<GetPlayerQuery, PlayerDto>(query);
    }
}

/*
Exemplo de configuração no Program.cs:

var builder = WebApplication.CreateBuilder(args);

// Registrar CQRS com pipeline behaviors
builder.Services.AddCQRSWithBehaviors(typeof(CreatePlayerCommandHandler).Assembly);

// Registrar behaviors customizados
builder.Services.AddScoped(typeof(AuditPipelineBehavior<>));
builder.Services.AddScoped(typeof(CachePipelineBehavior<,>));

// Registrar serviços de aplicação
builder.Services.AddScoped<PlayerService>();

var app = builder.Build();

// Exemplo de uso
app.MapPost("/players", async (PlayerService service, CreatePlayerRequest request) =>
{
    var result = await service.CreatePlayerAsync(request.Name, request.Email);
    return result.IsSuccess 
        ? Results.Ok(new { PlayerId = result.Value })
        : Results.BadRequest(result.Errors);
});

app.MapGet("/players/{id}", async (PlayerService service, int id) =>
{
    var result = await service.GetPlayerAsync(id);
    return result.IsSuccess 
        ? Results.Ok(result.Value)
        : Results.NotFound();
});

record CreatePlayerRequest(string Name, string Email);
*/
