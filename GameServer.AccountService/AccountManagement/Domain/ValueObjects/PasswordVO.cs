using System.Text.RegularExpressions;
using GameServer.AccountService.AccountManagement.Domain.Exceptions;
using GameServer.AccountService.AccountManagement.Domain.ValueObjects.Base;
using GameServer.AccountService.AccountManagement.Ports.Out.Security;

namespace GameServer.AccountService.AccountManagement.Domain.ValueObjects
{
    public enum PasswordStrength
    {
        Weak,
        Medium,
        Strong
    }

    public sealed class PasswordVO : ValueObject<PasswordVO>
    {
        private const int MinLength = 8;
        private const int MaxLength = 100;

        public string Hash { get; private set; }
        public PasswordStrength Strength { get; private set; }
        
        private PasswordVO() { }

        private PasswordVO(
            string existingHash, 
            PasswordStrength strength
            )
        {
            Hash = existingHash ?? throw new ArgumentNullException(nameof(existingHash));
            Strength = strength;
        }

        /// <summary>
        /// Cria e hasheia a senha a partir do texto claro através do serviço de hash.
        /// </summary>
        public static PasswordVO Create(
            string clearTextPassword,
            IPasswordHashService hasher
            )
        {
            ValidatePassword(clearTextPassword);
            var strength = EvaluateStrength(clearTextPassword);
            var hash = hasher.Hash(clearTextPassword);
            return new PasswordVO(hash, strength);
        }

        /// <summary>
        /// Recria o VO a partir de um hash existente (ex.: ao materializar do banco).
        /// </summary>
        public static PasswordVO FromHash(
            string existingHash,
            PasswordStrength strength = PasswordStrength.Medium
            )
        {
            if (string.IsNullOrWhiteSpace(existingHash))
                throw new ArgumentNullException(nameof(existingHash));

            // Strength desconhecida ao reconstituir do banco, assume Medium
            return new PasswordVO(existingHash, strength);
        }

        /// <summary>
        /// Verifica se o texto claro corresponde ao hash armazenado.
        /// </summary>
        public bool Verify(
            string clearTextPassword,
            IPasswordHashService hasher
            )
        {
            if (string.IsNullOrEmpty(clearTextPassword))
                throw new ArgumentNullException(nameof(clearTextPassword));

            return hasher.Verify(clearTextPassword, Hash);
        }

        /// <summary>
        /// Garante que a senha obedeça às regras de segurança.
        /// </summary>
        /// <summary>
        /// Garante que a senha obedeça às regras de segurança.
        /// </summary>
        public static void ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new DomainException("Senha não pode ser nula ou vazia");

            if (password.Length < MinLength)
                throw new DomainException($"Senha deve ter no mínimo {MinLength} caracteres");

            if (password.Length > MaxLength)
                throw new DomainException($"Senha não pode exceder {MaxLength} caracteres");

            // Cache dos resultados de Regex para evitar múltiplas execuções
            bool hasUpperCase = Regex.IsMatch(password, "[A-Z]");
            bool hasLowerCase = Regex.IsMatch(password, "[a-z]");
            bool hasDigit = Regex.IsMatch(password, "[0-9]");
            bool hasSpecialChar = Regex.IsMatch(password, @"[!@#$%^&*(),.?"":\{\}|<>]");

            if (!hasUpperCase || !hasLowerCase || !hasDigit || !hasSpecialChar)
            {
                throw new DomainException("Senha deve conter pelo menos uma letra maiúscula, uma minúscula, um número e um caractere especial");
            }
        }

        /// <summary>
        /// Avalia a força da senha com base em comprimento e diversidade de caracteres.
        /// </summary>
        private static PasswordStrength EvaluateStrength(string password)
        {
            int score = 0;
    
            // Verificações de comprimento
            if (password.Length >= MinLength) score++;
            if (password.Length >= 12) score++;
    
            // Cache dos resultados de Regex para evitar múltiplas execuções
            bool hasUpperCase = Regex.IsMatch(password, "[A-Z]");
            bool hasLowerCase = Regex.IsMatch(password, "[a-z]");
            bool hasDigit = Regex.IsMatch(password, "[0-9]");
            bool hasSpecialChar = Regex.IsMatch(password, @"[!@#$%^&*(),.?"":\{\}|<>]");
    
            // Atribuição de pontos
            if (hasUpperCase) score++;
            if (hasLowerCase) score++;
            if (hasDigit) score++;
            if (hasSpecialChar) score++;

            return score switch
            {
                <= 2 => PasswordStrength.Weak,
                <= 4 => PasswordStrength.Medium,
                _ => PasswordStrength.Strong
            };
        }

        protected override bool EqualsCore(PasswordVO? other) =>
            Hash == other?.Hash && Strength == other.Strength;

        protected override int ComputeHashCode() =>
            HashCode.Combine(Hash, Strength);

        public override string ToString() =>
            $"PasswordVO[Strength={Strength}]";
    }
}
