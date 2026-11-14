using Casino.Application.Tables;
using Casino.Domain.Blackjack;
using Casino.Domain.Cards;
using Casino.Tests.Blackjack;

namespace Casino.Tests.Tables;

/// <summary>Vérifie ce qui sort réellement du serveur vers chaque écran.</summary>
public class TableSessionTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 20, 0, 0, TimeSpan.Zero);

    private static (TableSession Session, Guid Alice, Guid Bob) OpenTable(params Rank[] cards)
    {
        var game = new BlackjackGame(new BlackjackRules(), new BlackjackTimings(), new StackedShoe(cards), new FixedRandom());
        var session = new TableSession(Guid.NewGuid(), "ABCD", "admin-token", game, Now);
        var (alice, bob) = (Guid.NewGuid(), Guid.NewGuid());

        session.Mutate(Now, g =>
        {
            g.AddPlayer(alice, "Alice");
            g.AddPlayer(bob, "Bob");
        });
        session.TrackPlayerConnection(alice, connected: true, Now);
        session.TrackPlayerConnection(bob, connected: true, Now);

        return (session, alice, bob);
    }

    [Fact]
    public void Player_view_never_contains_the_dealer_hole_card()
    {
        // Alice 10+9, Bob 9+7, croupier 6 visible + 8 cachée.
        var (session, alice, bob) = OpenTable(Rank.Ten, Rank.Nine, Rank.Six, Rank.Nine, Rank.Seven, Rank.Eight);

        var snapshot = session.Mutate(Now, g =>
        {
            g.PlaceBet(alice, 2, Now);
            g.PlaceBet(bob, 2, Now);
        });

        foreach (var view in snapshot.Players.Values)
        {
            var card = Assert.Single(view.Dealer.Cards);
            Assert.Equal("6", card.Rank);
            Assert.True(view.Dealer.HasHiddenCard);
            Assert.Equal(6, view.Dealer.Total);
        }

        Assert.Single(snapshot.Table.Dealer.Cards);   // l'écran table non plus
    }

    [Fact]
    public void Player_view_only_exposes_their_own_cards_and_options()
    {
        var (session, alice, bob) = OpenTable(Rank.Ten, Rank.Nine, Rank.Six, Rank.Nine, Rank.Seven, Rank.Eight);

        var snapshot = session.Mutate(Now, g =>
        {
            g.PlaceBet(alice, 2, Now);
            g.PlaceBet(bob, 3, Now);
        });

        var aliceView = snapshot.Players[alice];
        Assert.Equal(["10", "9"], aliceView.Me.Hands.Single().Cards.Select(c => c.Rank));
        Assert.True(aliceView.Me.IsMyTurn);
        Assert.NotEmpty(aliceView.Me.AllowedActions);
        Assert.Equal("Bob", Assert.Single(aliceView.Others).Name);

        var bobView = snapshot.Players[bob];
        Assert.Equal(["9", "7"], bobView.Me.Hands.Single().Cards.Select(c => c.Rank));
        Assert.False(bobView.Me.IsMyTurn);
        Assert.Empty(bobView.Me.AllowedActions);
        Assert.Equal(alice, bobView.ActivePlayerId);

        // L'écran table, lui, voit les deux mains.
        Assert.All(snapshot.Table.Seats, seat => Assert.Equal(2, seat.Hands.Single().Cards.Count));
    }

    [Fact]
    public void Every_snapshot_has_a_higher_version_and_a_countdown()
    {
        var (session, alice, _) = OpenTable();

        var first = session.Mutate(Now, g => g.PlaceBet(alice, 2, Now));
        var second = session.Mutate(Now.AddSeconds(5), g => g.PlaceBet(alice, 3, Now.AddSeconds(5)));

        Assert.True(second.Table.Version > first.Table.Version);
        Assert.Equal(20_000, first.Players[alice].RemainingMs);
        Assert.Equal(15_000, second.Players[alice].RemainingMs);
    }

    [Fact]
    public void Table_becomes_idle_when_the_last_screen_disconnects()
    {
        var (session, alice, bob) = OpenTable();
        Assert.Null(session.IdleSince);

        session.TrackPlayerConnection(alice, connected: false, Now);
        Assert.Null(session.IdleSince);

        session.TrackPlayerConnection(bob, connected: false, Now.AddMinutes(1));
        Assert.Equal(Now.AddMinutes(1), session.IdleSince);

        session.TrackAdminConnection(connected: true, Now.AddMinutes(2));
        Assert.Null(session.IdleSince);
    }
}
