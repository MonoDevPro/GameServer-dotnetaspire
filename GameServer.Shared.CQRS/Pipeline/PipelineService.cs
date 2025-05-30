using GameServer.Shared.CQRS.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameServer.Shared.CQRS.Pipeline;

/// <summary>
/// Serviço responsável por executar a pipeline de behaviors
/// </summary>
public class PipelineService : IPipelineService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PipelineService> _logger;

    public PipelineService(IServiceProvider serviceProvider, ILogger<PipelineService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executa a pipeline de behaviors para um request específico
    /// </summary>
    /// <typeparam name="TRequest">Tipo do request</typeparam>
    /// <typeparam name="TResponse">Tipo do response</typeparam>
    /// <param name="request">Request a ser processado</param>
    /// <param name="handler">Handler final a ser executado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Response processado pela pipeline</returns>
    public async Task<TResponse> Execute<TRequest, TResponse>(
        TRequest request,
        Func<TRequest, CancellationToken, Task<TResponse>> handler,
        CancellationToken cancellationToken = default)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogDebug("Iniciando pipeline para {RequestName}", requestName);

        try
        {
            // Obter todos os behaviors para este tipo de request
            var behaviors = GetBehaviors<TRequest, TResponse>().ToList();

            if (!behaviors.Any())
            {
                _logger.LogDebug("Nenhum behavior encontrado para {RequestName}, executando handler diretamente", requestName);
                return await handler(request, cancellationToken);
            }

            _logger.LogDebug("Executando {BehaviorCount} behaviors para {RequestName}", behaviors.Count(), requestName);

            // Criar a pipeline invertida - o último behavior chama o handler
            RequestHandlerDelegate<TResponse> pipeline = () => handler(request, cancellationToken);

            // Construir a pipeline de trás para frente
            behaviors.Reverse();
            pipeline = behaviors
                .Aggregate(pipeline, (next, behavior) =>
                    () => behavior.Handle(request, next, cancellationToken));

            // Executar a pipeline
            var result = await pipeline();

            _logger.LogDebug("Pipeline concluída com sucesso para {RequestName}", requestName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro na execução da pipeline para {RequestName}", requestName);
            throw;
        }
    }

    /// <summary>
    /// Obter todos os behaviors registrados para um tipo específico de request/response
    /// </summary>
    private IEnumerable<IPipelineBehavior<TRequest, TResponse>> GetBehaviors<TRequest, TResponse>()
    {
        try
        {
            return _serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao obter behaviors para {RequestType}/{ResponseType}",
                typeof(TRequest).Name, typeof(TResponse).Name);
            return [];
        }
    }
}

/// <summary>
/// Pipeline específica para comandos
/// </summary>
public class CommandPipeline
{
    private readonly IPipelineService _pipelineService;

    public CommandPipeline(IPipelineService pipelineService)
    {
        _pipelineService = pipelineService ?? throw new ArgumentNullException(nameof(pipelineService));
    }

    /// <summary>
    /// Executa pipeline para comando sem retorno específico
    /// </summary>
    public async Task<Result> Execute<TCommand>(
        TCommand command,
        Func<TCommand, CancellationToken, Task<Result>> handler,
        CancellationToken cancellationToken = default)
    {
        return await _pipelineService.Execute(command, handler, cancellationToken);
    }

    /// <summary>
    /// Executa pipeline para comando com retorno específico
    /// </summary>
    public async Task<Result<TResult>> Execute<TCommand, TResult>(
        TCommand command,
        Func<TCommand, CancellationToken, Task<Result<TResult>>> handler,
        CancellationToken cancellationToken = default)
    {
        return await _pipelineService.Execute(command, handler, cancellationToken);
    }
}

/// <summary>
/// Pipeline específica para queries
/// </summary>
public class QueryPipeline
{
    private readonly PipelineService _pipelineService;

    public QueryPipeline(PipelineService pipelineService)
    {
        _pipelineService = pipelineService ?? throw new ArgumentNullException(nameof(pipelineService));
    }

    /// <summary>
    /// Executa pipeline para query
    /// </summary>
    public async Task<Result<TResult>> Execute<TQuery, TResult>(
        TQuery query,
        Func<TQuery, CancellationToken, Task<Result<TResult>>> handler,
        CancellationToken cancellationToken = default)
    {
        return await _pipelineService.Execute(query, handler, cancellationToken);
    }
}
