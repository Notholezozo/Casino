using Casino.Domain.Blackjack;
using Casino.Domain.Common;
using static Casino.Domain.Cards.Rank;

namespace Casino.Tests.Blackjack;

// Ordre de la donne : une carte par joueur, une au croupier (visible), puis une seconde par joueur,
// et la carte cachée du croupier. Les sabots truqués ci-dessous suivent cet ordre.
public class BlackjackGameTests
{
    // ---------------------------------------------------------------- Mises

    [Fact]
    public void Betting_has_no_timer_until_someone_bets()
    {
        var t = new GameFixture(new StackedShoe(), players: 2);

        Assert.Equal(GamePhase.Betting, t.Game.Phase);
        Assert.Null(t.Game.Deadline);
        Assert.False(t.Game.Tick(t.Now.AddHours(1)));
    }

    [Fact]
    public void First_bet_starts_the_timer_and_waits_for_the_others()
    {
        var t = new GameFixture(new StackedShoe(), players: 2);

        t.Bet(0, 3);

        Assert.Equal(GamePhase.Betting, t.Game.Phase);
        Assert.Equal(t.Now + GameFixture.Timings.Betting, t.Game.Deadline);
    }

    [Fact]
    public void Round_is_dealt_as_soon_as_every_connected_player_answered()
    {
        var t = new GameFixture(new StackedShoe(Ten, Nine, Six, Eight, Seven, Ten), players: 2);

        t.Bet(0, 3);
        t.Bet(1, 0);

        Assert.Equal(GamePhase.PlayerTurns, t.Game.Phase);
        Assert.True(t.Seat(0).InRound);
        Assert.False(t.Seat(1).InRound);
        Assert.Empty(t.Seat(1).Hands);
    }

    [Fact]
    public void Players_who_did_not_answer_in_time_sit_out()
    {
        var t = new GameFixture(new StackedShoe(Ten, Six, Eight, Ten), players: 2);

        t.Bet(0, 5);
        t.Elapse();

        Assert.Equal(GamePhase.PlayerTurns, t.Game.Phase);
        Assert.Equal(0, t.Seat(1).Bet);
        Assert.False(t.Seat(1).InRound);
    }

    [Fact]
    public void Disconnected_player_does_not_block_the_deal()
    {
        var t = new GameFixture(new StackedShoe(Ten, Six, Eight, Ten), players: 2);
        t.Game.SetConnected(t.Players[1], false, t.Now);

        t.Bet(0, 5);

        Assert.Equal(GamePhase.PlayerTurns, t.Game.Phase);
    }

