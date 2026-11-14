using Casino.Application.Blackjack.Contracts;

namespace Casino.Application.Abstractions;

/// <summary>Pousse l'état du jeu vers les clients connectés. Implémenté par l'API (SignalR).</summary>
public interface IGameNotifier
{
    Task SendPlayerStateAsync(Guid playerId, PlayerViewDto view);

    Task SendTableStateAsync(string tableCode, TableViewDto view);

    /// <summary>Prévient tous les clients d'une table qu'elle vient d'être fermée.</summary>
    Task SendTableClosedAsync(string tableCode);

    /// <summary>Prévient un joueur qu'il a été retiré de la table.</summary>
    Task SendRemovedAsync(Guid playerId);
}
