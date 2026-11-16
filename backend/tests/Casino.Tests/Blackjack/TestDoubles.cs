using Casino.Domain.Blackjack;
using Casino.Domain.Cards;

namespace Casino.Tests.Blackjack;

/// <summary>Sabot truqué : distribue les cartes dans l'ordre fourni.</summary>
internal sealed class StackedShoe(params Rank[] ranks) : IShoe
{
    private readonly Queue<Card> _cards = new(ranks.Select(r => new Card(r, Suit.Spades)));

    public List<Card> Discarded { get; } = [];

    public int Remaining => _cards.Count;

    public int Total => 208;

    public Card Draw() => _cards.Dequeue();

    public void Discard(IEnumerable<Card> cards) => Discarded.AddRange(cards);
}

internal sealed class FixedRandom(int value = 0) : IRandomSource
{
    public int Next(int exclusiveMax) => value % exclusiveMax;
}

/// <summary>Monte une table avec des joueurs connectés et une horloge manuelle.</summary>
internal sealed class GameFixture
{
    public static readonly BlackjackTimings Timings = new();

    public GameFixture(StackedShoe shoe, int players = 1, BlackjackRules? rules = null)
    {
        Shoe = shoe;
        Game = new BlackjackGame(rules ?? new BlackjackRules(), Timings, shoe, new FixedRandom());
        Players = Enumerable.Range(1, players).Select(i =>
        {
            var id = Guid.NewGuid();
            Game.AddPlayer(id, $"J{i}");
            Game.SetConnected(id, true, Now);
            return id;
        }).ToList();
    }

    public BlackjackGame Game { get; }

    public StackedShoe Shoe { get; }

    public IReadOnlyList<Guid> Players { get; }

    public DateTimeOffset Now { get; private set; } = new(2026, 1, 1, 20, 0, 0, TimeSpan.Zero);

    public Seat Seat(int index) => Game.Seats.Single(s => s.PlayerId == Players[index]);

    public void Bet(int index, int sips) => Game.PlaceBet(Players[index], Domain.Blackjack.Bet.Sips(sips), Now);

    public void BetCulSec(int index) => Game.PlaceBet(Players[index], Domain.Blackjack.Bet.CulSec, Now);

    public void Act(int index, PlayerAction action) => Game.Act(Players[index], action, Now);

    /// <summary>Avance l'horloge jusqu'à l'échéance courante et la déclenche.</summary>
    public void Elapse()
    {
        Now = Game.Deadline ?? throw new InvalidOperationException("Aucune échéance en cours.");
        Assert.True(Game.Tick(Now));
    }

    /// <summary>Laisse le croupier jouer jusqu'au règlement.</summary>
    public void PlayDealer()
    {
        Assert.Equal(GamePhase.DealerTurn, Game.Phase);
        while (Game.Phase == GamePhase.DealerTurn)
        {
            Elapse();
        }
    }
}
