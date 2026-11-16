using Casino.Application.Blackjack.Contracts;
using Casino.Application.Common;
using Casino.Application.Tables;
using Casino.Domain.Blackjack;

namespace Casino.Application.Blackjack;

/// <summary>Cas d'usage du blackjack : relaie les décisions des joueurs au moteur et diffuse le résultat.</summary>
public interface IBlackjackService
{
    Task PlaceBetAsync(SessionIdentity player, Bet bet, CancellationToken ct = default);

    Task ActAsync(SessionIdentity player, PlayerAction action, CancellationToken ct = default);

    Task DistributeAsync(SessionIdentity player, IReadOnlyList<DrinkAllocationDto>? allocations, CancellationToken ct = default);
}

internal sealed class BlackjackService(IGameCommandRunner runner) : IBlackjackService
{
    public Task PlaceBetAsync(SessionIdentity player, Bet bet, CancellationToken ct = default) =>
        runner.RunAsync<BlackjackGame>(player, (game, playerId, now) => game.PlaceBet(playerId, bet, now), ct);

    public Task ActAsync(SessionIdentity player, PlayerAction action, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(action))
        {
            throw new AppException(ErrorKind.Validation, "invalid_action", "Action inconnue.");
        }

        return runner.RunAsync<BlackjackGame>(player, (game, playerId, now) => game.Act(playerId, action, now), ct);
    }

    public Task DistributeAsync(
        SessionIdentity player, IReadOnlyList<DrinkAllocationDto>? allocations, CancellationToken ct = default)
    {
        var byPlayer = DrinkAllocations.ByPlayer(allocations);
        return runner.RunAsync<BlackjackGame>(
            player, (game, playerId, now) => game.Distribute(playerId, byPlayer, now), ct);
    }
}

/// <summary>Regroupe une répartition reçue de l'API : plusieurs lignes pour un même joueur sont additionnées.</summary>
internal static class DrinkAllocations
{
    public static IReadOnlyDictionary<Guid, int> ByPlayer(IReadOnlyList<DrinkAllocationDto>? allocations) =>
        (allocations ?? [])
            .GroupBy(a => a.PlayerId)
            .ToDictionary(g => g.Key, g => g.Sum(a => a.Amount));
}

/// <summary>Vues du blackjack pour une table donnée.</summary>
internal sealed class BlackjackViewBuilder(BlackjackGame game) : IGameViewBuilder
{
    public object BuildTableView(string tableCode, long version, DateTimeOffset now) =>
        BlackjackViewMapper.ToTableView(game, tableCode, version, now);

    public object? BuildPlayerView(Guid playerId, string tableCode, long version, DateTimeOffset now) =>
        game.Seats.FirstOrDefault(s => s.PlayerId == playerId) is { } seat
            ? BlackjackViewMapper.ToPlayerView(game, seat, tableCode, version, now)
            : null;
}
