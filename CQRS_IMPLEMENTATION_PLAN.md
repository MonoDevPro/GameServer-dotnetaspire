# Plano de Implementação - Melhorias CQRS

## 🚀 Fase 1: Shared Library Foundation

### 1.1 Criar Projeto Shared

```bash
cd /home/filipe/Desenvolvimento/NOVOSERVER-ASPIRE/GameServer
dotnet new classlib -n GameServer.Shared.CQRS
dotnet sln add GameServer.Shared.CQRS/GameServer.Shared.CQRS.csproj
```

### 1.2 Estrutura Proposta

```
GameServer.Shared.CQRS/
├── Commands/
│   ├── ICommand.cs
│   ├── ICommandHandler.cs
│   ├── ICommandBus.cs
│   └── CommandBus.cs
├── Queries/
│   ├── IQuery.cs
│   ├── IQueryHandler.cs
│   ├── IQueryBus.cs
│   └── QueryBus.cs
├── Results/
│   ├── Result.cs
│   └── ResultExtensions.cs
├── Behaviors/
│   ├── ICommandBehavior.cs
│   └── IQueryBehavior.cs
└── DependencyInjection/
    └── ServiceCollectionExtensions.cs
```

## 🔧 Implementações Específicas

### 1. Result<T> Unificado e Robusto

```csharp
// GameServer.Shared.CQRS/Results/Result.cs
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

    // Conversões implícitas
    public static implicit operator Result(Result<T> result) =>
        result.IsSuccess ? Result.Success() : Result.Failure(result.Errors);

    // Tentativa de obter valor com segurança
    public bool TryGetValue([NotNullWhen(true)] out T? value)
    {
        value = IsSuccess ? Value : default;
        return IsSuccess;
    }
}
```

### 2. Interfaces Padronizadas

```csharp
// GameServer.Shared.CQRS/Commands/ICommand.cs
namespace GameServer.Shared.CQRS.Commands;

/// <summary>
/// Marcador para comandos que não retornam dados específicos
/// </summary>
public interface ICommand
{
}

/// <summary>
/// Interface para comandos que retornam dados específicos
/// </summary>
/// <typeparam name="TResult">Tipo do resultado do comando</typeparam>
public interface ICommand<TResult> : ICommand
{
}
```

```csharp
// GameServer.Shared.CQRS/Commands/ICommandHandler.cs
using GameServer.Shared.CQRS.Results;

namespace GameServer.Shared.CQRS.Commands;

/// <summary>
/// Handler para comandos que não retornam dados específicos
/// </summary>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Handler para comandos que retornam dados específicos
/// </summary>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
```

```csharp
// GameServer.Shared.CQRS/Commands/ICommandBus.cs
using GameServer.Shared.CQRS.Results;

namespace GameServer.Shared.CQRS.Commands;

/// <summary>
/// Interface unificada para o barramento de comandos
/// </summary>
public interface ICommandBus
{
    /// <summary>
    /// Envia um comando que não retorna dados específicos
    /// </summary>
    Task<Result> SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand;

    /// <summary>
    /// Envia um comando que retorna dados específicos
    /// </summary>
    Task<Result<TResult>> SendAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>;
}
```

### 3. CommandBus Robusto com Logging e Error Handling

```csharp
// GameServer.Shared.CQRS/Commands/CommandBus.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GameServer.Shared.CQRS.Results;

namespace GameServer.Shared.CQRS.Commands;

public class CommandBus : ICommandBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CommandBus> _logger;

    public CommandBus(IServiceProvider serviceProvider, ILogger<CommandBus> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        var commandName = typeof(TCommand).Name;
        using var scope = _logger.BeginScope("Executing command {CommandName}", commandName);
        
        _logger.LogInformation("Iniciando execução do comando {CommandName}", commandName);

        try
        {
            var handler = _serviceProvider.GetService<ICommandHandler<TCommand>>();
            if (handler == null)
            {
                var errorMessage = $"Nenhum handler encontrado para o comando {commandName}";
                _logger.LogError(errorMessage);
                return Result.Failure(errorMessage);
            }

            var result = await handler.HandleAsync(command, cancellationToken);
            
            if (result.IsSuccess)
                _logger.LogInformation("Comando {CommandName} executado com sucesso", commandName);
            else
                _logger.LogWarning("Comando {CommandName} falhou: {Errors}", commandName, string.Join("; ", result.Errors));

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Comando {CommandName} foi cancelado", commandName);
            return Result.Failure("Operação cancelada");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao executar comando {CommandName}", commandName);
            return Result.Failure($"Erro interno: {ex.Message}");
        }
    }

    public async Task<Result<TResult>> SendAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>
    {
        var commandName = typeof(TCommand).Name;
        using var scope = _logger.BeginScope("Executing command {CommandName} with result {ResultType}", commandName, typeof(TResult).Name);
        
        _logger.LogInformation("Iniciando execução do comando {CommandName}", commandName);

        try
        {
            var handler = _serviceProvider.GetService<ICommandHandler<TCommand, TResult>>();
            if (handler == null)
            {
                var errorMessage = $"Nenhum handler encontrado para o comando {commandName}";
                _logger.LogError(errorMessage);
                return Result<TResult>.Failure(errorMessage);
            }

            var result = await handler.HandleAsync(command, cancellationToken);
            
            if (result.IsSuccess)
                _logger.LogInformation("Comando {CommandName} executado com sucesso", commandName);
            else
                _logger.LogWarning("Comando {CommandName} falhou: {Errors}", commandName, string.Join("; ", result.Errors));

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Comando {CommandName} foi cancelado", commandName);
            return Result<TResult>.Failure("Operação cancelada");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao executar comando {CommandName}", commandName);
            return Result<TResult>.Failure($"Erro interno: {ex.Message}");
        }
    }
}
```

