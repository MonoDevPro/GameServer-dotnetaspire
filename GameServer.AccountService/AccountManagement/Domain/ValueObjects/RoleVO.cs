using GameServer.AccountService.AccountManagement.Domain.ValueObjects.Base;

namespace GameServer.AccountService.AccountManagement.Domain.ValueObjects;

public sealed class RoleVO : ValueObject<RoleVO>
{
    public string Value { get; }

    private static readonly HashSet<string> ValidRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "player",           // Jogador comum
        "vip",              // Jogador com privilégios especiais
        "moderator",        // Pode moderar chat, relatórios
        "gamemaster",       // Pode manipular elementos do jogo
        "admin",            // Acesso administrativo completo
        "support",          // Suporte ao cliente
        "contentCreator"    // Criadores de conteúdo parceiros
    };

    private RoleVO(string value)
    {
        Value = value.ToLowerInvariant();
    }

    public static RoleVO Create(string role)
    {
        Validate(role);
        return new RoleVO(role);
    }

    private static void Validate(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentNullException(nameof(role));

        if (!ValidRoles.Contains(role.ToLowerInvariant()))
            throw new ArgumentException($"Função inválida: {role}");
    }

    public bool CanModerateChat() => 
        new[] { "moderator", "gamemaster", "admin" }.Contains(Value);

    public bool CanManageAccounts() => 
        new[] { "admin" }.Contains(Value);

    protected override bool EqualsCore(RoleVO? other) => Value == other?.Value;
    protected override int ComputeHashCode() => Value.GetHashCode();
}