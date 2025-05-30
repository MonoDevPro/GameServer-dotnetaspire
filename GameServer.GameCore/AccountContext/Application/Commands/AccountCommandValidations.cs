using GameServer.Shared.CQRS.Examples;
using GameServer.Shared.CQRS.Validation;

namespace GameServer.GameCore.AccountContext.Application.Commands;

public class AccountCommandValidations
{
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
}