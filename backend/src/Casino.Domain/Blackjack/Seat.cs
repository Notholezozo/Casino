namespace Casino.Domain.Blackjack;

/// <summary>Un joueur assis à la table et son état pour la manche en cours.</summary>
public sealed class Seat
{
    private readonly List<Hand> _hands = [];

    internal Seat(Guid playerId, string name)
    {
        PlayerId = playerId;
        Name = name;
    }

    public Guid PlayerId { get; }

    public string Name { get; }

    public bool IsConnected { get; internal set; }

    /// <summary>Quantité misée. Null = n'a pas encore répondu ; 0 = passe la manche.</summary>
    public int? Bet { get; internal set; }

    /// <summary>Unité de la mise en cours (gorgées ou culs secs).</summary>
    public DrinkUnit BetUnit { get; internal set; }

    /// <summary>Vrai si le joueur a des cartes dans la manche en cours.</summary>
    public bool InRound { get; internal set; }

    public IReadOnlyList<Hand> Hands => _hands;

    /// <summary>
    /// Unité du bilan ci-dessous. Distincte de <see cref="BetUnit"/> : le bilan reste lisible
    /// pendant que le joueur choisit déjà sa mise suivante, peut-être dans une autre unité.
    /// </summary>
    public DrinkUnit ResultUnit { get; internal set; }

    /// <summary>Quantité perdue sur ses propres mains pendant la manche, en <see cref="ResultUnit"/>.</summary>
    public int Lost { get; internal set; }

    /// <summary>Quantité gagnée pendant la manche, à distribuer aux autres, en <see cref="ResultUnit"/>.</summary>
    public int ToDistribute { get; internal set; }

    public bool DistributionDone { get; internal set; }

    /// <summary>Cumuls depuis l'arrivée à la table.</summary>
    public DrinkTotals Totals { get; internal set; }

    internal List<Hand> MutableHands => _hands;

    internal void ResetRoundOutcome()
    {
        ResultUnit = DrinkUnit.Sip;
        Lost = 0;
        ToDistribute = 0;
        DistributionDone = false;
    }
}

/// <summary>Cumuls d'un joueur : gorgées et culs secs, bus et distribués.</summary>
public readonly record struct DrinkTotals(int SipsDrunk, int SipsGiven, int CulsSecsDrunk, int CulsSecsGiven)
{
    public DrinkTotals AddDrunk(int amount, DrinkUnit unit) => unit == DrinkUnit.CulSec
        ? this with { CulsSecsDrunk = CulsSecsDrunk + amount }
        : this with { SipsDrunk = SipsDrunk + amount };

    public DrinkTotals AddGiven(int amount, DrinkUnit unit) => unit == DrinkUnit.CulSec
        ? this with { CulsSecsGiven = CulsSecsGiven + amount }
        : this with { SipsGiven = SipsGiven + amount };
}

/// <summary>Ce qu'un gagnant fait boire à un autre joueur. Les noms sont figés au moment du don.</summary>
public sealed record DrinkTransfer(
    Guid FromPlayerId,
    string FromPlayerName,
    Guid ToPlayerId,
    string ToPlayerName,
    int Amount,
    DrinkUnit Unit);

/// <summary>Photo d'une manche terminée, destinée à l'historique.</summary>
public sealed record RoundSummary(
    int RoundNumber,
    IReadOnlyList<Cards.Card> DealerCards,
    int DealerTotal,
    IReadOnlyList<RoundHandSummary> Hands,
    IReadOnlyList<DrinkTransfer> Transfers,
    IReadOnlyList<PlayerTotals> Totals);

public sealed record RoundHandSummary(
    Guid PlayerId,
    IReadOnlyList<Cards.Card> Cards,
    int Total,
    int Bet,
    DrinkUnit Unit,
    HandOutcome Outcome);

public sealed record PlayerTotals(Guid PlayerId, DrinkTotals Totals);
