using Casino.Application.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Casino.Application.Tables;

/// <summary>
/// Battement d'horloge du serveur : fait avancer les phases minutées de chaque table
/// et ferme les tables abandonnées. Appelé à intervalle court par un service d'arrière-plan.
/// </summary>
public interface IGameClock
{
    Task TickAsync(CancellationToken ct = default);
}

internal sealed class GameClock(
    ITableRegistry registry,
    ITableService tables,
    ISnapshotDispatcher dispatcher,
    TimeProvider clock,
    IOptions<TablesOptions> options,
    ILogger<GameClock> logger) : IGameClock
{
    public async Task TickAsync(CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();

        foreach (var session in registry.Sessions)
        {
            try
            {
                if (session.IdleSince is { } idleSince && now - idleSince >= options.Value.IdleTimeout)
                {
                    logger.LogInformation("Table {Code} inactive, fermeture automatique", session.Code);
                    await tables.CloseAsync(session, ct);
                    continue;
                }

                if (session.Tick(now) is { } snapshot)
                {
                    await dispatcher.DispatchAsync(snapshot, ct);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Une table en erreur ne doit pas figer les autres.
                logger.LogError(ex, "Erreur pendant le battement d'horloge de la table {Code}", session.Code);
            }
        }
    }
}
