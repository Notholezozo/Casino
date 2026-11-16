using Casino.Application.Common;
using Casino.Domain.Common;

namespace Casino.Application.Tables;

/// <summary>
/// Exécute la décision d'un joueur sur le moteur de sa table, puis diffuse le résultat.
/// Partagé par les services de chaque jeu : la table et le joueur viennent du jeton.
/// </summary>
public interface IGameCommandRunner
{
    Task RunAsync<TGame>(
        SessionIdentity player,
        Action<TGame, Guid, DateTimeOffset> command,
        CancellationToken ct = default)
        where TGame : class, ITableGame;
}

internal sealed class GameCommandRunner(
    ITableRegistry registry,
    ISnapshotDispatcher dispatcher,
    TimeProvider clock) : IGameCommandRunner
{
    public Task RunAsync<TGame>(
        SessionIdentity player,
        Action<TGame, Guid, DateTimeOffset> command,
        CancellationToken ct = default)
        where TGame : class, ITableGame
    {
        if (!registry.TryGet(player.TableCode, out var session))
        {
            throw AppException.TableNotFound();
        }

        var playerId = player.PlayerId
            ?? throw new AppException(ErrorKind.Forbidden, "forbidden", "Cette action est réservée aux joueurs.");
        var now = clock.GetUtcNow();

        var snapshot = session.Mutate<TGame>(now, game => command(game, playerId, now));
        return dispatcher.DispatchAsync(snapshot, ct);
    }
}
