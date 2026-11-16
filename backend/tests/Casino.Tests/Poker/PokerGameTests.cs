using Casino.Domain.Cards;
using Casino.Domain.Common;
using Casino.Domain.Poker;
using Casino.Tests.Blackjack;

namespace Casino.Tests.Poker;

/// <summary>Jeu truqué : distribue les cartes dans l'ordre fourni (notation "AS", "10H", "7D"…).</summary>
internal sealed class StackedDeck(params string[] cards) : IShoe
{
    private readonly Queue<Card> _cards = new(cards.Select(Parse));

    public int Remaining => _cards.Count;

    public int Total => 52;

    public Card Draw() => _cards.Dequeue();

    public void Discard(IEnumerable<Card> cards)
    {
    }

    public static Card Parse(string text)
    {
        var suit = text[^1] switch
        {
            'C' => Suit.Clubs,
            'D' => Suit.Diamonds,
            'H' => Suit.Hearts,
            _ => Suit.Spades,
        };
        var rank = text[..^1] switch
        {
            "A" => Rank.Ace,
            "K" => Rank.King,
            "Q" => Rank.Queen,
            "J" => Rank.Jack,
            var number => (Rank)int.Parse(number),
        };
        return new Card(rank, suit);
    }
}

/// <summary>Monte une table de poker avec des joueurs connectés et une horloge manuelle.</summary>
internal sealed class PokerFixture
{
    public static readonly PokerTimings Timings = new();

    public PokerFixture(int players, PokerRules? rules = null, params string[] cards)
    {
        Game = new PokerGame(rules ?? new PokerRules(), Timings, () => new StackedDeck(cards), new FixedRandom());
        Players = Enumerable.Range(1, players).Select(i =>
        {
            var id = Guid.NewGuid();
            Game.AddPlayer(id, $"J{i}");
            Game.SetConnected(id, true, Now);
            return id;
        }).ToList();
    }

    public PokerGame Game { get; }

    public IReadOnlyList<Guid> Players { get; }

    public DateTimeOffset Now { get; private set; } = new(2026, 1, 1, 20, 0, 0, TimeSpan.Zero);

    public PokerSeat Seat(int index) => Game.Seats.Single(s => s.PlayerId == Players[index]);

    public void Act(int index, PokerAction action, int raiseBy = 0) =>
        Game.Act(Players[index], action, raiseBy, Now);

    /// <summary>Avance l'horloge jusqu'à l'échéance courante et la déclenche.</summary>
    public void Elapse()
    {
        Now = Game.Deadline ?? throw new InvalidOperationException("Aucune échéance en cours.");
        Assert.True(Game.Tick(Now));
    }

    /// <summary>Tout le monde checke jusqu'à l'abattage (à appeler une fois le pré-flop égalisé).</summary>
    public void CheckDown()
    {
        while (Game.IsBettingPhase)
        {
            Game.Act(Game.ActiveSeat!.PlayerId, PokerAction.Check, 0, Now);
        }
    }
}

public class PokerHandEvaluatorTests
{
    private static PokerHandValue Hand(params string[] cards) =>
        PokerHandEvaluator.Evaluate(cards.Select(StackedDeck.Parse).ToList());

    [Fact]
    public void Categories_rank_in_the_standard_order()
    {
        PokerHandValue[] ascending =
        [
            Hand("AS", "KD", "9H", "7C", "3S"),      // High card
            Hand("AS", "AD", "9H", "7C", "3S"),      // Pair
            Hand("AS", "AD", "9H", "9C", "3S"),      // Two pair
            Hand("AS", "AD", "AH", "7C", "3S"),      // Three of a kind
            Hand("9S", "8D", "7H", "6C", "5S"),      // Straight
            Hand("AS", "JS", "9S", "7S", "3S"),      // Flush
            Hand("AS", "AD", "AH", "7C", "7S"),      // Full house
            Hand("AS", "AD", "AH", "AC", "3S"),      // Four of a kind
            Hand("9S", "8S", "7S", "6S", "5S"),      // Straight flush
        ];

        Assert.Equal(Enum.GetValues<PokerHandCategory>(), ascending.Select(h => h.Category));
        Assert.Equal(ascending, ascending.OrderBy(h => h));
    }

    [Fact]
    public void Best_five_cards_are_picked_among_seven()
    {
        var value = Hand("AS", "KS", "2D", "QS", "JS", "10S", "2C");

        Assert.Equal(PokerHandCategory.StraightFlush, value.Category);
    }