## 📋 Fase 2: Migração do GameCore

### 2.1 Atualizar GameCore.csproj
```xml
<PackageReference Include="GameServer.Shared.CQRS" Version="1.0.0" />
```

### 2.2 Criar Adapter Temporário

```csharp
// GameServer.GameCore/Shared/CQRS/Legacy/ResultAdapter.cs
using OldResult = GameServer.GameCore.Shared.CQRS.Results.Result;
using NewResult = GameServer.Shared.CQRS.Results.Result;

namespace GameServer.GameCore.Shared.CQRS.Legacy;

/// <summary>
/// Adapter temporário para migração gradual dos Results
/// </summary>
public static class ResultAdapter
{
    public static NewResult ToNew(OldResult oldResult)
    {
        return oldResult.IsSuccess 
            ? NewResult.Success() 
            : NewResult.Failure(oldResult.Errors);
    }

    public static NewResult<T> ToNew<T>(OldResult<T> oldResult)
    {
        return oldResult.IsSuccess 
            ? NewResult<T>.Success(oldResult.Value!) 
            : NewResult<T>.Failure(oldResult.Errors);
    }

    public static OldResult ToOld(NewResult newResult)
    {
        return newResult.IsSuccess 
            ? OldResult.Success() 
            : OldResult.Failure(newResult.Errors);
    }

    public static OldResult<T> ToOld<T>(NewResult<T> newResult)
    {
        return newResult.IsSuccess 
            ? OldResult<T>.Success(newResult.Value) 
            : OldResult<T>.Failure(newResult.Errors);
    }
}
```

## 📋 Fase 3: Script de Migração Automatizada

```bash
#!/bin/bash
# migration-script.sh

echo "🚀 Iniciando migração CQRS..."

# 1. Criar shared library
echo "📦 Criando GameServer.Shared.CQRS..."
dotnet new classlib -n GameServer.Shared.CQRS
dotnet sln add GameServer.Shared.CQRS/GameServer.Shared.CQRS.csproj

# 2. Adicionar dependências necessárias
cd GameServer.Shared.CQRS
dotnet add package Microsoft.Extensions.DependencyInjection.Abstractions
dotnet add package Microsoft.Extensions.Logging.Abstractions
cd ..

# 3. Referenciar shared library nos projetos
echo "🔗 Adicionando referências..."
dotnet add GameServer.GameCore/GameServer.GameCore.csproj reference GameServer.Shared.CQRS/GameServer.Shared.CQRS.csproj
dotnet add GameServer.AccountService/GameServer.AccountService.csproj reference GameServer.Shared.CQRS/GameServer.Shared.CQRS.csproj

# 4. Build para verificar
echo "🔨 Testando build..."
dotnet build

echo "✅ Migração base concluída!"
```

## 🧪 Testes Unitários para Validação

```csharp
// GameServer.Shared.CQRS.Tests/Results/ResultTests.cs
using GameServer.Shared.CQRS.Results;
using Xunit;

namespace GameServer.Shared.CQRS.Tests.Results;

public class ResultTests
{
    [Fact]
    public void Success_Should_Create_Successful_Result()
    {
        // Act
        var result = Result.Success();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Success_With_Value_Should_Create_Successful_Result()
    {
        // Arrange
        const string value = "test";

        // Act
        var result = Result<string>.Success(value);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_Should_Create_Failed_Result()
    {
        // Arrange
        const string error = "Test error";

        // Act
        var result = Result.Failure(error);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
        Assert.Equal(error, result.Errors.First());
    }

    [Fact]
    public void Value_On_Failed_Result_Should_Throw()
    {
        // Arrange
        var result = Result<string>.Failure("Error");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Map_On_Success_Should_Transform_Value()
    {
        // Arrange
        var result = Result<int>.Success(5);

        // Act
        var mapped = result.Map(x => x.ToString());

        // Assert
        Assert.True(mapped.IsSuccess);
        Assert.Equal("5", mapped.Value);
    }

    [Fact]
    public void Map_On_Failure_Should_Propagate_Errors()
    {
        // Arrange
        var result = Result<int>.Failure("Error");

        // Act
        var mapped = result.Map(x => x.ToString());

        // Assert
        Assert.False(mapped.IsSuccess);
        Assert.Equal(result.Errors, mapped.Errors);
    }
}
```

## 📚 Próximos Passos

### Imediatos (Semana 1):
1. ✅ Executar script de criação da shared library
2. ✅ Implementar Result<T> unificado
3. ✅ Criar testes unitários
4. ✅ Validar build sem erros

### Médio Prazo (Semanas 2-3):
1. 🔄 Migrar GameCore para usar shared library
2. 🔄 Atualizar AccountService
3. 🔄 Criar adapters para compatibilidade

### Longo Prazo (Semanas 4-6):
1. 🎯 Migrar AuthService (Operation → Result)
2. 🎯 Remover código legacy
3. 🎯 Documentar padrões finais
4. 🎯 Treinamento da equipe

---

**Status:** Pronto para implementação  
**Complexidade:** Média  
**Impacto:** Alto  
**Risco:** Baixo (com migração gradual)
