using GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Results;
using GameServer.AccountService.AccountManagement.Domain.ValueObjects.Base;

namespace GameServer.AccountService.AccountManagement.Application.CQRS.Queries.Results;

public class ResultQuery<T> : ValueObject<ResultQuery<T>>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public IReadOnlyCollection<string> Errors { get; }
    public string? ErrorMessage => Errors.FirstOrDefault();
    
    private ResultQuery(bool isSuccess, T? value, IEnumerable<string>? errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();

        if (isSuccess && value == null)
        {
            throw new InvalidOperationException("Um Result de sucesso não pode ter valor nulo");
        }
    }
    
    public static ResultQuery<T> Success(T value) => new(true, value, null);

    public static ResultQuery<T> Failure(string error) => new(false, default, new[] { error });

    public static ResultQuery<T> Failure(IEnumerable<string> errors) => new(false, default, errors);
    
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<IReadOnlyCollection<string>, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value!) : onFailure(Errors);
    
    protected override bool EqualsCore(ResultQuery<T>? other)
    {
        if (other is null) return false;
        if (IsSuccess != other.IsSuccess) return false;
        
        if (IsSuccess)
            return EqualityComparer<T>.Default.Equals(Value, other.Value);
            
        return Errors.SequenceEqual(other.Errors);
    }
    
    protected override int ComputeHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + IsSuccess.GetHashCode();
            
            if (IsSuccess)
                hash = hash * 23 + (Value?.GetHashCode() ?? 0);
            else
                foreach (var error in Errors)
                    hash = hash * 23 + (error?.GetHashCode() ?? 0);
                    
            return hash;
        }
    }
    
    // Método para converter Result em Result<T> (útil)
    public static implicit operator ResultQuery<T>(ResultCommand resultCommand) =>
        resultCommand.IsSuccess ? Success(default!) : Failure(resultCommand.Errors);
}