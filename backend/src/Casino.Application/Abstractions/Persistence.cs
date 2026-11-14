using Casino.Domain.Blackjack;
using Casino.Domain.Entities;

namespace Casino.Application.Abstractions;

/// <summary>Accès aux tables et à leurs joueurs. Implémenté par la couche Infrastructure (EF Core).</summary>
public interface ITableRepository
{
    Task AddAsync(GameTable table, CancellationToken ct = default);

    Task AddPlayerAsync(Player player, CancellationToken ct = default);

    Task MarkPlayerLeftAsync(Guid playerId, DateTime leftAtUtc, CancellationToken ct = default);

    Task CloseAsync(Guid tableId, DateTime closedAtUtc, CancellationToken ct = default);

    /// <summary>Ferme toutes les tables encore ouvertes ; renvoie leur nombre.</summary>
    Task<int> CloseAllOpenAsync(DateTime closedAtUtc, CancellationToken ct = default);
}

/// <summary>Historique des manches.</summary>
public interface IRoundRepository
{
    /// <summary>Enregistre une manche terminée et met à jour les cumuls de gorgées des joueurs.</summary>
    Task AddAsync(Round round, IReadOnlyList<PlayerTotals> totals, CancellationToken ct = default);
}
