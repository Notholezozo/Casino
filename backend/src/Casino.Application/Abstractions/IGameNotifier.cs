namespace Casino.Application.Abstractions;

/// <summary>
/// Pousse l'état du jeu vers les clients connectés. Implémenté par l'API (SignalR).
/// Les vues sont propres à chaque jeu : elles voyagent ici comme de simples objets à sérialiser.
/// </summary>
public interface IGameNotifier
{
    Task SendPlayerStateAsync(Guid playerId, object view);

    Task SendTableStateAsync(string tableCode, object view);

    /// <summary>Prévient tous les clients d'une table qu'elle vient d'être fermée.</summary>
    Task SendTableClosedAsync(string tableCode);

    /// <summary>Prévient un joueur qu'il a été retiré de la table.</summary>
    Task SendRemovedAsync(Guid playerId);
}
