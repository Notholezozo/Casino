using Casino.Application.Blackjack.Contracts;
using Casino.Domain.Blackjack;
using Casino.Domain.Cards;

namespace Casino.Application.Blackjack;

/// <summary>
/// Projette l'état du moteur vers les vues envoyées aux clients.
/// C'est ici qu'est garanti qu'un téléphone ne reçoit jamais les cartes des autres ni la carte cachée du croupier.
/// </summary>
internal static class BlackjackViewMapper
{
    public static TableViewDto ToTableView(BlackjackGame game, string tableCode, long version, DateTimeOffset now) =>
        new(
            version,
            tableCode,
            game.Phase,
            RemainingMs(game, now),
            game.RoundNumber,
            ToDealer(game),
            game.Seats.Select(seat => new SeatDto(
                seat.PlayerId,
                seat.Name,
                seat.IsConnected,
                seat.Bet,
                seat.InRound,
                ToHands(game, seat),
                ReferenceEquals(seat, game.ActiveSeat),
                ToResult(game, seat),
                seat.TotalDrunk,
                seat.TotalGiven)).ToList(),
            game.ActiveSeat?.PlayerId,
            game.Rules.MaxPlayers,
            new ShoeDto(game.ShoeRemaining, game.ShoeTotal),
            game.ResultsAvailable ? game.Transfers.Select(ToTransfer).ToList() : []);

    public static PlayerViewDto ToPlayerView(
        BlackjackGame game, Seat seat, string tableCode, long version, DateTimeOffset now) =>
        new(
            version,
            tableCode,
            game.Phase,
            RemainingMs(game, now),
            game.RoundNumber,
            ToDealer(game),
            new PlayerSelfDto(
                seat.PlayerId,
                seat.Name,
                seat.Bet,
                seat.InRound,
                ToHands(game, seat),
                ReferenceEquals(seat, game.ActiveSeat),
                game.GetAllowedActions(seat.PlayerId),
                ToResult(game, seat),
                seat.TotalDrunk,
                seat.TotalGiven),
            game.Seats
                .Where(other => !ReferenceEquals(other, seat))
                .Select(other => new OtherPlayerDto(other.PlayerId, other.Name, other.IsConnected, other.InRound))
                .ToList(),
            game.ActiveSeat?.PlayerId,
            new BetLimitsDto(game.Rules.MinBet, game.Rules.MaxBet));

    private static int? RemainingMs(BlackjackGame game, DateTimeOffset now) =>
        game.Deadline is { } deadline
            ? Math.Max(0, (int)(deadline - now).TotalMilliseconds)
            : null;

    private static DealerDto ToDealer(BlackjackGame game)
    {
        // Tant que la carte cachée n'est pas retournée, seule la première carte quitte le serveur.
        var visible = game.DealerHoleRevealed ? game.DealerCards : game.DealerCards.Take(1).ToList();
        var value = HandEvaluator.Evaluate(visible);

        return new DealerDto(
            visible.Select(ToCard).ToList(),
            HasHiddenCard: !game.DealerHoleRevealed && game.DealerCards.Count > 1,
            value.Total,
            value.IsSoft,
            IsBlackjack: game.DealerHoleRevealed && HandEvaluator.IsNatural(game.DealerCards),
            value.IsBust);
    }

    private static List<HandDto> ToHands(BlackjackGame game, Seat seat) =>
        seat.Hands.Select(hand => new HandDto(
            hand.Cards.Select(ToCard).ToList(),
            hand.Value.Total,
            hand.Value.IsSoft,
            hand.Bet,
            hand.Status,
            hand.Outcome,
            ReferenceEquals(hand, game.ActiveHand))).ToList();

    private static RoundResultDto? ToResult(BlackjackGame game, Seat seat)
    {
        if (!game.ResultsAvailable)
        {
            return null;
        }

        var received = game.Transfers.Where(t => t.ToPlayerId == seat.PlayerId).Select(ToTransfer).ToList();
        var given = game.Transfers.Where(t => t.FromPlayerId == seat.PlayerId).Select(ToTransfer).ToList();

        return new RoundResultDto(
            seat.SipsLost,
            seat.SipsToDistribute,
            CanDistribute: game.Seats.Count > 1,
            seat.DistributionDone,
            received,
            given,
            SipsToDrink: seat.SipsLost + received.Sum(t => t.Sips));
    }

    private static CardDto ToCard(Card card) => new(card.RankLabel, card.Suit.ToString());

    private static SipTransferDto ToTransfer(SipTransfer t) =>
        new(t.FromPlayerId, t.FromPlayerName, t.ToPlayerId, t.ToPlayerName, t.Sips);
}
