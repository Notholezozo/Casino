using Casino.Application.Blackjack;
using Casino.Application.Blackjack.Contracts;
using Casino.Application.Common;
using Casino.Application.Poker.Contracts;
using Casino.Application.Tables;
using Casino.Domain.Cards;
using Casino.Domain.Poker;

namespace Casino.Application.Poker;

/// <summary>Cas d'usage du poker : relaie les décisions des joueurs au moteur et diffuse le résultat.</summary>
public interface IPokerService
{
    Task ActAsync(SessionIdentity player, PokerAction action, int raiseBy, CancellationToken ct = default);

    Task DistributeAsync(SessionIdentity player, IReadOnlyList<DrinkAllocationDto>? allocations, CancellationToken ct = default);
}

internal sealed class PokerService(IGameCommandRunner runner) : IPokerService
{
    public Task ActAsync(SessionIdentity player, PokerAction action, int raiseBy, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(action))
        {
            throw new AppException(ErrorKind.Validation, "invalid_action", "Action inconnue.");
        }

        return runner.RunAsync<PokerGame>(
            player, (game, playerId, now) => game.Act(playerId, action, raiseBy, now), ct);
    }

    public Task DistributeAsync(
        SessionIdentity player, IReadOnlyList<DrinkAllocationDto>? allocations, CancellationToken ct = default)
    {
        var byPlayer = DrinkAllocations.ByPlayer(allocations);
        return runner.RunAsync<PokerGame>(
            player, (game, playerId, now) => game.Distribute(playerId, byPlayer, now), ct);
    }
}

/// <summary>
/// Vues du poker pour une table donnée. C'est ici qu'est garanti que les cartes privées ne
/// quittent le serveur que vers leur propriétaire, ou vers tous à l'abattage.
/// </summary>
internal sealed class PokerViewBuilder(PokerGame game) : IGameViewBuilder
{
    public object BuildTableView(string tableCode, long version, DateTimeOffset now) =>
        new PokerTableViewDto
        {
            Version = version,
            TableCode = tableCode,
            Phase = game.Phase,
            RemainingMs = RemainingMs(now),
            HandNumber = game.HandNumber,
            Board = game.Board.Select(ToCard).ToList(),
            Pot = game.Pot,
            CurrentBet = game.CurrentBet,
            Seats = game.Seats.Select(seat => ToSeat(seat, viewerId: null)).ToList(),
            ActivePlayerId = game.ActiveSeat?.PlayerId,
            MaxPlayers = game.Rules.MaxPlayers,
            Limits = ToLimits(),
            Showdown = game.Showdown,
            ResultsAvailable = game.ResultsAvailable,
            Transfers = ToTransfers(),
        };

    public object? BuildPlayerView(Guid playerId, string tableCode, long version, DateTimeOffset now)
    {
        var me = game.Seats.FirstOrDefault(s => s.PlayerId == playerId);
        if (me is null)
        {
            return null;
        }

        var isMyTurn = ReferenceEquals(me, game.ActiveSeat);
        return new PokerPlayerViewDto
        {
            Version = version,
            TableCode = tableCode,
            Phase = game.Phase,
            RemainingMs = RemainingMs(now),
            HandNumber = game.HandNumber,
            Board = game.Board.Select(ToCard).ToList(),
            Pot = game.Pot,
            CurrentBet = game.CurrentBet,
            Seats = game.Seats.Select(seat => ToSeat(seat, playerId)).ToList(),
            ActivePlayerId = game.ActiveSeat?.PlayerId,
            MaxPlayers = game.Rules.MaxPlayers,
            Limits = ToLimits(),
            Showdown = game.Showdown,
            ResultsAvailable = game.ResultsAvailable,
            Transfers = ToTransfers(),
            Me = new PokerSelfDto(
                playerId,
                isMyTurn,
                game.GetAllowedActions(playerId),
                isMyTurn ? game.AmountToCall(me) : 0,
                CanDistribute: game.Seats.Count > 1),
        };
    }

    private PokerSeatDto ToSeat(PokerSeat seat, Guid? viewerId)
    {
        var hasCards = seat.HoleCards.Count > 0;
        var visible = hasCards && (seat.PlayerId == viewerId || (game.Showdown && seat.IsLive));

        return new PokerSeatDto(
            seat.PlayerId,
            seat.Name,
            seat.IsConnected,
            seat.InHand,
            seat.Folded,
            hasCards,
            visible ? seat.HoleCards.Select(ToCard).ToList() : null,
            seat.StreetBet,
            seat.TotalBet,
            IsActive: ReferenceEquals(seat, game.ActiveSeat),
            IsButton: ReferenceEquals(seat, game.ButtonSeat),
            IsSmallBlind: ReferenceEquals(seat, game.SmallBlindSeat),
            IsBigBlind: ReferenceEquals(seat, game.BigBlindSeat),
            seat.IsWinner,
            seat.HandName,
            seat.ToDistribute,
            seat.DistributionDone,
            SipsToDrink: game.ResultsAvailable
                ? game.Transfers.Where(t => t.ToPlayerId == seat.PlayerId).Sum(t => t.Amount)
                : 0,
            new DrinkTotalsDto(
                seat.Totals.SipsDrunk, seat.Totals.SipsGiven, seat.Totals.CulsSecsDrunk, seat.Totals.CulsSecsGiven));
    }

    private int? RemainingMs(DateTimeOffset now) =>
        game.Deadline is { } deadline ? Math.Max(0, (int)(deadline - now).TotalMilliseconds) : null;

    private PokerLimitsDto ToLimits() =>
        new(game.Rules.SmallBlind, game.Rules.BigBlind, game.Rules.MinRaise, game.Rules.MaxRaise);

    private List<DrinkTransferDto> ToTransfers() =>
        game.ResultsAvailable
            ? game.Transfers
                .Select(t => new DrinkTransferDto(
                    t.FromPlayerId, t.FromPlayerName, t.ToPlayerId, t.ToPlayerName, t.Amount, t.Unit))
                .ToList()
            : [];

    private static CardDto ToCard(Card card) => new(card.RankLabel, card.Suit.ToString());
}
