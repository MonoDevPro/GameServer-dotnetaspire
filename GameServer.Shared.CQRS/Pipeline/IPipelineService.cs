using GameServer.Shared.CQRS.Results;

namespace GameServer.Shared.CQRS.Pipeline;

/// <summary>
/// Interface para o serviço responsável por executar a pipeline de behaviors
/// </summary>
public interface IPipelineService
{
    /// <summary>
    /// Executa a pipeline de behaviors para um request específico
    /// </summary>
    /// <typeparam name="TRequest">Tipo do request</typeparam>
    /// <typeparam name="TResponse">Tipo do response</typeparam>
    /// <param name="request">Request a ser processado</param>
    /// <param name="handler">Handler final a ser executado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Response processado pela pipeline</returns>
    Task<TResponse> Execute<TRequest, TResponse>(
        TRequest request,
        Func<TRequest, CancellationToken, Task<TResponse>> handler,
        CancellationToken cancellationToken = default);
}
