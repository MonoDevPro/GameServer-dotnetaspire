# GameServer Shared Projects - Guia de Uso

## 📖 Visão Geral

Esta documentação descreve como usar os projetos compartilhados da arquitetura GameServer para garantir consistência entre todos os microsserviços.

## 🏗️ Projetos Disponíveis

### **GameServer.Shared.CQRS**
Implementação completa do padrão CQRS com observabilidade integrada.

### **GameServer.ServiceDefaults** 
Configurações padrão do .NET Aspire para todos os microsserviços.

### **GameServer.Shared.Database**
Abstrações e utilitários para acesso a dados (Read/Write repositories).

### **GameServer.Shared**
Utilitários gerais (PagedList, etc.).

## 🚀 Como Usar

### **1. Configurando CQRS em um Microsserviço**

```csharp
// Program.cs
using GameServer.Shared.CQRS.DependencyInjection;
using GameServer.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Configurações padrão do Aspire
builder.AddServiceDefaults();

// Configurar CQRS com auto-registro
builder.Services.AddCQRS(Assembly.GetExecutingAssembly());

var app = builder.Build();
app.MapDefaultEndpoints();
app.Run();
```

### **2. Criando um Command Handler**

```csharp
// Commands/CreateAccountCommand.cs
using GameServer.Shared.CQRS.Commands;

public record CreateAccountCommand(string Email, string Name) : ICommand<Guid>;

// Handlers/CreateAccountCommandHandler.cs
using GameServer.Shared.CQRS.Commands;
using GameServer.Shared.CQRS.Results;

public class CreateAccountCommandHandler : ICommandHandler<CreateAccountCommand, Guid>
{
    private readonly IAccountRepository _repository;
    
    public CreateAccountCommandHandler(IAccountRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> HandleAsync(CreateAccountCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var account = new Account(command.Email, command.Name);
            await _repository.AddAsync(account, cancellationToken);
            
            return Result<Guid>.Success(account.Id);
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure($"Erro ao criar conta: {ex.Message}");
        }
    }
}
```

### **3. Criando um Query Handler**

```csharp
// Queries/GetAccountQuery.cs
using GameServer.Shared.CQRS.Queries;

public record GetAccountQuery(Guid Id) : IQuery<AccountDto>;

public record AccountDto(Guid Id, string Email, string Name);

// Handlers/GetAccountQueryHandler.cs
using GameServer.Shared.CQRS.Queries;
using GameServer.Shared.CQRS.Results;

public class GetAccountQueryHandler : IQueryHandler<GetAccountQuery, AccountDto>
{
    private readonly IReaderRepository<Account> _repository;
    
    public GetAccountQueryHandler(IReaderRepository<Account> repository)
    {
        _repository = repository;
    }

    public async Task<Result<AccountDto>> HandleAsync(GetAccountQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var account = await _repository.QuerySingleAsync(
                filter: a => a.Id == query.Id,
                selector: a => new AccountDto(a.Id, a.Email, a.Name),
                cancellationToken);

            return account != null 
                ? Result<AccountDto>.Success(account)
                : Result<AccountDto>.Failure("Conta não encontrada");
        }
        catch (Exception ex)
        {
            return Result<AccountDto>.Failure($"Erro ao buscar conta: {ex.Message}");
        }
    }
}
```

### **4. Usando nos Controllers**

```csharp
[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IQueryBus _queryBus;

    public AccountsController(ICommandBus commandBus, IQueryBus queryBus)
    {
        _commandBus = commandBus;
        _queryBus = queryBus;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        var command = new CreateAccountCommand(request.Email, request.Name);
        var result = await _commandBus.SendAsync<CreateAccountCommand, Guid>(command);

        return result.IsSuccess 
            ? Ok(new { Id = result.Value })
            : BadRequest(new { Errors = result.Errors });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAccount(Guid id)
    {
        var query = new GetAccountQuery(id);
        var result = await _queryBus.SendAsync<GetAccountQuery, AccountDto>(query);

        return result.IsSuccess 
            ? Ok(result.Value)
            : NotFound(new { Errors = result.Errors });
    }
}
```

### **5. Usando Extension Methods**

```csharp
using GameServer.Shared.CQRS.Extensions;

// Encadeamento funcional
var result = await _commandBus.SendAsync<CreateAccountCommand, Guid>(command)
    .OnSuccessAsync(async id => await _logger.LogInformation("Conta criada: {Id}", id))
    .Map(id => new { AccountId = id, CreatedAt = DateTime.UtcNow });

// Combinando múltiplos Results
var combinedResult = ResultExtensions.Combine(
    result1,
    result2,
    result3
);
```

## 🔧 Configurações Avançadas

### **Habilitando Behaviors**

```csharp
// Para usar ValidationBehavior, instale FluentValidation
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// Os behaviors são aplicados automaticamente via Decorator pattern
```

### **Configurando Performance Monitoring**

```csharp
// Os PerformanceBehaviors são aplicados automaticamente
// Logs de performance aparecem quando comandos/queries demoram muito
```

## 📊 Observabilidade

### **Logs Automáticos**
- ✅ Início/fim de cada comando/query
- ✅ Performance timing
- ✅ Erros com stack trace
- ✅ Validações falharam

### **Métricas Disponíveis**
- ✅ Tempo de execução
- ✅ Taxa de sucesso/falha
- ✅ Contadores por tipo de comando/query

## 🎯 Melhores Práticas

1. **Sempre use Result<T>** para retornos de handlers
2. **Mantenha handlers simples** - uma responsabilidade por handler
3. **Use CancellationToken** em todas as operações assíncronas
4. **Valide inputs** usando FluentValidation
5. **Log contexto suficiente** para debugging
6. **Teste handlers isoladamente** usando mocks

## 🔍 Troubleshooting

### **Handler não encontrado**
Verifique se:
- O assembly foi registrado no `AddCQRS()`
- O handler implementa a interface correta
- O handler está com escopo correto (Scoped)

### **Performance ruim**
- Verifique logs de PerformanceBehavior
- Use projeções nas queries (selector)
- Implemente caching quando apropriado

### **Erros de validação**
- Verifique se FluentValidation está configurado
- Implemente IValidator<TCommand> para seus comandos
