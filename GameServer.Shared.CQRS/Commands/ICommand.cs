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