    [Fact]
    public void Wheel_is_the_lowest_straight()
    {
        var wheel = Hand("AS", "2D", "3H", "4C", "5S");
        var sixHigh = Hand("2D", "3H", "4C", "5S", "6S");

        Assert.Equal(PokerHandCategory.Straight, wheel.Category);
        Assert.True(sixHigh.CompareTo(wheel) > 0);
    }

    [Fact]
    public void Kicker_breaks_a_tie_between_equal_pairs()
    {
        var aceKicker = Hand("KS", "KD", "AH", "7C", "3S");
        var queenKicker = Hand("KH", "KC", "QH", "7D", "3D");

        Assert.True(aceKicker.CompareTo(queenKicker) > 0);
    }
}

// Ordre de la donne : une carte par joueur en partant de la petite blinde, deux tours,
// puis le flop (3 cartes), le tournant et la rivière. En tête-à-tête J1 a le bouton et la
// petite blinde : les cartes vont donc à J1, J2, J1, J2.
public class PokerGameTests
{
    // J1 : A♠ A♦ (paire d'as) ; J2 : K♣ Q♣ ; tableau sans aide pour J2.
    private static readonly string[] AcesWin = ["AS", "KC", "AD", "QC", "2H", "7D", "9S", "4C", "JD"];

    [Fact]
    public void No_hand_is_dealt_with_a_single_connected_player()
    {
        var t = new PokerFixture(players: 2, cards: AcesWin);
        t.Game.SetConnected(t.Players[1], false, t.Now);

        Assert.Equal(PokerPhase.Waiting, t.Game.Phase);
        Assert.Null(t.Game.Deadline);
    }

    [Fact]
    public void Heads_up_the_button_posts_the_small_blind_and_acts_first()
    {
        var t = new PokerFixture(players: 2, cards: AcesWin);

        t.Elapse();

        Assert.Equal(PokerPhase.PreFlop, t.Game.Phase);
        Assert.Same(t.Seat(0), t.Game.ButtonSeat);
        Assert.Equal(1, t.Seat(0).TotalBet);
        Assert.Equal(2, t.Seat(1).TotalBet);
        Assert.Equal(3, t.Game.Pot);
        Assert.Same(t.Seat(0), t.Game.ActiveSeat);
        Assert.Equal([PokerAction.Fold, PokerAction.Call, PokerAction.Raise], t.Game.GetAllowedActions(t.Players[0]));
        Assert.All(t.Game.Seats, s => Assert.Equal(2, s.HoleCards.Count));
    }

    [Fact]
    public void With_three_players_blinds_follow_the_button_and_the_button_speaks_first()
    {
        var t = new PokerFixture(players: 3, cards: ["AS", "KC", "2D", "AD", "QC", "3D", "2H", "7D", "9S", "4C", "JD"]);

        t.Elapse();

        Assert.Same(t.Seat(0), t.Game.ButtonSeat);
        Assert.Same(t.Seat(1), t.Game.SmallBlindSeat);
        Assert.Same(t.Seat(2), t.Game.BigBlindSeat);
        Assert.Same(t.Seat(0), t.Game.ActiveSeat);
    }

    [Fact]
    public void Acting_out_of_turn_is_rejected()
    {
        var t = new PokerFixture(players: 2, cards: AcesWin);
        t.Elapse();

        var error = Assert.Throws<GameRuleException>(() => t.Act(1, PokerAction.Check));
        Assert.Equal("not_your_turn", error.Code);
    }

    [Fact]
    public void Big_blind_keeps_the_option_after_a_call()
    {
        var t = new PokerFixture(players: 2, cards: AcesWin);
        t.Elapse();

        t.Act(0, PokerAction.Call);

        Assert.Equal(PokerPhase.PreFlop, t.Game.Phase);
        Assert.Same(t.Seat(1), t.Game.ActiveSeat);
        Assert.Contains(PokerAction.Check, t.Game.GetAllowedActions(t.Players[1]));

        t.Act(1, PokerAction.Check);

        Assert.Equal(PokerPhase.Flop, t.Game.Phase);
        Assert.Equal(3, t.Game.Board.Count);
        // Après le flop, c'est le joueur après le bouton qui parle.
        Assert.Same(t.Seat(1), t.Game.ActiveSeat);
    }

    [Fact]
    public void Folding_gives_the_pot_without_a_showdown_and_nobody_drinks_their_own_bet()
    {
        var t = new PokerFixture(players: 2, cards: AcesWin);
        t.Elapse();

        t.Act(0, PokerAction.Fold);

        Assert.Equal(PokerPhase.Distribution, t.Game.Phase);
        Assert.False(t.Game.Showdown);
        Assert.True(t.Seat(1).IsWinner);
        Assert.Equal(3, t.Seat(1).ToDistribute);

        t.Game.Distribute(t.Players[1], new Dictionary<Guid, int> { [t.Players[0]] = 3 }, t.Now);

        Assert.Equal(PokerPhase.Results, t.Game.Phase);
        Assert.Equal(3, t.Seat(0).Totals.SipsDrunk);   // reçu du gagnant, pas sa blinde
        Assert.Equal(0, t.Seat(1).Totals.SipsDrunk);
        Assert.Equal(3, t.Seat(1).Totals.SipsGiven);
    }

