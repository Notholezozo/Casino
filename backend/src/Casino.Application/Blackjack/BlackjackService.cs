using Casino.Application.Blackjack.Contracts;
using Casino.Application.Common;
using Casino.Application.Tables;
using Casino.Domain.Blackjack;

namespace Casino.Application.Blackjack;

/// <summary>Cas d'usage du blackjack : relaie les décisions des joueurs au moteur et diffuse le résultat.</summary>
public interface IBlackjackService
{
    Task PlaceBetAsync(SessionIdentity player, int sips, CancellationToken ct = default);

    Task ActAsync(SessionIdentity player, PlayerAction action, CancellationToken ct = default);

    Task DistributeAsync(SessionIdentity player, IReadOnlyList<SipAllocationDto>? allocations, CancellationToken ct = default);

    PlayerViewDto GetPlayerView(SessionIdentity player);

    TableViewDto GetTableView(SessionIdentity admin);
}

internal sealed class BlackjackService(
    ITableRegistry registry,
    ISnapshotDispatcher dispatcher,
    TimeProvider clock) : IBlackjackService
{
    public Task PlaceBetAsync(SessionIdentity player, int sips, CancellationToken ct = default) =>
        ExecuteAsync(player, (game, playerId, now) => game.PlaceBet(playerId, sips, now), ct);

    public Task ActAsync(SessionIdentity player, PlayerAction action, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(action))
        {
            throw new AppException(ErrorKind.Validation, "invalid_action", "Action inconnue.");
        }

        return ExecuteAsync(player, (game, playerId, now) => game.Act(playerId, action, now), ct);
    }

    public Task DistributeAsync(
        SessionIdentity player, IReadOnlyList<SipAllocationDto>? allocations, CancellationToken ct = default)
    {
        // Plusieurs lignes pour un même joueur sont additionnées.
        var byPlayer = (allocations ?? [])
            .GroupBy(a => a.PlayerId)
            .ToDictionary(g => g.Key, g => g.Sum(a => a.Sips));

        return ExecuteAsync(player, (game, playerId, now) => game.Distribute(playerId, byPlayer, now), ct);
    }

    public PlayerViewDto GetPlayerView(SessionIdentity player) =>
        GetSession(player).GetPlayerView(RequirePlayerId(player), clock.GetUtcNow())
        ?? throw AppException.TableNotFound();

    public TableViewDto GetTableView(SessionIdentity admin) =>
        GetSession(admin).GetTableView(clock.GetUtcNow());

    private Task ExecuteAsync(
        SessionIdentity player, Action<BlackjackGame, Guid, DateTimeOffset> command, CancellationToken ct)
    {
        var session = GetSession(player);
        var playerId = RequirePlayerId(player);
        var now = clock.GetUtcNow();

        var snapshot = session.Mutate(now, game => command(game, playerId, now));
        return dispatcher.DispatchAsync(snapshot, ct);
    }

    private TableSession GetSession(SessionIdentity identity) =>
        registry.TryGet(identity.TableCode, out var session) ? session : throw AppException.TableNotFound();

    private static Guid RequirePlayerId(SessionIdentity identity) =>
        identity.PlayerId
        ?? throw new AppException(ErrorKind.Forbidden, "forbidden", "Cette action est réservée aux joueurs.");
}
