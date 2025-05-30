using GameServer.Shared.CQRS.Results;

namespace GameServer.Shared.CQRS.Extensions;

/// <summary>
/// Extension methods para facilitar o uso dos Results
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Executa uma ação se o resultado for sucesso
    /// </summary>
    public static async Task<Result<T>> OnSuccessAsync<T>(this Result<T> result, Func<T, Task> action)
    {
        if (result.IsSuccess)
            await action(result.Value);
        return result;
    }

    /// <summary>
    /// Executa uma ação se o resultado for falha
    /// </summary>
    public static async Task<Result<T>> OnFailureAsync<T>(this Result<T> result, Func<IReadOnlyList<string>, Task> action)
    {
        if (result.IsFailure)
            await action(result.Errors);
        return result;
    }

    /// <summary>
    /// Mapeia o valor de um Result de sucesso
    /// </summary>
    public static Result<TOutput> Map<TInput, TOutput>(this Result<TInput> result, Func<TInput, TOutput> mapper)
    {
        return result.IsSuccess
            ? Result<TOutput>.Success(mapper(result.Value))
            : Result<TOutput>.Failure(result.Errors);
    }

    /// <summary>
    /// Mapeia o valor de um Result de sucesso de forma assíncrona
    /// </summary>
    public static async Task<Result<TOutput>> MapAsync<TInput, TOutput>(
        this Result<TInput> result,
        Func<TInput, Task<TOutput>> mapper)
    {
        return result.IsSuccess
            ? Result<TOutput>.Success(await mapper(result.Value))
            : Result<TOutput>.Failure(result.Errors);
    }

    /// <summary>
    /// Encadeia Results (Bind operation)
    /// </summary>
    public static Result<TOutput> Bind<TInput, TOutput>(
        this Result<TInput> result,
        Func<TInput, Result<TOutput>> binder)
    {
        return result.IsSuccess
            ? binder(result.Value)
            : Result<TOutput>.Failure(result.Errors);
    }

    /// <summary>
    /// Encadeia Results de forma assíncrona
    /// </summary>
    public static async Task<Result<TOutput>> BindAsync<TInput, TOutput>(
        this Result<TInput> result,
        Func<TInput, Task<Result<TOutput>>> binder)
    {
        return result.IsSuccess
            ? await binder(result.Value)
            : Result<TOutput>.Failure(result.Errors);
    }

    /// <summary>
    /// Combina múltiplos Results
    /// </summary>
    public static Result Combine(params Result[] results)
    {
        var failures = results.Where(r => r.IsFailure)
                              .SelectMany(r => r.Errors)
                              .ToList();

        return failures.Count == 0
            ? Result.Success()
            : Result.Failure(failures);
    }

    /// <summary>
    /// Combina múltiplos Results com valores
    /// </summary>
    public static Result<T[]> Combine<T>(params Result<T>[] results)
    {
        var failures = results.Where(r => r.IsFailure)
                              .SelectMany(r => r.Errors)
                              .ToList();

        if (failures.Count > 0)
            return Result<T[]>.Failure(failures);

        var values = results.Select(r => r.Value).ToArray();
        return Result<T[]>.Success(values);
    }

    /// <summary>
    /// Retorna o valor ou um valor padrão
    /// </summary>
    public static T GetValueOrDefault<T>(this Result<T> result, T defaultValue = default!)
    {
        return result.IsSuccess ? result.Value : defaultValue;
    }

    /// <summary>
    /// Retorna o valor ou executa uma função para obter o valor padrão
    /// </summary>
    public static T GetValueOrDefault<T>(this Result<T> result, Func<T> defaultValueFactory)
    {
        return result.IsSuccess ? result.Value : defaultValueFactory();
    }

    /// <summary>
    /// Converte Result para string com formatação padronizada
    /// </summary>
    public static string ToDetailedString(this Result result)
    {
        return result.IsSuccess
            ? "Success"
            : $"Failure: {string.Join("; ", result.Errors)}";
    }
}