    [Fact]
    public void Best_hand_wins_the_whole_pot_at_showdown()
    {
        var t = new PokerFixture(players: 2, cards: AcesWin);
        t.Elapse();
        t.Act(0, PokerAction.Raise, raiseBy: 4);   // enchère à 6
        t.Act(1, PokerAction.Call);

        t.CheckDown();

        Assert.True(t.Game.Showdown);
        Assert.Equal(5, t.Game.Board.Count);
        Assert.True(t.Seat(0).IsWinner);
        Assert.False(t.Seat(1).IsWinner);
        Assert.Equal("Pair", t.Seat(0).HandName);
        Assert.Equal("High card", t.Seat(1).HandName);
        Assert.Equal(12, t.Seat(0).ToDistribute);

        var round = t.Game.TakeCompletedRound();
        Assert.Null(round);   // l'historique n'est produit qu'une fois le pot distribué
    }

    [Fact]
    public void Equal_hands_split_the_pot()
    {
        // Les deux joueurs jouent le tableau : quinte A-K-Q-J-10.
        var t = new PokerFixture(players: 2, cards: ["2S", "3C", "2D", "3D", "AH", "KD", "QS", "JC", "10D"]);
        t.Elapse();
        t.Act(0, PokerAction.Call);
        t.Act(1, PokerAction.Check);

        t.CheckDown();

        Assert.All(t.Game.Seats, s => Assert.True(s.IsWinner));
        Assert.All(t.Game.Seats, s => Assert.Equal(2, s.ToDistribute));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Raise_outside_the_limits_is_rejected(int raiseBy)
    {
        var t = new PokerFixture(players: 2, cards: AcesWin);
        t.Elapse();

        var error = Assert.Throws<GameRuleException>(() => t.Act(0, PokerAction.Raise, raiseBy));
        Assert.Equal("invalid_raise", error.Code);
    }

    [Fact]
    public void Raises_are_capped_per_street()
    {
        var t = new PokerFixture(players: 2, cards: AcesWin);
        t.Elapse();

        t.Act(0, PokerAction.Raise, raiseBy: 1);
        t.Act(1, PokerAction.Raise, raiseBy: 1);
        t.Act(0, PokerAction.Raise, raiseBy: 1);

        Assert.Equal([PokerAction.Fold, PokerAction.Call], t.Game.GetAllowedActions(t.Players[1]));
    }

    [Fact]
    public void Timeout_checks_when_possible_and_folds_otherwise()
    {
        var t = new PokerFixture(players: 2, cards: AcesWin);
        t.Elapse();
        t.Act(0, PokerAction.Call);

        t.Elapse();   // la grosse blinde peut checker
        Assert.Equal(PokerPhase.Flop, t.Game.Phase);

        t.Act(1, PokerAction.Raise, raiseBy: 2);
        t.Elapse();   // J1 doit payer : il se couche

        Assert.True(t.Seat(0).Folded);
        Assert.True(t.Seat(1).IsWinner);
    }

    [Fact]
    public void Leaving_during_a_hand_folds_and_leaves_the_bet_in_the_pot()
    {
        var t = new PokerFixture(players: 3, cards: ["AS", "KC", "2D", "AD", "QC", "3D", "2H", "7D", "9S", "4C", "JD"]);
        t.Elapse();
        t.Act(0, PokerAction.Call);

        t.Game.RemovePlayer(t.Players[0], t.Now);

        Assert.Equal(2, t.Game.Seats.Count);
        Assert.Equal(5, t.Game.Pot);   // 1 + 2 de blindes, 2 du joueur parti
        Assert.Equal(PokerPhase.PreFlop, t.Game.Phase);
    }

    [Fact]
    public void Button_moves_to_the_next_player_each_hand()
    {
        var t = new PokerFixture(players: 2, cards: AcesWin);
        t.Elapse();
        t.Act(0, PokerAction.Fold);
        t.Elapse();   // distribution automatique
        t.Elapse();   // fin du résultat
        Assert.Equal(PokerPhase.Waiting, t.Game.Phase);

        t.Elapse();   // nouvelle donne

        Assert.Equal(2, t.Game.HandNumber);
        Assert.Same(t.Seat(1), t.Game.ButtonSeat);
    }
}
