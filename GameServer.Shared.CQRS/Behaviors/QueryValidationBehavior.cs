using GameServer.Shared.CQRS.Queries;
using GameServer.Shared.CQRS.Results;
using GameServer.Shared.CQRS.Validation;
using Microsoft.Extensions.Logging;

namespace GameServer.Shared.CQRS.Behaviors;

/// <summary>
/// Behavior para validação automática de queries
/// </summary>
public class QueryValidationBehavior<TQuery, TResult> : IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    private readonly IQueryHandler<TQuery, TResult> _handler;
    private readonly IEnumerable<IValidator<TQuery>> _validators;
    private readonly ILogger<QueryValidationBehavior<TQuery, TResult>> _logger;

    public QueryValidationBehavior(
        IQueryHandler<TQuery, TResult> handler,
        IEnumerable<IValidator<TQuery>> validators,
        ILogger<QueryValidationBehavior<TQuery, TResult>> logger)
    {
        _handler = handler;
        _validators = validators;
        _logger = logger;
    }

    public async Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
    {
        var queryName = typeof(TQuery).Name;

        if (_validators.Any())
        {
            _logger.LogDebug("Validando query {QueryName}", queryName);

            var context = new ValidationContext<TQuery>(query);
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .Select(f => f.Message)
                .ToList();

            if (failures.Count > 0)
            {
                _logger.LogWarning("Query {QueryName} falhou na validação: {Errors}",
                    queryName, string.Join("; ", failures));
                return Result<TResult>.Failure(failures);
            }
        }

        return await _handler.HandleAsync(query, cancellationToken);
    }
}
