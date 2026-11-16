using Casino.Domain.Blackjack;

namespace Casino.Domain.Common;

/// <summary>Un joueur assis à une table, quel que soit le jeu.</summary>
public sealed record TablePlayer(Guid Id, string Name);

/// <summary>
/// Ce que tout jeu de table sait faire, vu de l'extérieur : accueillir des joueurs et avancer
/// avec l'horloge. Les décisions de jeu (miser, tirer, relancer…) restent propres à chaque moteur.
/// <para>
/// Comme les moteurs eux-mêmes, une implémentation est pure (ni horloge ni E/S) et n'est pas
/// thread-safe : l'appelant fournit l'instant courant et sérialise les accès.
/// </para>
/// </summary>
public interface ITableGame
{
    int MaxPlayers { get; }

    IReadOnlyList<TablePlayer> Players { get; }

    void AddPlayer(Guid playerId, string name);

    void RemovePlayer(Guid playerId, DateTimeOffset now);

    void SetConnected(Guid playerId, bool isConnected, DateTimeOffset now);

    /// <summary>Fait avancer la phase en cours si son échéance est passée. Renvoie vrai si l'état a changé.</summary>
    bool Tick(DateTimeOffset now);

    /// <summary>Renvoie la manche qui vient de se terminer, une seule fois, pour l'historique.</summary>
    RoundSummary? TakeCompletedRound();
}
