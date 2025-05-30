using System.Diagnostics.CodeAnalysis;

namespace GameServer.Shared.CQRS.Results;

public abstract class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<string> Errors { get; }

    protected Result(bool isSuccess, IEnumerable<string>? errors = null)
    {
        IsSuccess = isSuccess;
        var errorList = errors?.Where(e => !string.IsNullOrWhiteSpace(e))?.ToList() ?? new List<string>();
        Errors = errorList.AsReadOnly();

        // Validações de integridade
        if (isSuccess && Errors.Any())
            throw new InvalidOperationException("Um resultado de sucesso não pode conter erros.");
        if (!isSuccess && !Errors.Any())
            throw new InvalidOperationException("Um resultado de falha deve conter pelo menos um erro.");
    }

    public static Result Success() => new SuccessResult();
    public static Result Failure(string error) => new FailureResult(new[] { error });
    public static Result Failure(IEnumerable<string> errors) => new FailureResult(errors);
    public static Result Failure(Exception exception) => new FailureResult(new[] { exception.Message });

    // Métodos de conveniência
    public Result OnSuccess(Action action)
    {
        if (IsSuccess) action();
        return this;
    }

    public Result OnFailure(Action<IReadOnlyList<string>> action)
    {
        if (IsFailure) action(Errors);
        return this;
    }

    private sealed class SuccessResult : Result
    {
        public SuccessResult() : base(true) { }
    }

    private sealed class FailureResult : Result
    {
        public FailureResult(IEnumerable<string> errors) : base(false, errors) { }
    }
}

public sealed class Result<T> : Result
{
    private readonly T _value;

    [MemberNotNullWhen(true, nameof(_value))]
    public bool HasValue => IsSuccess && _value is not null;

    public T Value
    {
        get
        {
            if (IsFailure)
                throw new InvalidOperationException($"Não é possível acessar o valor de um resultado de falha. Erros: {string.Join(", ", Errors)}");
            return _value;
        }
    }

    private Result(bool isSuccess, T value, IEnumerable<string>? errors = null)
        : base(isSuccess, errors)
    {
        _value = value;

        if (isSuccess && value is null)
            throw new InvalidOperationException("Um resultado de sucesso não pode ter valor nulo.");
    }

    public static Result<T> Success(T value) => new(true, value);
    public static new Result<T> Failure(string error) => new(false, default!, new[] { error });
    public static new Result<T> Failure(IEnumerable<string> errors) => new(false, default!, errors);
    public static new Result<T> Failure(Exception exception) => new(false, default!, new[] { exception.Message });

    // Métodos funcionais
    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        return IsSuccess
            ? Result<TNew>.Success(mapper(Value))
            : Result<TNew>.Failure(Errors);
    }

    public async Task<Result<TNew>> MapAsync<TNew>(Func<T, Task<TNew>> mapper)
    {
        return IsSuccess
            ? Result<TNew>.Success(await mapper(Value))
            : Result<TNew>.Failure(Errors);
    }

    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess) action(Value);
        return this;
    }

    public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> binder)
    {
        return IsSuccess ? binder(Value) : Result<TNew>.Failure(Errors);
    }

    public async Task<Result<TNew>> BindAsync<TNew>(Func<T, Task<Result<TNew>>> binder)
    {
        return IsSuccess ? await binder(Value) : Result<TNew>.Failure(Errors);
    }

    // Conversões explícitas para Result base
    public Result ToResult() =>
        IsSuccess ? Result.Success() : Result.Failure(Errors);

    // Métodos de conveniência para tratamento de erros
    public T GetValueOrDefault(T defaultValue = default!) =>
        IsSuccess ? Value : defaultValue;

    public T GetValueOrThrow() =>
        IsSuccess ? Value : throw new InvalidOperationException($"Resultado em falha: {string.Join(", ", Errors)}");

    // Match pattern para programação funcional
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<IReadOnlyList<string>, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value) : onFailure(Errors);

    public async Task<TResult> MatchAsync<TResult>(Func<T, Task<TResult>> onSuccess, Func<IReadOnlyList<string>, Task<TResult>> onFailure) =>
        IsSuccess ? await onSuccess(Value) : await onFailure(Errors);
}
