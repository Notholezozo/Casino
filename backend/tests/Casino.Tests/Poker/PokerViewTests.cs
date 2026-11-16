using Casino.Application.Poker;
using Casino.Application.Poker.Contracts;
using Casino.Domain.Poker;

namespace Casino.Tests.Poker;

/// <summary>Vérifie ce qui sort réellement du serveur : qui voit quelles cartes privées.</summary>
public class PokerViewTests
{
    private static readonly string[] AcesWin = ["AS", "KC", "AD", "QC", "2H", "7D", "9S", "4C", "JD"];

    [Fact]
    public void Hole_cards_only_reach_their_owner_during_the_hand()
    {
        var t = new PokerFixture(players: 2, cards: AcesWin);
        t.Elapse();
        var views = new PokerViewBuilder(t.Game);

        // L'écran table est vu de tous : aucune carte privée.
        var table = (PokerTableViewDto)views.BuildTableView("ABCD", 1, t.Now);
        Assert.All(table.Seats, seat =>
        {
            Assert.True(seat.HasCards);
            Assert.Null(seat.Cards);
        });

        var mine = (PokerPlayerViewDto)views.BuildPlayerView(t.Players[0], "ABCD", 1, t.Now)!;
        Assert.Equal(["A", "A"], mine.Seats[0].Cards!.Select(c => c.Rank));
        Assert.Null(mine.Seats[1].Cards);
        Assert.True(mine.Me.IsMyTurn);
        Assert.Equal(1, mine.Me.AmountToCall);
    }

    [Fact]
    public void Live_hands_are_revealed_to_everyone_at_showdown()
    {
        var t = new PokerFixture(players: 2, cards: AcesWin);
        t.Elapse();
        t.Act(0, PokerAction.Call);
        t.Act(1, PokerAction.Check);
        t.CheckDown();

        var table = (PokerTableViewDto)new PokerViewBuilder(t.Game).BuildTableView("ABCD", 1, t.Now);

        Assert.True(table.Showdown);
        Assert.All(table.Seats, seat => Assert.NotNull(seat.Cards));
        Assert.Equal("Pair", table.Seats[0].HandName);
    }

    [Fact]
    public void A_folded_hand_stays_hidden_even_after_the_hand()
    {
        var t = new PokerFixture(players: 2, cards: AcesWin);
        t.Elapse();
        t.Act(0, PokerAction.Fold);

        var table = (PokerTableViewDto)new PokerViewBuilder(t.Game).BuildTableView("ABCD", 1, t.Now);

        Assert.False(table.Showdown);
        Assert.All(table.Seats, seat => Assert.Null(seat.Cards));   // gagner sans abattage ne montre rien
    }
}