    [Fact]
    public void Nothing_is_dealt_when_everybody_passes()
    {
        var t = new GameFixture(new StackedShoe(), players: 2);

        t.Bet(0, 0);
        t.Bet(1, 0);

        Assert.Equal(GamePhase.Betting, t.Game.Phase);
        Assert.Null(t.Game.Deadline);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void Bet_outside_limits_is_rejected(int sips)
    {
        var t = new GameFixture(new StackedShoe());

        var error = Assert.Throws<GameRuleException>(() => t.Bet(0, sips));
        Assert.Equal("invalid_bet", error.Code);
    }

    // ---------------------------------------------------------------- Tours des joueurs

    [Fact]
    public void Dealer_hole_card_stays_hidden_during_player_turns()
    {
        var t = new GameFixture(new StackedShoe(Ten, Six, Eight, Ten));

        t.Bet(0, 2);

        Assert.False(t.Game.DealerHoleRevealed);
        Assert.Equal(t.Players[0], t.Game.ActiveSeat!.PlayerId);
        Assert.Equal([PlayerAction.Hit, PlayerAction.Stand, PlayerAction.Double], t.Game.GetAllowedActions(t.Players[0]));
    }

    [Fact]
    public void Acting_out_of_turn_is_rejected()
    {
        var t = new GameFixture(new StackedShoe(Ten, Nine, Six, Eight, Seven, Ten), players: 2);
        t.Bet(0, 2);
        t.Bet(1, 2);

        var error = Assert.Throws<GameRuleException>(() => t.Act(1, PlayerAction.Hit));
        Assert.Equal("not_your_turn", error.Code);
    }

    [Fact]
    public void Standing_higher_than_the_dealer_wins_the_bet_to_distribute()
    {
        // Joueur 10+9 = 19, croupier 10+8 = 18.
        var t = new GameFixture(new StackedShoe(Ten, Ten, Nine, Eight), players: 2);
        t.Bet(0, 4);
        t.Bet(1, 0);

        t.Act(0, PlayerAction.Stand);
        t.PlayDealer();

        Assert.Equal(HandOutcome.Win, t.Seat(0).Hands[0].Outcome);
        Assert.Equal(4, t.Seat(0).ToDistribute);
        Assert.Equal(0, t.Seat(0).Lost);
        Assert.Equal(GamePhase.Distribution, t.Game.Phase);
    }

    [Fact]
    public void Busting_loses_the_bet_and_dealer_does_not_draw()
    {
        // Joueur 10+6 puis 10 = 26. Croupier 5+10 = 15 ne tire pas : plus aucune main en jeu.
        var t = new GameFixture(new StackedShoe(Ten, Five, Six, Ten, Ten));
        t.Bet(0, 7);

        t.Act(0, PlayerAction.Hit);
        t.PlayDealer();

        Assert.Equal(HandStatus.Bust, t.Seat(0).Hands[0].Status);
        Assert.Equal(HandOutcome.Lose, t.Seat(0).Hands[0].Outcome);
        Assert.Equal(7, t.Seat(0).Lost);
        Assert.Equal(2, t.Game.DealerCards.Count);
        Assert.Equal(GamePhase.Results, t.Game.Phase);
        Assert.Equal(7, t.Seat(0).Totals.SipsDrunk);
    }

    [Fact]
    public void Dealer_draws_to_seventeen_and_busting_dealer_pays_everyone()
    {
        // Joueur 10+2 = 12 reste. Croupier 6+10 = 16, tire 10 = 26.
        var t = new GameFixture(new StackedShoe(Ten, Six, Two, Ten, Ten), players: 2);
        t.Bet(0, 3);
        t.Bet(1, 0);

        t.Act(0, PlayerAction.Stand);
        t.PlayDealer();

        Assert.Equal(3, t.Game.DealerCards.Count);
        Assert.Equal(HandOutcome.Win, t.Seat(0).Hands[0].Outcome);
    }

    [Fact]
    public void Dealer_stands_on_soft_seventeen()
    {
        // Croupier A+6 = 17 soft : il reste. Joueur 10+8 = 18 gagne.
        var t = new GameFixture(new StackedShoe(Ten, Ace, Eight, Six, Ten));
        t.Bet(0, 1);

        t.Act(0, PlayerAction.Stand);
        t.PlayDealer();

        Assert.Equal(2, t.Game.DealerCards.Count);
        Assert.Equal(HandOutcome.Win, t.Seat(0).Hands[0].Outcome);
    }

    [Fact]
    public void Equal_totals_are_a_push()
    {
        var t = new GameFixture(new StackedShoe(Ten, Ten, Eight, Eight));
        t.Bet(0, 6);

        t.Act(0, PlayerAction.Stand);
        t.PlayDealer();

        Assert.Equal(HandOutcome.Push, t.Seat(0).Hands[0].Outcome);
        Assert.Equal(0, t.Seat(0).Lost);
        Assert.Equal(0, t.Seat(0).ToDistribute);
    }

    [Fact]
    public void Natural_blackjack_pays_one_and_a_half_rounded_up()
    {
        // Joueur A+K, croupier 9+7. Mise 5 → 8 gorgées à distribuer.
        var t = new GameFixture(new StackedShoe(Ace, Nine, King, Seven), players: 2);
        t.Bet(0, 5);
        t.Bet(1, 0);

        // Aucune main à jouer : on passe directement au croupier.
        t.PlayDealer();

        Assert.Equal(HandStatus.Blackjack, t.Seat(0).Hands[0].Status);
        Assert.Equal(HandOutcome.Blackjack, t.Seat(0).Hands[0].Outcome);
        Assert.Equal(8, t.Seat(0).ToDistribute);
    }

    [Fact]
    public void Dealer_blackjack_ends_the_round_before_anyone_plays()
    {
        // J1 : 10+9 perd. J2 : A+K fait égalité. Croupier A+K.
        var t = new GameFixture(new StackedShoe(Ten, Ace, Ace, Nine, King, King), players: 2);
        t.Bet(0, 4);
        t.Bet(1, 4);

        Assert.Equal(GamePhase.DealerTurn, t.Game.Phase);
        t.PlayDealer();

        Assert.Equal(HandOutcome.Lose, t.Seat(0).Hands[0].Outcome);
        Assert.Equal(HandOutcome.Push, t.Seat(1).Hands[0].Outcome);
        Assert.Equal(4, t.Seat(0).Lost);
    }

    [Fact]
    public void Double_doubles_the_bet_and_draws_exactly_one_card()
    {
        // Joueur 6+5 = 11, double, tire 10 = 21. Croupier 10+8 = 18.
        var t = new GameFixture(new StackedShoe(Six, Ten, Five, Eight, Ten), players: 2);
        t.Bet(0, 10);
        t.Bet(1, 0);

        t.Act(0, PlayerAction.Double);

        var hand = t.Seat(0).Hands[0];
        Assert.Equal(20, hand.Bet);
        Assert.Equal(3, hand.Cards.Count);
        Assert.Equal(HandStatus.Doubled, hand.Status);
        Assert.Equal(GamePhase.DealerTurn, t.Game.Phase);

        t.PlayDealer();
        Assert.Equal(20, t.Seat(0).ToDistribute);
    }

    [Fact]
    public void Double_is_not_offered_after_a_hit()
    {
        var t = new GameFixture(new StackedShoe(Two, Ten, Three, Eight, Four));
        t.Bet(0, 1);

        t.Act(0, PlayerAction.Hit);

        Assert.DoesNotContain(PlayerAction.Double, t.Game.GetAllowedActions(t.Players[0]));
    }

    [Fact]
    public void Split_creates_two_hands_played_one_after_the_other()
    {
        // Joueur 8+8, croupier 10+7. Split → 8+3 et 8+2.
        var t = new GameFixture(new StackedShoe(Eight, Ten, Eight, Seven, Three, Two, Ten, Nine));
        t.Bet(0, 4);
        Assert.Contains(PlayerAction.Split, t.Game.GetAllowedActions(t.Players[0]));

        t.Act(0, PlayerAction.Split);

        var seat = t.Seat(0);
        Assert.Equal(2, seat.Hands.Count);
        Assert.All(seat.Hands, h => Assert.Equal(4, h.Bet));
        Assert.Same(seat.Hands[0], t.Game.ActiveHand);

        t.Act(0, PlayerAction.Hit);   // 8+3+10 = 21 → reste automatiquement
        Assert.Same(seat.Hands[1], t.Game.ActiveHand);

        t.Act(0, PlayerAction.Hit);   // 8+2+9 = 19
        t.Act(0, PlayerAction.Stand);
        t.PlayDealer();

        Assert.All(seat.Hands, h => Assert.Equal(HandOutcome.Win, h.Outcome));
        Assert.Equal(8, seat.ToDistribute);
    }

    [Fact]
    public void Split_aces_get_one_card_each_and_twenty_one_is_not_a_blackjack()
    {
        // Joueur A+A, croupier 10+9. Split → A+K (21, pas un blackjack) et A+5.
        var t = new GameFixture(new StackedShoe(Ace, Ten, Ace, Nine, King, Five), players: 2);
        t.Bet(0, 2);
        t.Bet(1, 0);

        t.Act(0, PlayerAction.Split);

        var seat = t.Seat(0);
        Assert.All(seat.Hands, h => Assert.Equal(HandStatus.Stood, h.Status));
        Assert.Equal(GamePhase.DealerTurn, t.Game.Phase);

        t.PlayDealer();
        Assert.Equal(HandOutcome.Win, seat.Hands[0].Outcome);   // 21 paie 1 pour 1
        Assert.Equal(HandOutcome.Lose, seat.Hands[1].Outcome);  // 16 contre 19
        Assert.Equal(2, seat.ToDistribute);
        Assert.Equal(2, seat.Lost);
    }

    [Fact]
    public void Split_is_limited_by_the_max_number_of_hands()
    {
        var rules = new BlackjackRules { MaxHandsPerPlayer = 2 };
        var t = new GameFixture(new StackedShoe(Eight, Ten, Eight, Seven, Eight, Two), rules: rules);
        t.Bet(0, 1);

        t.Act(0, PlayerAction.Split);   // 8+8 et 8+2

        Assert.DoesNotContain(PlayerAction.Split, t.Game.GetAllowedActions(t.Players[0]));
    }

    [Fact]
    public void Turn_timeout_makes_the_hand_stand()
    {
        var t = new GameFixture(new StackedShoe(Ten, Nine, Six, Eight, Seven, Ten), players: 2);
        t.Bet(0, 2);
        t.Bet(1, 2);

        t.Elapse();

        Assert.Equal(HandStatus.Stood, t.Seat(0).Hands[0].Status);
        Assert.Equal(t.Players[1], t.Game.ActiveSeat!.PlayerId);
    }

    // ---------------------------------------------------------------- Gorgées

    [Fact]
    public void Winner_distributes_and_receivers_drink()
    {
        // J1 19 gagne 4. J2 16 perd 3, J3 passe. Croupier 18.
        var t = new GameFixture(new StackedShoe(Ten, Ten, Ten, Nine, Six, Eight), players: 3);
        t.Bet(0, 4);
        t.Bet(1, 3);
        t.Bet(2, 0);
        t.Act(0, PlayerAction.Stand);
        t.Act(1, PlayerAction.Stand);
        t.PlayDealer();
        Assert.Equal(GamePhase.Distribution, t.Game.Phase);

        t.Game.Distribute(
            t.Players[0],
            new Dictionary<Guid, int> { [t.Players[1]] = 1, [t.Players[2]] = 3 },
            t.Now);

        Assert.Equal(GamePhase.Results, t.Game.Phase);
        Assert.Equal(0, t.Seat(0).Totals.SipsDrunk);
        Assert.Equal(4, t.Seat(0).Totals.SipsGiven);
        Assert.Equal(4, t.Seat(1).Totals.SipsDrunk);   // 3 perdues + 1 reçue
        Assert.Equal(3, t.Seat(2).Totals.SipsDrunk);   // reçues alors qu'il passait

        var round = t.Game.TakeCompletedRound();
        Assert.NotNull(round);
        Assert.Equal(2, round.Hands.Count);
        Assert.Equal(2, round.Transfers.Count);
        Assert.Null(t.Game.TakeCompletedRound());
    }

    [Theory]
    [InlineData(3)]   // pas assez
    [InlineData(5)]   // trop
    public void Distribution_must_match_the_exact_total(int sips)
    {
        var t = new GameFixture(new StackedShoe(Ten, Ten, Nine, Eight), players: 2);
        t.Bet(0, 4);
        t.Bet(1, 0);
        t.Act(0, PlayerAction.Stand);
        t.PlayDealer();

        var error = Assert.Throws<GameRuleException>(() =>
            t.Game.Distribute(t.Players[0], new Dictionary<Guid, int> { [t.Players[1]] = sips }, t.Now));
        Assert.Equal("invalid_distribution", error.Code);
    }

    [Fact]
    public void Winner_cannot_give_sips_to_themself()
    {
        var t = new GameFixture(new StackedShoe(Ten, Ten, Nine, Eight), players: 2);
        t.Bet(0, 4);
        t.Bet(1, 0);
        t.Act(0, PlayerAction.Stand);
        t.PlayDealer();

        Assert.Throws<GameRuleException>(() =>
            t.Game.Distribute(t.Players[0], new Dictionary<Guid, int> { [t.Players[0]] = 4 }, t.Now));
    }

    [Fact]
    public void Distribution_timeout_spreads_the_sips_automatically()
    {
        var t = new GameFixture(new StackedShoe(Ten, Ten, Nine, Eight), players: 3);
        t.Bet(0, 5);
        t.Bet(1, 0);
        t.Bet(2, 0);
        t.Act(0, PlayerAction.Stand);
        t.PlayDealer();

        t.Elapse();

        Assert.Equal(GamePhase.Results, t.Game.Phase);
        Assert.Equal(5, t.Game.Transfers.Sum(x => x.Amount));
        Assert.Equal(5, t.Seat(1).Totals.SipsDrunk + t.Seat(2).Totals.SipsDrunk);
        Assert.DoesNotContain(t.Game.Transfers, x => x.ToPlayerId == t.Players[0]);
    }

    [Fact]
    public void Solo_winner_skips_the_distribution()
    {
        var t = new GameFixture(new StackedShoe(Ten, Ten, Nine, Eight));
        t.Bet(0, 4);
        t.Act(0, PlayerAction.Stand);

        t.PlayDealer();

        Assert.Equal(GamePhase.Results, t.Game.Phase);
        Assert.Empty(t.Game.Transfers);
    }

    // ---------------------------------------------------------------- Cul sec

    private static readonly BlackjackRules CulSecRules = new() { AllowCulSec = true };

    [Fact]
    public void Cul_sec_is_refused_when_the_table_does_not_allow_it()
    {
        var t = new GameFixture(new StackedShoe());

        var error = Assert.Throws<GameRuleException>(() => t.BetCulSec(0));
        Assert.Equal("cul_sec_not_allowed", error.Code);
    }

    [Fact]
    public void Only_one_cul_sec_can_be_bet_at_a_time()
    {
        var t = new GameFixture(new StackedShoe(), rules: CulSecRules);

        var error = Assert.Throws<GameRuleException>(() =>
            t.Game.PlaceBet(t.Players[0], new Bet(2, DrinkUnit.CulSec), t.Now));
        Assert.Equal("invalid_bet", error.Code);
    }

    [Fact]
    public void Losing_a_cul_sec_is_counted_apart_from_sips()
    {
        // Joueur 10+6 puis 10 = 26.
        var t = new GameFixture(new StackedShoe(Ten, Five, Six, Ten, Ten), rules: CulSecRules);
        t.BetCulSec(0);

        t.Act(0, PlayerAction.Hit);
        t.PlayDealer();

        var seat = t.Seat(0);
        Assert.Equal(DrinkUnit.CulSec, seat.ResultUnit);
        Assert.Equal(1, seat.Lost);
        Assert.Equal(new DrinkTotals(SipsDrunk: 0, SipsGiven: 0, CulsSecsDrunk: 1, CulsSecsGiven: 0), seat.Totals);
    }

    [Fact]
    public void Winning_a_cul_sec_makes_another_player_finish_their_glass()
    {
        // J1 10+9 = 19 contre 18.
        var t = new GameFixture(new StackedShoe(Ten, Ten, Nine, Eight), players: 2, rules: CulSecRules);
        t.BetCulSec(0);
        t.Bet(1, 0);
        t.Act(0, PlayerAction.Stand);
        t.PlayDealer();

        t.Game.Distribute(t.Players[0], new Dictionary<Guid, int> { [t.Players[1]] = 1 }, t.Now);

        var transfer = Assert.Single(t.Game.Transfers);
        Assert.Equal(DrinkUnit.CulSec, transfer.Unit);
        Assert.Equal(1, t.Seat(1).Totals.CulsSecsDrunk);
        Assert.Equal(0, t.Seat(1).Totals.SipsDrunk);
        Assert.Equal(1, t.Seat(0).Totals.CulsSecsGiven);
    }

    [Fact]
    public void Blackjack_on_a_cul_sec_gives_two()
    {
        var t = new GameFixture(new StackedShoe(Ace, Nine, King, Seven), players: 2, rules: CulSecRules);
        t.BetCulSec(0);
        t.Bet(1, 0);

        t.PlayDealer();

        Assert.Equal(2, t.Seat(0).ToDistribute);
        Assert.Equal(DrinkUnit.CulSec, t.Seat(0).ResultUnit);
    }

    [Fact]
    public void Doubling_a_cul_sec_puts_two_at_stake()
    {
        // Joueur 6+5, double, tire 2 = 13. Croupier 10+8 = 18.
        var t = new GameFixture(new StackedShoe(Six, Ten, Five, Eight, Two), rules: CulSecRules);
        t.BetCulSec(0);

        t.Act(0, PlayerAction.Double);
        t.PlayDealer();

        Assert.Equal(2, t.Seat(0).Lost);
        Assert.Equal(2, t.Seat(0).Totals.CulsSecsDrunk);
    }

    [Fact]
    public void A_player_can_receive_sips_and_culs_secs_in_the_same_round()
    {
        // J1 (cul sec) 19 et J2 (3 gorgées) 20 gagnent contre 18 ; J3 passe et reçoit tout.
        var t = new GameFixture(new StackedShoe(Ten, Ten, Ten, Nine, Ten, Eight), players: 3, rules: CulSecRules);
        t.BetCulSec(0);
        t.Bet(1, 3);
        t.Bet(2, 0);
        t.Act(0, PlayerAction.Stand);
        t.Act(1, PlayerAction.Stand);
        t.PlayDealer();

        t.Game.Distribute(t.Players[0], new Dictionary<Guid, int> { [t.Players[2]] = 1 }, t.Now);
        t.Game.Distribute(t.Players[1], new Dictionary<Guid, int> { [t.Players[2]] = 3 }, t.Now);

        Assert.Equal(GamePhase.Results, t.Game.Phase);
        Assert.Equal(new DrinkTotals(SipsDrunk: 3, SipsGiven: 0, CulsSecsDrunk: 1, CulsSecsGiven: 0), t.Seat(2).Totals);
    }

    [Fact]
    public void Bet_limits_chosen_for_the_table_are_enforced()
    {
        var rules = new BlackjackRules { MinBet = 3, MaxBet = 6 };
        var t = new GameFixture(new StackedShoe(Ten, Six, Eight, Ten), players: 2, rules: rules);

        Assert.Throws<GameRuleException>(() => t.Bet(0, 2));
        Assert.Throws<GameRuleException>(() => t.Bet(0, 7));
        t.Bet(0, 3);
        t.Bet(1, 0);   // passer reste toujours possible

        Assert.Equal(3, t.Seat(0).Bet);
    }

    // ---------------------------------------------------------------- Enchaînement

    [Fact]
    public void After_results_cards_are_discarded_and_betting_reopens()
    {
        var t = new GameFixture(new StackedShoe(Ten, Ten, Nine, Eight));
        t.Bet(0, 4);
        t.Act(0, PlayerAction.Stand);
        t.PlayDealer();

        t.Elapse();

        Assert.Equal(GamePhase.Betting, t.Game.Phase);
        Assert.Null(t.Game.Deadline);
        Assert.Null(t.Seat(0).Bet);
        Assert.Empty(t.Seat(0).Hands);
        Assert.Empty(t.Game.DealerCards);
        Assert.Equal(4, t.Shoe.Discarded.Count);
        Assert.True(t.Game.ResultsAvailable);   // le bilan reste lisible pendant les mises
    }

    [Fact]
    public void Active_player_leaving_passes_the_turn()
    {
        var t = new GameFixture(new StackedShoe(Ten, Nine, Six, Eight, Seven, Ten), players: 2);
        t.Bet(0, 2);
        t.Bet(1, 2);

        t.Game.RemovePlayer(t.Players[0], t.Now);

        Assert.Equal(t.Players[1], t.Game.ActiveSeat!.PlayerId);
        Assert.Equal(2, t.Shoe.Discarded.Count);
    }

    [Fact]
    public void Table_resets_when_the_last_player_leaves()
    {
        var t = new GameFixture(new StackedShoe(Ten, Six, Eight, Ten));
        t.Bet(0, 2);

        t.Game.RemovePlayer(t.Players[0], t.Now);

        Assert.Equal(GamePhase.Betting, t.Game.Phase);
        Assert.Null(t.Game.Deadline);
        Assert.Empty(t.Game.Seats);
        Assert.Empty(t.Game.DealerCards);
    }

    [Fact]
    public void Seventh_player_is_refused()
    {
        var t = new GameFixture(new StackedShoe(), players: 6);

        var error = Assert.Throws<GameRuleException>(() => t.Game.AddPlayer(Guid.NewGuid(), "J7"));
        Assert.Equal("table_full", error.Code);
    }
}
