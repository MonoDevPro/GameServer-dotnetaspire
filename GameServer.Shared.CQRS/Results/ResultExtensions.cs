using System.Diagnostics.CodeAnalysis;

namespace GameServer.Shared.CQRS.Results;

public static class ResultExtensions
{
    /// <summary>
    /// Tenta obter o valor com segurança de um Result<T>
    /// </summary>
    public static bool TryGetValue<T>(this Result<T> result, [NotNullWhen(true)] out T? value)
    {
        value = result.IsSuccess ? result.Value : default;
        return result.IsSuccess;
    }

    /// <summary>
    /// Converte um Result<T> para Result se não houver necessidade do valor
    /// </summary>
    public static Result ToResult<T>(this Result<T> result) => result;

    /// <summary>
    /// Combina múltiplos Results, falhando se algum falhar
    /// </summary>
    public static Result Combine(params Result[] results)
    {
        var failures = results.Where(r => r.IsFailure).ToList();
        if (!failures.Any())
            return Result.Success();

        var allErrors = failures.SelectMany(f => f.Errors).ToList();
        return Result.Failure(allErrors);
    }

    /// <summary>
    /// Combina múltiplos Results<T>, retornando lista se todos tiverem sucesso
    /// </summary>
    public static Result<List<T>> Combine<T>(params Result<T>[] results)
    {
        var failures = results.Where(r => r.IsFailure).ToList();
        if (failures.Any())
        {
            var allErrors = failures.SelectMany(f => f.Errors).ToList();
            return Result<List<T>>.Failure(allErrors);
        }

        var values = results.Select(r => r.Value).ToList();
        return Result<List<T>>.Success(values);
    }

    /// <summary>
    /// Executa uma ação de forma condicional baseada no resultado
    /// </summary>
    public static async Task<Result> OnSuccessAsync(this Result result, Func<Task> action)
    {
        if (result.IsSuccess)
            await action();
        return result;
    }

    /// <summary>
    /// Executa uma ação de forma condicional baseada no resultado com valor
    /// </summary>
    public static async Task<Result<T>> OnSuccessAsync<T>(this Result<T> result, Func<T, Task> action)
    {
        if (result.IsSuccess)
            await action(result.Value);
        return result;
    }

    /// <summary>
    /// Transforma um Result em outro tipo
    /// </summary>
    public static Result<TOutput> Transform<TInput, TOutput>(
        this Result<TInput> result,
        Func<TInput, Result<TOutput>> transformer)
    {
        return result.IsSuccess ? transformer(result.Value) : Result<TOutput>.Failure(result.Errors);
    }

    /// <summary>
    /// Valida um resultado contra uma condição, falhando se não atender
    /// </summary>
    public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, string errorMessage)
    {
        if (result.IsFailure)
            return result;

        return predicate(result.Value)
            ? result
            : Result<T>.Failure(errorMessage);
    }

    /// <summary>
    /// Aplica múltiplas validações em sequência
    /// </summary>
    public static Result<T> EnsureAll<T>(this Result<T> result, params (Func<T, bool> predicate, string error)[] validations)
    {
        if (result.IsFailure)
            return result;

        var errors = new List<string>();
        foreach (var (predicate, error) in validations)
        {
            if (!predicate(result.Value))
                errors.Add(error);
        }

        return errors.Any()
            ? Result<T>.Failure(errors)
            : result;
    }

    /// <summary>
    /// Filtra um Result<T> baseado em uma condição
    /// </summary>
    public static Result<T> Where<T>(this Result<T> result, Func<T, bool> predicate, string errorMessage)
    {
        return result.Ensure(predicate, errorMessage);
    }

    /// <summary>
    /// Converte exceções em Results
    /// </summary>
    public static Result<T> Try<T>(Func<T> action)
    {
        try
        {
            return Result<T>.Success(action());
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(ex);
        }
    }

    /// <summary>
    /// Versão assíncrona de Try
    /// </summary>
    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> action)
    {
        try
        {
            var result = await action();
            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(ex);
        }
    }

    /// <summary>
    /// Aplica uma função que pode falhar a um Result
    /// </summary>
    public static async Task<Result<TOutput>> SelectAsync<TInput, TOutput>(
        this Result<TInput> result,
        Func<TInput, Task<Result<TOutput>>> selector)
    {
        return result.IsSuccess
            ? await selector(result.Value)
            : Result<TOutput>.Failure(result.Errors);
    }
}
