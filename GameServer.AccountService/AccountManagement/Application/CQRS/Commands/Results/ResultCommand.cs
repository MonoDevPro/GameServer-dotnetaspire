using GameServer.AccountService.AccountManagement.Domain.ValueObjects.Base;

namespace GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Results;

public class ResultCommand : ValueObject<ResultCommand>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyCollection<string> Errors { get; }
    public string? ErrorMessage => Errors.FirstOrDefault();
    
    private ResultCommand(bool isSuccess, IEnumerable<string>? errors)
    {
        IsSuccess = isSuccess;
        Errors = errors?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
    }
    
    public static ResultCommand Success() => new(true, null);
    public static ResultCommand Failure(string error) => new(false, new[] { error });
    public static ResultCommand Failure(IEnumerable<string> errors) => new(false, errors);
    
    protected override bool EqualsCore(ResultCommand? other)
    {
        if (other is null) return false;
        if (IsSuccess != other.IsSuccess) return false;
        
        // Para casos de sucesso, não há mais nada para comparar
        if (IsSuccess) return true;
            
        // Para casos de falha, comparar erros
        return Errors.SequenceEqual(other.Errors);
    }
    
    protected override int ComputeHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + IsSuccess.GetHashCode();
            
            if (!IsSuccess)
                foreach (var error in Errors)
                    hash = hash * 23 + (error?.GetHashCode() ?? 0);
                    
            return hash;
        }
    }
    
    public TResult Match<TResult>(
        Func<TResult> onSuccess,
        Func<IReadOnlyCollection<string>, TResult> onFailure) =>
        IsSuccess ? onSuccess() : onFailure(Errors);
}