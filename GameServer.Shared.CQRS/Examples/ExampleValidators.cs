using GameServer.Shared.CQRS.Validation;

namespace GameServer.Shared.CQRS.Examples;

/// <summary>
/// Exemplo de comando para demonstrar validação
/// </summary>
public class CreateUserCommand
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int Age { get; set; }
    public Guid? ProfileId { get; set; }
}

/// <summary>
/// Exemplo de validador para o comando CreateUserCommand
/// </summary>
public class CreateUserCommandValidator : BaseValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        // Validação de Username
        RuleForNotEmpty(cmd => cmd.Username, "Username é obrigatório");
        RuleForMinLength(cmd => cmd.Username, 3, "Username deve ter pelo menos 3 caracteres");
        RuleForMaxLength(cmd => cmd.Username, 50, "Username deve ter no máximo 50 caracteres");
        RuleFor(cmd => cmd.Username,
            username => ValidationUtils.IsAlphaNumeric(username),
            "Username deve conter apenas letras e números");

        // Validação de Email
        RuleForNotEmpty(cmd => cmd.Email, "Email é obrigatório");
        RuleFor(cmd => cmd.Email,
            email => ValidationUtils.IsValidEmail(email),
            "Email deve ter um formato válido");

        // Validação de Password
        RuleForNotEmpty(cmd => cmd.Password, "Password é obrigatório");
        RuleFor(cmd => cmd.Password,
            password => ValidationUtils.IsStrongPassword(password),
            "Password deve ter pelo menos 8 caracteres, incluindo maiúscula, minúscula, número e caractere especial");

        // Validação de Age
        RuleForRange(cmd => cmd.Age, 13, 120, "Idade deve estar entre 13 e 120 anos");

        // Validação de ProfileId (opcional, mas se fornecido deve ser válido)
        AddRule(cmd => cmd.ProfileId == null || ValidationUtils.IsValidGuid(cmd.ProfileId),
            "ProfileId deve ser um GUID válido se fornecido");
    }
}

/// <summary>
/// Exemplo de validador composto que combina múltiplas validações
/// </summary>
public class CreateUserCommandCompositeValidator : CompositeValidator<CreateUserCommand>
{
    public CreateUserCommandCompositeValidator() : base(
        new CreateUserCommandValidator(),
        new CreateUserBusinessRulesValidator())
    {
    }
}

/// <summary>
/// Exemplo de validador com regras de negócio específicas
/// </summary>
public class CreateUserBusinessRulesValidator : BaseValidator<CreateUserCommand>
{
    public CreateUserBusinessRulesValidator()
    {
        // Regras de negócio específicas
        AddRule(cmd => !cmd.Username.Contains("admin", StringComparison.OrdinalIgnoreCase),
            "Username não pode conter a palavra 'admin'");

        AddRule(cmd => !cmd.Email.EndsWith("@temp.com", StringComparison.OrdinalIgnoreCase),
            "Emails temporários não são permitidos");

        // Validação assíncrona seria implementada sobrescrevendo ValidateAsync
        // Por exemplo, verificar se username já existe no banco
    }
}

/// <summary>
/// Exemplo de validador customizado com lógica complexa
/// </summary>
public class CreateUserCustomValidator : IValidator<CreateUserCommand>
{
    public ValidationResult Validate(CreateUserCommand instance)
    {
        var errors = new List<ValidationError>();

        // Validação customizada complexa
        if (instance.Age < 18 && instance.Email.Contains("work"))
        {
            errors.Add(new ValidationError(
                "Menores de idade não podem usar emails corporativos",
                nameof(instance.Email),
                "UNDERAGE_WORK_EMAIL"));
        }

        // Múltiplas validações customizadas
        if (instance.Username.Length > 20 && instance.Age > 60)
        {
            errors.Add(new ValidationError(
                "Usuários com mais de 60 anos devem usar usernames mais curtos",
                nameof(instance.Username),
                "SENIOR_USERNAME_LENGTH"));
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
    }

    public Task<ValidationResult> ValidateAsync(CreateUserCommand instance, CancellationToken cancellationToken = default)
    {
        // Aqui poderia fazer validações assíncronas, como verificar no banco de dados
        return Task.FromResult(Validate(instance));
    }

    public ValidationResult Validate(ValidationContext<CreateUserCommand> context)
    {
        // Usar contexto para validações mais específicas
        var result = Validate(context.Instance);

        // Adicionar informações do contexto se necessário
        if (context.Properties.ContainsKey("IsUpdate"))
        {
            // Lógica específica para updates
        }

        return result;
    }

    public Task<ValidationResult> ValidateAsync(ValidationContext<CreateUserCommand> context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Validate(context));
    }
}
