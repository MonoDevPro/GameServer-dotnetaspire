using GameServer.Shared.CQRS.Pipeline;
using GameServer.Shared.CQRS.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameServer.Shared.CQRS.Queries;

public class QueryBus : IQueryBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QueryBus> _logger;
    private readonly QueryPipeline _pipeline;

    public QueryBus(IServiceProvider serviceProvider, ILogger<QueryBus> logger, QueryPipeline pipeline)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
    }

    public async Task<Result<TResult>> SendAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
    {
        var queryName = typeof(TQuery).Name;
        using var scope = _logger.BeginScope("Executing query {QueryName} with result {ResultType}", queryName, typeof(TResult).Name);

        _logger.LogInformation("Iniciando execução da query {QueryName}", queryName);

        try
        {
            // Usar pipeline para executar a query
            return await _pipeline.Execute<TQuery, TResult>(query, async (qry, token) =>
            {
                var handler = _serviceProvider.GetService<IQueryHandler<TQuery, TResult>>();
                if (handler == null)
                {
                    var errorMessage = $"Nenhum handler encontrado para a query {queryName}";
                    _logger.LogError(errorMessage);
                    return Result<TResult>.Failure(errorMessage);
                }

                return await handler.HandleAsync(qry, token);
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Query {QueryName} foi cancelada", queryName);
            return Result<TResult>.Failure("Operação cancelada");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao executar query {QueryName}", queryName);
            return Result<TResult>.Failure($"Erro interno: {ex.Message}");
        }
    }
}
