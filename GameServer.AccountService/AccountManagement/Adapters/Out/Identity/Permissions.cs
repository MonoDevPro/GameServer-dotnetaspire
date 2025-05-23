namespace GameServer.AccountService.AccountManagement.Adapters.Out.Identity;

/// <summary>
/// Constantes para permissões de usuário no sistema
/// </summary>
public static class Permissions
{
    /// <summary>
    /// Permissões relacionadas a contas de usuário
    /// </summary>
    public static class Account
    {
        private const string Prefix = "Account.";

        /// <summary>
        /// Permissão para visualizar contas
        /// </summary>
        public const string View = Prefix + "View";

        /// <summary>
        /// Permissão para criar contas
        /// </summary>
        public const string Create = Prefix + "Create";

        /// <summary>
        /// Permissão para editar contas
        /// </summary>
        public const string Edit = Prefix + "Edit";

        /// <summary>
        /// Permissão para desativar contas
        /// </summary>
        public const string Deactivate = Prefix + "Deactivate";

        /// <summary>
        /// Permissão para banir contas
        /// </summary>
        public const string Ban = Prefix + "Ban";

        /// <summary>
        /// Permissão para remover banimento de contas
        /// </summary>
        public const string Unban = Prefix + "Unban";

        /// <summary>
        /// Permissão para alterar o tipo de conta
        /// </summary>
        public const string ChangeType = Prefix + "ChangeType";
    }

    /// <summary>
    /// Permissões relacionadas a funções administrativas
    /// </summary>
    public static class Admin
    {
        private const string Prefix = "Admin.";

        /// <summary>
        /// Permissão para acessar o painel administrativo
        /// </summary>
        public const string AccessPanel = Prefix + "AccessPanel";

        /// <summary>
        /// Permissão para gerenciar permissões de usuário
        /// </summary>
        public const string ManagePermissions = Prefix + "ManagePermissions";

        /// <summary>
        /// Permissão para gerenciar roles
        /// </summary>
        public const string ManageRoles = Prefix + "ManageRoles";

        /// <summary>
        /// Permissão para visualizar logs do sistema
        /// </summary>
        public const string ViewLogs = Prefix + "ViewLogs";

        /// <summary>
        /// Permissão para configurar o sistema
        /// </summary>
        public const string SystemConfig = Prefix + "SystemConfig";
    }

    /// <summary>
    /// Permissões relacionadas ao jogo
    /// </summary>
    public static class Game
    {
        private const string Prefix = "Game.";

        /// <summary>
        /// Permissão para acessar comandos de GM
        /// </summary>
        public const string GMCommands = Prefix + "GMCommands";

        /// <summary>
        /// Permissão para gerenciar itens
        /// </summary>
        public const string ManageItems = Prefix + "ManageItems";

        /// <summary>
        /// Permissão para gerenciar personagens
        /// </summary>
        public const string ManageCharacters = Prefix + "ManageCharacters";

        /// <summary>
        /// Permissão para moderar chat
        /// </summary>
        public const string ModerateChat = Prefix + "ModerateChat";

        /// <summary>
        /// Permissão para teleportar
        /// </summary>
        public const string Teleport = Prefix + "Teleport";

        /// <summary>
        /// Permissão para invisibilidade de GM
        /// </summary>
        public const string GMInvisibility = Prefix + "GMInvisibility";
    }
}